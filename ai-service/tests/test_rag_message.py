"""Tests voor de opbouw van het RAG-gebruikersbericht (rag._build_user_message)."""

from __future__ import annotations

from app.rag import _build_user_message, _system_prompt


def test_met_fragmenten_bevat_vraag_en_bronnen():
    contexts = [
        {"sourceName": "openingsuren.pdf", "content": "Wij openen om 9u."},
        {"sourceName": "koelketen.pdf", "content": "Bewaar tussen 2 en 8 graden."},
    ]

    bericht = _build_user_message("Wanneer openen jullie?", contexts)

    assert "Wanneer openen jullie?" in bericht
    # Elke bron staat als [Bron: ...] gemarkeerd, met zijn inhoud.
    assert "[Bron: openingsuren.pdf]" in bericht
    assert "Wij openen om 9u." in bericht
    assert "[Bron: koelketen.pdf]" in bericht


def test_zonder_fragmenten_zegt_expliciet_niets_gevonden():
    bericht = _build_user_message("Bestaat er een fietsvergoeding?", [])

    assert "Bestaat er een fietsvergoeding?" in bericht
    assert "geen relevante fragmenten" in bericht.lower()


def test_systeemprompt_vult_de_apotheeknaam_in():
    # MT6: de placeholder wordt vervangen door de naam van de eigen apotheek.
    prompt = _system_prompt("Apotheek Zonnebloem")
    assert "Apotheek Zonnebloem" in prompt
    assert "{{organisatie}}" not in prompt


def test_systeemprompt_valt_terug_op_neutrale_naam():
    for leeg in (None, "", "   "):
        prompt = _system_prompt(leeg)
        assert "de apotheek" in prompt
        assert "{{organisatie}}" not in prompt
