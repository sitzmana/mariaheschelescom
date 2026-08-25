"""
Computes the CSP sha256 hash of the inline import map in index.html and checks it against
staticwebapp.config.json.

An import map has to be inline, so the Content Security Policy has to allow it by hash. That
hash is easy to invalidate by accident: bumping the three.js version changes the script body,
and the only symptom is that 3D silently stops working in production while everything looks
fine locally, because the CSP is applied by Static Web Apps and not by the dev server.

Run with --check (as CI does) to fail on drift, or with no arguments to print the current hash.
"""

import argparse
import base64
import hashlib
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
INDEX = ROOT / "src" / "MariaHescheles.Web" / "wwwroot" / "index.html"
CONFIG = ROOT / "src" / "MariaHescheles.Web" / "wwwroot" / "staticwebapp.config.json"


def import_map_hash() -> str:
    html = INDEX.read_text(encoding="utf-8")
    match = re.search(r'<script type="importmap">(.*?)</script>', html, re.S)
    if match is None:
        sys.exit(f"No inline import map found in {INDEX}")

    # The hash covers the element's exact text content, whitespace included.
    digest = hashlib.sha256(match.group(1).encode("utf-8")).digest()
    return "sha256-" + base64.b64encode(digest).decode()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="exit non-zero if the CSP is stale")
    args = parser.parse_args()

    expected = import_map_hash()

    if not args.check:
        print(expected)
        return 0

    config = CONFIG.read_text(encoding="utf-8")
    if expected in config:
        print(f"CSP import map hash is current: {expected}")
        return 0

    print(
        f"::error::The Content Security Policy in {CONFIG.name} does not contain the current "
        f"import map hash. Expected {expected}. Update the script-src directive, or run "
        f"'python tools/csp_hash.py' to print it."
    )
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
