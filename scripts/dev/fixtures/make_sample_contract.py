#!/usr/bin/env python3
"""Builds contracts/examples/sample-contract.pdf: a synthetic 3-page contract whose text and positions match
contracts/examples/extraction.v2.example.json, so the example result highlights the right places.

Used by the dev seed and the e2e tests. Synthetic text only, never a real contract.
Run from anywhere: python scripts/dev/fixtures/make_sample_contract.py  (needs reportlab)
"""
import json
from pathlib import Path

from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas
from reportlab.lib.utils import simpleSplit

HERE = Path(__file__).resolve().parent
EXAMPLE = HERE.parents[2] / "contracts" / "examples" / "extraction.v2.example.json"
OUT = HERE.parents[2] / "contracts" / "examples" / "sample-contract.pdf"


def main() -> None:
    example = json.loads(EXAMPLE.read_text(encoding="utf-8"))
    width, height = A4
    pages = example["document"]["page_count"]
    pdf = canvas.Canvas(str(OUT), pagesize=A4)
    pdf.setTitle("Sample Cloud Services Agreement (synthetic)")

    for page in range(1, pages + 1):
        if page == 1:
            pdf.setFont("Helvetica-Bold", 14)
            pdf.drawString(0.1 * width, height - 0.08 * height, "CLOUD SERVICES AGREEMENT (SYNTHETIC SAMPLE)")
        for clause in example["clauses"]:
            regions = [r for r in clause["location"]["regions"] if r["page"] == page]
            if not regions:
                continue
            # A clause split over two pages puts its sentences in its regions in order.
            sentences = split_for(clause, len(clause["location"]["regions"]))
            index = clause["location"]["regions"].index(regions[0])
            text = sentences[index]
            region = regions[0]
            x = region["x"] * width
            y = height - region["y"] * height
            box = region["width"] * width
            if index == 0:
                pdf.setFont("Helvetica-Bold", 10)
                y -= 11
                pdf.drawString(x, y, f'{clause["number"]}. {clause["heading"]}')
            pdf.setFont("Helvetica", 10)
            for line in simpleSplit(text, "Helvetica", 10, box):
                y -= 13
                pdf.drawString(x, y, line)
        pdf.setFont("Helvetica", 8)
        pdf.drawString(0.45 * width, 0.03 * height, f"Page {page} / {pages}")
        pdf.showPage()
    pdf.save()
    print(f"Wrote {OUT}")


def split_for(clause: dict, parts: int) -> list[str]:
    if parts == 1:
        return [clause["text"]]
    sentences = [s.strip() + "." for s in clause["text"].split(".") if s.strip()]
    return [" ".join(sentences[:-1]), sentences[-1]]


if __name__ == "__main__":
    main()
