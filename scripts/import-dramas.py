#!/usr/bin/env python3
"""Import drama discs from a legacy client download cache into a running aisp-emu.

The cache folder is user/<uid>/<slot>/dl/drama/ as the original client left it: a UTF-16 list.csv with the
columns 名称,取得時間,使用時間,コンテンツ,ID,ロック,公式配信,お気に入り,使用枚数 and one ai<ID>.txt pack per
row. The pack holds only the script and the actor table, so author, blurb, genre, price and the original
posting date come from a UTF-8 sidecar CSV with a header row and these optional columns, keyed by id:

    id,title,author,genre,comment,price,date,public,official

Rows missing from the sidecar get the command-line defaults. genre is 0-9 or the Japanese tab name; rows
flagged 公式配信 default to 1 (オフィシャル). date is ISO 8601 or Unix seconds. public (1/0) is the upload
dialog's 公開する, whether buyers may open the manuscript in the editor; official (1/0) files the disc under
the PC library's 公式配信 tab. Both default to 1 for imports (--no-public / --no-official flip that). Every disc is registered under its original id, on sale, owned by
the account named with --owner (required; the shop shows the author column, the account only collects the
sales). --replace refreshes discs that already exist under their id, keeping purchases and counters.

Examples:
    python3 scripts/import-dramas.py --dir ~/legacy/dl/drama --owner official --author "aisp-emu" --dry-run
    python3 scripts/import-dramas.py --dir ~/legacy/dl/drama --owner official --meta discs.csv \\
        --url http://127.0.0.1:8080 --api-key "$API_KEY"
"""

from __future__ import annotations

import argparse
import csv
import io
import json
import os
import sys
import urllib.error
import urllib.request
import uuid
from pathlib import Path

LIST_COLUMNS = ["名称", "取得時間", "使用時間", "コンテンツ", "ID", "ロック", "公式配信", "お気に入り", "使用枚数"]


def read_list_csv(path: Path) -> list[dict[str, str]]:
    raw = path.read_bytes()
    text = raw.decode("utf-16") if raw.startswith(b"\xff\xfe") else raw.decode("utf-8-sig")
    rows = []
    for line in text.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        cells = line.split(",")
        rows.append({name: (cells[i].strip() if i < len(cells) else "") for i, name in enumerate(LIST_COLUMNS)})
    return rows


def read_sidecar(path: Path | None) -> dict[str, dict[str, str]]:
    if path is None:
        return {}
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return {row["id"].strip(): row for row in csv.DictReader(handle) if row.get("id", "").strip()}


def multipart(fields: dict[str, str], file_name: str, file_bytes: bytes) -> tuple[bytes, str]:
    boundary = "----aisp" + uuid.uuid4().hex
    out = io.BytesIO()
    for name, value in fields.items():
        out.write(f"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n".encode())
        out.write(value.encode("utf-8"))
        out.write(b"\r\n")
    out.write(
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"pack\"; filename=\"{file_name}\"\r\n"
        f"Content-Type: application/octet-stream\r\n\r\n".encode()
    )
    out.write(file_bytes)
    out.write(f"\r\n--{boundary}--\r\n".encode())
    return out.getvalue(), f"multipart/form-data; boundary={boundary}"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dir", required=True, type=Path, help="legacy dl/drama folder with list.csv and ai*.txt")
    parser.add_argument("--owner", required=True, help="account username that owns the imported listings")
    parser.add_argument("--meta", type=Path, help="UTF-8 sidecar CSV: id,title,author,genre,comment,price,date")
    parser.add_argument("--author", default="", help="default author name")
    parser.add_argument("--genre", default="", help="default genre (0-9 or name); 公式配信 rows default to 1")
    parser.add_argument("--comment", default="", help="default blurb")
    parser.add_argument("--price", default="0", help="default price in デレ")
    parser.add_argument("--public", action=argparse.BooleanOptionalAction, default=True, help="default for the public column (公開する)")
    parser.add_argument("--official", action=argparse.BooleanOptionalAction, default=True, help="default for the official column (公式配信)")
    parser.add_argument("--replace", action="store_true", help="update discs that already exist under their id (content refresh); purchases and counters are kept")
    parser.add_argument("--only", default="", help="comma-separated ids to import")
    parser.add_argument("--url", default=os.environ.get("URL", "http://127.0.0.1:8080"))
    parser.add_argument("--api-key", default=os.environ.get("API_KEY", ""))
    parser.add_argument("--dry-run", action="store_true", help="print what would be sent and stop")
    args = parser.parse_args()

    if not args.dry_run and not args.api_key:
        print("error: --api-key (or API_KEY) is required", file=sys.stderr)
        return 2

    rows = read_list_csv(args.dir / "list.csv")
    meta = read_sidecar(args.meta)
    only = {x.strip() for x in args.only.split(",") if x.strip()}
    failures = 0
    for row in rows:
        script_id = row["ID"]
        if row["コンテンツ"] != "ai" or (only and script_id not in only):
            continue
        pack = args.dir / f"ai{script_id}.txt"
        if not pack.is_file():
            print(f"{script_id}: missing {pack.name}, skipped")
            failures += 1
            continue
        side = meta.get(script_id, {})
        official = row["公式配信"] == "1"
        fields = {
            "owner": args.owner,
            "title": side.get("title") or row["名称"],
            "author": side.get("author") or args.author,
            "genre": side.get("genre") or args.genre or ("1" if official else "0"),
            "comment": side.get("comment") or args.comment,
            "price": side.get("price") or args.price,
            "date": side.get("date") or row["取得時間"],
            "public": side.get("public") or ("1" if args.public else "0"),
            "official": side.get("official") or ("1" if args.official else "0"),
            "replace": "1" if args.replace else "0",
        }
        if args.dry_run:
            print(f"{script_id}: {json.dumps(fields, ensure_ascii=False)} ({pack.stat().st_size} bytes)")
            continue
        body, content_type = multipart(fields, pack.name, pack.read_bytes())
        request = urllib.request.Request(
            f"{args.url.rstrip('/')}/api/adventure/listings/{script_id}",
            data=body,
            method="POST",
            headers={"Content-Type": content_type, "X-Api-Key": args.api_key},
        )
        try:
            with urllib.request.urlopen(request) as response:
                print(f"{script_id}: {response.status} {response.read().decode('utf-8')}")
        except urllib.error.HTTPError as error:
            failures += 1
            print(f"{script_id}: {error.code} {error.read().decode('utf-8', 'replace')}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
