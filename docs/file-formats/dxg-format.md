# AISpace DXG Binary Geometry Format (`.dxg`)

## Overview

DXG is a **compiled binary mesh geometry** format loaded into `dxGeom` objects (`aisp-decompiled.c:790076`). Files contain mesh groups with packed vertex/face data, plus optional sections for materials, textures, bounding volumes, collision planes, and rigid-body dynamics.

The main loader is `sub_82C260` (line 790196). Header validation is `sub_8333F0` (line 795900).

Format version evolved: **v1.0.0** (group+material+texture) → **v1.0.1** (+bounds) → **v1.0.3** (+collision) → **v1.0.7** (+dynamics).

## File Header (12 bytes)

Validated by `sub_8333F0` (line 795900):

```c
if (*(WORD *)(a2 + 4) != 1 || (*(WORD *)(a2 + 6) | 0x10000u) > 0x10007)
    return 0;  // reject
```

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 4 | char[4] | `signature` | 4-byte file signature (not checked by the validator — may be validated by caller before `sub_8334A0`, or may be a remnant). Python script reads `DXG ` (0x44 0x58 0x47 0x20). |
| 0x04 | 2 | uint16 LE | **`versionMajor`** | Must be `1` |
| 0x06 | 2 | uint16 LE | **`versionMinor`** | Must be ≤ `7` (checked as `(minor | 0x10000) <= 0x10007`) |
| 0x08 | 1 | uint8 | **`sectionFlags`** | Bitmask of present sections (see below) |
| 0x09 | 1 | — | Reserved | Not accessed by any decompiled code |
| 0x0A | 2 | — | Reserved | Not accessed by any decompiled code |

### Section Flags (offset 0x08)

| Bit | Mask | Section | Accessor | Added In | Description |
|-----|------|---------|----------|----------|-------------|
| 0 | 0x01 | **Group** | `sub_833420` (795909) | v1.0.0 | Mesh groups: vertex data, face indices, consolidated position/normal/UV arrays |
| 1 | 0x02 | **Material** | `sub_8334E0` (795976) | v1.0.0 | Material library: RGBA diffuse colors + 64-byte material descriptors |
| 2 | 0x04 | **Texture** | `sub_833530` (796006) | v1.0.0 | Texture references: index array + 64-byte texture name strings |
| 3 | 0x08 | **Bounds** | `sub_833590` (796036) | v1.0.1 | Per-group bounding sphere/box entries (60 bytes each) |
| 4 | 0x10 | **Collision** | `sub_833600` (796070) | v1.0.3 | Collision mesh: k-DOP plane entries (48 bytes each) |
| 5 | 0x20 | **Dynamics** | `sub_833670` (796108) | v1.0.7 | Rigid body joints and physics data |

Sections appear sequentially in the file. Each section is only present if its flag bit is set AND the file version meets the minimum version requirement.

---

## Group Section (if bit 0 set)

Starts immediately after the 12-byte file header.

### Group Section Header (8 bytes)

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x0C | 2 | uint16 LE | **`m_GroupCount`** | Number of mesh groups (line 790422) |
| 0x0E | 2 | — | Padding | Not accessed |
| 0x10 | 4 | uint32 LE | **`m_GroupSectionSize`** | Total byte size of the group section payload (used by `sub_8334C0` at line 795971 to calculate material section offset) |
| 0x14 | 4 | uint32 LE | **`m_GroupNameOffset`** | Offset from section_start+12 to the group name string list (line 795783) |

### Group Name List

Located at `section_start + 12 + m_GroupNameOffset`. Contains `m_GroupCount` null-terminated Shift-JIS strings. Accessed via `sub_8332D0` (line 795778).

### Group Entry

Each group entry has variable size: `*(entry + 8) + 28` bytes (line 795759, `sub_8332B0`).

#### Group Entry Header

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 2 | — | Reserved | Not read directly |
| 0x02 | 2 | — | Reserved | Not read |
| 0x04 | 1 | uint8 | **`m_MeshPartCount`** | Number of sub-mesh parts in this group (line 803847) |
| 0x05 | 1 | int8 | **`m_MaterialIndex`** | Material slot index, or `-1` if none (line 803692) |
| 0x06 | 6 | — | Reserved | Not accessed |
| 0x0C | 2 | uint16 LE | **`m_VertexPosCount`** | Total vertex position floats in consolidated array (×3 for XYZ triples, 4 bytes each) |
| 0x0E | 2 | uint16 LE | **`m_NormalCount`** | Total normal floats in consolidated array (×3 for NX,NY,NZ triples, 4 bytes each) |
| 0x10 | 2 | uint16 LE | **`m_UVCount`** | Total UV floats in consolidated array (×2 for U,V pairs, 4 bytes each) |
| 0x12 | 2 | uint16 LE | **`m_ColorCount`** | Total vertex color components (×4 for RGBA, 2 bytes each = RGBA16) |
| 0x14 | 2 | uint16 LE | **`m_WeightCount`** | Total vertex weight floats (4 bytes each) |
| 0x16 | 2 | — | Reserved | Not accessed |
| 0x18 | 4 | uint32 LE | **`m_BlendIndexCount`** | Total blend index values (4 bytes each) |

Each count is the number of **individual scalar elements**, not triples/vectors. For example, `m_VertexPosCount = 90` means 30 position triples (90 floats = 360 bytes).

### Sub-Mesh Header (`m_MeshPartCount` entries)

Accessed via `sub_8331A0(groupEntry, partIndex)` (line 795653):

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 2 | uint16 LE | **`m_SubVertexCount`** | Vertices in this sub-mesh (line 803710) |
| 0x02 | 2 | uint16 LE | **`m_SubFaceCount`** | Triangle faces in this sub-mesh (line 803713) |
| 0x04 | 1 | uint8 | **`m_HasUV`** | UV channel count flag (0 = no UV, >0 = UV channels present, up to 8 channels — line 803714) |
| 0x05 | 1 | uint8 | **`m_HasSkinData`** | Has bone/skinning data (line 803758) |
| 0x06 | 2 | — | Reserved | Not accessed |
| 0x08 | 4 | uint32 LE | **`m_ExtraDataSize`** | Extra data block size (normals/skinning). If > 0, extra per-vertex data follows the vertex/face data (line 803748) |

### Sub-Mesh Data Arrays (per sub-mesh, in order)

#### Packed Vertex Data (10 bytes × `m_SubVertexCount`)

Accessed via `sub_833070(header)` → `header + 16` (line 795532). Compact vertex format:
- Position: half-float XYZ (6 bytes)
- Normal: packed 4 bytes
- **Total: 10 bytes per vertex**

#### Face Index Data (6 bytes × `m_SubFaceCount`)

Accessed via `sub_833080(header)` — follows vertex data. Triangle list:
- Index A: uint16 LE (2 bytes)
- Index B: uint16 LE (2 bytes)
- Index C: uint16 LE (2 bytes)
- **Total: 6 bytes per face**

Indices reference the sub-mesh's own vertex array (0 to `m_SubVertexCount - 1`).

#### UV Data (if `m_HasUV > 0`)

Accessed via `sub_833090(header)`. Texture coordinate pairs, one per vertex. Size depends on UV channel count (up to 8 × 2 floats per vertex).

#### Extra Data (if `m_ExtraDataSize > 0`)

Accessed via `sub_8330B0(header)` (line 795562). Contains per-vertex normal data and/or skinning bone indices.

### Consolidated Arrays (after all sub-meshes)

Shared per-group data arrays, accessed after all sub-mesh data by:

| Array | Accessor | Line | Element Size | Count Field |
|-------|----------|------|-------------|-------------|
| **Vertex positions** | `sub_8331D0` | 795672 | 4 bytes (float) × 3 | `m_VertexPosCount` |
| **Normals** | `sub_8331F0` | 795685 | 4 bytes (float) × 3 | `m_NormalCount` |
| **UVs** | `sub_833210` | 795696 | 4 bytes (float) × 2 | `m_UVCount` |
| **Vertex colors** | `sub_833230` | 795707 | 2 bytes × 4 (RGBA16) | `m_ColorCount` |
| **Weights** | `sub_833250` | 795718 | 4 bytes (float) | `m_WeightCount` |
| **Blend indices** | `sub_833270` | 795729 | 4 bytes | `m_BlendIndexCount` |

---

## Material Section (if bit 1 set, requires v1.0.0+)

Loaded at lines 790271–790302.

### Material Sub-Header (12 bytes)

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 1 | uint8 | **`m_MaterialCount`** | Number of materials |
| 0x01 | 3 | — | Padding | |
| 0x04 | 4 | uint32 LE | `m_DataSize` | Section data size |
| 0x08 | 4 | uint32 LE | **`m_ColorOffset`** | Offset from section_start+12 to diffuse color array |

### Payload

**Diffuse Color Array** (at `section_start + 12 + m_ColorOffset`):
- `m_MaterialCount × 4 bytes` — RGBA8 per material (byte R, G, B, A)

**Material Data Array** (immediately after color array):
- `m_MaterialCount × 64 bytes` — Full material descriptors (likely D3DMATERIAL9: Diffuse, Ambient, Specular, Emissive, Power, plus padding)

---

## Texture Section (if bit 2 set, requires v1.0.0+)

Loaded at lines 790303–790480.

### Texture Sub-Header (12 bytes)

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 1 | uint8 | **`m_TextureCount`** | Number of textures |
| 0x01 | 7 | — | Padding | |
| 0x08 | 4 | uint32 LE | `m_IndexOffset` | Offset to index/data arrays |
| 0x0C | 4 | uint32 LE | `m_NameArrayOffset` | Offset to texture name array (from section_start to name data) |

### Payload

**Texture Index Array** (at `section_start + 12 + m_IndexOffset`):
- `m_TextureCount × 1 byte` — Numerical texture IDs

**Texture Name Array** (at `section_start + m_NameArrayOffset`):
- `m_TextureCount × 64 bytes` — Fixed-width texture filenames/paths (64 bytes each)

---

## Bounds Section (if bit 3 set, requires v1.0.1+)

Loaded at lines 790331–790350.

### Bounds Sub-Header

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 1 | uint8 | **`m_BoundsCount`** | Number of bounding entries |
| 0x01 | 7 | — | Padding | |

Payload at `section_start + 8` (via `sub_74D940`):

**Bounding entries**: `m_BoundsCount × 60 bytes` each. 15 floats representing a bounding volume transformation matrix plus sphere radius (decomposed by `sub_833AA0` → `D3DXMatrixInverse` at line 766666).

---

## Collision Section (if bit 4 set, requires v1.0.3+)

Loaded at lines 790355–790406.

### Collision Sub-Header

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 1 | uint8 | **`m_CollisionCount`** | Number of collision entries |
| 0x01 | 7 | — | Padding | |

Payload at `section_start + 8`:

**Collision entries**: `m_CollisionCount × 48 bytes` each. 12 floats representing 3 offset vectors + 3 normal vectors + 3 cross vectors + 3 additional vectors. Converted to 64-byte collision planes by `sub_82BBC0`.

---

## Dynamics Section (if bit 5 set, requires v1.0.7+)

Loaded at line 790408. Calls `sub_808050` (line 761858).

Contains two subsections:

### Joint Collision Bodies
- Count from `*(a2)`
- **44 bytes per entry**: position, quaternion rotation, collision body params (half-length, radius, friction coefficient)

### Dynamic Joints
- Count from `*(a2 + 4)`
- Variable size, traversed via `sub_8333B0`
- Each entry: joint type, mass, list of `dxJointConnection` objects (**20 bytes each**: parent/child bone IDs, position, axis, limits)

---

## Section Offset Calculation

Sections are sequential. The offset to each section is computed by summing the sizes of all preceding sections plus their 12-byte sub-headers. `sub_833670` (line 796108–796132) shows the cascade:

```
material_offset = group_section_size + 8
texture_offset  = material_offset + material_data_size + 12  (via sub_833510)
bounds_offset   = texture_offset  + texture_data_size + 12   (via sub_833570)
collision_offset= bounds_offset   + bounds_data_size + 12    (via sub_8335E0)
dynamics_offset = collision_offset+ collision_data_size + 12 (via sub_833650)
```

## File Layout Summary

```
Offset  Size     Section
======  ====     =======
0x00    4        signature
0x04    2        versionMajor  (= 1)
0x06    2        versionMinor  (≤ 7)
0x08    1        sectionFlags  (bitmask)
0x09    1        (reserved)
0x0A    2        (reserved)

--- Group Section (if bit 0) ---
0x0C    2        m_GroupCount
0x0E    2        (padding)
0x10    4        m_GroupSectionSize
0x14    4        m_GroupNameOffset
0x18    var      Group entries (each: header + sub-meshes + consolidated arrays)

--- Material Section (if bit 1) ---
        12       sub-header
        var      RGBA colors × materialCount
        var      Material data × materialCount (64 bytes each)

--- Texture Section (if bit 2) ---
        12       sub-header
        var      Texture indices × textureCount (1 byte each)
        var      Texture names × textureCount (64 bytes each)

--- Bounds Section (if bit 3) ---
        12       sub-header
        var      Bounding entries × count (60 bytes each)

--- Collision Section (if bit 4) ---
        12       sub-header
        var      Collision entries × count (48 bytes each)

--- Dynamics Section (if bit 5) ---
        12       sub-header
        var      Joint collision bodies × count (44 bytes each)
        var      Dynamic joints (variable)
```

## Existing Tool

`scripts/readDXGFile.py` — parses the binary format, extracts group/mesh headers and per-group vertex/face data. Note: uses older generic field names that don't match the decompiled names in this document.

## Key Function References

| Function | Line | Purpose |
|----------|------|---------|
| `sub_8333F0` | 795900 | Header validation |
| `sub_8334A0` | 795954 | DXG reader init |
| `sub_82C260` | 790196 | Main loader |
| `sub_833420` | 795909 | Group section accessor |
| `sub_8334E0` | 795976 | Material section accessor |
| `sub_833530` | 796006 | Texture section accessor |
| `sub_833590` | 796036 | Bounds section accessor |
| `sub_833600` | 796070 | Collision section accessor |
| `sub_833670` | 796108 | Dynamics section accessor |
| `sub_8331A0` | 795653 | Sub-mesh header accessor |
| `sub_8332B0` | 795759 | Group entry iterator |
| `sub_8332D0` | 795778 | Group name lookup |
| `sub_833070` | 795530 | Packed vertex data pointer |
| `sub_833080` | 795536 | Face index data pointer |
| `sub_749DB0` | 617329 | Section data accessor (+12 offset) |
| `sub_74D940` | 620522 | Section data accessor (+8 offset) |
