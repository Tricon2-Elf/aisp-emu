# AISpace MRB Animation Format (`.mrb`)

## Overview

MRB (Model Resource Binary) files contain **bone hierarchy + skeletal animation keyframe data**. They use the same chunk-based container as `.vra` files. Motion data lives in **chunk type 3**.

The client loads MRB files alongside `.vra` models via `dxModel_vtbl__func_91` (`aisp-decompiled.c:768226`). Character and object animations reference MRB files; event-driven animation states use separate `.asd` files.

Typical paths:
```
item/1/01/00001/anim/100010000_0_0_01011.MRB     — item animation
world/object/00/anim/OA_00_ma_cherry01A.mrb       — world object animation
chara/8/80069/anim/...MRB                          — character equipment animation
```

## Chunk Container

MRB files use the **same chunk-based container** as `.vra` files. The container parser is `sub_835630` (`aisp-decompiled.c:797561`). Each chunk has a type byte; motion data is in **chunk type 3**.

Container loading: `sub_813FB0` (line 770766) opens the file, iterates chunks, and passes chunk type 3 data to the MRB parser.

## Binary Layout (within chunk type 3)

The MRB parser `sub_813D20` (line 770645) reads this structure:

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | — | — | Chunk header (not part of the motion data) |
| 0x04 | — | buffer | **Track source** — reference buffer, copied to dxMotion+24 |
| 0x28 | 4 | uint32 | **Flags** — bitmask. Bits 0,1,5,6,7,8 must be set (mask 0x1E3). Bit 3 = has special keyframes. Bit 4 = has speed override. |
| 0x34 | — | char* | **BoneNameList** — null-separated string of bone names (e.g. `RootBox\0Bip01\0Bip01 Pelvis\0...`) |
| 0x38 | 4 | float | **KeyframeCount** — number of keyframes per bone |
| 0x40 | — | float* | **KeyframeTimes** — array of float timestamps, one per keyframe |
| 0x50 | 4 | uint32 | **SpecialKeyCount** — number of special keyframe entries |
| 0x58 | — | uint32* | **SpecialKeyData** — stride 68 bytes per entry (1 float time + 16 ints) |
| 0x64 | 4 | uint32** | **SpeedOverride** — float value, only if flag bit 4 is set |
| 0x68 | 4 | uint32 | **PosDataCount** — element count for position track |
| 0x6C | 4 | uint32 | **PosStride** — per-element stride multiplier |
| 0x70 | — | void* | **PositionTrack** — raw position keyframe data |
| 0x74 | 4 | uint32 | **RotDataCount** — element count for rotation track |
| 0x78 | 4 | uint32 | **RotStride** — per-element stride multiplier |
| 0x7C | — | void* | **RotationTrack** — raw rotation keyframe data (quaternions) |
| 0x80 | 4 | uint32 | **ScaleDataCount** — element count for scale track |
| 0x84 | 4 | uint32 | **ScaleStride** — per-element stride multiplier |
| 0x88 | — | void* | **ScaleTrack** — raw scale keyframe data |
| 0x8C | 4 | uint32 | **BoneCount** — number of bones |
| 0x90 | — | uint16* | **KeyframeIndices** — uint16[3] per keyframe per bone: `[pos_idx, rot_idx, scale_idx]`. Total size = `6 × KeyframeCount × BoneCount` bytes |

## Keyframe Interpolation

For a given time `t`, the client finds the two bracketing keyframes (via `sub_813000`, line 769939) and interpolates linearly between them (via `sub_813400`, line 770141):

```
For each bone:
    entry = keyframe_indices[bone_idx * keyframe_count * 3 + keyframe_idx * 3]
    pos   = lerp(position_track[entry[0] * pos_stride],  key0, key1)
    rot   = slerp(rotation_track[entry[1] * rot_stride],  key0, key1)
    scale = lerp(scale_track[entry[2] * scale_stride],    key0, key1)
```

Tracks are stored per-bone with stride = 40 bytes: `pos[12] + rot[16] + scale[12]`.

## Per-Bone Defaults

Each bone is initialized (via `sub_8132C0`, line 770079) with:
- `pos[4]` = {0, 0, 0, 0} — quaternion/position
- `float[2]` = {0, 0}
- `float[4]` = {1.0, 1.0, 1.0, 1.0} — scale defaults

## Special Keyframes

When flag bit 3 (0x08) is set, special keyframes exist at chunk offset 0x58. Each entry is **68 bytes (17 ints)**:
- `float time` — trigger timestamp
- 16 ints — event parameters

Used for events embedded in the animation (effects, sound triggers, motion callbacks).

## Speed Override

When flag bit 4 (0x10) is set, the chunk offset 0x64 contains a float motion speed multiplier. This overrides the default dxMotion speed of 200.0.

## Motion Playback

The animation system uses `dxMotion` (line 6157) with 4 motion slots per `CAIModel`:

```
CAIModel::field_60C[0..3] = CAIModelMotion slots
    → dxMotion (raw track data)
        → Keyframe times, position/rotation/scale tracks
        → Bone keyframe index table
        → Per-bone interpolated state (40 bytes/bone)
```

Playback flow:
1. `dxModel_vtbl__func_91` loads the `.MRB` file
2. `sub_6DD860` (line 534865) starts playback on a motion slot (0-3)
3. Each frame: `sub_6DDDC0` (line 535116) → `sub_813530` (line 770176) → interpolates bone poses from keyframe data
4. `sub_6DD9C0` (line 534939) computes final bone transforms

## File Relationships

```
{item,chara}/.../attr/{name}.vra       — model description
    ↓ references
{item,chara}/.../attr/{name}.dxg       — geometry (meshes, vertices)
{item,chara}/.../attr/{name}.dds       — texture

{item,chara}/.../anim/{name}_MOVE.MRB  — skeletal animation (chunk type 3)
{item,chara}/.../anim/{name}.asd       — animation state events (separate format)

world/object/.../anim/OA_*.mrb          — world object animation
```

## Key Client Functions

| Function | Line | Description |
|----------|------|-------------|
| `dxModel_vtbl__func_91` | 768226 | Primary MRB loader |
| `sub_813FB0` | 770766 | Open MRB chunk container, find type=3 chunk |
| `sub_813D20` | 770645 | **MRB binary parser** — bone names, keyframes, tracks |
| `sub_8132C0` | 770079 | Bone name parser + per-bone storage allocation |
| `sub_813000` | 769939 | Keyframe lookup — find bracketing keyframes |
| `sub_813400` | 770141 | Bone pose interpolation |
| `sub_813530` | 770176 | Apply motion to all bones |
| `sub_6DD860` | 534865 | Start motion playback on slot |
| `sub_6DDDC0` | 535116 | Update motion state at current time |
| `sub_6DD9C0` | 534939 | Compute animated bone pose from motion data |
