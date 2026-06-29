# AISpace ASD Animation State Format (`.asd`)

## Overview

ASD (Animation State Data) files contain a **flat event list** that drives animation state machines. Each event is **24 bytes (6 floats/ints)** with no header or magic number — the file starts directly with the first event's time value.

ASD files are loaded alongside `.mrb` and `.vra` files by `sub_6DB570` (`aisp-decompiled.c:533110`). The loader is `sub_6DA250` (line 532006), which reads the file into `CAIModel + 1892`.

Typical paths:
```
chara/7/00301/anim/700301_0_00_000_100000030.asd — character animation events
chara/7/00301/anim/700301_0_00_000_100000040.asd — character animation events
```

The loading order in `sub_6DB570` (line 533110):
1. Try `{base}_MOVE.MRB` — skeletal animation data
2. Try `{base}.asd` — event-based animation state machine

## Event Record (24 bytes)

| Offset | Size | Type | Name | Description |
|--------|------|------|------|-------------|
| 0x00 | 4 | float | **Time** | Trigger timestamp in seconds |
| 0x04 | 4 | int | **EventType** | Event type code (1-13) |
| 0x08 | 4 | int | **Param1** | Type-specific parameter |
| 0x0C | 4 | int | **Param2** | Type-specific parameter |
| 0x10 | 4 | int | **Param3** | Type-specific parameter |
| 0x14 | 4 | float | **Duration** | Next-event advance control. If ≤ 0, event sequence stops. |

Events are processed sequentially in time order by `sub_6D9DB0` (line 531728), the event dispatcher.

## Event Types

| Type | Description | Parameters |
|------|-------------|-----------|
| **1** | Sound/effect | Param1: -2 = stop, -1 = pause, other = play sound ID |
| **2** | Parameter set | General parameter assignment |
| **3** | Animation speed | Param1 × 0.01 = new speed multiplier |
| **4** | Motion position | Two values × 0.01 / × 0.001 for position/rotation |
| **5** | Item equip/unequip | Triggers equipment visibility toggle |
| **6** | Character effect | Triggers another character-side effect |
| **7** | Motion trigger | `CAIMotion::sub_6DD860` on `field_60C` — play animation slot |
| **8** | Sub-motion dispatch | `sub_6D9C90` — dispatch to sub-motion controller |
| **0xA** | Sound playback | Plays a sound by ID |
| **0xD** | Special effect | Triggers a special/particle effect |

## Seeking

Events advance forward through the list. A record where **Duration ≤ 0** causes the sequence to stop advancing.

The client seeks by:
- Skipping records forward to a target time (`sub_6D9C00`, line 531609)
- Processing events within a time range (`sub_6DA180`, line 531952)

Each seek advances the current position pointer by one event (stride = 6 floats = 24 bytes) and checks the next event's Duration field against 0 to determine whether to continue.

## Loading

`sub_6DA250` (line 532006):

1. Resets current time tracking to `FLT_MAX`
2. Loads the `.asd` file via the resource manager / pack system
3. Sets buffer pointers: start pointer → `this+28`, current position → `this+36`
4. Sets start time sentinel to `-1000000.0`

The loaded buffer is stored as a flat `float*` array. Events are accessed by offset: `buffer + 6 * event_index`.

## Relationship to MRB

| Format | What it provides | Storage |
|--------|-----------------|---------|
| `.mrb` | Skeletal bone animation keyframes (position, rotation, scale tracks) | Chunk type 3 in chunk container, loaded into `dxMotion` |
| `.asd` | Timeline events (sounds, speed changes, motion triggers) | Flat event array, loaded into `CAIModel + 1892` |

Both files are loaded for the same model. The MRB handles the actual bone animation; the ASD handles the event timeline that triggers during playback.

## File Relationships

```
{item,chara}/.../attr/{name}.vra       — model description
{item,chara}/.../attr/{name}.dxg       — geometry

{item,chara}/.../anim/{name}_MOVE.MRB  — skeletal animation
{item,chara}/.../anim/{name}.asd       — animation state events
```

## Key Client Functions

| Function | Line | Description |
|----------|------|-------------|
| `sub_6DA250` | 532006 | **ASD file loader** — reads file, sets up buffer pointers |
| `sub_6D9C00` | 531609 | ASD seek forward to target time |
| `sub_6DA180` | 531952 | ASD seek range + process events in time window |
| `sub_6D9DB0` | 531728 | **ASD event dispatcher** — 13 event types (switch statement) |
| `sub_6DB570` | 533110 | Build MRB/ASD paths, load both for a model |
