"""
Genereert fictieve sample-PDF's voor PharmaDocs:
  - facturen (om de AI-extractie te testen, Fase 2)
  - procedures (om de RAG-assistent te voeden, Fase 4)

Dev-only hulpscript. Vereist: reportlab.
Gebruik:  python scripts/generate_samples.py
"""

from __future__ import annotations

from datetime import date
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import mm
from reportlab.platypus import (
    SimpleDocTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)

ROOT = Path(__file__).resolve().parent.parent
INVOICE_DIR = ROOT / "sample-data" / "invoices"
PROCEDURE_DIR = ROOT / "sample-data" / "procedures"

APOTHEEK = {
    "naam": "Apotheek De Wit",
    "adres": "Kerkstraat 12, 2000 Antwerpen",
    "btw": "BE 0123.456.789",
}

# --- Fictieve facturen ---------------------------------------------------

INVOICES = [
    {
        "bestand": "factuur-febelco-2026-0417.pdf",
        "leverancier": {
            "naam": "Febelco CV",
            "adres": "Industriepark-West 45, 9100 Sint-Niklaas",
            "btw": "BE 0400.111.222",
        },
        "nummer": "F2026-0417",
        "datum": date(2026, 6, 12),
        "vervaldatum": date(2026, 7, 12),
        "btw_pct": 6,
        "lijnen": [
            ("Paracetamol Teva 500mg - 100 tabl.", 24, 2.15),
            ("Ibuprofen EG 400mg - 30 tabl.", 40, 3.42),
            ("Amoxicilline Sandoz 500mg - 16 caps.", 15, 5.10),
            ("Dafalgan 1g - 8 bruistabl.", 30, 3.95),
            ("Steriele kompressen 10x10cm - 100 st.", 12, 4.80),
        ],
    },
    {
        "bestand": "factuur-cerp-25-8842.pdf",
        "leverancier": {
            "naam": "CERP Belgium NV",
            "adres": "Boomsesteenweg 690, 2610 Wilrijk",
            "btw": "BE 0435.333.444",
        },
        "nummer": "25/8842",
        "datum": date(2026, 6, 18),
        "vervaldatum": date(2026, 7, 18),
        "btw_pct": 6,
        "lijnen": [
            ("Omeprazole Mylan 20mg - 28 caps.", 20, 4.65),
            ("Zaldiar 37,5mg/325mg - 60 tabl.", 10, 9.20),
            ("Ventolin aerosol 100mcg - 200 dosis", 18, 4.35),
            ("Bepanthen zalf 30g", 25, 6.10),
        ],
    },
    {
        "bestand": "factuur-pharma-belgium-PB260073.pdf",
        "leverancier": {
            "naam": "Pharma Belgium NV",
            "adres": "Rue des Trois Arbres 16, 1180 Brussel",
            "btw": "BE 0412.555.666",
        },
        "nummer": "PB-260073",
        "datum": date(2026, 6, 25),
        "vervaldatum": date(2026, 7, 25),
        "btw_pct": 21,
        "lijnen": [
            ("Nivea handcrème 100ml", 36, 2.75),
            ("La Roche-Posay Anthelios SPF50+ 50ml", 12, 12.40),
            ("Compeed blarenpleisters - 5 st.", 20, 4.15),
            ("Digitale koortsthermometer", 8, 6.90),
        ],
    },
    {
        "bestand": "factuur-febelco-2026-0503.pdf",
        "leverancier": {
            "naam": "Febelco CV",
            "adres": "Industriepark-West 45, 9100 Sint-Niklaas",
            "btw": "BE 0400.111.222",
        },
        "nummer": "F2026-0503",
        "datum": date(2026, 7, 1),
        "vervaldatum": date(2026, 7, 31),
        "btw_pct": 6,
        "lijnen": [
            ("Metformine EG 850mg - 60 tabl.", 30, 3.05),
            ("Levothyrox 75mcg - 100 tabl.", 22, 7.80),
            ("Insuline glargine 100E/ml - pen", 14, 28.50),
            ("Bloeddrukmeter Omron bovenarm", 4, 34.90),
        ],
    },
    {
        "bestand": "factuur-multipharma-MP-2026-1189.pdf",
        "leverancier": {
            "naam": "Multipharma Distributie",
            "adres": "Zenithgebouw, Koning Albert II-laan 37, 1030 Brussel",
            "btw": "BE 0401.777.888",
        },
        "nummer": "MP-2026-1189",
        "datum": date(2026, 7, 2),
        "vervaldatum": date(2026, 8, 1),
        "btw_pct": 6,
        "lijnen": [
            ("Cetirizine Sandoz 10mg - 30 tabl.", 45, 2.60),
            ("Nasonex neusspray 50mcg", 16, 8.15),
            ("Voltaren Emulgel 100g", 22, 9.45),
            ("Imodium 2mg - 20 caps.", 18, 6.30),
            ("Sportverband elastisch 8cm", 14, 3.20),
        ],
    },
]


def build_invoice(spec: dict) -> None:
    styles = getSampleStyleSheet()
    small = ParagraphStyle("small", parent=styles["Normal"], fontSize=9, leading=12)
    right = ParagraphStyle("right", parent=small, alignment=2)
    title = ParagraphStyle("title", parent=styles["Title"], fontSize=20)

    path = INVOICE_DIR / spec["bestand"]
    doc = SimpleDocTemplate(
        str(path), pagesize=A4,
        leftMargin=20 * mm, rightMargin=20 * mm,
        topMargin=18 * mm, bottomMargin=18 * mm,
    )
    lev = spec["leverancier"]
    flow = []

    # Kop: leverancier links, "FACTUUR" rechts
    header = Table(
        [[
            Paragraph(
                f"<b>{lev['naam']}</b><br/>{lev['adres']}<br/>BTW {lev['btw']}", small
            ),
            Paragraph("<b>FACTUUR</b>", title),
        ]],
        colWidths=[100 * mm, 60 * mm],
    )
    header.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "TOP")]))
    flow += [header, Spacer(1, 8 * mm)]

    # Meta + klant
    meta = Table(
        [
            [Paragraph("<b>Factuurnummer:</b>", small), Paragraph(spec["nummer"], small),
             Paragraph("<b>Klant:</b>", small), Paragraph(APOTHEEK["naam"], small)],
            [Paragraph("<b>Factuurdatum:</b>", small),
             Paragraph(spec["datum"].strftime("%d/%m/%Y"), small),
             Paragraph("", small), Paragraph(APOTHEEK["adres"], small)],
            [Paragraph("<b>Vervaldatum:</b>", small),
             Paragraph(spec["vervaldatum"].strftime("%d/%m/%Y"), small),
             Paragraph("", small), Paragraph("BTW " + APOTHEEK["btw"], small)],
        ],
        colWidths=[35 * mm, 45 * mm, 20 * mm, 60 * mm],
    )
    flow += [meta, Spacer(1, 8 * mm)]

    # Lijnitems
    rows = [["Omschrijving", "Aantal", "Eenheidsprijs", "Totaal"]]
    subtotaal = 0.0
    for omschrijving, aantal, prijs in spec["lijnen"]:
        lijntotaal = round(aantal * prijs, 2)
        subtotaal += lijntotaal
        rows.append([
            omschrijving, str(aantal),
            f"€ {prijs:,.2f}".replace(",", " "),
            f"€ {lijntotaal:,.2f}".replace(",", " "),
        ])

    subtotaal = round(subtotaal, 2)
    btw_bedrag = round(subtotaal * spec["btw_pct"] / 100, 2)
    totaal = round(subtotaal + btw_bedrag, 2)

    table = Table(rows, colWidths=[85 * mm, 20 * mm, 30 * mm, 30 * mm])
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#0f766e")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTSIZE", (0, 0), (-1, -1), 9),
        ("ALIGN", (1, 0), (-1, -1), "RIGHT"),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#f1f5f9")]),
        ("LINEBELOW", (0, 0), (-1, 0), 0.5, colors.HexColor("#0f766e")),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    flow += [table, Spacer(1, 6 * mm)]

    # Totalen
    totals = Table(
        [
            ["Subtotaal", f"€ {subtotaal:,.2f}".replace(",", " ")],
            [f"BTW {spec['btw_pct']}%", f"€ {btw_bedrag:,.2f}".replace(",", " ")],
            ["Totaal te betalen", f"€ {totaal:,.2f}".replace(",", " ")],
        ],
        colWidths=[45 * mm, 35 * mm],
        hAlign="RIGHT",
    )
    totals.setStyle(TableStyle([
        ("FONTSIZE", (0, 0), (-1, -1), 10),
        ("ALIGN", (0, 0), (-1, -1), "RIGHT"),
        ("FONTNAME", (0, 2), (-1, 2), "Helvetica-Bold"),
        ("LINEABOVE", (0, 2), (-1, 2), 0.5, colors.black),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
    ]))
    flow += [totals, Spacer(1, 12 * mm)]

    flow.append(Paragraph(
        f"Gelieve te betalen voor {spec['vervaldatum'].strftime('%d/%m/%Y')} "
        f"met vermelding van factuurnummer {spec['nummer']}.<br/>"
        "IBAN BE68 5390 0754 7034 &#8211; BIC GKCCBEBB", small,
    ))
    doc.build(flow)
    print(f"  factuur  -> {path.relative_to(ROOT)}  (totaal € {totaal:.2f})")


# --- Fictieve procedures -------------------------------------------------

PROCEDURES = [
    {
        "bestand": "procedure-openingsuren.pdf",
        "titel": "Procedure - Openingsuren en wachtdienst",
        "alineas": [
            "Apotheek De Wit is geopend van maandag tot vrijdag van 8u30 tot 18u30, "
            "doorlopend. Op zaterdag is de apotheek open van 9u00 tot 13u00. "
            "Op zon- en feestdagen is de apotheek gesloten.",
            "Tijdens de middagpauze blijft de apotheek open; er is steeds minstens "
            "één apotheker aanwezig.",
            "Buiten de openingsuren verwijst een affiche aan de voordeur en de website "
            "naar de dichtstbijzijnde wachtapotheek. De wachtdienst kan ook geraadpleegd "
            "worden via www.apotheek.be of het nummer 0903 99 000.",
            "Bij een dringend geneesmiddel tijdens de wachtdienst is een wettelijk "
            "wachthonorarium van toepassing bovenop de prijs van het geneesmiddel.",
        ],
    },
    {
        "bestand": "procedure-terugbetaling.pdf",
        "titel": "Procedure - Terugbetaling en derdebetalersregeling",
        "alineas": [
            "De terugbetaling van geneesmiddelen verloopt via het RIZIV. De patiënt "
            "betaalt enkel het remgeld; het resterende bedrag wordt rechtstreeks met "
            "de mutualiteit afgerekend via de derdebetalersregeling.",
            "Voorwaarde voor terugbetaling is een geldig voorschrift en een gekende, "
            "in orde zijnde verzekerbaarheid van de patiënt. De verzekerbaarheid wordt "
            "elektronisch gecontroleerd via MyCareNet vóór aflevering.",
            "Voor sommige geneesmiddelen (hoofdstuk IV) is een voorafgaande toelating "
            "van de adviserend arts nodig. Zonder geldige machtiging is er geen "
            "terugbetaling en betaalt de patiënt de volledige prijs.",
            "Magistrale bereidingen worden terugbetaald volgens de officiële "
            "vergoedingstarieven. Niet-terugbetaalbare producten worden steeds "
            "volledig door de patiënt betaald.",
        ],
    },
    {
        "bestand": "procedure-bestellen.pdf",
        "titel": "Procedure - Bestellen bij groothandel",
        "alineas": [
            "Bestellingen bij de groothandel (Febelco, CERP, Pharma Belgium) gebeuren "
            "tweemaal per werkdag: een ochtendbestelling vóór 10u00 en een "
            "namiddagbestelling vóór 16u00. Leveringen volgen respectievelijk rond "
            "de middag en de late namiddag.",
            "Een geneesmiddel dat niet op voorraad is, wordt bij voorkeur besteld voor "
            "levering dezelfde dag. De patiënt wordt verwittigd zodra het product "
            "beschikbaar is.",
            "Verdovende middelen (verdovende en psychotrope stoffen) worden apart "
            "besteld en bij ontvangst geregistreerd in het verdovingsregister, met "
            "controle van de geleverde hoeveelheid tegenover de bestelbon.",
            "Bij ontvangst van een levering wordt de leveringsbon gecontroleerd tegen "
            "de bestelling: aantal, product en houdbaarheidsdatum. Afwijkingen worden "
            "genoteerd en gemeld aan de groothandel.",
        ],
    },
    {
        "bestand": "procedure-koelketen.pdf",
        "titel": "Procedure - Koelketen en bewaring",
        "alineas": [
            "Koelkastgeneesmiddelen (o.a. insuline, bepaalde vaccins) worden bewaard "
            "tussen 2°C en 8°C. De temperatuur van de koelkast wordt tweemaal per dag "
            "geregistreerd, 's ochtends bij opening en 's avonds bij sluiting.",
            "Bij een temperatuurafwijking buiten de marge 2-8°C worden de betrokken "
            "producten in quarantaine geplaatst en wordt de firma of de groothandel "
            "gecontacteerd om de bruikbaarheid te beoordelen. Er wordt niets "
            "afgeleverd zolang de bruikbaarheid niet bevestigd is.",
            "Bij aflevering van een koelkastgeneesmiddel krijgt de patiënt de "
            "instructie om het product onmiddellijk en gekoeld te vervoeren en thuis "
            "opnieuw in de koelkast te plaatsen.",
            "De koelkast wordt uitsluitend voor geneesmiddelen gebruikt en is voorzien "
            "van een geijkte thermometer met min/max-registratie.",
        ],
    },
]


def build_procedure(spec: dict) -> None:
    styles = getSampleStyleSheet()
    body = ParagraphStyle("body", parent=styles["Normal"], fontSize=11, leading=16, spaceAfter=8)
    h = ParagraphStyle("h", parent=styles["Title"], fontSize=17, spaceAfter=14)
    meta = ParagraphStyle("meta", parent=styles["Normal"], fontSize=9,
                          textColor=colors.grey, spaceAfter=16)

    path = PROCEDURE_DIR / spec["bestand"]
    doc = SimpleDocTemplate(
        str(path), pagesize=A4,
        leftMargin=22 * mm, rightMargin=22 * mm, topMargin=20 * mm, bottomMargin=20 * mm,
    )
    flow = [
        Paragraph(spec["titel"], h),
        Paragraph(f"{APOTHEEK['naam']} &#8211; interne procedure &#8211; versie 2026", meta),
    ]
    for alinea in spec["alineas"]:
        flow.append(Paragraph(alinea, body))
    doc.build(flow)
    print(f"  procedure -> {path.relative_to(ROOT)}")


def main() -> None:
    INVOICE_DIR.mkdir(parents=True, exist_ok=True)
    PROCEDURE_DIR.mkdir(parents=True, exist_ok=True)
    print("Facturen genereren:")
    for spec in INVOICES:
        build_invoice(spec)
    print("Procedures genereren:")
    for spec in PROCEDURES:
        build_procedure(spec)
    print(f"\nKlaar: {len(INVOICES)} facturen + {len(PROCEDURES)} procedures.")


if __name__ == "__main__":
    main()
