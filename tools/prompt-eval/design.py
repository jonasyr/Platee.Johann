"""Erzeugt einen D-optimalen Split-Plot-Versuchsplan per Koordinatenaustausch.

Warum nicht einfach ein Katalogplan: Unsere Faktoren sind kategorial, die Laufzahl ist durch
das Budget vorgegeben und nicht durch eine Formel, und die Struktur ist ein Split-Plot. Kein
Katalogplan erfuellt alle drei Bedingungen gleichzeitig. Rechnergestuetzte optimale Plaene
schon -- bei gleicher Laufzahl gewinnen sie messbar (Allen & Tseng, 2011: D-Effizienz 43,7
gegen 49,5 %, G-Effizienz 71,2 gegen 86,6 %).

Warum Split-Plot: Die Systemnachricht ist teuer zu wechseln. Sie geht in jeden Abschnittsaufruf
ein, und jede Aenderung wirft den Praefix-Cache weg. Der Abschnitts-Wortlaut ist dagegen billig.
Ein Plan, der das ignoriert, verlangt staendige Wechsel des teuren Faktors -- und seine
Auswertung testet den teuren Faktor dann gegen den falschen, zu kleinen Fehlerterm und blaeht
den Fehler erster Art auf (Frey et al., 2024).

Die beiden Ebenen heissen hier Whole Plot (Systemnachricht) und Subplot (Abschnitt). Innerhalb
eines Whole Plots bleibt die Systemnachricht fest, und mehrere Subplot-Laeufe variieren nur den
Abschnitts-Wortlaut.

Literatur: Jones & Goos (2007), doi:10.1111/j.1467-9876.2007.00581.x -- kandidatenmengenfreier
Algorithmus fuer D-optimale Split-Plot-Plaene.
"""

from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from itertools import combinations
from pathlib import Path

import numpy as np

# Faktornamen wie im Generator `variants.py`. Die Reihenfolge legt die Spalten fest.
WHOLE_PLOT_FACTORS: tuple[str, ...] = (
    "s1_denkprozess",
    "s2_grossschreibung",
    "s3_verbote",
    "s4_zentralregel",
)
SUBPLOT_FACTORS: tuple[str, ...] = (
    "a1_sprachpraemisse",
    "a2_leerfall",
    "a3_vollvertrag",
)


@dataclass(frozen=True)
class DesignSpec:
    """Umfang und Annahmen eines Plans."""

    whole_plots: int = 12
    """Zahl der Systemnachricht-Fassungen.

    Diese Zahl -- nicht die Gesamtzahl der Laeufe -- bestimmt die Praezision der teuren
    Faktoren. Nach Achsenabschnitt und vier Haupteffekten bleiben bei 12 Whole Plots sieben
    Freiheitsgrade fuer den Whole-Plot-Fehler; bei 8 waeren es nur drei.

    Ein Vergleich mehrerer Zuschnitte zeigte ausserdem, dass 8 und 12 perfekt balanciert und
    unkorreliert aufgehen, 10 dagegen nicht (hoechste Faktorkorrelation 0,2 statt 0,0). Bei
    vier binaeren Faktoren sind Vielfache von acht die guenstigen Zahlen -- eine krumme
    Whole-Plot-Zahl kostet Struktur, ohne etwas zu sparen.
    """

    runs_per_plot: int = 4
    """Abschnitts-Laeufe je Systemnachricht."""

    variance_ratio: float = 1.0
    """Verhaeltnis Whole-Plot- zu Subplot-Varianz.

    Unbekannt, bis der Pilotlauf es schaetzt. 1,0 ist die neutrale Annahme. Der erzeugte Plan
    haengt davon ab, deshalb wird der verwendete Wert in den Plan geschrieben -- und der Plan
    nach dem Pilotlauf einmal neu erzeugt, falls der geschaetzte Wert deutlich abweicht.
    """

    starts: int = 200
    """Zufaellige Startpunkte. Koordinatenaustausch findet nur lokale Optima."""

    seed: int = 20260913

    @property
    def n_runs(self) -> int:
        return self.whole_plots * self.runs_per_plot


def model_matrix(whole: np.ndarray, sub: np.ndarray) -> np.ndarray:
    """Baut die Modellmatrix: Achsenabschnitt, alle Haupteffekte, alle WP-mal-SP-Wechselwirkungen.

    Wechselwirkungen *innerhalb* der Whole-Plot-Ebene sind bewusst nicht im Modell. Sie waeren
    nur mit der Zahl der Whole Plots schaetzbar, nicht mit der Zahl der Laeufe -- bei zehn
    Systemnachricht-Fassungen bliebe nach vier Haupteffekten und dem Achsenabschnitt zu wenig
    uebrig. Wechselwirkungen zwischen den Ebenen sind dagegen billig: sie leben im
    Subplot-Stratum, wo die Praezision hoch ist.
    """
    n = whole.shape[0]
    columns: list[np.ndarray] = [np.ones(n)]
    columns.extend(whole.T)
    columns.extend(sub.T)
    for i in range(whole.shape[1]):
        for j in range(sub.shape[1]):
            columns.append(whole[:, i] * sub[:, j])
    return np.column_stack(columns)


def _shrinkage(spec: DesignSpec) -> float:
    """Der Faktor c aus V_block^-1 = I_s - c * J_s.

    V = I + d * Z Z' mit Z als Zugehoerigkeit zum Whole Plot. Fuer einen Block der Groesse s
    gilt V_block = I_s + d * J_s, und die Inverse ist I_s - d/(1 + s*d) * J_s.
    """
    s, d = spec.runs_per_plot, spec.variance_ratio
    return d / (1.0 + s * d)


def log_d_criterion(whole: np.ndarray, sub: np.ndarray, spec: DesignSpec) -> float:
    """log|X' V^-1 X|. Groesser ist besser; -inf bei singulaerem Plan.

    Die Blockstruktur wird ausgenutzt, statt eine dichte N-mal-N-Matrix zu bilden. Mit
    V_block^-1 = I - c*J gilt

        X' V^-1 X  =  X'X  -  c * Σ_Bloecke (Spaltensumme des Blocks)(...)'

    Das ist derselbe Wert, kostet aber nur eine Summe je Block statt zweier Matrixprodukte mit
    der vollen Kovarianzmatrix. Der erste Entwurf baute die dichte Matrix bei *jedem* einzelnen
    Faktortausch neu -- bei Hunderttausenden Taeuschen war das der Grund, warum der
    Planvergleich den Rechner an die Speichergrenze gebracht hat.
    """
    x = model_matrix(whole, sub)
    blocks = x.reshape(spec.whole_plots, spec.runs_per_plot, x.shape[1])
    block_sums = blocks.sum(axis=1)
    information = x.T @ x - _shrinkage(spec) * (block_sums.T @ block_sums)
    sign, logdet = np.linalg.slogdet(information)
    return logdet if sign > 0 else -np.inf


def _expand(whole_settings: np.ndarray, spec: DesignSpec) -> np.ndarray:
    """Dehnt eine Einstellung je Whole Plot auf alle Laeufe dieses Whole Plots aus."""
    return np.repeat(whole_settings, spec.runs_per_plot, axis=0)


def coordinate_exchange(spec: DesignSpec) -> tuple[np.ndarray, np.ndarray, float]:
    """Sucht den besten Plan ueber mehrere zufaellige Startpunkte.

    Getauscht wird ein Faktor nach dem anderen: fuer jeden Whole Plot jede
    Systemnachricht-Einstellung, fuer jeden Lauf jede Abschnitts-Einstellung. Ein Tausch wird
    uebernommen, wenn er das Kriterium verbessert. Weil das Verfahren nur lokale Optima findet,
    laeuft es aus vielen Startpunkten und behaelt das beste Ergebnis.
    """
    rng = np.random.default_rng(spec.seed)
    n_wp, n_sp = len(WHOLE_PLOT_FACTORS), len(SUBPLOT_FACTORS)

    best_whole: np.ndarray | None = None
    best_sub: np.ndarray | None = None
    best_score = -np.inf

    for _ in range(spec.starts):
        whole_settings = rng.choice([-1.0, 1.0], size=(spec.whole_plots, n_wp))
        sub = rng.choice([-1.0, 1.0], size=(spec.n_runs, n_sp))
        whole = _expand(whole_settings, spec)
        score = log_d_criterion(whole, sub, spec)

        improved = True
        while improved:
            improved = False

            for plot in range(spec.whole_plots):
                for factor in range(n_wp):
                    whole_settings[plot, factor] *= -1
                    candidate = _expand(whole_settings, spec)
                    candidate_score = log_d_criterion(candidate, sub, spec)
                    if candidate_score > score + 1e-9:
                        score, whole, improved = candidate_score, candidate, True
                    else:
                        whole_settings[plot, factor] *= -1

            for run in range(spec.n_runs):
                for factor in range(n_sp):
                    sub[run, factor] *= -1
                    candidate_score = log_d_criterion(whole, sub, spec)
                    if candidate_score > score + 1e-9:
                        score, improved = candidate_score, True
                    else:
                        sub[run, factor] *= -1

        if score > best_score:
            best_score, best_whole, best_sub = score, whole.copy(), sub.copy()

    assert best_whole is not None and best_sub is not None
    return best_whole, best_sub, best_score


def balance_report(whole: np.ndarray, sub: np.ndarray) -> dict[str, dict[str, float]]:
    """Prueft Balance und Korrelationen -- die beiden Dinge, die man mit blossem Auge sieht.

    Ein Faktor mit ungleicher Verteilung der Stufen wird unpraeziser geschaetzt; zwei stark
    korrelierte Faktoren lassen sich schlechter trennen. Beides soll nahe null liegen.
    """
    names = list(WHOLE_PLOT_FACTORS) + list(SUBPLOT_FACTORS)
    matrix = np.column_stack([whole, sub])
    report: dict[str, dict[str, float]] = {}
    for index, name in enumerate(names):
        column = matrix[:, index]
        report[name] = {
            "anteil_oben": float((column > 0).mean()),
            "ungleichgewicht": float(abs(column.mean())),
        }
    worst_pair, worst_value = None, 0.0
    for i, j in combinations(range(len(names)), 2):
        correlation = abs(float(np.corrcoef(matrix[:, i], matrix[:, j])[0, 1]))
        if correlation > worst_value:
            worst_pair, worst_value = (names[i], names[j]), correlation
    report["_hoechste_korrelation"] = {
        "paar": " / ".join(worst_pair) if worst_pair else "",
        "wert": round(worst_value, 4),
    }
    return report


def to_factor_rows(whole: np.ndarray, sub: np.ndarray, spec: DesignSpec) -> list[dict]:
    """Uebersetzt den Plan in Faktorvektoren, wie `variants.py` sie erwartet."""
    rows: list[dict] = []
    for run in range(spec.n_runs):
        row: dict[str, object] = {
            name: bool(whole[run, index] > 0) for index, name in enumerate(WHOLE_PLOT_FACTORS)
        }
        row.update(
            {name: bool(sub[run, index] > 0) for index, name in enumerate(SUBPLOT_FACTORS)}
        )
        row["_whole_plot"] = run // spec.runs_per_plot
        rows.append(row)
    return rows


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--whole-plots", type=int, default=12)
    parser.add_argument("--runs-per-plot", type=int, default=4)
    parser.add_argument(
        "--variance-ratio",
        type=float,
        default=1.0,
        help="Whole-Plot- zu Subplot-Varianz. Nach dem Pilotlauf mit dem geschaetzten Wert "
        "erneut aufrufen.",
    )
    parser.add_argument("--starts", type=int, default=200)
    parser.add_argument("--seed", type=int, default=20260913)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args(argv)

    spec = DesignSpec(
        whole_plots=args.whole_plots,
        runs_per_plot=args.runs_per_plot,
        variance_ratio=args.variance_ratio,
        starts=args.starts,
        seed=args.seed,
    )
    whole, sub, score = coordinate_exchange(spec)
    rows = to_factor_rows(whole, sub, spec)
    balance = balance_report(whole, sub)

    args.out.write_text(
        json.dumps(rows, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    meta = args.out.with_suffix(".meta.json")
    meta.write_text(
        json.dumps(
            {
                "spec": spec.__dict__,
                "log_d": round(float(score), 4),
                "parameter": model_matrix(whole, sub).shape[1],
                "freiheitsgrade": spec.n_runs - model_matrix(whole, sub).shape[1],
                "balance": balance,
            },
            ensure_ascii=False,
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )

    parameters = model_matrix(whole, sub).shape[1]
    print(f"{spec.n_runs} Laeufe in {spec.whole_plots} Whole Plots zu je {spec.runs_per_plot}")
    print(f"Modell: {parameters} Parameter, {spec.n_runs - parameters} Freiheitsgrade uebrig")
    print(f"log|X'V^-1X| = {score:.4f}   (Varianzverhaeltnis d = {spec.variance_ratio})")
    print(f"hoechste Faktorkorrelation: {balance['_hoechste_korrelation']['wert']}")
    worst = max(
        (v["ungleichgewicht"] for k, v in balance.items() if not k.startswith("_")),
        default=0.0,
    )
    print(f"groesstes Ungleichgewicht:  {worst:.4f}")
    print(f"\nPlan: {args.out}\nKennzahlen: {meta}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
