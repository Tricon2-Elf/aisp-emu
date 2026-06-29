# AISpace VRA Model Format (`.vra`)

## Overview

VRA is a **text-based, Shift-JIS-encoded model description format**. It acts as a "model manifest" — it describes structure (meshes, materials, textures, physics, animation, effects) while referencing separate binary `.dxg` files for actual geometry data and `.dds` files for texture data.

The client loads models via `CAIModel::LoadVRA(filename)` (`aisp-decompiled.c:533535`), which reads the file via `fopen`/`fread` (line 763532), parses the text into a tree, then dispatches each section to the appropriate subsystem.

Typical file paths:
```
chara/2/02201/attr/202201_0_00_000.vra     — character base physics
chara/8/80069/attr/880069_1_00_000.vra     — character equipment
item/4/09/00000/attr/40900000_1_0.vra      — item model
fx/07/EP_07_000_00.vra                     — particle effect
fx/07/attr/EZ_07_000_00.vra                — effect sub-model (referenced by EP)
tools/validespace/attr/validespace.vra     — tool/editor model
world/object/20/attr/OZ_20_ks_board03A.vra — world prop
world/field/M01/M01_01.vra                 — field heightmap/chip map
world/field/M01/M01_01_obj.vra             — field object placements
world/field/M01/M01_01_fx.vra              — field FX placements
world/field/M01/M01_01_ocld.vra            — field occlusion data
equipment/04/001/attr/EZ_04_001_0_000.vra  — equipment model
```

---

## Syntax

### Tree Structure

VRA uses `{ }` blocks to create a named node tree:

```vra
{
    root_node
    {
        child_node
        {
            grandchild
        }
    }
    {
        sibling_node
    }
}
```

- `{ block_name` opens a new child node with the given name
- `}` closes the current node and returns to its parent
- Indentation is for readability only — the parser ignores it
- **Empty blocks** (e.g., `{geometry\n}`) are valid and signal "no meshes in this VRA" — the actual DXG reference is in `attribute`

### Properties

Properties are key-value pairs within a block:

```
name type[count] value1, value2, ...
```

The array count `[count]` is optional — if omitted, a single value is parsed. Values can span multiple lines; the parser accumulates until a closing `)` is found.

**Example:**
```vra
(pos vector3[1] 1.0, 2.0, 3.0)
(collisionIndex int16[4] 
    11, 12, 7, 5)
```

### Comments

`#` starts a comment that continues to the end of the line (line 800429-800433).

### Delimiters

Whitespace, commas, and newlines separate tokens. Periods (`.`) in float values are preserved.

---

## Type Keywords

The parser (`sub_819DC0`, line 775277) dispatches based on the type string:

| Keyword   | C Type             | Bytes per element | Example |
|-----------|--------------------|-------------------|---------|
| `int8`    | signed byte        | 1                 | `int8[3] 1, -2, 127` |
| `int16`   | signed short       | 2                 | `int16[2] 100, 200` |
| `int32`   | signed int         | 4                 | `int32[1] 99999` |
| `float`   | float              | 4                 | `float[3] 1.0, 2.5, -3.0` |
| `vector2` | float[2]           | 8                 | `vector2[2] 0,1, 2,3` |
| `vector3` | float[3]           | 12                | `vector3[1] 1.0, 2.0, 3.0` |
| `vector4` | float[4]           | 16                | `vector4[1] 1,2,3,4` |
| `quat`    | float[4] (quat)    | 16                | `quat[1] 0,0,0,1` |
| `matrix`  | float[16] (4×4)    | 64                | `matrix[1] 1,0,0,0, 0,1,...` |
| `string`  | text               | variable          | `string[1] "hello"` |

### Type Parser Functions

| Type      | Parser function | Line     |
|-----------|----------------|----------|
| `int8`    | `sub_819250`   | 774759   |
| `int16`   | `sub_819340`   | 774800   |
| `int32`   | `sub_819430`   | 774841   |
| `float`   | `sub_819520`   | 774882   |
| `string`  | `sub_819600`   | 774917   |
| `vector2` | `sub_8196D0`   | 774945   |
| `vector3` | `sub_8197B0`   | 774988   |
| `vector4` | `sub_819890`   | 775031   |
| `quat`    | `sub_819970`   | 775074   |
| `matrix`  | `sub_819A50`   | 775117   |

### Tokenizer

The tokenizer (`sub_838860`, line 800372) produces token types:

| Token | Character | Meaning |
|-------|-----------|---------|
| 4 | `{` | Open block |
| 5 | `}` | Close block |
| 6 | `(` | Open property |
| 7 | `)` | Close property |
| 8 | `[` | Open array count |
| 9 | `]` | Close array count |
| 1 | digits | Integer literal |
| 2 | `0x...` | Hex literal |
| 3 | float | Float literal |
| -1 | EOF | End of buffer |

### Node Tree (dxVRANode)

The parsed output is a `dxVRANode` tree (`sub_81A090`, line 775386). Each node has:
- A **name** (the key before the value)
- Zero or more **child nodes** (sub-blocks)
- Zero or more **typed properties** (key-value pairs with type metadata)

**Node push** (`sub_819C90`, line 775235): `{ name` → creates a child node, pushes it onto the stack as the current parent.

**Node pop** (`sub_819BD0`, line 775200): `}` → pops back to the parent node.

### Runtime Property Lookup

- **`sub_82B570`** (line 789600): Searches a node's children by name, optionally filtering by type ID. Returns the matching child node or null. Used pervasively to read named properties from blocks.
- **`sub_80F570`** (line ~808900): Looks up a top-level section by name from a parsed VRA root node. Used to find top-level blocks like `"geometry"`, `"material_set"`, `"attribute"`, `"Models"`, `"fx_objects"`, `"Version"`, `"world"`, `"occluder_set"`, etc.
- **`sub_73AF90`** (line 606638): Alternative lookup used during model section dispatch in `dxModel_vtbl__func_47`.
- **`sub_82B610`** (line 789635): Same as `sub_82B570` but filters by type ID.

---

## Filesystem Layout & VRA Types

The game organizes VRA files into distinct categories, each with its own expected top-level sections:

### Model VRAs (`geometry` + `material_set` + `attribute`)

Used for: items, tools, simple world objects, equipment, FX sub-models.

**Typical sections:** `geometry` (with `mesh` sub-blocks), `material_set` (with `material` sub-blocks), `attribute` (with `Geometry` property pointing to a `.dxg` file).

**Real file example** (`tools/validespace/attr/validespace.vra`):
```vra
{geometry
    {mesh
        (name string[1] "01")
        {lod0
            (material_id int32[1] 0)
        }
    }
}
{material_set
    {material
        (id int32[1] 0)
        {texture
            {diffuse
                (fname string[1] "tools\\validespace\\tex\\10500060_0_0_0.dds")
            }
        }
        {param_block
            (twoSided int32[1] 1)
            (ambient vector3[1] 0.588000, 0.588000, 0.588000)
            (diffuse vector3[1] 0.588000, 0.588000, 0.588000)
            (glossiness float[1] 0.100000)
            (fx_fname string[1] "shader/pandora.fx")
            (fx_technique string[1] "Blend")
            (fog_enable int32[1] 1)
            ...
        }
    }
}
{attribute
    (Geometry string[1] "tools\\validespace\\model\\01.dxg")
}
```

### Character Physics VRAs (`geometry` empty + `attribute` + `dynamics_set`)

Used for: character body part physics (collision capsules, cloth/hair dynamics).

These VRAs have an **empty** `geometry` block (`{geometry\n}`) — the actual geometry reference is in `attribute`. The `dynamics_set` block contains the collision and dynamics data. Characters typically have one base VRA (`_0_00_000.vra`) with `dynamics_set` and multiple equipment VRAs (`_1_XX_000.vra`) that may also have physics.

**Real file example** (`chara/2/02201/attr/202201_0_00_000.vra`):
```vra
{geometry
}
{attribute
    (Geometry string[1] "chara\\2\\01201\\model\\201201_0_00_000.dxg")
}
{dynamics_set
    {collision                         ← capsule collision body
        (id int32[1] 0)
        (name string[1] "DAtr_Lcalf")
        (HalfLength float[1] 25.000000)
        (Radius float[1] 5.000000)
        (FricCoef float[1] 0.300000)
        (pos vector3[1] 23.708565, -0.269755, -1.502332)
        (rot quat[1] -0.492065, 0.509194, 0.497060, 0.501522)
        (link string[1] "Bip01 L Calf")
    }
    {dynamics                          ← cloth/hair dynamics group
        (id int32[1] 0)
        (BlendRate float[1] 0.000000)
        {joint                         ← physics joint (bone-linked particle)
            (id int32[1] 0)
            (Mass float[1] 2.000000)
            (Radius float[1] 3.937008)
            (AirFricCoef float[1] 0.000000)
            (StabilityCoef float[1] 2.000000)
            (RestitutionCoef float[1] 0.000000)
            (BlendRate float[1] 0.000000)
            (Fix int8[1] 1)
            (link string[1] "BoneKim_01")
        }
        {connection                    ← spring connection between joints
            (id int32[1] 0)
            (connectType int8[1] 0)
            (parentJointID int16[1] 0)
            (childJointID int16[1] 1)
            (Radius float[1] 8.637008)
            (FricCoef float[1] 0.050000)
            (SpringCoef float[1] 0.300000)
            (collisionIndex int16[4] 11, 12, 7, 5)
        }
    }
}
```

### FX VRAs (`Version` + `Target` + `Header` + `Plug` tree)

Used for: particle effects, animated sprites, effect sequences.

FX VRAs have a completely different structure from model VRAs. They define a hierarchy of "plugs" (emitters) with animation tracks for position, color, scale, and object references.

**Real file example** (`fx/07/EP_07_000_00.vra`):
```vra
{Version (major int16[1] 1) (minor int16[1] 0)}
{Target                                    ← effect attachment
    (type int32[1] 1)                      ← target type
    (extend_visible int8[1] 1)             ← inherit visibility
    (extend_pos int8[1] 1)                 ← inherit position
    (extend_rot int8[1] 1)                 ← inherit rotation
    (extend_scale int8[1] 1)               ← inherit scale
    (center_pos int8[1] 0)
    (find_bone int8[1] 0)
    (find_socket int8[1] 1)
    (owner_name string[1] "S60")           ← socket/bone name
    (goal_name string[1] "")
    (move_frame float[1] 0.000000)
    (move_vel float[1] 0.000000)
    (move_acl float[1] 0.000000)
}
{Header                                    ← animation timing
    (frame int16[1] 120)                   ← total frames
    (loop int8[1] 0)
    (loop_ratio float[1] 0.000000)
    (plug_end_wait int8[1] 1)
}
{Plug                                      ← emitter/plug
    (name string[1] "Cyalume")
    (start float[1] 0.000000)
    (end float[1] 1.000000)
    (loop int8[1] 0)
    {Work}                                 ← empty works = no animation
    {Plug                                  ← child plug
        (name string[1] "Pointer") (start float[1] 0.0) (end float[1] 1.0) (loop int8[1] 0)
        {Object}                           ← no 3D object in this plug
        {Work                              ← animation tracks
            {Color                         ← color keyframes
                (ratio float[3] 0.0, 0.5, 1.0)
                (color vector4[3] ...)
            }
        }
        {Plug                              ← child plug: "pointerA"
            (name string[1] "pointerA") (start float[1] 0.0) (end float[1] 1.0) (loop int8[1] 0)
            {Object
                {Model                     ← sub-model reference
                    (name string[1] "Model")
                    (file string[1] "fx/07/attr/EZ_07_000_00.vra")
                    (billboard_x int8[1] 0)
                    (billboard_y int8[1] 0)
                    (billboard_z int8[1] 0)
                }
            }
            {Work}
        }
    }
    {Plug                                  ← plug: "light_color"
        (name string[1] "light_color")
        {Object
            {Sprite                        ← sprite object
                (name string[1] "Sprite")
                (file string[1] "fx/image/ET_07_000_00.tga")
                (dims vector2[1] 20.0, 20.0)
                (color vector4[1] 1.0, 1.0, 1.0, 1.0)
                (billboard int8[1] 1)
                (blend int32[1] 2)         ← additive blend
                (anim_type int32[1] 0)
                (anim_uv vector2[1] 0.0, 0.0)
            }
        }
        {Work
            {Pos (ratio float[2] 0.0, 1.0) (type int32[1] 0) (pos vector3[1] 0,0,15) ...}
            {Color (ratio float[3] 0.0, 0.5, 1.0) (color vector4[3] ...)}
            {Scale (type int32[1] 1) (ratio float[3] 0.0, 0.5, 1.0) (scale vector3[3] ...)}
        }
    }
}
```

### FX Sub-Model VRAs (same as Model VRAs)

Referenced by FX plugs (the `file` in `{Model}`). These are standard model VRAs with `geometry`, `material_set`, and `attribute`.

**Real file example** (`fx/07/attr/EZ_07_000_00.vra`):
```vra
{geometry {mesh (name string[1] "EM_07_000_00") {lod0 (material_id int32[1] 0)}}}
{material_set {material (id int32[1] 0) {texture {diffuse (fname string[1] "fx\\07\\tex\\ET_07_000_00.dds")}} {param_block ...}}}
{attribute (Geometry string[1] "fx\\07\\model\\EM_07_000_00.dxg")}
```

### Field VRAs (map/terrain)

#### Main Field VRA (`_01.vra`)

Contains terrain chip placement (heightmap tiles) and environment settings.

**Real file example** (`world/field/M01/M01_01.vra`):
```vra
{Version (Major int16[1] 1) (Minor int16[1] 0)}
{Config
    (GridNums int32[1] 50)       ← number of grid cells per axis
    (ChipSize float[1] 2400.0)   ← world-space size of each chip
}
{Env                            ← environment / lighting
    {Fog
        (Enable int32[1] 1)
        (Color vector3[1] 0.745, 0.863, 0.941)
        (FarZ float[1] 50000.0)
        (NearZ float[1] 1000.0)
    }
    {Ambient
        (UpperColor vector3[1] 0.8, 0.784, 0.831)
        (LowerColor vector3[1] 0.031, 0.475, 0.529)
    }
    {Light
        (Dir vector3[1] 0.379, -0.924, -0.042)
        (Diffuse vector3[1] 0.776, 0.776, 0.776)
    }
}
{ChipData                       ← terrain tile placement
    {chip0
        (MdlName string[1] "world\\field\\M01\\attr\\FZ_M01_road01_AAD")
        (Size vector2[1] 1.0, 1.0)        ← scale
        (Cull int32[1] 0)
        (Cell vector2[1] 25.0, 24.0)      ← grid position
        (Dir int32[1] 0)                  ← rotation index (0/1/2/3)
        (Height float[1] 0.0)
        (Flags int32[1] 0)
    }
    ...                                    ← up to chipN for grid cells
}
```

Some field VRAs have a `HeightUnit` parameter in `Config` (e.g., `P24_01.vra` has `(HeightUnit int32[1] 5)`).

#### Field Chip List VRA (`_list.vra`)

Simplified index of terrain chip model names, used for preloading.

**Real file example** (`M01_01_list.vra`):
```vra
{ChipData
    (Size int32[1] 21)
    (Name string[21] "world\\field\\M01\\attr\\FZ_M01_road01_AAD", ...)
    (CellSize vector2[21] 1.0, 1.0, ...)
    {Images
        (View string[21] "", ...)         ← per-chip view image (empty = none)
        (Height string[21] "", ...)       ← per-chip heightmap image
        (Move string[21] "", ...)         ← per-chip move/flow image
    }
}
```

#### Field Object VRA (`_obj.vra`)

Places world objects (trees, buildings, props) in the field.

**Real file example** (`M01_01_obj.vra`):
```vra
{Version (major int32[1] 1) (minor int32[1] 1)}
{Models
    {object
        (name string[1] "OZ_00_ma_cherry01A")    ← model name
        (category string[1] "00")                 ← object category folder
        (renderid int32[1] 11)                    ← render group/priority
        (pos vector3[1] 11040.0, 23.3262, -4977.92) ← world position
        (rot vector4[1] 0.0, 0.173648, 0.0, 0.984808) ← rotation (axis-angle)
        (scale vector3[1] 1.0, 1.0, 1.0)
        (physics int8[1] 0)                       ← has collision physics
        (shadow int8[1] 0)                        ← receives shadows
        (castshadow int8[1] 1)                    ← casts shadows
    }
    ...
}
```

#### Field Far Object VRA (`_far.vra`)

Same format as `_obj.vra` (`{Version, Models, object...}`), but contains distant/low-detail objects. Used for LOD.

#### Field FX VRA (`_fx.vra`)

Places particle effects in the field.

**Real file example** (`M01_01_fx.vra`):
```vra
{Version (major int32[1] 1) (minor int32[1] 0)}
{fx_objects
    {fx
        (id string[1] "03_001_00")        ← FX ID (category_sub_type)
        (pos vector3[1] -993.516, 64.4763, -830.474)
        (rot vector4[1] 0.0, 0.0, 0.0, 1.0)
        (scale vector3[1] 1.0, 1.0, 1.0)
        (dispcull_off int32[1] 1)          ← optional: disable distance culling
    }
    ...
}
```

#### Field Link VRA (`_link.vra`)

Cross-references to other VRAs, used for map streaming.

**Real file example** (`P24_01_link.vra`):
```vra
{object (fname string[1] "C:\\headlock\\AQI_grp\\data\\world\\field\\P02\\P02_01_obj.vra")}
{farobj (fname string[1] "C:\\headlock\\AQI_grp\\data\\world\\field\\P02\\P02_01_far.vra")}
{fx (fname string[1] "")}
{sky (fname string[1] "C:\\headlock\\AQI_grp\\data\\world\\sky\\P02\\attr\\SZ_P02_se_sky02A.vra")}
{filter (fname string[1] "") (type int32[1] 0) (alpha int32[1] 255)}
```

#### Field Occlusion VRA (`_ocld.vra`)

Defines occluder quads for visibility culling.

**Real file example** (`M01_01_ocld.vra`):
```vra
{header (version int16[2] 0, 2)}
{geometry}                       ← empty
{occluder_set
    {occluder
        (pos vector3[4]          ← 4 corners of a quad
            -6813.53, -900.0, -944.73,
            -6813.53, 900.0, -944.74,
            -11527.66, -900.0, 3769.62,
            -11527.65, 900.0, 3769.62)
    }
    ...
}
```

#### Field Terrain Chip VRA (`FZ_*.vra`)

Individual terrain tile models. These are standard model VRAs with `geometry`, `material_set`, `attribute`. Contain multi-layered terrain texture information. Can have `Images` sub-blocks with `Height` and `Move` texture references (used for terrain blending/flow maps).

---

## Complete Section / Block Reference

All known sections, alphabetically. Each section includes: description, parent blocks it appears in, what it contains, the client function that processes it, and what parameters are read.

---

### `Ambient`

**Parent:** `Env` (field VRAs)

**Contains:** Two `vector3` properties controlling ambient sky/ground color (hemisphere lighting).

**Properties read by client:**
| Property | Type | Description |
|----------|------|-------------|
| `UpperColor` | `vector3[1]` | Sky hemisphere ambient color |
| `LowerColor` | `vector3[1]` | Ground hemisphere ambient color |

**Real example:** See `Env` section above.

---

### `attribute`

**Parent:** VRA root (model VRAs, equipment VRAs)

**Contains:** References to external binary resources. The client reads three named properties via `sub_82B610` (line 769347-769371):

| Property   | Type | Description |
|------------|------|-------------|
| `Geometry`  | `string[1]` | Path to `.dxg` binary geometry file (e.g., `"chara\\2\\01201\\model\\201201_0_00_000.dxg"`) |
| `Material`  | `string[1]` | Optional: CSV of material replacement paths |
| `Motion`    | `string[1]` | Optional: motion/animation file path |

**Client processing** (line 769347-769371):
- `Geometry`: Resolves path, loads `.dxg` via `sub_82F330`, optionally calls `sub_82B8F0` for tangent generation.
- `Material`: Resolves path, loads material overrides via `dxModel_vtbl__func_73`.
- `Motion`: Resolves path, loads motion/animation data via `dxModel_vtbl__func_91`.

**Real example:**
```vra
{attribute
    (Geometry string[1] "tools\\validespace\\model\\01.dxg")
}
```

---

### `bb` / `bounding`

**Not present** in any examined real VRA files or decompiled code. Bounding volumes are likely computed from geometry at load time rather than stored in VRAs.

---

### `ChipData`

**Parent:** VRA root (field VRAs)

**Contains:** `chip0` .. `chipN` sub-blocks for terrain tile placement. In `_list.vra` files, contains `Size`, `Name`, `CellSize` arrays and an `Images` sub-block.

**Client processing** (line 617560+): Reads chip data to build terrain grid.

**Properties per chip:**
| Property | Type | Description |
|----------|------|-------------|
| `MdlName` | `string[1]` | Path to terrain chip VRA (without `.vra` extension) |
| `Size` | `vector2[1]` | Scale factors (X, Y) |
| `Cull` | `int32[1]` | Culling flag |
| `Cell` | `vector2[N]` | Grid cell positions (pairs of floats) |
| `Dir` | `int32[N]` | Rotation direction index per cell |
| `Height` | `float[N]` | Height offset per cell |
| `Flags` | `int32[N]` | Per-cell flags |

**Chip list (`_list.vra`) variant:**
| Property | Type | Description |
|----------|------|-------------|
| `Size` | `int32[1]` | Number of chips |
| `Name` | `string[N]` | Array of chip model paths |
| `CellSize` | `vector2[N]` | Array of chip dimensions |

**`Images` sub-block (in `_list.vra`):**
| Property | Type | Description |
|----------|------|-------------|
| `View` | `string[N]` | Per-chip view/color texture |
| `Height` | `string[N]` | Per-chip heightmap texture |
| `Move` | `string[N]` | Per-chip flow/animation texture |

---

### `chip0` .. `chipN`

**Parent:** `ChipData` (field VRAs)

**Contains:** Individual terrain tile instance data. See `ChipData` for properties.

---

### `collision`

**Parent:** `dynamics_set`

**Contains:** Capsule collision body for character hitbox/hurtbox. Processed by `cls_809B80::LoadVRA` (line 761590+).

**Properties read by client:**
| Property | Type | Description |
|----------|------|-------------|
| `id` | `int32[1]` | Collision body index |
| `name` | `string[1]` | Display name (e.g., "DAtr_Lcalf") |
| `HalfLength` | `float[1]` | Capsule half-length |
| `Radius` | `float[1]` | Capsule radius |
| `FricCoef` | `float[1]` | Friction coefficient |
| `pos` | `vector3[1]` | Local-space position |
| `rot` | `quat[1]` | Local-space rotation (quaternion) |
| `link` | `string[1]` | Skeleton bone name to attach to (e.g., "Bip01 L Calf") |

**Client uses:** Creates a `dxJointCollision` object, sets properties, and links to skeleton bone via `cls_809B80::GetParameterIdx`.

---

### `Config`

**Parent:** VRA root (field VRAs)

**Contains:** Field/map grid configuration.

| Property | Type | Description |
|----------|------|-------------|
| `GridNums` | `int32[1]` | Number of grid cells per axis |
| `ChipSize` | `float[1]` | World-space size of each terrain chip |
| `HeightUnit` | `int32[1]` | (optional) Height scale unit |

---

### `connection`

**Parent:** `dynamics`

**Contains:** Spring connection between two physics joints in a dynamics group. Processed at line 761774+.

**Properties read by client:**
| Property | Type | Description |
|----------|------|-------------|
| `id` | `int32[1]` | Connection index |
| `connectType` | `int8[1]` | Connection type (0 = linear, 1 = angular/bend) |
| `parentJointID` | `int16[1]` | Parent joint index |
| `childJointID` | `int16[1]` | Child joint index |
| `Radius` | `float[1]` | Connection constraint radius |
| `FricCoef` | `float[1]` | Friction coefficient |
| `SpringCoef` | `float[1]` | Spring stiffness coefficient |
| `collisionIndex` | `int16[N]` | Indices of collision bodies that can interact |

---

### `diffuse`

**Parent:** `texture`

**Contains:** Single property pointing to the diffuse/albedo texture `.dds` file.

| Property | Type | Description |
|----------|------|-------------|
| `fname` | `string[1]` | Path to `.dds` texture file |

**Real example:**
```vra
{diffuse
    (fname string[1] "item\\4\\09\\00000\\tex\\40900000_1_0_0.dds")
}
```

---

### `dynamics`

**Parent:** `dynamics_set`

**Contains:** A dynamics group (cloth/hair simulation). Contains `joint` sub-blocks and `connection` sub-blocks. Processed at line 761664+.

**Properties read by client:**
| Property | Type | Description |
|----------|------|-------------|
| `id` | `int32[1]` | Dynamics group index |
| `BlendRate` | `float[1]` | Animation-to-physics blend rate |

**Structure:**
```vra
{dynamics
    (id int32[1] 0)
    (BlendRate float[1] 0.0)
    {joint ...}          ← multiple joints
    {connection ...}     ← multiple connections
}
```

---

### `dynamics_set`

**Parent:** VRA root (character physics VRAs)

**Contains:** All physics data for a character model. Contains `collision` sub-blocks (capsule hitboxes) and `dynamics` sub-blocks (cloth/hair groups).

**Client processing:** `dxModel_vtbl__func_77` (line 768490) looks up `dynamics_set` and calls `sub_807810` to process collision and dynamics data. The main processing function `cls_809B80::LoadVRA` starts at line 761560+.

**Real example:** See Character Physics VRAs section above.

---

### `emissive`

**Parent:** `param_block`

Material emissive color (self-illumination). Not commonly used — default is black.

| Property | Type | Description |
|----------|------|-------------|
| `emissive` | `vector3[1]` | Emissive color RGB |

**Client processing** (`sub_820B30`, line 781905): Reads emissive value into material at offset 28.

---

### `Env`

**Parent:** VRA root (field VRAs)

**Contains:** Environment/lighting settings. Sub-blocks: `Fog`, `Ambient`, `Light`.

---

### `farobj`

**Parent:** VRA root (field `_link.vra`)

**Contains:** Cross-reference to a far-object VRA file.

| Property | Type | Description |
|----------|------|-------------|
| `fname` | `string[1]` | Absolute path to `_far.vra` file |

---

### `filter`

**Parent:** VRA root (field `_link.vra`)

**Contains:** Post-processing filter settings.

| Property | Type | Description |
|----------|------|-------------|
| `fname` | `string[1]` | Filter texture path (empty = none) |
| `type` | `int32[1]` | Filter type ID |
| `alpha` | `int32[1]` | Filter alpha (0-255) |

---

### `Fog`

**Parent:** `Env` (field VRAs)

**Contains:** Fog/atmosphere settings.

| Property | Type | Description |
|----------|------|-------------|
| `Enable` | `int32[1]` | Fog enabled (1 = on) |
| `Color` | `vector3[1]` | Fog color RGB |
| `FarZ` | `float[1]` | Far fog distance |
| `NearZ` | `float[1]` | Near fog distance |

---

### `fx`

**As top-level block in `_link.vra`:** Cross-reference to FX placement file.
| Property | Type | Description |
|----------|------|-------------|
| `fname` | `string[1]` | Path to `_fx.vra` file |

**As child of `fx_objects`:** Individual particle effect instance placement.
| Property | Type | Description |
|----------|------|-------------|
| `id` | `string[1]` | FX identifier (e.g., "03_001_00") |
| `pos` | `vector3[1]` | World position |
| `rot` | `vector4[1]` | Rotation (axis-angle or quaternion) |
| `scale` | `vector3[1]` | Scale |
| `on_time` | `float[1]` | (optional) Auto-on time |
| `off_time` | `float[1]` | (optional) Auto-off time |
| `dispcull_off` | `int32[1]` | (optional) Disable distance culling |

**Client processing** (line 616631+): Parses fx_objects, creates `CHLFxObject`, loads the corresponding `fx/XX/EP_XX_XX.vra`, and sets position/rotation/scale.

---

### `fx_objects`

**Parent:** VRA root (field `_fx.vra`)

**Contains:** `fx` sub-blocks for effect placement in a field. Processed at line 616631.

---

### `geometry`

**Parent:** VRA root (model VRAs, terrain chip VRAs, occlusion VRAs)

**Contains:** `mesh` sub-blocks. May be **empty** (`{geometry\n}`), meaning no meshes are defined here and the actual geometry reference is in `attribute`.

**Client processing** (`dxModel_vtbl__func_74`, line 768358): Checks for `geometry` and `material_set` blocks; calls vtable function 300 to bind geometry to materials.

**In `dxModel_vtbl__func_75`** (line 769433): Iterates meshes by name, matches each `lod0` level to a material by `material_id`, and calls `sub_811A90` to load DXG geometry.

**Real example with meshes:**
```vra
{geometry
    {mesh
        (name string[1] "OZ_20_ks_board03A_00")
        {lod0
            (material_id int32[1] 0)
        }
    }
    {mesh
        (name string[1] "OZ_20_ks_board03A_01")
        {lod0
            (material_id int32[1] 1)
        }
    }
}
```

---

### `Header`

**Parent:** VRA root (FX VRAs)

**Contains:** FX animation timing parameters.

| Property | Type | Description |
|----------|------|-------------|
| `frame` | `int16[1]` | Total animation frames |
| `loop` | `int8[1]` | Loop flag (0 = once, 1 = loop) |
| `loop_ratio` | `float[1]` | Loop blend ratio |
| `plug_end_wait` | `int8[1]` | Wait for plug end flag |

---

### `header`

**Parent:** VRA root (occlusion VRAs `_ocld.vra`)

**Contains:** Occlusion data version.

| Property | Type | Description |
|----------|------|-------------|
| `version` | `int16[2]` | Version (0, 2 observed) |

---

### `joint`

**Parent:** `dynamics`

**Contains:** Physics joint (particle) within a dynamics group. Represents a simulated point mass linked to a skeleton bone. Processed at line 761705+.

**Properties read by client:**
| Property | Type | Description |
|----------|------|-------------|
| `id` | `int32[1]` | Joint index within its dynamics group |
| `Mass` | `float[1]` | Point mass weight |
| `Radius` | `float[1]` | Joint collision radius |
| `AirFricCoef` | `float[1]` | Air friction (drag) coefficient |
| `StabilityCoef` | `float[1]` | Stability/rigidity coefficient |
| `RestitutionCoef` | `float[1]` | Bounce restitution |
| `BlendRate` | `float[1]` | Animation-to-physics blend |
| `Fix` | `int8[1]` | Fixed joint (1 = follows bone exactly, 0 = simulated) |
| `link` | `string[1]` | Skeleton bone name to attach to |

---

### `Light`

**Parent:** `Env` (field VRAs)

**Contains:** Directional sun light settings.

| Property | Type | Description |
|----------|------|-------------|
| `Dir` | `vector3[1]` | Sun direction vector (normalized) |
| `Diffuse` | `vector3[1]` | Sun color RGB |

---

### `LightID`

**Property:** Read via `sub_73AF90` in `dxModel_vtbl__func_47` (line 768462). Associates a light ID with model parts. Not a block — appears as a top-level property in some VRAs.

---

### `lod0`

**Parent:** `mesh`

**Contains:** Level-of-detail 0 (highest detail) binding. Associates the mesh with a material ID.

| Property | Type | Description |
|----------|------|-------------|
| `material_id` | `int32[1]` | Index into `material_set` materials |

**Client processing** (line 769498+): `sprintf("lod%d", lod_level)` to find the LOD sub-block, then reads `material_id` to match the mesh to a material by `id`.

**Equipment VRAs** can have a `param_block` inside `lod0`:
```vra
{lod0
    (material_id int32[1] 0)
    {param_block}
}
```

---

### `material`

**Parent:** `material_set`

**Contains:** A single material definition with an `id` property, a `texture` sub-block, and a `param_block` sub-block for shader/render state parameters.

| Property | Type | Description |
|----------|------|-------------|
| `id` | `int32[1]` | Material index (referenced by `material_id` in `lod0`) |

---

### `material_set`

**Parent:** VRA root (model VRAs)

**Contains:** `material` sub-blocks. Each material defines textures and shader parameters.

**Client processing:** 
- `dxModel_vtbl__func_74` (line 768358) checks for `material_set` existence.
- Material processing function at line 769373 iterates `material_set` children and calls `sub_821590` for each material.
- **Texture loading** (line 781847+): For each material, looks up `texture` sub-block, reads `diffuse/fname`, and loads `.dds` via `dxBuffer_vtbl__func_3`.
- **Shader parameters** (line 781899+): Reads `param_block` and applies all material properties.

---

### `mesh`

**Parent:** `geometry`

**Contains:** A named mesh with LOD levels. Multiple meshes in `geometry` represent sub-meshes of a single model.

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string[1]` | Mesh name (e.g., "OZ_20_ks_board03A_00") |

**Client processing** (line 769470+): Matches mesh by `name` to entries in the geometry data, then for each LOD level matches `material_id` to materials in `material_set`.

---

### `Model` (FX sub-block)

**Parent:** `Object` (in FX VRA `Plug.Object`)

**Contains:** Reference to a sub-model (.vra file) for use as a particle.

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string[1]` | Display name |
| `file` | `string[1]` | Path to `.vra` file |
| `billboard_x` | `int8[1]` | Billboard lock X axis |
| `billboard_y` | `int8[1]` | Billboard lock Y axis |
| `billboard_z` | `int8[1]` | Billboard lock Z axis |

---

### `Models`

**Parent:** VRA root (field `_obj.vra` / `_far.vra`)

**Contains:** `object` sub-blocks for world prop placement.

**Client processing** (line 617153): Looks up `"Models"` via `sub_80F570`, then for each child, calls `sub_747BE0` to process each object placement.

---

### `Motion`

**Property:** Read via `sub_73AF90` in `dxModel_vtbl__func_47` (line 768443) and via `sub_82B610` in `attribute` processing (line 769365). When present in `attribute`, loads motion/animation data. Not a standalone block in VRAs.

---

### `object`

**As child of `Models`** (field object VRA): A world object placement.

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string[1]` | Object model name (e.g., "OZ_00_ma_cherry01A") |
| `category` | `string[1]` | Object category folder (e.g., "00") |
| `renderid` | `int32[1]` | Render group/priority |
| `pos` | `vector3[1]` | World position |
| `rot` | `vector4[1]` | Rotation (axis-angle: x,y,z,w) |
| `scale` | `vector3[1]` | Scale |
| `physics` | `int8[1]` | Has physics collision (0/1) |
| `shadow` | `int8[1]` | Receives shadows (0/1) |
| `castshadow` | `int8[1]` | Casts shadows (0/1) |

**Client processing** (line 615953+): Constructs `.vra` path as `world/object/<category>/attr/<name>.vra`, then sets position, rotation, scale, and physics/shadow flags.

**As class_type in world/room VRAs** (line 789332): An entity type in world chunks, along with `model`, `camera`, `light`, `worldmodel`.

**As link VRA block** (`_link.vra`): Cross-reference to object placement file.
| Property | Type | Description |
|----------|------|-------------|
| `fname` | `string[1]` | Path to `_obj.vra` |

---

### `occluder`

**Parent:** `occluder_set`

**Contains:** A single occlusion quad (4 corner positions).

| Property | Type | Description |
|----------|------|-------------|
| `pos` | `vector3[4]` | Four 3D positions defining a quad |

---

### `occluder_set`

**Parent:** VRA root (occlusion VRAs)

**Contains:** `occluder` sub-blocks. Processed at line 808896.

---

### `Object` (FX block)

**Parent:** `Plug` (in FX VRAs)

**Contains:** Visual object definition for a plug (either `Model` or `Sprite`). May be empty (`{Object\n}`), meaning the plug has no visual.

---

### `param_block`

**Parent:** `material` or `lod0` (equipment VRAs)

**Contains:** Material shader parameters and render states. Processed at line 781514 (equipment variant) and line 781899 (standard variant).

**Properties read by client** (line 781902-781933):

| Property | Type | Read by | Offset | Description |
|----------|------|---------|--------|-------------|
| `draw_priority` | `int32[1]` | `sub_820BC0` (int) | `this[0]` | Render draw order |
| `ambient` | `vector3[1]` | `sub_820B30` (float3) | `this[1-3]` | Ambient color |
| `diffuse` | `vector3[1]` | `sub_820B30` (float3) | `this[4-6]` | Diffuse color |
| `emissive` | `vector3[1]` | `sub_820B30` (float3) | `this[7-9]` | Emissive color |
| `specular` | `vector3[1]` | `sub_820B30` (float3) | `this[10-12]` | Specular color |
| `glossiness` | `float[1]` | `sub_820B60` (float) | `this[13]` | Gloss/shininess (multiplied by 100) |
| `opacity` | `float[1]` | `sub_820B60` (float) | `this[14]` | Opacity (0 = transparent, 1 = opaque) |
| `fx_fname` | `string[1]` | `sub_82B570` → `sub_821DD0` | - | Effect/shader file (e.g., `"shader/pandora.fx"`) |
| `fx_technique` | `string[1]` | `sub_82B570` → `sub_821A80` | - | Shader technique name (e.g., `"Blend", "BlendColor", "RigidColorTex"`) |
| `fog_enable` | `int32[1]` | `sub_820B90` (bool) | `this[20]` | Enable fog |
| `alpha_test_enable` | `int32[1]` | `sub_820B90` (bool) | `this[15]` | Enable alpha test |
| `alpha_ref` | `int32[1]` | `sub_820BC0` (int) | `this[62]` (byte) | Alpha reference value |
| `alpha_func` | `string[1]` | `sub_820BF0` (enum) | `this[16]` | Alpha compare function (e.g., `"Greater"`) |
| `alpha_blend_enable` | `int32[1]` | `sub_820B90` (bool) | `this[17]` | Enable alpha blending |
| `dest_blend` | `string[1]` | `sub_820BF0` (enum) | `this[18]` | Destination blend mode (e.g., `"Zero"`, `"One"`, `"InvSrcAlpha"`) |
| `src_blend` | `string[1]` | `sub_820BF0` (enum) | `this[19]` | Source blend mode (e.g., `"One"`, `"SrcAlpha"`) |
| `twoSided` | `int32[1]` | `sub_820B90` (bool) | `this[21]` | Double-sided rendering |
| `u_add` | `float[1]` | `sub_820B60` (float) | `this[51]` | U texture coordinate offset (scroll) |
| `v_add` | `float[1]` | `sub_820B60` (float) | `this[52]` | V texture coordinate offset (scroll) |

**Blend mode enum values** (`off_99DF48`, line 781928): `"Zero"`, `"One"`, `"SrcColor"`, `"InvSrcColor"`, `"SrcAlpha"`, `"InvSrcAlpha"`, `"DestAlpha"`, `"InvDestAlpha"`, `"DestColor"`, `"InvDestColor"`, `"SrcAlphaSat"`, `"BothSrcAlpha"`, `"BothInvSrcAlpha"`, `"BlendFactor"`, `"InvBlendFactor"`.

**Alpha func enum values** (`off_99DF08`, line 781926): `"Never"`, `"Less"`, `"Equal"`, `"LessEqual"`, `"Greater"`, `"NotEqual"`, `"GreaterEqual"`, `"Always"`.

---

### `PhysicsGeom`

**Property:** Read via `sub_73AF90` in `dxModel_vtbl__func_47` (line 768428). Loads physics collision geometry. Not a block — read as a property.

---

### `Plug`

**Parent:** VRA root or parent `Plug` (FX VRAs)

**Contains:** A particle emitter / animation plug. Can contain child `Plug` nodes (forming a tree), `Object`, and `Work` sub-blocks.

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string[1]` | Plug/emitter name |
| `start` | `float[1]` | Start time (normalized 0-1) |
| `end` | `float[1]` | End time (normalized 0-1) |
| `loop` | `int8[1]` | Loop flag |

---

### `Pos` / `Color` / `Scale`

**Parent:** `Work` (in FX VRAs)

Animation keyframe tracks for position, color, and scale over time. Each has `ratio` (keyframe times) and corresponding value arrays.

**`Pos` track:**
| Property | Type | Description |
|----------|------|-------------|
| `ratio` | `float[N]` | Keyframe time ratios |
| `type` | `int32[1]` | Interpolation type |
| `pos` | `vector3[1]` | Base position |
| `pos_rnd` | `vector3[1]` | Random position offset range |

**`Color` track:**
| Property | Type | Description |
|----------|------|-------------|
| `ratio` | `float[N]` | Keyframe time ratios |
| `color` | `vector4[N]` | RGBA values at each keyframe |

**`Scale` track:**
| Property | Type | Description |
|----------|------|-------------|
| `type` | `int32[1]` | Scale type |
| `ratio` | `float[N]` | Keyframe time ratios |
| `scale` | `vector3[N]` | Scale values at each keyframe |
| `scale_rnd` | `vector3[N]` | Random scale offset range |

---

### `sky`

**Parent:** VRA root (field `_link.vra`)

**Contains:** Cross-reference to sky model VRA.

| Property | Type | Description |
|----------|------|-------------|
| `fname` | `string[1]` | Path to sky VRA file |

---

### `sound` / `audio`

**Not present** in any examined real VRA files. Sound data is stored in separate dedicated files, not in VRAs.

---

### `Sprite`

**Parent:** `Object` (in FX VRAs)

| Property | Type | Description |
|----------|------|-------------|
| `name` | `string[1]` | Display name |
| `file` | `string[1]` | Path to sprite image (`.tga` or similar) |
| `dims` | `vector2[1]` | Sprite dimensions (width, height) |
| `color` | `vector4[1]` | Tint color RGBA |
| `billboard` | `int8[1]` | Billboard flag |
| `base_x` | `int8[1]` | Base alignment X |
| `base_y` | `int8[1]` | Base alignment Y |
| `reverse_x` | `int8[1]` | Reverse/flip X |
| `reverse_y` | `int8[1]` | Reverse/flip Y |
| `blend` | `int32[1]` | Blend mode (2 = additive, 3 = ...) |
| `anim_type` | `int32[1]` | Animation type |
| `anim_uv` | `vector2[1]` | UV animation parameters |

---

### `Target`

**Parent:** VRA root (FX VRAs)

**Contains:** Effect attachment/targeting parameters.

| Property | Type | Description |
|----------|------|-------------|
| `type` | `int32[1]` | Target type (1 = world/socket) |
| `extend_visible` | `int8[1]` | Inherit visibility from parent |
| `extend_pos` | `int8[1]` | Inherit position from parent |
| `extend_rot` | `int8[1]` | Inherit rotation from parent |
| `extend_scale` | `int8[1]` | Inherit scale from parent |
| `center_pos` | `int8[1]` | Center position |
| `find_bone` | `int8[1]` | Search for bone by name |
| `find_socket` | `int8[1]` | Search for socket by name |
| `owner_name` | `string[1]` | Bone/socket name (e.g., "S60") |
| `goal_name` | `string[1]` | Goal/target name |
| `move_frame` | `float[1]` | Movement frame |
| `move_vel` | `float[1]` | Movement velocity |
| `move_acl` | `float[1]` | Movement acceleration |

---

### `texture`

**Parent:** `material`

**Contains:** Texture map definitions. Typically has a `diffuse` sub-block with an `fname` property pointing to a `.dds` file.

**Client processing** (line 781847+): Looks up `texture`, reads `diffuse/fname`, loads `.dds` texture via `dxBuffer_vtbl__func_3`, and binds to the material slot. Supports up to 4 textures per material.

---

### `Version`

**Parent:** VRA root (many VRA types: field VRAs, FX VRAs)

**Contains:** Version metadata.

| Property | Type | Description |
|----------|------|-------------|
| `major` / `Major` | `int16[1]` or `int32[1]` | Major version number |
| `minor` / `Minor` | `int16[1]` or `int32[1]` | Minor version number |

**Client processing** (`sub_745E30`, line 614600): Reads `major` and `minor` from the `Version` block to validate file format. Note: field VRAs use `Major`/`Minor` (capitalized), FX VRAs use `major`/`minor` (lowercase).

---

### `Work`

**Parent:** `Plug` (in FX VRAs)

**Contains:** Animation keyframe tracks (`Pos`, `Color`, `Scale`) for a plug. May be empty (`{Work\n}`), meaning no animation.

---

### `world`

**Parent:** VRA root (world/room VRAs — internal, not in our extracted samples)

**Contains:** Room/chunk entity data. Looked up by `sub_80F570("world", ...)` at line 789278. Entities within use `class_type` to distinguish `model`, `camera`, `light`, `object`, `worldmodel`.

---

### `worldmodel`

**Class type:** entity in world/room VRAs with `class_type` property set to `"worldmodel"` (line 789347). Handles world chunk sub-model loading.

---

## Client Processing Flow

### Main Load Pipeline

```
CAIModel::LoadVRA("chara/2/02201/attr/202201_0_00_000.vra")
    │  (line 533535)
    ├── dxBuffer_vtbl__func_3(filename)
    │   │  fopen("rb") → fread entire file  (line 763532)
    │   └── raw Shift-JIS text buffer
    │
    ├── sub_81A090(text_buffer)
    │   │  (line 775386) — main tree parser
    │   ├── sub_838860()  — tokenizer  (line 800372)
    │   ├── sub_819DC0()  — property parser: match type keyword → parse values  (line 775277)
    │   │   └── sub_819180()  — array count parser [N]  (line 774730)
    │   ├── sub_819C90()  — { → push child node  (line 775235)
    │   └── sub_819BD0()  — } → pop to parent node  (line 775200)
    │
    └── sub_6DB990(model, vra_tree)
        │  (line 533260) — bind VRA tree to model object
        │   calls vtable[156]: model-specific bind
        │   calls ModelManager::Init()
        │   calls sub_7459F0 — register model with manager
        │   calls vtable[296]: dispatch sections
        │   calls vtable[76]: init pass (11)
        │   sets flags, calls sub_80FFB0
        │
        └── (virtual dispatch to dxModel_vtbl__func_74)
            │  (line 768352)
            ├── sub_806CC0("geometry")    → find geometry block
            ├── sub_806CC0("material_set") → find materials block
            └── vtbl[300](geometry, material_set) → bind
                │
                └── dxModel_vtbl__func_75 (line 769433)
                    │  for each mesh name:
                    │    for each LOD level:
                    │      read "material_id" → match to material by "id"
                    │      call sub_811A90 → load DXG
                    │
                    ├── Geometry dispatch (vtbl[188] = dxModel_vtbl__func_47)
                    │   (line 768366)
                    │   ├── "Geometry"    → load .dxg file
                    │   ├── "Material"    → load material override
                    │   ├── "Motion"      → load animation
                    │   ├── "PhysicsGeom" → load physics geometry
                    │   └── "LightID"     → set light association
                    │
                    ├── material_set processing (line 769373)
                    │   └── for each material:
                    │       sub_821590 → process material
                    │       │  (line 781800+)
                    │       ├── sub_80F570("texture") → find texture block
                    │       │   └── for each texture slot (up to 4):
                    │       │       sub_82B570("fname") → get .dds path
                    │       │       dxBuffer_vtbl__func_3 → load .dds to D3D texture
                    │       │       sub_821380 → bind texture to material slot
                    │       │
                    │       └── sub_80F570("param_block") → find params
                    │           ├── "ambient"    → set ambient color
                    │           ├── "diffuse"    → set diffuse color
                    │           ├── "emissive"   → set emissive color
                    │           ├── "specular"   → set specular color
                    │           ├── "glossiness" → set shininess (×100)
                    │           ├── "opacity"    → set alpha
                    │           ├── "fx_fname"   → load .fx shader
                    │           ├── "fx_technique" → select technique
                    │           ├── "fog_enable" → set fog
                    │           ├── "alpha_test_enable" → set alpha test
                    │           ├── "alpha_ref"  → set reference value
                    │           ├── "alpha_func" → set compare function
                    │           ├── "alpha_blend_enable" → set blending
                    │           ├── "dest_blend" → set dest blend mode
                    │           ├── "src_blend"  → set source blend mode
                    │           ├── "twoSided"   → set double-sided
                    │           ├── "u_add"      → set U scroll
                    │           └── "v_add"      → set V scroll
                    │
                    ├── dxModel_vtbl__func_77 (line 768490)
                    │   └── sub_806CC0("dynamics_set")
                    │       └── sub_807810 → process physics
                    │           │  (cls_809B80::LoadVRA, line 761560+)
                    │           ├── for each "collision":
                    │           │   create dxJointCollision capsule
                    │           │   read: id, name, HalfLength, Radius, FricCoef,
                    │           │         pos, rot, link (bone name)
                    │           │
                    │           └── for each "dynamics":
                    │               create dxDynamics group
                    │               read: id, BlendRate
                    │               ├── for each "joint":
                    │               │   create dxJoint particle
                    │               │   read: id, Mass, Radius, AirFricCoef,
                    │               │         RestitutionCoef, StabilityCoef,
                    │               │         BlendRate, Fix, link
                    │               │
                    │               └── for each "connection":
                    │                   create dxJointConnection spring
                    │                   read: id, connectType, parentJointID,
                    │                         childJointID, Radius, FricCoef,
                    │                         SpringCoef, collisionIndex
                    │
                    └── Additional: Models/fx_objects are processed at
                        higher-level map/field loading, not in model load
```

### Field Loading Flow

```
Field loader
├── sub_745E30("Version") → validate version
├── sub_80F570("Config")  → read GridNums, ChipSize
├── sub_80F570("Env")     → read Fog, Ambient, Light
│   ├── Fog: Enable, Color, FarZ, NearZ
│   ├── Ambient: UpperColor, LowerColor
│   └── Light: Dir, Diffuse
│
├── ChipData processing (line 617560+)
│   └── for each chipN:
│       read MdlName, Size, Cull, Cell[N], Dir[N], Height[N], Flags[N]
│
├── Object loading (_obj.vra)
│   └── sub_80F570("Models") → for each object:
│       └── read name, category, renderid, pos, rot, scale,
│            physics, shadow, castshadow
│       └── construct path: world/object/<category>/attr/<name>.vra
│       └── load and place object
│
├── FX loading (_fx.vra)
│   └── sub_80F570("fx_objects") → for each fx:
│       └── read id, pos, rot, scale
│       └── construct path: fx/<category>/EP_<id>.vra
│       └── create CHLFxObject, load FX VRA
│
└── Occlusion loading (_ocld.vra)
    └── sub_80F570("occluder_set") → for each occluder:
        └── read pos[4] → create occluder quad
```

### FX Loading Flow

```
Load FX VRA (fx/07/EP_07_000_00.vra)
├── sub_745E30("Version") → validate version
├── "Target" → read attachment params (type, extend_*, owner_name, etc.)
├── "Header" → read frame count, loop settings
│
└── Recursive Plug tree:
    ├── for each "Plug":
    │   read name, start, end, loop
    │   ├── "Object":
    │   │   ├── "Model"  → read name, file (.vra path), billboard flags
    │   │   └── "Sprite" → read name, file (.tga), dims, color, blend mode
    │   │
    │   └── "Work":
    │       ├── "Pos"   → read ratio[], type, pos, pos_rnd
    │       ├── "Color" → read ratio[], color[] (RGBA keyframes)
    │       └── "Scale" → read type, ratio[], scale[], scale_rnd[]
    │
    └── child "Plug" → recurse
```

---

## File Relationships

```
VRA files reference:
    .dxg  — Binary geometry data (vertex/index buffers)
    .dds  — Texture data (DXT compressed)
    .tga  — Sprite/effect images (FX)
    .fx   — Shader effect files (e.g., "shader/pandora.fx")
    .vra  — Other VRA files (sub-models, FX sub-models, linked fields)

Typical asset paths:
    model:  <category>/<id>/model/<name>.dxg
    texture: <category>/<id>/tex/<name>.dds
    VRA:    <category>/<id>/attr/<name>.vra

Example chara path:
    chara/2/02201/attr/202201_0_00_000.vra
                   → attribute/Geometry: "chara\\2\\01201\\model\\201201_0_00_000.dxg"
                   → (texture is inherited from character base, not in physics VRA)

Example equipment path:
    equipment/04/001/attr/EZ_04_001_0_000.vra
                   → attribute/Geometry: "equipment\\04\\001\\model\\EM_04_001_0_000.dxg"
                   → material_set/material/texture/diffuse/fname: "equipment\\04\\001\\tex\\ET_04_001_0_0000.dds"

Example FX path:
    fx/07/EP_07_000_00.vra
                   → Plug.Object.Model.file: "fx/07/attr/EZ_07_000_00.vra"
                   →  EZ_07_000_00.vra → attribute/Geometry: "fx\\07\\model\\EM_07_000_00.dxg"
                   →  EZ_07_000_00.vra → texture: "fx\\07\\tex\\ET_07_000_00.dds"
                   → Plug.Object.Sprite.file: "fx/image/ET_07_000_00.tga"
```

---

## VRA Sections by File Type

| File Type | Top-Level Sections |
|-----------|-------------------|
| **Model VRA** (item, tool, simple world obj, equipment, FX sub-model) | `geometry`, `material_set`, `attribute` |
| **Character Physics VRA** | `geometry` (empty), `attribute`, `dynamics_set` |
| **Character Equipment VRA** | `geometry`, `material_set`, `attribute` (may also have `dynamics_set`) |
| **FX VRA** (EP_*.vra) | `Version`, `Target`, `Header`, `Plug` (recursive) |
| **Field Main VRA** (_01.vra) | `Version`, `Config`, `Env`, `ChipData` |
| **Field Chip List VRA** (_list.vra) | `ChipData` (size + arrays + `Images`) |
| **Field Object VRA** (_obj.vra, _far.vra) | `Version`, `Models` |
| **Field FX VRA** (_fx.vra) | `Version`, `fx_objects` |
| **Field Link VRA** (_link.vra) | `object`, `farobj`, `fx`, `sky`, `filter` |
| **Field Occlusion VRA** (_ocld.vra) | `header`, `geometry`, `occluder_set` |
| **Terrain Chip VRA** (FZ_*.vra) | `geometry`, `material_set`, `attribute` (may have `Images` for height/move maps) |
