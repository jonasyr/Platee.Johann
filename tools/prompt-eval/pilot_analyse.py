"""Rechnet aus den Pilotdaten die fuenf Zahlen, die der Hauptlauf braucht.

Aufruf:
    uv run python pilot_analyse.py                 # Kennzahlen + Bewertungsseite
    uv run python pilot_analyse.py --noten <datei> # mit menschlichen Noten

Ohne menschliche Noten liefert der Lauf vier der fuenf Zahlen und erzeugt die blinde
Bewertungsseite. Mit Noten kommt die fuenfte dazu -- und mit ihr die Entscheidung, ob
Prediction-Powered Inference im Hauptlauf traegt.

Alle Verfahren hier arbeiten auf gespeicherten Rohdaten. Eine korrigierte Auswertung kostet
damit null API-Aufrufe. Das ist keine Bequemlichkeit: im Vorversuch meldete ein fehlerhafter
Sprachpruefer elf Vertragsverletzungen, die samt und sonders Fehlalarme waren. Nach Korrektur
liess sich alles neu rechnen, ohne einen einzigen Aufruf.
"""

from __future__ import annotations

import argparse
import html
import json
import math
import random
import statistics
from collections import defaultdict
from pathlib import Path

import numpy as np

DIMENSIONS = ("treue", "vollstaendigkeit", "klarheit")
SCALE = (1, 2, 3, 4, 5)

#: Die Dimension, an der der Hauptlauf haengt. Fuer ein Diktat-Archiv ist Treue der eigentliche
#: Massstab: eine unvollstaendige Notiz aergert, eine erfundene richtet Schaden an.
PRIMARY = "treue"


# ------------------------------------------------------------------ Kennzahlen, allgemein


def icc_3_1(matrix: np.ndarray) -> float:
    """ICC(3,1) -- zweifaktoriell gemischt, Konsistenz, Einzelmessung.

    Zeilen sind Objekte, Spalten Wiederholungen desselben Bewerters. Das ist die richtige Form
    fuer Test-Retest mit einem Bewerter: die Wiederholungen sind fest, nicht aus einer
    Grundgesamtheit gezogen (Shrout & Fleiss, 1979; McGraw & Wong, 1996).
    """
    n, k = matrix.shape
    if n < 2 or k < 2:
        return float("nan")
    grand = matrix.mean()
    ms_rows = k * ((matrix.mean(axis=1) - grand) ** 2).sum() / (n - 1)
    ms_cols = n * ((matrix.mean(axis=0) - grand) ** 2).sum() / (k - 1)
    residual = (
        ((matrix - matrix.mean(axis=1, keepdims=True) - matrix.mean(axis=0, keepdims=True) + grand) ** 2).sum()
        / ((n - 1) * (k - 1))
    )
    if ms_rows + (k - 1) * residual == 0:
        return float("nan")
    return float((ms_rows - residual) / (ms_rows + (k - 1) * residual))


def variance_components(groups: list[list[float]]) -> dict[str, float]:
    """Zerlegt die Streuung in zwischen und innerhalb der Gruppen.

    Einfaktorielle Varianzanalyse mit zufaelligem Effekt. tau^2 ist die Streuung zwischen
    Diktaten, sigma^2 das Rauschen innerhalb eines Diktats. Ihr Verhaeltnis entscheidet, ob das
    Budget in mehr Diktate oder in mehr Wiederholungen gehoert -- eine Faustregel dafuer gibt es
    nachweislich nicht (Hedges & Schauer, 2021).
    """
    groups = [g for g in groups if len(g) >= 2]
    if len(groups) < 2:
        return {"tau2": float("nan"), "sigma2": float("nan"), "verhaeltnis": float("nan")}

    sizes = [len(g) for g in groups]
    n_groups = len(groups)
    all_values = [v for g in groups for v in g]
    grand = statistics.fmean(all_values)

    ss_between = sum(len(g) * (statistics.fmean(g) - grand) ** 2 for g in groups)
    ss_within = sum((v - statistics.fmean(g)) ** 2 for g in groups for v in g)
    df_between, df_within = n_groups - 1, len(all_values) - n_groups
    if df_within <= 0:
        return {"tau2": float("nan"), "sigma2": float("nan"), "verhaeltnis": float("nan")}

    ms_between, ms_within = ss_between / df_between, ss_within / df_within
    # Bei ungleichen Gruppengroessen ist n_0 das harmonische Mittel-artige Gewicht.
    total = sum(sizes)
    n0 = (total - sum(s * s for s in sizes) / total) / (n_groups - 1)
    tau2 = max(0.0, (ms_between - ms_within) / n0)
    sigma2 = ms_within
    ratio = tau2 / sigma2 if sigma2 > 0 else float("inf")
    return {"tau2": round(tau2, 4), "sigma2": round(sigma2, 4), "verhaeltnis": round(ratio, 4)}


def _ranks(values: list[float]) -> list[float]:
    order = sorted(range(len(values)), key=lambda i: values[i])
    ranks = [0.0] * len(values)
    i = 0
    while i < len(order):
        j = i
        while j + 1 < len(order) and values[order[j + 1]] == values[order[i]]:
            j += 1
        average = (i + j) / 2 + 1
        for k in range(i, j + 1):
            ranks[order[k]] = average
        i = j + 1
    return ranks


def correlation(a: list[float], b: list[float]) -> dict[str, float]:
    """Pearson und Spearman. Beide, weil ordinale Noten die Pearson-Annahme dehnen."""
    if len(a) < 3:
        return {"pearson": float("nan"), "spearman": float("nan"), "n": len(a)}
    pearson = float(np.corrcoef(a, b)[0, 1])
    spearman = float(np.corrcoef(_ranks(a), _ranks(b))[0, 1])
    return {"pearson": round(pearson, 4), "spearman": round(spearman, 4), "n": len(a)}


def gwet_ac2(a: list[int], b: list[int], categories: tuple[int, ...] = SCALE) -> dict[str, float]:
    """Gwets AC2 mit ordinalen Gewichten, dazu gewichtetes Kappa zum Vergleich.

    Warum nicht einfach Kappa: Kappa schaetzt die Zufallsuebereinstimmung aus den
    Randverteilungen. Bei schiefer Verteilung -- und genau die ist zu erwarten, wenn die meisten
    Ausgaben gut sind -- wird diese Basis gross und drueckt Kappa nach unten, obwohl die rohe
    Uebereinstimmung hoch ist. Ein dokumentierter Fall zeigt 93 % Uebereinstimmung bei
    Kappa 0,65 und AC1 0,81. Mit Kappa als Kriterium wuerde man eine gelungene Kalibrierung fuer
    gescheitert erklaeren.
    """
    q = len(categories)
    index = {c: i for i, c in enumerate(categories)}
    span = categories[-1] - categories[0]
    weights = np.array(
        [[1 - ((x - y) / span) ** 2 for y in categories] for x in categories], dtype=float
    )

    n = len(a)
    if n == 0:
        return {"ac2": float("nan"), "kappa_gewichtet": float("nan"), "exakt": float("nan")}

    observed = statistics.fmean(weights[index[x], index[y]] for x, y in zip(a, b))
    exact = statistics.fmean(1.0 if x == y else 0.0 for x, y in zip(a, b))

    # Gwets Zufallsbasis: mittlere Randhaeufigkeit, nicht das Produkt zweier Randverteilungen.
    pi = np.array(
        [((sum(1 for x in a if x == c) + sum(1 for y in b if y == c)) / (2 * n)) for c in categories]
    )
    t_w = weights.sum()
    chance_gwet = (t_w / (q * (q - 1))) * float((pi * (1 - pi)).sum())
    ac2 = (observed - chance_gwet) / (1 - chance_gwet) if chance_gwet < 1 else float("nan")

    # Klassisches gewichtetes Kappa zum Vergleich.
    pa_ = np.array([sum(1 for x in a if x == c) / n for c in categories])
    pb_ = np.array([sum(1 for y in b if y == c) / n for c in categories])
    chance_kappa = float((weights * np.outer(pa_, pb_)).sum())
    kappa = (observed - chance_kappa) / (1 - chance_kappa) if chance_kappa < 1 else float("nan")

    return {
        "ac2": round(float(ac2), 4),
        "kappa_gewichtet": round(float(kappa), 4),
        "exakt": round(exact, 4),
        "gewichtet_beobachtet": round(observed, 4),
    }


# --------------------------------------------------------------------------- Auswertung


def mean_scores(row: dict) -> dict[str, float] | None:
    """Mittelt die Richter-Wiederholungen einer Ausgabe.

    Dieser Schritt ist wichtiger, als er aussieht: er nimmt das Richterrauschen aus den Zahlen
    heraus, bevor die Varianzkomponenten der *Erzeugung* geschaetzt werden. Ohne ihn steckte das
    Rauschen des Messgeraets in sigma^2 und die Budgetaufteilung waere systematisch falsch.
    """
    valid = [r["scores"] for r in row.get("ratings", []) if r.get("scores")]
    if not valid:
        return None
    return {dim: statistics.fmean(float(s[dim]) for s in valid) for dim in DIMENSIONS}


def judge_consistency(rows: list[dict]) -> dict[str, dict]:
    """Selbstkonsistenz: dieselbe Ausgabe, mehrfach bewertet."""
    result: dict[str, dict] = {}
    for dim in DIMENSIONS:
        matrix_rows: list[list[float]] = []
        for row in rows:
            values = [
                float(r["scores"][dim]) for r in row.get("ratings", []) if r.get("scores")
            ]
            if len(values) >= 2:
                matrix_rows.append(values)
        if not matrix_rows:
            result[dim] = {"icc_3_1": float("nan"), "gleiches_urteil": float("nan"), "n": 0}
            continue
        width = min(len(v) for v in matrix_rows)
        matrix = np.array([v[:width] for v in matrix_rows], dtype=float)
        identical = statistics.fmean(
            1.0 if len(set(v[:width])) == 1 else 0.0 for v in matrix_rows
        )
        result[dim] = {
            "icc_3_1": round(icc_3_1(matrix), 4),
            "gleiches_urteil": round(identical, 4),
            "n": len(matrix_rows),
        }
    return result


def generation_variance(rows: list[dict]) -> dict[str, dict]:
    """Varianzkomponenten und Rauschboden je Dimension."""
    result: dict[str, dict] = {}
    for dim in DIMENSIONS:
        by_item: dict[tuple[str, str], list[float]] = defaultdict(list)
        for row in rows:
            means = mean_scores(row)
            if means is None:
                continue
            by_item[(row["item_id"], row["variant_id"])].append(means[dim])

        components = variance_components(list(by_item.values()))
        spreads = [statistics.stdev(v) for v in by_item.values() if len(v) >= 2]
        result[dim] = {
            **components,
            "rauschboden_sd": round(statistics.fmean(spreads), 4) if spreads else float("nan"),
            "rauschboden_max": round(max(spreads), 4) if spreads else float("nan"),
            "zellen": len(by_item),
        }
    return result


def ppi_threshold(n_human: int) -> float:
    """1/sqrt(n-2): unterhalb dieser Korrelation schadet PPI, statt zu nuetzen.

    Die asymptotische Aussage "PPI ist nie schlechter als die menschlichen Labels allein" gilt
    im kleinen Stichprobenbereich nicht (arXiv:2505.20178).
    """
    return float("inf") if n_human <= 2 else 1.0 / math.sqrt(n_human - 2)


def correlation_interval(r: float, n: int, level: float = 1.96) -> tuple[float, float]:
    """Konfidenzintervall einer Korrelation ueber die Fisher-z-Transformation."""
    if n <= 3 or not -1.0 < r < 1.0:
        return (float("nan"), float("nan"))
    z = math.atanh(r)
    half = level / math.sqrt(n - 3)
    return (math.tanh(z - half), math.tanh(z + half))


def ppi_verdict(r: float, n: int) -> tuple[str, tuple[float, float]]:
    """Entscheidet ueber PPI -- und zwar strenger als die blosse Schwelle.

    ⚠ Die Schwelle allein reicht nicht, und das faellt erst beim Testen auf: bei n = 30 liegt
    sie bei 0,19. Eine Korrelation von 0,20 ueberschreitet sie zwar, ist aber von null nicht
    unterscheidbar -- im Test mit rein zufaelligen Noten hat genau das zweimal
    "bestanden" ergeben.

    Die Schwelle ist also eine *notwendige*, keine hinreichende Bedingung. Verlangt wird
    deshalb, dass die *untere* Grenze des Konfidenzintervalls ueber der Schwelle liegt. Liegt
    nur der Punktschaetzer darueber, lautet das Urteil "unsicher" -- und die ehrliche Antwort
    ist, mehr Noten zu vergeben, nicht das Verfahren zu starten.
    """
    threshold = ppi_threshold(n)
    low, high = correlation_interval(r, n)
    if math.isnan(low):
        return "zu wenige Noten", (low, high)
    if low > threshold:
        return "PPI traegt", (low, high)
    if r >= threshold:
        return "unsicher — Schwelle nur im Punktschaetzer, mehr Noten noetig", (low, high)
    return "PPI SCHADET — weglassen", (low, high)


# ------------------------------------------------------------------- Bewertungsseite


RATING_PAGE = """<!doctype html>
<meta charset="utf-8"><title>Blindbewertung Pilotlauf</title>
<style>
 body{{font:16px/1.6 system-ui,sans-serif;max-width:860px;margin:0 auto;padding:24px;background:#fafafa;color:#16211f}}
 h1{{font-size:1.4rem}} .karte{{background:#fff;border:1px solid #d8dedd;padding:18px;margin:22px 0}}
 .quelle{{background:#f3f5f4;padding:12px;white-space:pre-wrap;font-size:14px;max-height:260px;overflow:auto}}
 .ausgabe{{padding:12px;white-space:pre-wrap;border-left:3px solid #0e5c55}}
 .dim{{display:flex;gap:10px;align-items:center;margin:8px 0;flex-wrap:wrap}}
 .dim b{{width:150px}} label{{padding:4px 9px;border:1px solid #c9d0ce;cursor:pointer;border-radius:2px}}
 input{{margin-right:4px}} #fertig{{position:sticky;bottom:0;background:#0e5c55;color:#fff;padding:14px;text-align:center}}
 button{{font:inherit;padding:9px 18px;cursor:pointer}}
</style>
<h1>Blindbewertung — {n} Ausgaben</h1>
<p>Reihenfolge zufällig, Modellnoten nicht sichtbar. Bewerte nach derselben Vorschrift wie der
Richter. <b>Nicht</b> nachschlagen, was das Modell vergeben hat — sonst misst der Vergleich
Ankerwirkung statt Urteil.</p>
<ol style="background:#fff;border:1px solid #d8dedd;padding:18px 18px 18px 38px">
<li><b>Treue</b> — 5 = jede Aussage im Transkript belegt · 3 = eine Aussage geht darüber hinaus · 1 = mehrere erfunden</li>
<li><b>Vollständigkeit</b> — 5 = alles Wesentliche da · 3 = ein wesentlicher Punkt fehlt · 1 = mehrere fehlen</li>
<li><b>Klarheit</b> — 5 = durchgehend klar · 3 = eine Stelle unklar · 1 = mehrere unverständlich</li>
</ol>
<div id="liste"></div>
<div id="fertig"><button onclick="speichern()">Noten als JSON speichern</button></div>
<script>
const DATEN = {daten};
const liste = document.getElementById('liste');
DATEN.forEach((d, i) => {{
  const k = document.createElement('div'); k.className = 'karte';
  k.innerHTML = '<b>' + (i+1) + ' von ' + DATEN.length + '</b>'
    + '<p style="color:#6b7774;font-size:13px;margin:4px 0">Transkript</p>'
    + '<div class="quelle">' + d.transkript + '</div>'
    + '<p style="color:#6b7774;font-size:13px;margin:14px 0 4px">Erzeugte Zusammenfassung</p>'
    + '<div class="ausgabe">' + d.ausgabe + '</div>'
    + ['treue','vollstaendigkeit','klarheit'].map(dim =>
        '<div class="dim"><b>' + dim + '</b>' + [1,2,3,4,5].map(v =>
          '<label><input type="radio" name="' + d.id + '_' + dim + '" value="' + v + '">' + v + '</label>'
        ).join('') + '</div>').join('');
  liste.appendChild(k);
}});
function speichern() {{
  const noten = [];
  let fehlend = 0;
  DATEN.forEach(d => {{
    const eintrag = {{id: d.id}};
    ['treue','vollstaendigkeit','klarheit'].forEach(dim => {{
      const gewaehlt = document.querySelector('input[name="' + d.id + '_' + dim + '"]:checked');
      if (gewaehlt) eintrag[dim] = Number(gewaehlt.value); else fehlend++;
    }});
    if (Object.keys(eintrag).length > 1) noten.push(eintrag);
  }});
  if (fehlend) {{
    if (!confirm(fehlend + ' Noten fehlen noch. Trotzdem speichern?')) return;
  }}
  const blob = new Blob([JSON.stringify(noten, null, 2)], {{type: 'application/json'}});
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob); a.download = 'pilot_noten.json'; a.click();
}}
</script>
"""


def build_rating_page(payload: dict, sample: int, seed: int, target: Path) -> int:
    """Erzeugt die blinde Bewertungsseite aus einer echten Zufallsstichprobe.

    Die Stichprobe muss i.i.d. gezogen sein. Waehlte man stattdessen die schwierigen oder
    strittigen Faelle, waere die spaetere PPI-Korrektur ungueltig -- das Verfahren setzt eine
    Zufallsauswahl voraus.
    """
    rows = [r for r in payload["zeilen"] if r.get("output")]
    rng = random.Random(seed)
    picked = rng.sample(rows, min(sample, len(rows)))
    rng.shuffle(picked)

    daten = [
        {
            "id": f"{row['item_id']}|{row['variant_id']}|{row['run_index']}",
            "transkript": html.escape(payload["transkripte"][row["item_id"]][:4000]),
            "ausgabe": html.escape(row["output"][:6000]),
        }
        for row in picked
    ]
    target.write_text(
        RATING_PAGE.format(n=len(daten), daten=json.dumps(daten, ensure_ascii=False)),
        encoding="utf-8",
    )
    return len(daten)


def compare_with_human(payload: dict, notes_path: Path) -> dict:
    """Vergleicht menschliche Noten mit den Richternoten derselben Ausgaben."""
    notes = {n["id"]: n for n in json.loads(notes_path.read_text(encoding="utf-8"))}
    index = {
        f"{r['item_id']}|{r['variant_id']}|{r['run_index']}": r for r in payload["zeilen"]
    }

    result: dict[str, dict] = {}
    for dim in DIMENSIONS:
        human: list[int] = []
        judge: list[int] = []
        for key, note in notes.items():
            row = index.get(key)
            if row is None or dim not in note:
                continue
            means = mean_scores(row)
            if means is None:
                continue
            human.append(int(note[dim]))
            judge.append(int(round(means[dim])))
        if len(human) < 3:
            result[dim] = {"n": len(human), "hinweis": "zu wenige Noten"}
            continue
        result[dim] = {
            **correlation([float(v) for v in human], [float(v) for v in judge]),
            **gwet_ac2(human, judge),
        }
    return result


# -------------------------------------------------------------------------------- Main


def main(argv: list[str] | None = None) -> int:
    base = Path.home() / "Documents" / "Johann" / "prompt-sandbox" / "eval"
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--roh", type=Path, default=base / "pilot_raw.json")
    parser.add_argument("--noten", type=Path, help="pilot_noten.json aus der Bewertungsseite")
    parser.add_argument("--stichprobe", type=int, default=30, help="Ausgaben zum Bewerten")
    parser.add_argument("--seed", type=int, default=20260913)
    parser.add_argument("--out", type=Path, default=base / "pilot_kennzahlen.json")
    args = parser.parse_args(argv)

    if not args.roh.exists():
        raise SystemExit(f"Rohdaten fehlen: {args.roh}\nZuerst pilot.py laufen lassen.")
    payload = json.loads(args.roh.read_text(encoding="utf-8"))
    rows = payload["zeilen"]

    consistency = judge_consistency(rows)
    variance = generation_variance(rows)
    first_try = statistics.fmean(
        1.0 if r["gen"].get("well_formed_first_try") else 0.0 for r in rows
    )
    empty = sum(1 for r in rows if not r.get("output"))
    cached = [r["gen"].get("cached_tokens", 0) for r in rows]

    print("=" * 74)
    print("PILOTLAUF — Kennzahlen des Messinstruments")
    print("=" * 74)

    print("\n1. VARIANZKOMPONENTEN  (Richterrauschen vorher herausgemittelt)")
    print(f"   {'Dimension':<18}{'tau^2':>9}{'sigma^2':>10}{'tau^2/sigma^2':>15}")
    for dim, v in variance.items():
        mark = "  <-- primaer" if dim == PRIMARY else ""
        print(f"   {dim:<18}{v['tau2']:>9}{v['sigma2']:>10}{v['verhaeltnis']:>15}{mark}")
    primary = variance[PRIMARY]
    if not math.isnan(primary["tau2"]) and not math.isnan(primary["sigma2"]):
        print("\n1b. WAS WIEDERHOLUNGEN NOCH BRINGEN  (Korpus ist auf 60 Diktate begrenzt)")
        print(f"   {'K':>3}{'Varianz je Zelle':>20}{'gegenueber K=1':>18}")
        base = primary["tau2"] + primary["sigma2"]
        for k in (1, 2, 3, 4, 6, 8):
            var = primary["tau2"] + primary["sigma2"] / k
            print(f"   {k:>3}{var:>20.4f}{100 * var / base:>17.0f}%")
        print(f"   Untergrenze bei unendlich vielen Wiederholungen: {primary['tau2']:.4f}")
        print(
            "   Die Untergrenze ist tau^2 -- sie ist durch Wiederholungen nicht zu "
            "unterbieten,\n   nur durch mehr Diktate."
        )

    print("\n2. SELBSTKONSISTENZ DES RICHTERS")
    print(f"   {'Dimension':<18}{'ICC(3,1)':>10}{'gleiches Urteil':>18}{'n':>6}")
    for dim, v in consistency.items():
        print(f"   {dim:<18}{v['icc_3_1']:>10}{v['gleiches_urteil']:>18}{v['n']:>6}")
    same = consistency[PRIMARY]["gleiches_urteil"]
    if isinstance(same, float) and not math.isnan(same):
        print(
            "   "
            + (
                "unter 0,80 -> mehrere Richteraufrufe mitteln, nicht einem vertrauen"
                if same < 0.80
                else "ueber 0,80 -> ein Richteraufruf je Ausgabe genuegt"
            )
        )

    print("\n3. RAUSCHBODEN  (Grundlage der Aequivalenzmarge)")
    for dim, v in variance.items():
        print(
            f"   {dim:<18}mittlere SD ueber Wiederholungen {v['rauschboden_sd']:>7}"
            f"   groesste {v['rauschboden_max']:>6}"
        )
    print("   Ein Unterschied unterhalb dieser Streuung ist per Konstruktion nicht der Rede wert.")

    print("\n4. BETRIEBSKENNZAHLEN")
    print(f"   beim ersten Versuch wohlgeformt: {first_try:.3f}")
    print(f"   leere Ausgaben:                  {empty} von {len(rows)}")
    print(f"   Antworten mit Cache-Treffer:     {sum(1 for c in cached if c > 0)} von {len(rows)}")

    report = {
        "konfiguration": payload["konfiguration"],
        "varianzkomponenten": variance,
        "selbstkonsistenz": consistency,
        "erstversuchsquote": round(first_try, 4),
        "leere_ausgaben": empty,
    }

    print("\n5. KALIBRIERUNG GEGEN DEN MENSCHEN")
    if args.noten and args.noten.exists():
        human = compare_with_human(payload, args.noten)
        report["kalibrierung"] = human
        for dim, v in human.items():
            if "hinweis" in v:
                print(f"   {dim:<18}{v['hinweis']} (n={v['n']})")
                continue
            threshold = ppi_threshold(v["n"])
            verdict, (low, high) = ppi_verdict(v["pearson"], v["n"])
            v["ppi_urteil"] = verdict
            v["ki_95"] = [round(low, 4), round(high, 4)]
            print(
                f"   {dim:<18}r={v['pearson']:>6}  rho={v['spearman']:>6}  "
                f"AC2={v['ac2']:>6}  kappa_w={v['kappa_gewichtet']:>6}  n={v['n']}"
            )
            print(
                f"   {'':<18}95 %-KI [{low:.3f}; {high:.3f}]  gegen Schwelle "
                f"{threshold:.3f}  ->  {verdict}"
            )
        print("   AC2 ist das Hauptmass; Kappa steht daneben, weil es bei schiefen")
        print("   Randverteilungen systematisch zu niedrig ausfaellt.")
    else:
        target = args.roh.parent / "pilot_bewertung.html"
        count = build_rating_page(payload, args.stichprobe, args.seed, target)
        print(f"   Noch keine Noten. Bewertungsseite erzeugt: {count} Ausgaben")
        print(f"   {target}")
        print("   Im Browser oeffnen, blind bewerten, JSON speichern, dann:")
        print("   uv run python pilot_analyse.py --noten <pfad zur pilot_noten.json>")

    args.out.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(f"\nKennzahlen: {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
