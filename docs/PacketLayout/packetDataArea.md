# Area Server

## Ping (PingRequest / PingResponse)

- **Server:** Area (also Auth, Msg)
- **Direction:** ClientToServer / ServerToClient (same packet type both ways)
- **Packet ID (hex):** 0xC202
- **Packet ID (int):** 49666
- **Packet Size:** 4
- **Description:** Keep-alive / latency check; client and server echo a timestamp.

**Layout:**

```text
    UInt {Time}
```

## send_enter_areasv (AreasvEnterRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x4646
- **Packet ID (int):** 17990
- **Packet Size:** 24 (4 + 20)
- **Description:** Client enters the area server with user ID and OTP.

**Layout:**

```text
    UInt {UserID}
    FixedString(20, ASCII) {OTP}
```

## recv_enter_areasv_r (AreasvEnterResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x0149
- **Packet ID (int):** 329
- **Packet Size:** 8
- **Description:** Result of area enter and assigned object ID.

**Layout:**

```text
    UInt {Result}
    UInt {ObjID}
```

## send_leave_areasv (AreasvLeaveRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xF7B9
- **Packet ID (int):** 63417
- **Packet Size:** 0
- **Description:** Client requests to leave the area server.

**Layout:**

```text
    (empty)
```

## recv_leave_areasv_r (AreasvLeaveResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xE31D
- **Packet ID (int):** 58141
- **Packet Size:** 4
- **Description:** Result of area leave.

**Layout:**

```text
    UInt {Result}
```

## send_move_avatar (AvatarMoveRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x9483
- **Packet ID (int):** 38019
- **Packet Size:** 28 (2 × 14)
- **Description:** Client sends avatar movement (two MovementData entries).

**Layout:**

```text
    2 × (Float {X}, Float {Y}, Float {Z}, SByte {Rotation}, Byte {Animation})  // MovementData, 14 bytes each
```

## recv_notify_move_chara (AvatarNotifyMove)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xAADB
- **Packet ID (int):** 43739
- **Packet Size:** 22 (4 + 4 + 14)
- **Description:** Server notifies about an avatar’s movement (result, avatar ID, movement data).

**Layout:**

```text
    UInt {Result}
    UInt {AvatarId}
    Float {X}, Float {Y}, Float {Z}, SByte {Rotation}, Byte {Animation}  // MovementData
```

## send_get_ai_upload_rate (AiUploadRateGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xE30D
- **Packet ID (int):** 58125
- **Packet Size:** 0
- **Description:** Request AI upload rate info.

**Layout:**

```text
    (empty)
```

## recv_get_ai_upload_rate_r (AiUploadRateGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB2BC
- **Packet ID (int):** 45756
- **Packet Size:** 4
- **Description:** AI upload rate result.

**Layout:**

```text
    UInt {Result}
```

## send_get_ai_download_list (AiDownloadListGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x1D3F
- **Packet ID (int):** 7487
- **Packet Size:** 0
- **Description:** Request AI download list.

**Layout:**

```text
    (empty)
```

## recv_get_ai_download_list_r (AiDownloadListGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xBEE1
- **Packet ID (int):** 48865
- **Packet Size:** 8
- **Description:** AI download list result and count.

**Layout:**

```text
    UInt {Result}
    UInt {Downs}
```

## send_get_emotion_base_list (EmotionGetBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x7FCD
- **Packet ID (int):** 32717
- **Packet Size:** 0
- **Description:** Request base emotion list.

**Layout:**

```text
    (empty)
```

## recv_get_emotion_base_list_r (EmotionGetBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x28E3
- **Packet ID (int):** 10467
- **Packet Size:** 8
- **Description:** Base emotion list result and array length placeholder.

**Layout:**

```text
    UInt {Result}
    UInt {ArrayLength}  // 0 in current impl
```

## send_get_obtained_emotion_list (EmotionGetObtainedListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xFD42
- **Packet ID (int):** 64834
- **Packet Size:** 0
- **Description:** Request obtained emotion list.

**Layout:**

```text
    (empty)
```

## recv_get_obtained_emotion_list_r (EmotionGetObtainedListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xC3D7
- **Packet ID (int):** 50135
- **Packet Size:** 8
- **Description:** Obtained emotion list result.

**Layout:**

```text
    UInt {Result}
    UInt {EmotionIds}  // count or placeholder, 0 in current impl
```

## send_get_equip_order_list (EquipOrderListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xF74C
- **Packet ID (int):** 63308
- **Packet Size:** 0
- **Description:** Request equip order list.

**Layout:**

```text
    (empty)
```

## recv_get_equip_order_list_r (EquipOrderListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2DAE
- **Packet ID (int):** 11694
- **Packet Size:** 12
- **Description:** Equip order list (result, chara_order, job_order placeholders).

**Layout:**

```text
    UInt {Result}
    UInt {CharaOrder}
    UInt {JobOrder}
```

## send_get_friend_list_data (FriendGetListDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x805F
- **Packet ID (int):** 32863
- **Packet Size:** 0
- **Description:** Request friend list data.

**Layout:**

```text
    (empty)
```

## recv_get_friend_list_data_r (FriendGetListDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2411
- **Packet ID (int):** 9233
- **Packet Size:** 16
- **Description:** Friend list result and placeholders (friend_data, already_in, comment).

**Layout:**

```text
    UInt {Result}
    UInt {FriendData}
    UInt {AlreadyIn}
    UInt {Comment}
```

## send_get_friend_link_tag_data (FriendLinkTagGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0F97
- **Packet ID (int):** 3991
- **Packet Size:** 0
- **Description:** Request friend link tag data.

**Layout:**

```text
    (empty)
```

## recv_get_friend_link_tag_data_r (FriendLinkTagGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x239E
- **Packet ID (int):** 9118
- **Packet Size:** 24
- **Description:** Friend link tag result and placeholders.

**Layout:**

```text
    UInt {Result}
    UInt {AvatarId}
    UInt {TagData}
    UInt {Slot}
    UInt {QuestionnaireTagData}
    UInt {QuestionnaireSlot}
```

## send_get_furniture_base_list (FurnitureGetBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2FDA
- **Packet ID (int):** 12250
- **Packet Size:** 0
- **Description:** Request furniture base list.

**Layout:**

```text
    (empty)
```

## recv_get_furniture_base_list_r (FurnitureGetBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xA0D1
- **Packet ID (int):** 41169
- **Packet Size:** 8 + Count×12 (Count ≤ 300)
- **Description:** Furniture placement base list (g_pFurnitureMaybe). PlacementFlags selects the floor, wall, and ceiling placement tabs and snapping behavior; it is separate from furniture.csv アクション / click routing. The observed client does not read Type.

**Layout:**

```text
    UInt {Result}
    UInt {Count}
    Count × {
        UInt {ItemId}
        UInt {PlacementFlags: Floor=0x08, Wall=0x10, Ceiling=0x20}
        UInt {Type}
    }
```

## send_heroine_ticket_get_base (HeroineGetTicketBaseRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x25CE
- **Packet ID (int):** 9678
- **Packet Size:** 0
- **Description:** Request heroine ticket base.

**Layout:**

```text
    (empty)
```

## recv_heroine_ticket_get_base_r (HeroineGetTicketBaseResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x16E6
- **Packet ID (int):** 5862
- **Packet Size:** 4
- **Description:** Heroine ticket base (count or placeholder).

**Layout:**

```text
    UInt {HeroineTickets}
```

## send_get_item_list (ItemGetListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2A9A
- **Packet ID (int):** 10906
- **Packet Size:** 0
- **Description:** Request item list.

**Layout:**

```text
    (empty)
```

## recv_get_item_list_r (ItemGetListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xA522
- **Packet ID (int):** 42274
- **Packet Size:** 4
- **Description:** Item list result.

**Layout:**

```text
    UInt {Result}
```

## send_enter_map_data_request_end (MapDataEnterEndRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x04B4
- **Packet ID (int):** 1204
- **Packet Size:** 0
- **Description:** Notify map data enter end.

**Layout:**

```text
    (empty)
```

## recv_enter_map_data_request_end_r (MapDataEnterEndResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xBE02
- **Packet ID (int):** 48642
- **Packet Size:** 4
- **Description:** Map data enter end result.

**Layout:**

```text
    UInt {Result}
```

## send_enter_map (MapEnterRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2810
- **Packet ID (int):** 10256
- **Packet Size:** 8
- **Description:** Client trigger packet sent after entering a maplink volume. The decompiled client sends the **current** source map/channel here; the server is expected to resolve the touched maplink and then push `recv_notify_change_map` with the actual destination route.

**Layout:**

```text
    UInt {MapId}
    UInt {ChannelId}
```

**Decompiled behavior note:** `sub_790530` calls `CProtoArea_client::send_enter_map(GetMapId(), sub_6D76D0())`, so this packet is not the destination handoff by itself.

## recv_enter_map_r (MapEnterResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x1DCD
- **Packet ID (int):** 7629
- **Packet Size:** 4
- **Description:** Map enter result.

**Layout:**

```text
    UInt {Result}
```

## send_get_maplink_data (MapLinkGetDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x30C8
- **Packet ID (int):** 12488
- **Packet Size:** 8
- **Description:** Request map link data.

**Layout:**

```text
    UInt {MapId}
    UInt {ChannelId}
```

## recv_get_maplink_data_r (MapLinkGetDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x6C4E
- **Packet ID (int):** 27726
- **Packet Size:** 4
- **Description:** Map link data result.

**Layout:**

```text
    UInt {Result}
```

## recv_notify_maplink_data (MapLinkNotifyData)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x5755
- **Packet ID (int):** 22357
- **Packet Size:** 25 (4 + 21)
- **Description:** Server pushes a maplink to the client (result + position, yaw, half-extents).

**Layout (decompiled ReadMapLinKData order, 21 bytes after Result):**

```text
    UInt {Result}           // 4 bytes
    Float {PositionX}       // float_0
    Float {PositionY}       // float_4
    Float {PositionZ}       // float_8
    Byte {Yaw}              // 1 byte
    Float {HalfExtent1}     // float_10 — length of the maplink
    Float {HalfExtent2}     // float_14 — other extent (purpose unclear)
```

Client uses both in CAIProtoArea_vtbl__func_40 to size the trigger volume. HalfExtent1 is the maplink length; HalfExtent2 purpose is unclear from the decompiled callback.

**How the client knows where a maplink goes:** The maplink packet does **not** contain a destination map ID. The client gets destinations from **recv_notify_select_map** (see below). The client matches maplinks to destinations by **index**: the first maplink corresponds to the first entry in the select_map list, the second to the second, etc. So to tell the client where each maplink goes: send **recv_notify_select_map** with one entry per maplink, in the same order as the maplinks you sent. When the player enters a maplink trigger, the client uses the matching select_map entry (map ID, server info, etc.) to perform the map change (e.g. send_enter_map or channel select).

## recv_notify_select_map (NotifySelectMap)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x68A5
- **Packet ID (int):** 26789
- **Description:** Sends a list of map entries the client can use for map links / channel select. Order must match the order of maplinks sent via recv_notify_maplink_data so that maplink index N uses the Nth entry in this list as its destination.

**Layout (decompiled):**

- `sub_796C10(this, 15)` — minimum payload size 15 bytes.
- `ReadUint32` → **Count** (1–4).
- For each of Count entries, `sub_7987D0` reads one **select_map_t** from the packet: **109 bytes** per entry (4-byte map id, 97 bytes, then two 4-byte fields). The in-memory struct is 28 DWORDs (112 bytes); the packet representation is 109 bytes.
- Current emulation uses the following decompiled-backed direct-travel shape for each entry:
  - `UInt {MapId}`
  - `UShort {AreaServerPort}`
  - `Ascii[65] {AreaServerIp}` (fixed 65-byte field, no extra terminator byte)
  - `UInt {ChannelId}`
  - `UInt {RouteMapId}` (currently same value as `MapId`)
  - `UInt {MapSerialId}` (currently same value as `MapId`)
  - `UInt {RouteState}` (currently `0`)
  - `Float {SpawnX}`
  - `Float {SpawnY}`
  - `Float {SpawnZ}`
  - `Byte {Yaw}`
  - `Byte {Animation}` (currently `0`)
  - `UInt {Unknown1}` (currently `0`)
  - `UInt {Unknown2}` (currently `0`)

**Usage:** Send this after (or with) maplink data so the client knows which map each maplink leads to. Count and order must match your maplinks.

## recv_notify_change_map (NotifyChangeMap)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB315
- **Packet ID (int):** 45845
- **Packet Size:** 99
- **Description:** Server-driven area transition packet. After the client triggers `send_enter_map`, the server resolves the touched link and sends this route payload with the real destination map, spawn point, server info, and fade flag.

**Layout (decompiled-backed):**

```text
    UInt  {ChannelId}
    UInt  {MapId}
    UInt  {MapSerialId}
    UInt  {RouteState}
    Float {SpawnX}
    Float {SpawnY}
    Float {SpawnZ}
    SByte {Rotation}
    Byte  {Animation}
    Byte  {Flag}
    UShort {AreaServerPort}
    Ascii[65] {AreaServerIp}
    Byte  {FadeFlag}
```

## recv_notify_change_map_failed (NotifyChangeMapFailed)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x59A5
- **Packet ID (int):** 22949
- **Packet Size:** 4
- **Description:** Map change failure result.

**Layout:**

```text
    UInt {Result}
```

## send_get_mascot_count (MascotGetCountRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0CBC
- **Packet ID (int):** 3260
- **Packet Size:** 0
- **Description:** Request mascot count.

**Layout:**

```text
    (empty)
```

## recv_get_mascot_count_r (MascotGetCountResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x7790
- **Packet ID (int):** 30608
- **Packet Size:** 16
- **Description:** Mascot count result and placeholders.

**Layout:**

```text
    UInt {Result}
    UInt {Count}
    UInt {SerialId}
    UInt {Name}
```

## send_get_money_data (MoneyDataGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x61E7
- **Packet ID (int):** 25063
- **Packet Size:** 0
- **Description:** Request money data.

**Layout:**

```text
    (empty)
```

## recv_get_money_data_r (MoneyDataGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xDC19
- **Packet ID (int):** 56345
- **Packet Size:** 4
- **Description:** Money data result.

**Layout:**

```text
    UInt {Result}
```

## send_get_mission_data (MissionDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x7D29
- **Packet ID (int):** 32041
- **Packet Size:** 0
- **Description:** Request mission data.

**Layout:**

```text
    (empty)
```

## recv_get_mission_data_r (MissionDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x47F9
- **Packet ID (int):** 18425
- **Packet Size:** 4
- **Description:** Mission data result.

**Layout:**

```text
    UInt {Result}
```

## send_get_myroom_furniture (MyRoomGetFurnitureRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xE868
- **Packet ID (int):** 59496
- **Packet Size:** 0
- **Description:** Request my room furniture list.

**Layout:**

```text
    (empty)
```

## recv_get_myroom_furniture_r (MyRoomGetFurnitureResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x943D
- **Packet ID (int):** 37949
- **Packet Size:** 4
- **Description:** My room furniture result.

**Layout:**

```text
    UInt {Result}
```

## send_get_niconi_commons_base_list (NiconiCommonsBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x97B7
- **Packet ID (int):** 38839
- **Packet Size:** 0
- **Description:** Request Niconi commons base list.

**Layout:**

```text
    (empty)
```

## recv_get_niconi_commons_base_list_r (NiconiCommonsBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xE60C
- **Packet ID (int):** 58892
- **Packet Size:** 8
- **Description:** Niconi commons base list result.

**Layout:**

```text
    UInt {Result}
    UInt {CommonsBase}
```

## send_get_monster_data (NpcGetDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x461B
- **Packet ID (int):** 17947
- **Packet Size:** 0
- **Description:** Request NPC/monster data.

**Layout:**

```text
    (empty)
```

## recv_get_monster_data_r (NpcGetDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x4403
- **Packet ID (int):** 17411
- **Packet Size:** 4
- **Description:** NPC data result.

**Layout:**

```text
    UInt {Result}
```

## send_get_robo_list (RoboGetListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x44CE
- **Packet ID (int):** 17614
- **Packet Size:** 0
- **Description:** Request robo list.

**Layout:**

```text
    (empty)
```

## recv_get_robo_list_r (RoboGetListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xF606
- **Packet ID (int):** 62982
- **Packet Size:** 8
- **Description:** Robo list result and count.

**Layout:**

```text
    UInt {Result}
    UInt {RoboCount}
```

## send_robo_voice_type_update (RoboVoiceTypeUpdateRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x9305
- **Packet ID (int):** 37637
- **Packet Size:** 0
- **Description:** Request robo voice type update.

**Layout:**

```text
    (empty)
```

## recv_robo_voice_type_update_r (RoboVoiceTypeUpdateResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x8F10
- **Packet ID (int):** 36624
- **Packet Size:** 5 (4 + 1)
- **Description:** Robo voice type update result.

**Layout:**

```text
    UInt {Result}
    Byte {VoiceType}
```

## send_get_timezone (TimeZoneGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x5F53
- **Packet ID (int):** 24403
- **Packet Size:** 0
- **Description:** Request timezone info.

**Layout:**

```text
    (empty)
```

## recv_get_timezone_r (TimeZoneGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xCD38
- **Packet ID (int):** 52536
- **Packet Size:** 17 (4 + 4 + 4 + 4 + 1)
- **Description:** Timezone, time, max and flag.

**Layout:**

```text
    UInt {Result}
    UInt {Timezone}
    UInt {Time}
    UInt {TimeZoneMax}
    Byte {Flag}
```

## send_get_ucc_adv_figure_base_list (UccAdvFigureBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x86DD
- **Packet ID (int):** 34525
- **Packet Size:** 0
- **Description:** Request UCC advance figure base list.

**Layout:**

```text
    (empty)
```

## recv_get_ucc_adv_figure_base_list_r (UccAdvFigureBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x878A
- **Packet ID (int):** 34698
- **Packet Size:** 8
- **Description:** UCC advance figure base list result.

**Layout:**

```text
    UInt {Result}
    UInt {AdvFigures}
```

## send_get_ucc_voice_base_list (UccVoiceBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x1149
- **Packet ID (int):** 4425
- **Packet Size:** 0
- **Description:** Request UCC voice base list.

**Layout:**

```text
    (empty)
```

## recv_get_ucc_voice_base_list_r (UccVoiceBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xBB8F
- **Packet ID (int):** 48015
- **Packet Size:** 8
- **Description:** UCC voice base list result.

**Layout:**

```text
    UInt {Result}
    UInt {VoiceData}
```

## send_update_option (UpdateOptionRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x79A1
- **Packet ID (int):** 31137
- **Packet Size:** 0
- **Description:** Request option update.

**Layout:**

```text
    (empty)
```

## recv_update_option_r (UpdateOptionResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB314
- **Packet ID (int):** 45844
- **Packet Size:** 4
- **Description:** Option update result.

**Layout:**

```text
    UInt {Result}  // 1 = success in current impl
```

---

## send_item_discard (ItemDiscardRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xED61
- **Packet ID (int):** 60769
- **Packet Size:** 6
- **Description:** Bag 捨てる option. Sent by IF::CItemWindow (vtable slot 95) with the stack's serial and the count chosen in the quantity dialog. Opcode read from `CProtoArea_client::send_item_discard` (`mov word [buf], 0xED61`); the client declares it as `send_item_discard(serialid, num)`.

**Layout:**

```text
    UInt {SerialId}
    UShort {Num}
```

## recv_item_discard_r (ItemDiscardResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2546
- **Packet ID (int):** 9542
- **Packet Size:** 4
- **Description:** Result of send_item_discard. Client dispatcher `cmp eax,0x2546`; handler (CAIProtoArea slot 98 → 0x48CC50) shows UI message 0x149 when result is 0 and does nothing otherwise. The bag is **not** changed by this packet — send recv_item_update_num (copies remain) or recv_item_delete (stack gone) before it.

**Layout:**

```text
    UInt {Result}
```

## recv_item_discard_sum_r

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x6EE1
- **Packet Size:** 4
- **Description:** Declared in the client proto (`recv_item_discard_sum_r(result)`, RPC id 0x73) but the game handler only logs it and there is no matching send in this client build. Not sent by the emulator. Note: `PacketType.CloseAiPowerWindowResponse` currently claims 0x6EE1 too; the client dispatcher routes 0x6EE1 here.

**Layout:**

```text
    UInt {Result}
```

## recv_item_update_num (ItemUpdateNumNotify)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x05F8
- **Packet ID (int):** 1528
- **Packet Size:** 10
- **Description:** Sets the count of a stack in a place (bag = place 0). Client (CAIProtoArea slot 92 → 0x794C80) calls the item table's SetCount(serial, num, place) and refreshes the item window. Use recv_item_delete when the count reaches 0 so the record leaves the owned-item list.

**Layout:**

```text
    UInt {Place}
    UInt {SerialId}
    UShort {Num}
```

## send_trashbox_open (TrashboxOpenRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xF41F
- **Packet Size:** 0
- **Description:** Opens the bin. Sent from the item window's bin button (IF::CItemWindow slot 93) and from the battle result window's drop-parts message 0x82500010.

**Layout:**

```text
    (empty)
```

## recv_trashbox_open_r (TrashboxOpenResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x770E
- **Packet Size:** 4
- **Description:** Result 0 switches the item window into bin mode (mode 3); any other value shows an error and leaves the bag as it was.

**Layout:**

```text
    UInt {Result}
```

## send_trashbox_discard_item (TrashboxDiscardItemRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xB18E
- **Packet Size:** Variable, max 68 (4 + 10×4 + 4 + 10×2)
- **Description:** Confirm in bin mode. Every stack dropped into the bin, as two counted arrays filled from the same selection list (counts are equal, at most 10). The client does not touch its item table; the server must answer with recv_item_update_num / recv_item_delete per stack, then recv_trashbox_discard_item_r.

**Layout:**

```text
    UInt {SerialCount}
    UInt {SerialId}[SerialCount]
    UInt {NumCount}
    UShort {Num}[NumCount]
```

## recv_trashbox_discard_item_r (TrashboxDiscardItemResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xBBEB
- **Packet Size:** 4
- **Description:** Result of the bin discard. While the window is in bin mode the client answers this with send_trashbox_close regardless of result.

**Layout:**

```text
    UInt {Result}
```

## send_trashbox_close (TrashboxCloseRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x6A3A
- **Packet Size:** 0
- **Description:** Sent after recv_trashbox_discard_item_r, or by the window's close message (0x82040001) in bin mode.

**Layout:**

```text
    (empty)
```

## recv_trashbox_close_r (TrashboxCloseResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x9ABE
- **Packet Size:** 4
- **Description:** Tears the bin window down (0x495970); result is not inspected. The client dispatcher matches `cmp eax,0x9ABE` — the emulator previously sent 0x9A7E, which no client handler receives.

**Layout:**

```text
    UInt {Result}
```
