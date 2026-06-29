# AISpace .hed / .dat Archive Format

## Overview

Game content (models, textures, scripts, CSVs, audio, maps) is stored in paired **.hed** (header/index) and **.dat** (data) files. The .hed contains an encrypted file table; the .dat contains the raw file data. Each .hed can reference multiple .dat files (e.g., `field_0001.dat`, `field_0002.dat`).

The client scans the data directory (`argv[1]`) recursively for `*.hed` files at startup (`aisp-decompiled.c:637591` / `sub_763700`) using background worker threads (`sub_852880`). Discovered archives are indexed in a global hash table for fast lookup during gameplay.

---

## Header File (.hed) Format

### Header (16 bytes, unencrypted)

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0x00 | 4 | char[4] | Magic: `FPMF` (bytes `46 50 4D 46`) |
| 0x04 | 4 | uint32 LE | **Version** |
| 0x08 | 4 | uint32 LE | **DataSize** — byte length of the encrypted body |
| 0x0C | 4 | — | Unused |

The body immediately follows at offset 0x10 (16).

### Body (DataSize bytes, encrypted)

The body is encrypted with the subtractive-modulo algorithm (see below). The 64-byte key is derived from the client EXE at runtime — there is **no KeyId in the header**. (The `KeyId` concept in the Python extractor is a heuristic: it reads bytes 1–2 of the encrypted body, which happen to be deterministic enough to distinguish archive sets when the EXE isn't available.)

After decryption, the body layout is:

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0x00 | 8 | — | Unused |
| 0x08 | 1 | uint8 | **BaseNameChars** — number of UTF-16LE characters (byte length = value × 2) |
| 0x09 | chars×2 | UTF-16LE | **BaseName** — `printf`-style format string for .dat filenames, e.g. `field_%04x` |
| — | 16 | — | Unused |
| — | 4 | uint32 LE | **DatKeySize** — length of the optional per-file dat decryption key |
| — | DatKeySize | bytes | **DatKey** — if non-empty, each file in the .dat is decrypted with this key |
| — | 8 | — | Unused |
| — | 4 | uint32 LE | **FileCount** — number of file entries that follow |

Immediately after: **FileCount** entries, each:

| Offset | Size | Type | Description |
|--------|------|------|-------------|
| 0x00 | 1 | uint8 | **FolderNameChars** — bytes = value × 2 |
| — | chars×2 | UTF-16LE | **FolderName** — relative directory with trailing `\`, e.g. `.\map\pack\` |
| — | 1 | uint8 | **FileNameChars** — bytes = value × 2 |
| — | chars×2 | UTF-16LE | **FileName** — e.g. `garden_l.ymap.bin`. May contain embedded `\` path components. |
| — | 4 | uint32 LE | **PackNum** — index into the .dat set; substituted as `%04x` into BaseName |
| — | 4 | uint32 LE | **FileOffset** — byte offset within the .dat |
| — | 4 | uint32 LE | **FileSize** — byte length |
| — | 4 | uint32 LE | **Checksum** — not validated by the client (always 0 in practice) |

### Path Conventions

Paths use Windows-style `\` separators. The FolderName typically ends with `\`. The FileName may contain additional `\` separators for nested directories. The full virtual path is `FolderName + FileName`. When extracting to Linux/macOS, convert `\` to `/`.

### .dat File Path Resolution

```
{data_directory}/{BaseName_with_PackNum}.dat
```

`BaseName` is a format string like `field_%04x`. `%04x` is replaced with the zero-padded lowercase hex of `PackNum`:

- BaseName `field_%04x` + PackNum `0x0001` → `field_0001.dat`
- BaseName `chara/model/pack_%04x` + PackNum `0x0003` → `chara/model/pack_0003.dat`

`.dat` files are expected in the same directory as the `.hed` file.

---

## Data File (.dat) Format

.dat files are **raw concatenations** of file contents with no headers or framing. Each entry in the .hed points to a contiguous byte range:

- **FileOffset** — seek position within the .dat
- **FileSize** — bytes to read from that position

If `DatKeySize > 0`, the bytes are decrypted using the DatKey (same algorithm as the .hed body). If `DatKeySize == 0`, the contents are stored unencrypted.

---

## Encryption

**Algorithm** (matches `CScrambleAddKey::scramble` at `aisp-decompiled.c:821473`):

```python
def decrypt(key: bytes, data: bytes) -> bytes:
    out = bytearray(len(data))
    for i in range(len(data)):
        out[i] = (data[i] - key[i % len(key)]) & 0xFF
    return bytes(out)
```

Since subtraction is its own inverse mod 256, the same function both encrypts and decrypts.

### Key Hierarchy

| Key | Size | Source | Scope |
|-----|------|--------|-------|
| .hed body key | 64 bytes | Derived from client EXE at runtime | Protects the file index (.hed body) |
| DatKey | variable | Stored inside the .hed body | Protects individual file contents in .dat files (optional) |

### .hed Body Key Derivation

At startup, the client calls `sub_7623D0` (`aisp-decompiled.c:636814`):

1. `GetModuleFileNameW(0, ...)` — gets the path to its own EXE
2. `es::CEXEParser::Parse()` — parses the PE header
3. `sub_850D40(&parser, 0)` — reads the raw bytes of the **first executable code section** (`.text`) from the EXE on disk via `es::CStdFile::Seek(PointerToRawData)` + `Read(SizeOfRawData)`
4. Hashes the section data into a 64-byte key:

```c
byte key[64] = {0};
int v7 = 0;
for (int idx = 0; idx < section_size; idx++) {
    key[v7] = (idx + section_data[idx] + key[v7]) & 0xFF;  // rolling additive
    v7 = (v7 + 1) & 0x3F;  // wrap 0..63
}
```

This means the key is **deterministic for a given EXE binary** — different game builds produce different keys. No key data is stored in the .hed file itself.

The `KeyId` used in the Python extractor (e.g., `16F6`, `84D4`) is a **heuristic**: it reads bytes 1–2 of the still-encrypted body, reverses them, and hex-encodes the result. These 2 bytes happen to be consistent enough within an archive set to act as a discriminator when the original EXE isn't available. The client itself never reads or uses these bytes as an identifier.

---

## Client Loading Pipeline

```
LoadFileData(filename)
    └── sub_762E50() → sub_851F30() → sub_851CB0()
        ├── Hash table lookup → cache hit → return
        └── Cache miss:
            └── fm::CPackFileImpl::Load()
                └── ParseHeadFile()  (aisp-decompiled.c:823907)
                    ├── es::CStdFile::Open(.hed) → read 16-byte header
                    ├── Validate "FPMF" magic
                    ├── Read DataSize bytes of encrypted body
                    ├── CScrambleAddKey::scramble() → decrypt body with EXE-derived key
                    ├── Parse file entries (base name, folder, filename, dat id, offset, size)
                    └── Store entries in hash table
            └── On demand: fm::CPackFileImpl_vtbl__func_4()
                ├── Look up file entry by virtual path
                ├── Resolve .dat path: BaseName % PackNum
                ├── es::CStdFile::Open(.dat) → seek → read
                └── If DatKeySize > 0: decrypt with DatKey
```

---

## CsvFile::Open — Direct Disk Fallback

`CsvFile::Open` (`aisp-decompiled.c:580557`) has a dual-path architecture not present in `LoadFileData`:

1. Try pack system (`sub_850630`) first
2. If that fails **and the `a4` parameter is 1**, fall back to `es::CStdFile::Open` — a direct disk read

Only config files like `option.dat` enable this fallback (`a4=1`). Game content uses `LoadFileData` which always goes through the archive system (`a4=0` equivalent).
