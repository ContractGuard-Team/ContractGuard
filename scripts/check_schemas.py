#!/usr/bin/env python3
"""Check syntax/meta-schema of available JSON Schemas; not contract semantic validity."""
import json
import os
from pathlib import Path
import sys
from jsonschema.validators import validator_for

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / "contracts" / "extraction.schema.json",
    ROOT / "contracts" / "job-result.schema.json",
    ROOT / "security" / "schemas" / "security-findings.schema.json",
]

failed = False
for path in FILES:
    if not path.exists():
        print(f"SKIP (missing, if not implemented yet): {path.relative_to(ROOT)}")
        continue
    try:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        validator_for(data).check_schema(data)
        print(f"PASS schema: {path.relative_to(ROOT)}")
    except Exception as exc:
        failed = True
        print(f"FAIL schema: {path.relative_to(ROOT)}: {exc}")
sys.exit(1 if failed else 0)
