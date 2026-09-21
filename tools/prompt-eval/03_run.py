"""Laesst zwei Prompt-Varianten ueber den Korpus laufen und prueft die Ergebnisse.

Aufruf:
    03_run.py <abschnitt> <variante-a.json> <variante-b.json> [--repeats 3] [--limit N]

Die Varianten sind vollstaendige prompts.json-Dateien. Verglichen wird genau EIN
Abschnitt — zwei Dinge gleichzeitig zu aendern macht die Bewertung unmoeglich.

Geprueft wird zweistufig:
  1. objektiv und deterministisch (Sprache, Ueberschrift, Laenge, Platzhalter, Zahlen)
  2. durch ein ANDERES Modell als das erzeugende (Terra bewertet Lunas Ausgabe)

Die Qualitaetsentscheidung trifft am Ende ein Mensch. Die Maschine siebt nur aus,
was objektiv gegen den Vertrag verstoesst, damit niemand 60 Texte liest, von denen
die Haelfte schon formal falsch ist.
"""
import argparse, io, json, os, re, statistics, sys, time, urllib.request
from concurrent.futures import ThreadPoolExecutor

BASE = os.path.join(os.environ['USERPROFILE'], 'Documents', 'Johann')
EVAL = os.path.join(BASE, 'prompt-sandbox', 'eval')
GEN_MODEL = 'gpt-5.6-luna'      # erzeugt
JUDGE_MODEL = 'gpt-5.6-terra'   # bewertet — bewusst ein anderes Modell

# Abschnitt -> (Prompt-Schluessel, erwartet Ueberschrift?, Wortobergrenze oder None)
SECTIONS = {
    'abstract':       ('abstractPrompt',        False, 'word_limit'),
    'zusammenfassung': ('structuredPrompt',     True,  None),
    'ausfuehrlich':   ('prosePrompt',           False, None),
    'aufgaben':       ('aufgabePrompt',         False, None),
    'gespraechsnotiz': ('gespraechsnotizPrompt', False, None),
    'stundenzettel':  ('stundenzettelPrompt',   False, None),
    'analog':         ('analogPrompt',          False, None),
}

# Sprachpruefung: Der reale Fehlerfall ist "Ausgabe in der Sprache des Diktats statt
# deutsch". Ein Test auf VORHANDENE deutsche Stoppwoerter schlaegt dagegen bei kurzen,
# voellig korrekten Saetzen fehl — gemessen am 11.09.2026 waren alle 11 Treffer
# Fehlalarme wie „Es wurden keine relevanten Aussagen uebermittelt." Deshalb wird auf
# FREMDSPRACHIGKEIT geprueft, nicht auf Deutschsein.
FOREIGN_MARKERS = re.compile(
    r'\b(the|and|of|is|are|was|were|this|that|with|from|have|has|will|would|should)\b', re.I)
GERMAN_MARKERS = re.compile(
    r'\b(der|die|das|und|ist|sind|nicht|kein|keine|keinen|mit|für|von|vom|zum|zur|ein|eine|'
    r'einen|wird|wurde|wurden|sich|auch|dass|es|im|am|auf|bei|oder|als|wie|noch|nur)\b', re.I)


def api_key():
    for line in io.open(os.path.join(BASE, '.env'), encoding='utf-8-sig'):
        if line.strip().startswith('OPENAI_API_KEY='):
            return line.split('=', 1)[1].strip().strip('"\'')
    raise SystemExit('kein OPENAI_API_KEY gefunden')


KEY = None


def chat(model, system, user, max_tokens=4000):
    payload = {'model': model, 'max_completion_tokens': max_tokens,
               'messages': ([{'role': 'system', 'content': system}] if system else [])
                           + [{'role': 'user', 'content': user}]}
    req = urllib.request.Request(
        'https://api.openai.com/v1/chat/completions', data=json.dumps(payload).encode(),
        headers={'Authorization': f'Bearer {KEY}', 'Content-Type': 'application/json'})
    for attempt in range(3):
        try:
            d = json.load(urllib.request.urlopen(req, timeout=300))
            u = d['usage']
            return (d['choices'][0]['message']['content'] or '').strip(), {
                'in': u['prompt_tokens'], 'out': u['completion_tokens'],
                'think': u['completion_tokens_details']['reasoning_tokens'],
                'finish': d['choices'][0]['finish_reason']}
        except Exception as e:
            if attempt == 2:
                return '', {'error': str(e)[:160]}
            time.sleep(2 * (attempt + 1))


# ---------------------------------------------------------------- objektive Pruefungen

def checks(text, transcript, expects_heading, word_limit):
    """Nur eindeutig Entscheidbares. Unsicheres wird als Hinweis gemeldet, nicht als Fehler."""
    out = {'fehler': [], 'hinweise': []}

    if not text:
        out['fehler'].append('leere Ausgabe')
        return out

    de = len(GERMAN_MARKERS.findall(text))
    en = len(FOREIGN_MARKERS.findall(text))
    if en > de and en >= 3:
        out['fehler'].append(f'Ausgabe wirkt fremdsprachig ({en} fremde gegen {de} deutsche Marker)')
    elif de == 0 and len(re.findall(r'\S+', text)) >= 25:
        # Laengerer Text voellig ohne deutsche Funktionswoerter ist verdaechtig,
        # ein kurzer Satz dagegen nicht.
        out['hinweise'].append('keine deutschen Funktionswoerter gefunden')

    for ph in ('{transcript}', '{word_limit}', '{prose_summary}'):
        if ph in text:
            out['fehler'].append(f'Platzhalter durchgereicht: {ph}')

    first = text.lstrip().split('\n', 1)[0]
    starts_heading = first.startswith('#') or (first.startswith('**') and first.endswith('**'))
    if starts_heading and not expects_heading:
        out['fehler'].append(f'beginnt mit Ueberschrift: {first[:60]!r}')

    if word_limit:
        words = len(re.findall(r'\S+', text))
        if words > word_limit * 1.25:
            out['fehler'].append(f'{words} Woerter, Grenze {word_limit}')
        elif words > word_limit:
            out['hinweise'].append(f'{words} Woerter, Grenze {word_limit} (knapp darueber)')

    # Zahlen-Treue: bewusst nur Hinweis. Das Modell darf "acht Uhr dreissig" zu "08:30"
    # machen — eine Ziffer, die im Transkript fehlt, ist also nicht automatisch erfunden.
    t_digits = set(re.findall(r'\d+', transcript))
    o_digits = [d for d in re.findall(r'\d+', text) if len(d) >= 3]
    fremd = [d for d in o_digits if d not in t_digits]
    if fremd:
        out['hinweise'].append('Zahlen nicht woertlich im Transkript: ' + ', '.join(fremd[:6]))

    if not transcript.strip() and len(text) > 300:
        out['fehler'].append('langer Text trotz leerem Transkript')

    return out


# ---------------------------------------------------------------- Bewertung durch Terra

JUDGE_SYSTEM = (
    'Du bewertest die Qualitaet einer automatisch erzeugten Zusammenfassung eines '
    'Diktats. Du bewertest streng und begruendest knapp. Antworte ausschliesslich mit '
    'dem verlangten JSON-Objekt.'
)

JUDGE_TEMPLATE = """Hier ist das Transkript eines Diktats und ein daraus erzeugter Abschnitt.

Bewerte auf einer Skala von 1 bis 5:
- treue: Laesst sich jede Aussage auf das Transkript zuruueckfuehren? 5 = nichts hinzugefuegt,
  1 = erfindet Inhalte. Erfundene Zahlen, Namen oder Fristen sind der schwerste Fehler.
- vollstaendigkeit: Sind die wichtigen Punkte des Transkripts enthalten? 5 = nichts Wesentliches fehlt.
- klarheit: Ist der Text fuer einen Bueromitarbeiter klar und gut lesbar? 5 = sehr klar.

Nenne ausserdem in "maengel" hoechstens drei konkrete Maengel als kurze Stichpunkte.
Wenn etwas erfunden wurde, nenne es woertlich.

Antworte als JSON:
{{"treue": <1-5>, "vollstaendigkeit": <1-5>, "klarheit": <1-5>, "maengel": ["…"]}}

TRANSKRIPT:
{transcript}

ERZEUGTER ABSCHNITT:
{output}"""


def judge(transcript, output):
    if not output:
        return {'error': 'leere Ausgabe'}
    text, usage = chat(JUDGE_MODEL, JUDGE_SYSTEM,
                       JUDGE_TEMPLATE.format(transcript=transcript[:12000], output=output[:12000]),
                       max_tokens=1200)
    m = re.search(r'\{.*\}', text, re.S)
    if not m:
        return {'error': 'Bewertung nicht parsebar', 'usage': usage}
    try:
        v = json.loads(m.group(0))
        v['usage'] = usage
        return v
    except Exception as e:
        return {'error': f'JSON kaputt: {e}', 'usage': usage}


# ---------------------------------------------------------------- Lauf

def run_one(args):
    variant_name, prompts, section, item, rep = args
    key, expects_heading, limit_kind = SECTIONS[section]
    tmpl = prompts[key]
    word_limit = 120 if limit_kind == 'word_limit' else None

    user = tmpl.replace('{transcript}', item['text'])
    if word_limit:
        user = user.replace('{word_limit}', str(word_limit))

    text, usage = chat(GEN_MODEL, prompts['systemMessage'], user)
    res = {
        'variante': variant_name, 'item': item['id'], 'bucket': item['bucket'],
        'lauf': rep, 'ausgabe': text, 'usage': usage,
        'pruefung': checks(text, item['text'], expects_heading, word_limit),
    }
    if rep == 1:   # nur der erste Lauf wird bewertet, das spart zwei Drittel der Richter-Kosten
        res['bewertung'] = judge(item['text'], text)
    return res


def main():
    global KEY
    ap = argparse.ArgumentParser()
    ap.add_argument('section', choices=sorted(SECTIONS))
    ap.add_argument('variants', nargs='+', help='prompts.json-Dateien; erste ist die Referenz')
    ap.add_argument('--repeats', type=int, default=3)
    ap.add_argument('--limit', type=int, default=0, help='nur die ersten N Korpuselemente')
    ap.add_argument('--out', default=None)
    a = ap.parse_args()

    KEY = api_key()
    corpus = json.load(io.open(os.path.join(EVAL, 'corpus.json'), encoding='utf-8'))
    if a.limit:
        corpus = corpus[:a.limit]

    jobs = []
    for path in a.variants:
        name = os.path.splitext(os.path.basename(path))[0]
        prompts = json.load(io.open(path, encoding='utf-8-sig'))
        for item in corpus:
            for rep in range(1, a.repeats + 1):
                jobs.append((name, prompts, a.section, item, rep))

    print(f'{len(jobs)} Aufrufe: {len(a.variants)} Varianten x {len(corpus)} Diktate '
          f'x {a.repeats} Wiederholungen (+{len(a.variants) * len(corpus)} Bewertungen)')

    results, done = [], 0
    with ThreadPoolExecutor(max_workers=6) as pool:
        for r in pool.map(run_one, jobs):
            results.append(r)
            done += 1
            if done % 20 == 0:
                print(f'  {done}/{len(jobs)}', flush=True)

    out = a.out or os.path.join(EVAL, f'run_{a.section}_{int(time.time())}.json')
    json.dump({'section': a.section, 'repeats': a.repeats,
               'variants': [os.path.basename(v) for v in a.variants],
               'results': results},
              io.open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(f'\nErgebnisse: {out}')

    # Kurzueberblick
    print(f'\n{"Variante":28s} {"Fehler":>7s} {"Treue":>6s} {"Vollst":>7s} {"Klarh":>6s} {"Cent":>7s}')
    for path in a.variants:
        name = os.path.splitext(os.path.basename(path))[0]
        rs = [r for r in results if r['variante'] == name]
        errs = sum(len(r['pruefung']['fehler']) for r in rs)
        graded = [r['bewertung'] for r in rs if 'bewertung' in r and 'treue' in r.get('bewertung', {})]
        cent = sum((r['usage'].get('in', 0) * 0.20 + r['usage'].get('out', 0) * 1.20)
                   / 1e6 * 100 for r in rs)

        def avg(k):
            v = [g[k] for g in graded if isinstance(g.get(k), (int, float))]
            return statistics.mean(v) if v else float('nan')

        print(f'{name:28s} {errs:7d} {avg("treue"):6.2f} '
              f'{avg("vollstaendigkeit"):7.2f} {avg("klarheit"):6.2f} {cent:7.2f}')


if __name__ == '__main__':
    main()
