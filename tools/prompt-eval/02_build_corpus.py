"""Fuegt die drei Korpusquellen zu einer Datei zusammen und stuft sie ein.

Quellen: transkribierte Testaufnahmen, erfundene Diktate (#71), Archiv-Transkripte.
Die Einstufung nach Laenge dient spaeter dazu, Befunde nach Diktatgroesse zu trennen —
ein Prompt kann bei kurzen Diktaten gewinnen und bei langen verlieren.
"""
import io, json, os, glob, sys

BASE = os.path.join(os.environ['USERPROFILE'], 'Documents', 'Johann')
EVAL = os.path.join(BASE, 'prompt-sandbox', 'eval')
OUT = os.path.join(EVAL, 'corpus.json')
TOKENS_PER_MIN = 128          # aus dem echten Archiv abgeleitet (#71)
CHARS_PER_TOKEN = 4.3         # gemessen ueber 30 deutsche Texte (#71)


def bucket(tokens):
    if tokens < 40:
        return 'winzig'       # Randfall: fast nichts zu sagen
    if tokens < 150:
        return 'kurz'         # bis ~1 min
    if tokens < 400:
        return 'mittel'       # bis ~3 min
    return 'lang'


def add(items, ident, source, text):
    text = (text or '').strip()
    tokens = int(round(len(text) / CHARS_PER_TOKEN))
    items.append({
        'id': ident,
        'source': source,
        'chars': len(text),
        'tokens': tokens,
        'minutes': round(tokens / TOKENS_PER_MIN, 1),
        'bucket': 'leer' if tokens == 0 else bucket(tokens),
        'text': text,
    })


def main():
    items = []

    # 1. Echte Aufnahmen
    rec = os.path.join(EVAL, 'corpus_recordings.json')
    if os.path.exists(rec):
        for e in json.load(io.open(rec, encoding='utf-8')):
            add(items, 'aufnahme:' + e['id'], 'aufnahme', e['text'])

    # 2. Erfundene Diktate aus #71
    sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__))))
    try:
        from dictations import DICTATIONS
        for name, text in DICTATIONS:
            add(items, 'erfunden:' + name, 'erfunden', text)
    except ImportError:
        print('  Hinweis: dictations.py nicht gefunden, erfundene Diktate uebersprungen')

    # 3. Archiv
    for f in sorted(glob.glob(os.path.join(BASE, 'output', '**', '*_status.json'),
                              recursive=True)):
        d = json.load(io.open(f, encoding='utf-8-sig'))
        t = (d.get('transcript') or '').strip()
        if t:
            add(items, 'archiv:' + (d.get('jobId') or os.path.basename(f)), 'archiv', t)

    json.dump(items, io.open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

    print(f'{len(items)} Korpuselemente -> {OUT}\n')
    print(f'{"Quelle":12s} {"Anzahl":>6s} {"Median Token":>13s}')
    for src in ('aufnahme', 'erfunden', 'archiv'):
        g = sorted(i['tokens'] for i in items if i['source'] == src)
        if g:
            print(f'{src:12s} {len(g):6d} {g[len(g)//2]:13d}')
    print()
    print(f'{"Groesse":10s} {"Anzahl":>6s}')
    for b in ('leer', 'winzig', 'kurz', 'mittel', 'lang'):
        n = sum(1 for i in items if i['bucket'] == b)
        print(f'{b:10s} {n:6d}')


if __name__ == '__main__':
    main()
