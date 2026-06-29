#!/usr/bin/env python3
"""
AISpace Archive Tool — extract, list, pack, and bulk-extract .hed/.dat game archives.

Usage:
  aisp-archive list     <file.hed>                  List files in an archive
  aisp-archive extract  <file.hed> <out_dir>        Extract one archive
  aisp-archive extract-all <data_dir> <out_dir>     Bulk extract all .hed files
  aisp-archive pack     <in_dir> <out.hed>          Pack directory into .hed+.dat

All commands accept --exe for key derivation, --data-dir for .dat path resolution.
"""

import argparse
import os
import struct
import sys
import tarfile
import time
from io import BytesIO
from pathlib import Path

# ═══════════════════════════════════════════════════════════════════════════
# 1. Cryptography & Keys
# ═══════════════════════════════════════════════════════════════════════════

KNOWN_KEYS: dict[str, bytes] = {
    "16F6": bytes.fromhex("0BF616B76D6121407576152788F159AB88E7F1DA0F8B506AB1BD24B073C604FC"
                           "4309B3CDEBC7B166968AC013C8A056D065554F0AB2699C973106128A0FBF0F40"),
    "84D4": bytes.fromhex("5FD484E0C15C0C3DED9BF6087936013D3440783ACEB100A8E20879B8758B1018"
                           "0EA0D9D54D8F6058D1AE9A34EFD6A0E3E615047CA5AECE60D44EFF1D3C563CFA"),
    "9495": bytes.fromhex("779594CCB11842190CC2733ACA0B036853A589D31B2501414EFB833DFCBF653C"
                           "E63BCEAC30381EA25767DC0262C65E0FBD5A942DF4DD08958749388C86CDA06A"),
    "1D7A": bytes.fromhex("767A1DEF2FCF62C05CCC641CE80F6D5B5238D7EA0F6F0520AA7F0AED04320523"
                           "87899C001CC7507BA4C5268AA963772859DECD82199198578560D89EA7B63B47"),
    "4EF3": bytes.fromhex("A1F34E06C6C06133568D724A00768CEAA10E802730047C04CE908CE29FEE76A9"
                           "21A4A3F235A5C1770B2899288205ABBC1DAB3DFF644AC344894E45B6CC16C2DD"),
}


def derive_key_from_exe(exe_path: str) -> bytes | None:
    """Derive the 64-byte scramble key from the game EXE's .text section.

    Parse PE header, find the first
    IMAGE_SCN_CNT_CODE section, and hash its raw bytes into a 64-byte key via
    rolling additive accumulation wrapping at index 63.
    """
    try:
        with open(exe_path, "rb") as f:
            f.seek(0x3C)
            pe_offset = struct.unpack("<I", f.read(4))[0]
            f.seek(pe_offset)
            if f.read(4) != b"PE\x00\x00":
                return None
            coff = f.read(20)
            num_sections = struct.unpack("<H", coff[2:4])[0]
            opt_size = struct.unpack("<H", coff[16:18])[0]
            f.seek(pe_offset + 24 + opt_size)

            section_data = None
            for _ in range(num_sections):
                f.read(8)                              # name
                vsize, vaddr, raw_size, raw_offset = struct.unpack("<IIII", f.read(16))
                f.read(8)                              # relocations + line numbers
                chars = struct.unpack("<I", f.read(4))[0]
                if (chars & 0x20) and raw_size > 0:    # IMAGE_SCN_CNT_CODE
                    saved = f.tell()
                    f.seek(raw_offset)
                    section_data = f.read(raw_size)
                    f.seek(saved)
                    break

            if section_data is None:
                return None

        key = bytearray(64)
        v7 = 0
        for idx, b in enumerate(section_data):
            key[v7] = (idx + b + key[v7]) & 0xFF
            v7 = (v7 + 1) & 0x3F
        return bytes(key)
    except OSError:
        return None


def crypt(data: bytes, key: bytes, *, decrypt: bool = False) -> bytes:
    """Scramble / unscramble — matches CScrambleAddKey::scramble.

    The client uses ADDITION to encrypt (scramble) and SUBTRACTION to decrypt.
    Pass ``decrypt=True`` for decryption (subtract), default is encrypt (add).
    """
    if not key:
        return data
    kl = len(key)
    out = bytearray(len(data))
    for i in range(len(data)):
        if decrypt:
            out[i] = (data[i] - key[i % kl]) & 0xFF
        else:
            out[i] = (data[i] + key[i % kl]) & 0xFF
    return bytes(out)


def _key_id_from_body(raw: bytes) -> str:
    """Heuristic KeyId from encrypted body bytes 1-2 (reversed, uppercase hex)."""
    return raw[1:3][::-1].hex().upper()


# ═══════════════════════════════════════════════════════════════════════════
# 2. .hed parsing (pure — no .dat I/O)
# ═══════════════════════════════════════════════════════════════════════════

FileEntry = tuple[str, str, int, int, int]   # (folder, name, pack_num, offset, size)


def parse_hed(path: str | Path, data_dir: str | None = None) \
        -> tuple[str, bytes | None, list[FileEntry]] | None:
    """Decrypt a .hed file and return (base_name, dat_key, entries) or None."""
    raw = Path(path).read_bytes()
    if raw[:4] not in (b"FPMF", b"FMPF"):
        return None

    encrypted_body = raw[16:]
    key_id = _key_id_from_body(encrypted_body)
    hed_key = KNOWN_KEYS.get(key_id)

    if hed_key is None:
        for candidate in KNOWN_KEYS.values():
            try:
                buf = BytesIO(crypt(encrypted_body, candidate, decrypt=True))
                ctype = struct.unpack("<I", buf.read(4))[0]
                if ctype in (1, 2, 3):
                    hed_key = candidate
                    break
            except (ValueError, struct.error):
                continue
        if hed_key is None:
            return None

    buf = BytesIO(crypt(encrypted_body, hed_key, decrypt=True))
    base_name = ""
    dat_key: bytes | None = None
    entries: list[FileEntry] = []

    # Read chunked body
    while True:
        header = buf.read(8)
        if len(header) < 8:
            break
        ctype, csize = struct.unpack("<II", header)
        chunk = buf.read(csize)

        if ctype == 1:          # BaseName
            chars = chunk[0]
            base_name = chunk[1 : 1 + chars * 2].decode("utf-16-le")

        elif ctype == 2:        # DatKey
            flags = struct.unpack("<I", chunk[:4])[0]
            dat_key = chunk[4:] if len(chunk) > 4 else None
            if not dat_key:
                dat_key = None

        elif ctype == 3:        # File entries
            cb = BytesIO(chunk)
            entry_count = struct.unpack("<I", cb.read(4))[0]
            for _ in range(entry_count):
                try:
                    fl = cb.read(1)[0] * 2; folder = cb.read(fl).decode("utf-16-le")
                    nl = cb.read(1)[0] * 2; name   = cb.read(nl).decode("utf-16-le")
                    pn = struct.unpack("<I", cb.read(4))[0]
                    off = struct.unpack("<I", cb.read(4))[0]
                    sz  = struct.unpack("<I", cb.read(4))[0]
                    cb.read(8)       # m_Time
                    entries.append((folder, name, pn, off, sz))
                except (struct.error, UnicodeDecodeError, IndexError):
                    break

    return base_name, dat_key, entries


def _resolve_dat(base_name: str, pack_num: int, data_dir: str) -> Path | None:
    """Resolve a .dat file from base_name + pack_num relative to data_dir."""
    name = base_name.replace("%04x", f"{pack_num:04x}").lstrip("./")
    path = Path(data_dir) / name.replace("/", os.sep)
    return path if path.is_file() else None


# ═══════════════════════════════════════════════════════════════════════════
# 3. Single‑archive extract
# ═══════════════════════════════════════════════════════════════════════════

def extract_archive(hed_path: str, output_dir: str,
                    data_dir: str | None = None) -> int:
    """Extract one .hed archive to *output_dir*.  Returns number of files written."""
    parsed = parse_hed(hed_path, data_dir)
    if parsed is None:
        print(f"  Failed to parse {hed_path}")
        return 0

    base_name, dat_key, entries = parsed
    dat_dir = data_dir or os.path.dirname(os.path.abspath(hed_path)) or "."

    dat_cache: dict[Path, bytes] = {}
    count = 0
    for folder, name, pack_num, offset, size in entries:
        dat_path = _resolve_dat(base_name, pack_num, dat_dir)
        if dat_path is None:
            continue

        if dat_path not in dat_cache:
            dat_cache[dat_path] = dat_path.read_bytes()

        data = dat_cache[dat_path][offset : offset + size]
        if dat_key is not None:
            data = crypt(data, dat_key, decrypt=True)

        rel = os.path.join(folder.strip("./\\"), name.strip("./\\")).replace("\\", "/")
        out = Path(output_dir) / rel
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(data)
        count += 1

    return count


# ═══════════════════════════════════════════════════════════════════════════
# 4. List
# ═══════════════════════════════════════════════════════════════════════════

def _fmt(n: int) -> str:
    if n >= 1024 * 1024:
        return f"{n / (1024*1024):.1f} MB"
    if n >= 1024:
        return f"{n / 1024:.1f} KB"
    return f"{n} bytes"


def list_archive(hed_path: str, data_dir: str | None = None, *, header_only: bool = False) -> None:
    """Print a table of files in a .hed archive (or just header info)."""
    raw = Path(hed_path).read_bytes()
    if raw[:4] not in (b"FPMF", b"FMPF"):
        print(f"Invalid magic: {raw[:4]!r}")
        return

    version   = struct.unpack("<I", raw[4:8])[0]
    data_size = struct.unpack("<I", raw[8:12])[0]
    hdr_count = struct.unpack("<I", raw[12:16])[0]

    print(f"Magic:      {raw[:4].decode()}")
    print(f"Version:    {version}  (0x{version:08x})")
    print(f"DataSize:   {data_size}  ({_fmt(data_size)})")
    print(f"FileCount:  {hdr_count}")

    encrypted_body = raw[16:]
    key_id = _key_id_from_body(encrypted_body)
    print(f"KeyId:      {key_id}")

    parsed = parse_hed(hed_path, data_dir)
    if parsed is None:
        print("Body:       FAILED to decrypt/parse")
        return

    base_name, dat_key, entries = parsed
    print(f"BaseName:   {base_name!r}")
    print(f"DatKey:     {len(dat_key) if dat_key else 0} bytes")
    print(f"Entries:    {len(entries)}")

    # Show chunk details
    hed_key = KNOWN_KEYS.get(key_id)
    if hed_key:
        body = crypt(encrypted_body, hed_key, decrypt=True)
        buf = BytesIO(body)
        chunk_idx = 0
        while True:
            hdr = buf.read(8)
            if len(hdr) < 8:
                break
            ctype, csize = struct.unpack("<II", hdr)
            chunk = buf.read(csize)
            label = {1: "BaseName", 2: "DatKey", 3: "Entries"}.get(ctype, f"type={ctype}")
            print(f"  Chunk {chunk_idx}: {label}  ({_fmt(csize)})")
            chunk_idx += 1

    if header_only:
        return

    total_size = 0
    print(f"\n{'Size':>10}  Path")
    print(f"{'─'*10}  {'─'*60}")
    for folder, name, pack_num, offset, size in entries:
        rel = os.path.join(folder.strip("./\\"), name.strip("./\\")).replace("\\", "/")
        total_size += size
        kb = size / 1024
        s = f"{kb/1024:7.1f} MB" if kb >= 1024 else f"{kb:7.1f} KB"
        print(f"{s:>10}  {rel}")
    print(f"{'─'*10}  {'─'*60}")
    total_kb = total_size / 1024
    s = f"{total_kb/1024:.1f} MB" if total_kb >= 1024 else f"{total_kb:.1f} KB"
    print(f"  {s}  ({len(entries)} files)")


# ═══════════════════════════════════════════════════════════════════════════
# 5. Bulk extract
# ═══════════════════════════════════════════════════════════════════════════

def bulk_extract(data_root: str, output_root: str) -> None:
    """Extract ALL .hed archives under *data_root* into *output_root*.

    Skips files that already exist.  Caches .dat files lazily per archive.
    """
    root = Path(data_root)
    out  = Path(output_root)
    hed_files = sorted(root.rglob("*.hed"))

    total_extracted = 0
    total_skipped   = 0
    total_mb        = 0.0
    t0 = time.time()

    for idx, hed_path in enumerate(hed_files):
        parsed = parse_hed(hed_path, data_root)
        if parsed is None:
            continue

        base_name, dat_key, entries = parsed
        dat_cache: dict[Path, bytes] = {}
        extracted = skipped = written = 0

        for folder, name, pack_num, offset, size in entries:
            rel = os.path.join(folder.strip("./\\"), name.strip("./\\")).replace("\\", "/")
            out_path = out / rel
            if out_path.exists():
                skipped += 1
                continue

            dat_path = _resolve_dat(base_name, pack_num, data_root)
            if dat_path is None:
                continue

            if dat_path not in dat_cache:
                dat_cache[dat_path] = dat_path.read_bytes()

            data = dat_cache[dat_path][offset : offset + size]
            if dat_key is not None:
                data = crypt(data, dat_key, decrypt=True)

            out_path.parent.mkdir(parents=True, exist_ok=True)
            out_path.write_bytes(data)
            extracted += 1
            written   += size

        total_extracted += extracted
        total_skipped   += skipped
        total_mb        += written / (1024 * 1024)

        elapsed = time.time() - t0
        hed_rel = str(hed_path.relative_to(root))
        if extracted:
            print(f"[{idx+1:3d}/{len(hed_files)}] {elapsed:5.0f}s  {hed_rel:55s} "
                  f"{extracted:4d} new  {skipped:4d} ok  ({written/1024:6.0f} KB)")

    elapsed = time.time() - t0
    print(f"\nDone in {elapsed:.0f}s — "
          f"{total_extracted} new, {total_skipped} already present "
          f"({total_mb:.1f} MB written)")


# ═══════════════════════════════════════════════════════════════════════════
# 6. Pack (directory → .hed + .dat)
# ═══════════════════════════════════════════════════════════════════════════

def _filetime_from_path(path: str) -> int:
    """Convert file mtime to Windows FILETIME (100ns intervals since 1601-01-01)."""
    epoch_offset = 11644473600  # seconds between 1601-01-01 and 1970-01-01
    mtime = os.path.getmtime(path)
    return int((mtime + epoch_offset) * 10_000_000)


def _parse_hed_entries_with_time(hed_path: str, *,
                                 key_id: str = "9495",
                                 exe_path: str | None = None) -> list[tuple[str, str, int, int, int, int]]:
    """Parse a .hed file and return entries including m_Time field.
    Returns list of (folder, name, pack_num, offset, size, m_time)."""
    raw = Path(hed_path).read_bytes()
    if raw[:4] != b"FPMF":
        return []
    encrypted_body = raw[16:]

    hed_key = derive_key_from_exe(exe_path) if exe_path else None
    if not hed_key:
        hed_key = KNOWN_KEYS.get(key_id.upper())
    if not hed_key:
        return []

    buf = BytesIO(crypt(encrypted_body, hed_key, decrypt=True))
    entries: list[tuple[str, str, int, int, int, int]] = []

    while True:
        header = buf.read(8)
        if len(header) < 8:
            break
        ctype, csize = struct.unpack("<II", header)
        chunk = buf.read(csize)

        if ctype == 3:
            cb = BytesIO(chunk)
            entry_count = struct.unpack("<I", cb.read(4))[0]
            for _ in range(entry_count):
                try:
                    fl = cb.read(1)[0] * 2; folder = cb.read(fl).decode("utf-16-le")
                    nl = cb.read(1)[0] * 2; name = cb.read(nl).decode("utf-16-le")
                    pn = struct.unpack("<I", cb.read(4))[0]
                    off = struct.unpack("<I", cb.read(4))[0]
                    sz = struct.unpack("<I", cb.read(4))[0]
                    mt = struct.unpack("<Q", cb.read(8))[0]
                    entries.append((folder, name, pn, off, sz, mt))
                except (struct.error, UnicodeDecodeError, IndexError):
                    break
            break

    return entries

def pack_directory(input_dir: str, output_hed: str, *,
                   base_name: str = "pack_%04x.dat",
                   version: int = 1,
                   dat_key_size: int = 0,
                   dat_key_file: str | None = None,
                   entry_order_hed: str | None = None,
                   key_id: str = "9495",
                   exe_path: str | None = None,
                   max_dat_mb: int = 0,
                   strip_prefix: str = "",
                   folder_prefix: str = "") -> None:
    """Pack a directory tree into .hed + .dat files."""
    import hashlib

    input_dir = os.path.abspath(input_dir)

    # ── collect files ──────────────────────────────────────────────────
    files: list[dict] = []
    for root, _dirs, fnames in os.walk(input_dir):
        for fname in fnames:
            full = os.path.join(root, fname)
            rel = os.path.relpath(full, input_dir).replace("\\", "/")
            if strip_prefix and rel.startswith(strip_prefix):
                rel = rel[len(strip_prefix):]
            folder = os.path.dirname(rel)
            if folder:
                folder = ".\\" + folder.replace("/", "\\") + "\\"
            elif folder_prefix:
                folder = folder_prefix
            else:
                folder = ".\\"
            name = ".\\" + os.path.basename(rel)
            files.append({
                "folder": folder,
                "name":   name,
                "path":   full,
                "size":   os.path.getsize(full),
            })

    if not files:
        print("No files found.")
        return

    # ── resolve reference entry order & timestamps ──────────────────────
    ref_order: dict[str, list[dict]] | None = None
    ref_index: int = 0
    if entry_order_hed:
        ref_parsed = parse_hed(entry_order_hed)
        if ref_parsed:
            _, _, ref_entries = ref_parsed
            ref_order = {}
            ref_index = 0
            for folder, name, pn, off, sz in ref_entries:
                filename = name.lstrip(".\\")
                if filename not in ref_order:
                    ref_order[filename] = []
                ref_order[filename].append({"idx": ref_index, "folder": folder, "name": name,
                                          "pack_num": pn, "offset": off, "size": sz})
                ref_index += 1
            # Store m_Time too — need to re-parse the raw chunk 3 for timestamps
            # Do that inline below
            print(f"Reference .hed: {len(ref_entries)} entries")

    # ── build name→file map ────────────────────────────────────────────
    name_to_file: dict[str, dict] = {}
    for f in files:
        filename = os.path.basename(f["path"])
        name_to_file[filename] = f

    # ── order files by reference .hed entry order ────────────────────────
    ordered_files: list[dict] = []
    used_names: set[str] = set()

    if ref_order:
        assert entry_order_hed is not None
        ref_entries_with_mtime = _parse_hed_entries_with_time(
            entry_order_hed, key_id=key_id, exe_path=exe_path)

        for idx, (folder, name, pn, off, sz, m_time) in enumerate(ref_entries_with_mtime):
            filename = name.lstrip(".\\")
            if filename in name_to_file and filename not in used_names:
                f = dict(name_to_file[filename])
                f["folder"] = folder
                f["name"] = name
                f["pack_num"] = pn
                f["offset"] = off
                f["m_time"] = m_time
                ordered_files.append(f)
                used_names.add(filename)

        # Append any files not in reference (at the end)
        for f in files:
            fn = os.path.basename(f["path"])
            if fn not in used_names:
                f2 = dict(f)
                f2["pack_num"] = 0
                f2["m_time"] = _filetime_from_path(f["path"])
                ordered_files.append(f2)

        files = ordered_files
        print(f"Applied reference entry order ({len(used_names)} matched, {len(ordered_files)-len(used_names)} new)")
    else:
        files.sort(key=lambda f: (f["folder"], f["name"]))
        for f in files:
            f["pack_num"] = 0
            f["m_time"] = _filetime_from_path(f["path"])
    total_size = sum(f["size"] for f in files)
    print(f"Packing {len(files)} files ({total_size/1024/1024:.1f} MB) from {input_dir}")

    # ── resolve key ────────────────────────────────────────────────────
    hed_key = None
    if exe_path:
        hed_key = derive_key_from_exe(exe_path)
        if hed_key:
            print(f"Derived key from EXE: {hed_key.hex().upper()}")
    if not hed_key:
        hed_key = KNOWN_KEYS.get(key_id.upper())
    if not hed_key:
        print(f"No key available (unknown key-id: {key_id}).  Use --exe or --key-id.")
        sys.exit(1)
    print(f"HED key: {len(hed_key)} bytes")

    dat_key = b""
    if dat_key_file:
        with open(dat_key_file, "rb") as dkf:
            dat_key = dkf.read()
        print(f"DatKey: {len(dat_key)} bytes (from {dat_key_file})")
    elif dat_key_size > 0:
        dat_key = os.urandom(dat_key_size)
        print(f"DatKey: {len(dat_key)} bytes (random)")

    # ── assign to .dat chunks ──────────────────────────────────────────
    hed_dir = os.path.dirname(os.path.abspath(output_hed)) or "."
    os.makedirs(hed_dir, exist_ok=True)
    max_bytes = max_dat_mb * 1024 * 1024

    chunks: list[list[dict]] = []
    cur_chunk: list[dict] = []
    cur_off = 0
    use_ref_offsets = bool(ref_order)

    if not use_ref_offsets:
        for f in files:
            sz = f["size"]
            if max_bytes > 0 and cur_chunk and cur_off + sz > max_bytes:
                chunks.append(cur_chunk)
                cur_chunk = []
                cur_off = 0
            f["offset"] = cur_off
            f["pack_num"] = len(chunks)  # 0-based
            cur_chunk.append(f)
            cur_off += sz
        if cur_chunk:
            chunks.append(cur_chunk)
    else:
        # Preserve reference offsets and pack_nums; group by pack_num
        chunk_map: dict[int, list[dict]] = {}
        for f in files:
            pn = f["pack_num"]
            if pn not in chunk_map:
                chunk_map[pn] = []
            chunk_map[pn].append(f)
        chunks = [chunk_map[pn] for pn in sorted(chunk_map.keys())]

    # ── write .dat files ───────────────────────────────────────────────
    for chunk in chunks:
        pn = chunk[0]["pack_num"]
        dat_path = os.path.join(hed_dir,
                                base_name.replace("%04x", f"{pn:04x}"))
        os.makedirs(os.path.dirname(dat_path), exist_ok=True)
        with open(dat_path, "wb") as df:
            for f in chunk:
                with open(f["path"], "rb") as src:
                    df.write(crypt(src.read(), dat_key))
        chunk_size = sum(f["size"] for f in chunk)
        print(f"  {os.path.basename(dat_path)}: {len(chunk)} files, {chunk_size/1024:.1f} KB")

    # ── build .hed body (chunked format) ─────────────────────────────────
    enc = base_name.encode("utf-16-le")
    chunk1_data = BytesIO()
    chunk1_data.write(bytes([len(enc) // 2]))
    chunk1_data.write(enc)
    chunk1_data.write(b"\x00" * 7)
    chunk1_data.write(b"\x00")       # flag (always 0x00 in game files)
    chunk1 = chunk1_data.getvalue()

    chunk2_data = BytesIO()
    chunk2_data.write(struct.pack("<I", 0x00000101))   # flags (matches game: 0x0101)
    chunk2_data.write(dat_key)
    chunk2 = chunk2_data.getvalue()

    chunk3_data = BytesIO()
    chunk3_data.write(struct.pack("<I", len(files)))   # entry count (no padding)
    for f in files:
        fe = f["folder"].encode("utf-16-le")
        ne = f["name"].encode("utf-16-le")
        chunk3_data.write(bytes([len(fe) // 2]))
        chunk3_data.write(fe)
        chunk3_data.write(bytes([len(ne) // 2]))
        chunk3_data.write(ne)
        chunk3_data.write(struct.pack("<I", f["pack_num"]))
        chunk3_data.write(struct.pack("<I", f["offset"]))
        chunk3_data.write(struct.pack("<I", f["size"]))
        chunk3_data.write(struct.pack("<Q", f.get("m_time", 0)))
    chunk3 = chunk3_data.getvalue()

    body = BytesIO()
    body.write(struct.pack("<II", 1, len(chunk1)))      # type=1 (BaseName)
    body.write(chunk1)
    body.write(struct.pack("<II", 2, len(chunk2)))      # type=2 (DatKey)
    body.write(chunk2)
    body.write(struct.pack("<II", 3, len(chunk3)))      # type=3 (FileEntries)
    body.write(chunk3)

    raw_body = body.getvalue()
    encrypted = crypt(raw_body, hed_key)

    # ── write .hed ─────────────────────────────────────────────────────
    with open(output_hed, "wb") as hf:
        hf.write(b"FPMF")
        hf.write(struct.pack("<I", version))
        hf.write(struct.pack("<I", len(encrypted)))
        hf.write(struct.pack("<I", 3))   # chunk count (BaseName+DatKey+Entries = 3)
        hf.write(encrypted)

    total_kb = total_size / 1024
    s = f"{total_kb/1024:.1f} MB" if total_kb >= 1024 else f"{total_kb:.1f} KB"
    print(f"Wrote {output_hed} ({len(files)} files, {s})")


# ═══════════════════════════════════════════════════════════════════════════
# 7. Pack-all (one .hed per top-level subdirectory)
# ═══════════════════════════════════════════════════════════════════════════

def pack_all(input_dir: str, output_dir: str, *,
             version: int = 131073,
             dat_key_size: int = 0,
             dat_key_file: str | None = None,
             key_id: str = "9495",
             exe_path: str | None = None,
             max_dat_mb: int = 0) -> None:
    """Pack each top-level subdirectory of *input_dir* into its own .hed+.dat pair
    under *output_dir*, using the subdirectory name as the archive name."""
    import shutil

    input_dir = os.path.abspath(input_dir)
    output_dir = os.path.abspath(output_dir)
    os.makedirs(output_dir, exist_ok=True)

    # Get top-level subdirectories
    try:
        entries = sorted(os.listdir(input_dir))
    except OSError:
        print(f"Error: cannot list {input_dir}")
        sys.exit(1)

    subdirs = [d for d in entries if os.path.isdir(os.path.join(input_dir, d))]

    if not subdirs:
        print(f"No subdirectories found in {input_dir}")
        return

    print(f"Packing {len(subdirs)} subdirectories from {input_dir} → {output_dir}\n")

    for sub in subdirs:
        src = os.path.join(input_dir, sub)
        dst_hed = os.path.join(output_dir, f"{sub}.hed")
        base = f"./{sub}/%04x.dat"

        print(f"[{sub}] ", end="", flush=True)
        pack_directory(
            src, dst_hed,
            base_name=base,
            version=version,
            dat_key_size=dat_key_size,
            dat_key_file=dat_key_file,
            key_id=key_id,
            exe_path=exe_path,
            max_dat_mb=max_dat_mb,
            strip_prefix="",
            folder_prefix=f".\\{sub}\\",
        )


# ═══════════════════════════════════════════════════════════════════════════
# 8. CLI
# ═══════════════════════════════════════════════════════════════════════════

def _add_data_dir(p):
    p.add_argument("--data-dir", help="Data root directory for .dat path resolution")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="AISpace Archive Tool — extract, list, pack, and bulk-extract .hed/.dat archives")
    sub = parser.add_subparsers(dest="cmd")

    # list
    lst = sub.add_parser("list", help="List files in a .hed archive")
    lst.add_argument("hed_path")
    _add_data_dir(lst)
    lst.add_argument("--exe", help="Path to game EXE for key derivation (not usually needed)")
    lst.add_argument("--header", action="store_true", help="Show only header info, not file list")

    # extract
    ext = sub.add_parser("extract", help="Extract one .hed archive")
    ext.add_argument("hed_path")
    ext.add_argument("output_dir")
    _add_data_dir(ext)
    ext.add_argument("--exe", help="Path to game EXE for key derivation")

    # extract-all (bulk)
    ball = sub.add_parser("extract-all", help="Bulk-extract all .hed archives under a data directory")
    ball.add_argument("data_dir", help="Root data directory containing .hed files and .dat subdirectories")
    ball.add_argument("output_dir", help="Output directory for extracted files")

    # pack
    pk = sub.add_parser("pack", help="Pack a directory into .hed + .dat files")
    pk.add_argument("input_dir")
    pk.add_argument("output_hed")
    pk.add_argument("--base-name", default="pack_%04x.dat",
                    help="BaseName format for .dat files (default: pack_%%04x.dat)")
    pk.add_argument("--version", type=int, default=131073,
                    help="Archive version (default: 131073 = 0x20001)")
    pk.add_argument("--dat-key-size", type=int, default=0,
                    help="DatKey size in bytes, 0 = no per-file encryption (default)")
    pk.add_argument("--dat-key-file",
                    help="Load DatKey from file for exact game reproduction")
    pk.add_argument("--entry-order-hed",
                    help="Reference .hed to preserve entry order, pack_num offset, and timestamps")
    pk.add_argument("--key-id", default="9495",
                    help="Fallback KeyId if --exe not provided (default: 9495)")
    pk.add_argument("--exe", help="Path to game EXE for key derivation")
    pk.add_argument("--max-dat-mb", type=int, default=0,
                    help="Max MB per .dat file, 0 = single .dat (default)")
    pk.add_argument("--strip-prefix", default="",
                    help="Strip this prefix from file paths in the archive (e.g. ./)")

    # pack-all
    pa = sub.add_parser("pack-all", help="Pack each top-level subdirectory into its own .hed+.dat")
    pa.add_argument("input_dir", help="Directory containing subdirectories to pack")
    pa.add_argument("output_dir", help="Output directory for .hed and .dat files")
    pa.add_argument("--version", type=int, default=131073,
                    help="Archive version (default: 131073 = 0x20001)")
    pa.add_argument("--dat-key-size", type=int, default=0,
                    help="DatKey size in bytes, 0 = no per-file encryption (default)")
    pa.add_argument("--dat-key-file",
                    help="Load DatKey from file for exact game reproduction")
    pa.add_argument("--key-id", default="9495",
                    help="Fallback KeyId if --exe not provided (default: 9495)")
    pa.add_argument("--exe", help="Path to game EXE for key derivation")
    pa.add_argument("--max-dat-mb", type=int, default=0,
                    help="Max MB per .dat file, 0 = single .dat (default)")

    args = parser.parse_args()

    if args.cmd == "list":
        list_archive(args.hed_path, getattr(args, "data_dir", None),
                     header_only=getattr(args, "header", False))
    elif args.cmd == "extract":
        count = extract_archive(args.hed_path, args.output_dir,
                                getattr(args, "data_dir", None))
        print(f"Extracted {count} files.")
    elif args.cmd == "extract-all":
        bulk_extract(args.data_dir, args.output_dir)
    elif args.cmd == "pack":
        pack_directory(args.input_dir, args.output_hed,
                       base_name=args.base_name,
                       version=args.version,
                       dat_key_size=args.dat_key_size,
                       dat_key_file=getattr(args, "dat_key_file", None),
                       entry_order_hed=getattr(args, "entry_order_hed", None),
                       key_id=args.key_id,
                       exe_path=getattr(args, "exe", None),
                       max_dat_mb=args.max_dat_mb,
                       strip_prefix=args.strip_prefix)
    elif args.cmd == "pack-all":
        pack_all(args.input_dir, args.output_dir,
                 version=args.version,
                 dat_key_size=args.dat_key_size,
                 dat_key_file=getattr(args, "dat_key_file", None),
                 key_id=args.key_id,
                 exe_path=getattr(args, "exe", None),
                 max_dat_mb=args.max_dat_mb)
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
