"""Tests voor de zinsbewuste chunking (embeddings.chunk_text)."""

from __future__ import annotations

from app.embeddings import chunk_text


def test_lege_tekst_geeft_geen_chunks():
    assert chunk_text("") == []
    assert chunk_text("   \n\t  ") == []


def test_korte_tekst_blijft_een_chunk():
    tekst = "Openingsuren: elke werkdag van 9 tot 18 uur."
    chunks = chunk_text(tekst, target_chars=600)
    assert chunks == [tekst]


def test_witruimte_en_newlines_worden_genormaliseerd():
    # PDF's breken regels af met newlines; die mogen niet in de chunk-inhoud lekken.
    tekst = "Regel een.\n\n  Regel   twee.\tRegel drie."
    chunks = chunk_text(tekst, target_chars=600)
    assert len(chunks) == 1
    assert "\n" not in chunks[0]
    assert "  " not in chunks[0]


def test_lange_tekst_wordt_gesplitst_met_overlap():
    # Tien duidelijke zinnen; een kleine target dwingt meerdere chunks af.
    zinnen = [f"Dit is zin nummer {i} met wat vulling erbij." for i in range(10)]
    tekst = " ".join(zinnen)

    chunks = chunk_text(tekst, target_chars=80, overlap_sentences=1)

    assert len(chunks) > 1
    # Overlap: de laatste zin van een chunk keert terug als eerste van de volgende.
    for eerste, tweede in zip(chunks, chunks[1:]):
        laatste_zin = eerste.split(". ")[-1].rstrip(".")
        assert laatste_zin in tweede


def test_geen_overlap_wanneer_uitgezet():
    zinnen = [f"Korte zin {i}." for i in range(8)]
    tekst = " ".join(zinnen)

    chunks = chunk_text(tekst, target_chars=30, overlap_sentences=0)

    # Zonder overlap komt elke zin exact één keer voor over alle chunks samen.
    samengevoegd = " ".join(chunks)
    for zin in zinnen:
        assert samengevoegd.count(zin) == 1
