"""Tests fuer die Schaetzer aus `pilot_analyse.py`.

Diese Tests sind nicht Beiwerk. Die gesamte Budgetaufteilung des Hauptlaufs und die
Entscheidung ueber Prediction-Powered Inference haengen an diesen Zahlen -- ein stiller
Rechenfehler hier waere teurer als jeder Prompt-Fehler.

Geprueft wird deshalb gegen *bekannte* Werte: es werden Daten mit vorgegebener
Varianzstruktur erzeugt und nachgesehen, ob die Schaetzer sie wiederfinden. Genau diese Art
Pruefung fehlte im Vorversuch dem Sprachpruefer, der daraufhin elf Fehlalarme meldete.
"""

from __future__ import annotations

import importlib.util
import math
from pathlib import Path

import numpy as np
import pytest

_spec = importlib.util.spec_from_file_location(
    "pilot_analyse", Path(__file__).parent / "pilot_analyse.py"
)
assert _spec and _spec.loader
pa = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(pa)


def _grouped(tau2: float, sigma2: float, groups: int, per_group: int, seed: int) -> list[list[float]]:
    """Daten mit vorgegebener Zwischen- und Innerhalb-Streuung."""
    rng = np.random.default_rng(seed)
    return [
        list(rng.normal(rng.normal(0.0, math.sqrt(tau2)), math.sqrt(sigma2), size=per_group))
        for _ in range(groups)
    ]


@pytest.mark.parametrize(
    ("tau2", "sigma2"),
    [(0.25, 0.25), (0.04, 0.36), (0.50, 0.05), (0.00, 0.30)],
)
def test_varianzkomponenten_finden_wahre_werte(tau2: float, sigma2: float) -> None:
    estimate = pa.variance_components(_grouped(tau2, sigma2, groups=400, per_group=3, seed=7))
    assert estimate["tau2"] == pytest.approx(tau2, abs=0.05)
    assert estimate["sigma2"] == pytest.approx(sigma2, abs=0.05)


def test_varianzkomponenten_werden_nicht_negativ() -> None:
    """Ohne echte Zwischen-Streuung kann die Schaetzung rechnerisch unter null rutschen.

    Eine negative Varianz ist sinnlos und wuerde die Budgetformel zerlegen, deshalb wird sie
    bei null abgeschnitten.
    """
    estimate = pa.variance_components(_grouped(0.0, 1.0, groups=30, per_group=3, seed=11))
    assert estimate["tau2"] >= 0.0


def test_varianzkomponenten_bei_zu_wenig_daten() -> None:
    assert math.isnan(pa.variance_components([[1.0, 2.0]])["tau2"])


@pytest.mark.parametrize("wahr", [0.95, 0.70, 0.30])
def test_icc_findet_wahren_wert(wahr: float) -> None:
    error_var = 1.0
    object_var = wahr * error_var / (1 - wahr)
    rng = np.random.default_rng(3)
    matrix = np.array(
        [
            rng.normal(rng.normal(0.0, math.sqrt(object_var)), math.sqrt(error_var), size=3)
            for _ in range(400)
        ]
    )
    assert pa.icc_3_1(matrix) == pytest.approx(wahr, abs=0.07)


def test_icc_bei_perfekter_uebereinstimmung() -> None:
    matrix = np.array([[1.0, 1.0], [3.0, 3.0], [5.0, 5.0], [2.0, 2.0]])
    assert pa.icc_3_1(matrix) == pytest.approx(1.0, abs=1e-9)


def test_kappa_paradox_wird_sichtbar() -> None:
    """Bei schiefer Randverteilung faellt Kappa deutlich unter AC2.

    Das ist der dokumentierte Fall, der eine gelungene Kalibrierung als gescheitert ausweisen
    wuerde, wenn man Kappa als Kriterium nimmt.
    """
    a = [5] * 93 + [4] * 4 + [3] * 3
    b = [5] * 93 + [3] * 4 + [4] * 3
    result = pa.gwet_ac2(a, b)
    assert result["exakt"] == pytest.approx(0.93, abs=0.01)
    assert result["ac2"] > result["kappa_gewichtet"] + 0.15


def test_ac2_bei_perfekter_uebereinstimmung() -> None:
    values = [1, 2, 3, 4, 5] * 6
    assert pa.gwet_ac2(values, values)["ac2"] == pytest.approx(1.0, abs=1e-9)


def test_ac2_bestraft_systematische_abweichung() -> None:
    """Ein durchgehend um zwei Stufen milderer Bewerter darf nicht gut abschneiden."""
    human = [1, 2, 3, 1, 2, 3] * 5
    lenient = [3, 4, 5, 3, 4, 5] * 5
    assert pa.gwet_ac2(human, lenient)["ac2"] < 0.5


def test_ppi_schwelle_entspricht_der_quelle() -> None:
    """1/sqrt(n-2). Bei 30 menschlichen Noten sind das die zitierten 0,19."""
    assert pa.ppi_threshold(30) == pytest.approx(0.189, abs=0.001)
    assert pa.ppi_threshold(2) == math.inf


def test_korrelation_erkennt_monotonen_zusammenhang() -> None:
    a = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0]
    b = [1.0, 4.0, 9.0, 16.0, 25.0, 36.0]
    result = pa.correlation(a, b)
    assert result["spearman"] == pytest.approx(1.0, abs=1e-9)
    assert result["pearson"] < result["spearman"]


def test_mean_scores_mittelt_richterwiederholungen_und_ignoriert_fehler() -> None:
    row = {
        "ratings": [
            {"scores": {"treue": 5, "vollstaendigkeit": 4, "klarheit": 5}},
            {"scores": {"treue": 3, "vollstaendigkeit": 4, "klarheit": 5}},
            {"error": "unlesbar"},
        ]
    }
    means = pa.mean_scores(row)
    assert means is not None
    assert means["treue"] == pytest.approx(4.0)


def test_mean_scores_ohne_gueltige_bewertung() -> None:
    assert pa.mean_scores({"ratings": [{"error": "kaputt"}]}) is None


def test_ppi_urteil_verlangt_mehr_als_die_schwelle() -> None:
    """Eine Korrelation knapp ueber der Schwelle reicht nicht.

    Bei n = 30 liegt die Schwelle bei 0,19. Eine gemessene Korrelation von 0,20 ist von null
    nicht unterscheidbar -- im Test mit rein zufaelligen Noten hat genau das "bestanden"
    ergeben. Verlangt wird deshalb die untere Intervallgrenze, nicht der Punktschaetzer.
    """
    knapp, _ = pa.ppi_verdict(0.20, 30)
    assert knapp.startswith("unsicher")

    klar, (low, _) = pa.ppi_verdict(0.75, 30)
    assert klar == "PPI traegt"
    assert low > pa.ppi_threshold(30)

    schlecht, _ = pa.ppi_verdict(0.05, 30)
    assert "SCHADET" in schlecht


def test_konfidenzintervall_wird_mit_n_enger() -> None:
    schmal = pa.correlation_interval(0.6, 300)
    breit = pa.correlation_interval(0.6, 20)
    assert (schmal[1] - schmal[0]) < (breit[1] - breit[0])
