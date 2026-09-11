"""Macht aus einem Lauf zwei Berichte: HTML zum Lesen, Markdown zum Einchecken.

Aufruf:
    04_report.py <run_....json> [--sample 2]

--sample N nimmt je Groessenklasse N Diktate in den Nebeneinander-Vergleich auf.
Die Kennzahlen umfassen immer den ganzen Korpus; nur die gelesenen Beispiele
werden gestichprobt, damit der Bericht lesbar bleibt.
"""
import argparse, io, json, html, os, statistics, collections

BUCKETS = ['leer', 'winzig', 'kurz', 'mittel', 'lang']


def money(rs):
    return sum((r['usage'].get('in', 0) * 0.20 + r['usage'].get('out', 0) * 1.20) / 1e6 * 100
               for r in rs)


def agg(results, variants):
    """Kennzahlen je Variante, gesamt und je Groessenklasse."""
    out = {}
    for v in variants:
        rs = [r for r in results if r['variante'] == v]
        graded = [r['bewertung'] for r in rs if isinstance(r.get('bewertung'), dict)
                  and 'treue' in r['bewertung']]

        def avg(key, subset=None):
            src = subset if subset is not None else graded
            vals = [g[key] for g in src if isinstance(g.get(key), (int, float))]
            return statistics.mean(vals) if vals else None

        # Wackligkeit: Elemente, bei denen nur MANCHE Wiederholungen einen Fehler zeigen.
        per_item = collections.defaultdict(list)
        for r in rs:
            per_item[r['item']].append(bool(r['pruefung']['fehler']))
        flaky = sum(1 for v_ in per_item.values() if 0 < sum(v_) < len(v_))

        buckets = {}
        for b in BUCKETS:
            brs = [r for r in rs if r['bucket'] == b]
            if not brs:
                continue
            bg = [r['bewertung'] for r in brs if isinstance(r.get('bewertung'), dict)
                  and 'treue' in r['bewertung']]
            buckets[b] = {
                'n': len(brs),
                'fehler': sum(len(r['pruefung']['fehler']) for r in brs),
                'treue': avg('treue', bg), 'vollstaendigkeit': avg('vollstaendigkeit', bg),
                'klarheit': avg('klarheit', bg),
            }

        out[v] = {
            'n': len(rs),
            'fehler': sum(len(r['pruefung']['fehler']) for r in rs),
            'hinweise': sum(len(r['pruefung']['hinweise']) for r in rs),
            'wacklig': flaky,
            'treue': avg('treue'), 'vollstaendigkeit': avg('vollstaendigkeit'),
            'klarheit': avg('klarheit'),
            'cent': money(rs),
            'buckets': buckets,
        }
    return out


def fmt(x, nd=2):
    return '—' if x is None else f'{x:.{nd}f}'


def sample_items(results, per_bucket):
    by_bucket = collections.defaultdict(list)
    seen = set()
    for r in results:
        if r['item'] in seen:
            continue
        seen.add(r['item'])
        by_bucket[r['bucket']].append(r['item'])
    picked = []
    for b in BUCKETS:
        picked.extend(by_bucket.get(b, [])[:per_bucket])
    return picked


def build_markdown(data, stats, picked):
    v = data['variants']
    L = []
    L.append(f"# Prompt-Lauf: {data['section']}\n")
    L.append(f"Varianten: {', '.join(v)} · {data['repeats']} Wiederholungen je Diktat\n")
    L.append('## Gesamt\n')
    L.append('| Variante | Fehler | wacklig | Treue | Vollst. | Klarheit | Cent |')
    L.append('|---|---:|---:|---:|---:|---:|---:|')
    for name in v:
        s = stats[name]
        L.append(f"| {name} | {s['fehler']} | {s['wacklig']} | {fmt(s['treue'])} | "
                 f"{fmt(s['vollstaendigkeit'])} | {fmt(s['klarheit'])} | {s['cent']:.2f} |")
    L.append('\n> „wacklig" = Diktate, bei denen nur ein Teil der Wiederholungen einen '
             'Fehler zeigte. Ein Prompt, der nur manchmal gegen den Vertrag verstösst, '
             'ist schlechter als sein Mittelwert aussieht.\n')

    L.append('## Nach Diktatlänge\n')
    L.append('| Variante | Klasse | n | Fehler | Treue | Vollst. | Klarheit |')
    L.append('|---|---|---:|---:|---:|---:|---:|')
    for name in v:
        for b, s in stats[name]['buckets'].items():
            L.append(f"| {name} | {b} | {s['n']} | {s['fehler']} | {fmt(s['treue'])} | "
                     f"{fmt(s['vollstaendigkeit'])} | {fmt(s['klarheit'])} |")

    errs = [r for r in data['results'] if r['pruefung']['fehler']]
    L.append(f'\n## Vertragsverletzungen ({len(errs)})\n')
    if not errs:
        L.append('Keine.\n')
    else:
        for r in errs[:60]:
            L.append(f"- **{r['variante']}** · `{r['item']}` (Lauf {r['lauf']}): "
                     + '; '.join(r['pruefung']['fehler']))

    L.append('\n## Bemängelt durch den Richter\n')
    for r in data['results']:
        b = r.get('bewertung') or {}
        for m in (b.get('maengel') or [])[:3]:
            L.append(f"- **{r['variante']}** · `{r['item']}`: {m}")

    L.append('\n## Gegenüberstellung (Stichprobe)\n')
    for item in picked:
        L.append(f'\n### `{item}`\n')
        for name in v:
            r = next((x for x in data['results']
                      if x['item'] == item and x['variante'] == name and x['lauf'] == 1), None)
            if not r:
                continue
            b = r.get('bewertung') or {}
            L.append(f"**{name}** — Treue {b.get('treue','—')} · "
                     f"Vollst. {b.get('vollstaendigkeit','—')} · Klarheit {b.get('klarheit','—')}\n")
            L.append('```\n' + (r['ausgabe'] or '(leer)')[:2500] + '\n```\n')
    return '\n'.join(L)


CSS = """
body{font:14px/1.55 -apple-system,Segoe UI,sans-serif;margin:0;padding:24px 32px;color:#222;background:#fff}
h1{font-size:22px;margin:0 0 4px} h2{font-size:17px;margin:28px 0 8px;border-bottom:1px solid #e3e3e3;padding-bottom:4px}
h3{font-size:14px;margin:18px 0 6px;font-family:ui-monospace,Consolas,monospace;color:#555}
table{border-collapse:collapse;margin:8px 0 16px;font-size:13px}
th,td{border:1px solid #ddd;padding:5px 10px;text-align:left} th{background:#f5f5f5;font-weight:600}
td.num{text-align:right;font-variant-numeric:tabular-nums}
.cols{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:14px}
.card{border:1px solid #ccc;border-radius:3px;padding:10px 12px;background:#fafafa;min-width:0}
.card h4{margin:0 0 6px;font-size:13px}
pre{white-space:pre-wrap;word-wrap:break-word;font:12px/1.5 ui-monospace,Consolas,monospace;background:#fff;border:1px solid #e5e5e5;padding:8px;border-radius:3px;margin:0;max-height:460px;overflow:auto}
.bad{color:#C0281C;font-weight:600} .warn{color:#C67C00} .ok{color:#2E7D32}
.meta{color:#888;font-size:12px} .pill{display:inline-block;background:#eee;border-radius:10px;padding:1px 8px;font-size:11px;margin-right:4px}
"""


def build_html(data, stats, picked):
    v = data['variants']
    e = html.escape
    H = [f'<!doctype html><meta charset="utf-8"><title>Prompt-Lauf {e(data["section"])}</title>'
         f'<style>{CSS}</style>',
         f'<h1>Prompt-Lauf: {e(data["section"])}</h1>',
         f'<p class="meta">Varianten: {e(", ".join(v))} · {data["repeats"]} Wiederholungen je Diktat</p>']

    H.append('<h2>Gesamt</h2><table><tr><th>Variante</th><th>Fehler</th><th>wacklig</th>'
             '<th>Treue</th><th>Vollst.</th><th>Klarheit</th><th>Cent</th></tr>')
    for name in v:
        s = stats[name]
        cls = 'bad' if s['fehler'] else 'ok'
        H.append(f'<tr><td>{e(name)}</td><td class="num {cls}">{s["fehler"]}</td>'
                 f'<td class="num">{s["wacklig"]}</td><td class="num">{fmt(s["treue"])}</td>'
                 f'<td class="num">{fmt(s["vollstaendigkeit"])}</td>'
                 f'<td class="num">{fmt(s["klarheit"])}</td>'
                 f'<td class="num">{s["cent"]:.2f}</td></tr>')
    H.append('</table><p class="meta">„wacklig" = Diktate, bei denen nur ein Teil der '
             'Wiederholungen einen Fehler zeigte.</p>')

    H.append('<h2>Nach Diktatlänge</h2><table><tr><th>Variante</th><th>Klasse</th><th>n</th>'
             '<th>Fehler</th><th>Treue</th><th>Vollst.</th><th>Klarheit</th></tr>')
    for name in v:
        for b, s in stats[name]['buckets'].items():
            H.append(f'<tr><td>{e(name)}</td><td>{b}</td><td class="num">{s["n"]}</td>'
                     f'<td class="num">{s["fehler"]}</td><td class="num">{fmt(s["treue"])}</td>'
                     f'<td class="num">{fmt(s["vollstaendigkeit"])}</td>'
                     f'<td class="num">{fmt(s["klarheit"])}</td></tr>')
    H.append('</table>')

    errs = [r for r in data['results'] if r['pruefung']['fehler']]
    H.append(f'<h2>Vertragsverletzungen ({len(errs)})</h2>')
    if not errs:
        H.append('<p class="ok">Keine.</p>')
    else:
        H.append('<ul>')
        for r in errs[:80]:
            H.append(f'<li><b>{e(r["variante"])}</b> · <code>{e(r["item"])}</code> '
                     f'(Lauf {r["lauf"]}): <span class="bad">'
                     f'{e("; ".join(r["pruefung"]["fehler"]))}</span></li>')
        H.append('</ul>')

    H.append('<h2>Gegenüberstellung (Stichprobe)</h2>')
    for item in picked:
        H.append(f'<h3>{e(item)}</h3><div class="cols">')
        for name in v:
            r = next((x for x in data['results']
                      if x['item'] == item and x['variante'] == name and x['lauf'] == 1), None)
            if not r:
                continue
            b = r.get('bewertung') or {}
            pills = (f'<span class="pill">Treue {b.get("treue","—")}</span>'
                     f'<span class="pill">Vollst. {b.get("vollstaendigkeit","—")}</span>'
                     f'<span class="pill">Klarheit {b.get("klarheit","—")}</span>')
            maengel = ''.join(f'<li class="warn">{e(m)}</li>' for m in (b.get('maengel') or []))
            fehler = ''.join(f'<li class="bad">{e(m)}</li>' for m in r['pruefung']['fehler'])
            H.append(f'<div class="card"><h4>{e(name)}</h4>{pills}'
                     f'<ul>{fehler}{maengel}</ul>'
                     f'<pre>{e((r["ausgabe"] or "(leer)")[:4000])}</pre></div>')
        H.append('</div>')
    return '\n'.join(H)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('--sample', type=int, default=2)
    a = ap.parse_args()

    data = json.load(io.open(a.run, encoding='utf-8'))
    variants = sorted({r['variante'] for r in data['results']})
    data['variants'] = variants
    stats = agg(data['results'], variants)
    picked = sample_items(data['results'], a.sample)

    stem = os.path.splitext(a.run)[0]
    io.open(stem + '.md', 'w', encoding='utf-8').write(build_markdown(data, stats, picked))
    io.open(stem + '.html', 'w', encoding='utf-8').write(build_html(data, stats, picked))
    print(f'{stem}.html\n{stem}.md')


if __name__ == '__main__':
    main()
