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
- **Description:** Sent unconditionally by the client after every area enter (post-map-enter init). Asks for the author revenue share the original service applied to user-made aiちゅーん (AI tune) sold in the shop.

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
- **Description:** Not a result code. On the original service this was the author's revenue share in percent of the sale price in デレ (the in-game currency). The client stores it in its AI content manager and shows `sale price * RatePercent / 100` as 「1冊あたりの収益」 (revenue per copy) in the aiちゅーん upload window. Served from `Server.AiUploadRatePercent`.

**Layout:**

```text
    UInt {RatePercent}
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
- **Description:** Declared in the client proto (`recv_item_discard_sum_r(result)`, RPC id 0x73) but the game handler only logs it and there is no matching send in this client build. Not sent by the emulator.

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
## recv_close_aipower_window_r (CloseAiPowerWindowResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x6B53
- **Packet Size:** 4
- **Description:** Result of closing the AI power window. The client reads a fixed 4 bytes. Not sent by the emulator. The packet table previously listed it as 0x6EE1, which the client dispatcher routes to recv_item_discard_sum_r.

## send_get_adventure_upload_rate (AdventureUploadRateGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x71CF
- **Packet Size:** 0
- **Description:** Sent unconditionally by the client after every area enter (post-map-enter init), alongside send_get_ai_upload_rate. Asks for the author revenue share the original service applied to user-made drama (adventure) discs sold in the shop.

**Layout:**

```text
    UInt {Result}
    (empty)
```

## recv_get_adventure_upload_rate_r (AdventureUploadRateGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x9061
- **Packet Size:** 4
- **Description:** Not a result code. On the original service this was the author's revenue share in percent of the sale price in デレ (the in-game currency). The client reads a fixed 4 bytes, stores it in its drama content manager and shows `sale price * RatePercent / 100` as 「1冊あたりの収益」 (revenue per copy) in the drama upload window. Served from `Server.AdventureUploadRatePercent`.

**Layout:**

```text
    UInt {RatePercent}
```

## recv_adventure_shop_started (AdventureShopStartedNotify)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x03EA
- **Packet Size:** variable (45 bytes when empty; client buffer 0x294E0)
- **Description:** Pushed after the player talks to the drama disc shop's 販売担当 clerk (はっぴぃ・すとぉりぃ販売). Not the 8-byte shape of upload-started: the client parser (case 0x7BC061) reads a catalog snapshot and never an NPC id. The window sends nothing on open, so this is the only source of its initial lineup (newest first, up to 50), the ranking tab (up to 5, by sales) and the buyer's 購入履歴 (up to 50, sent oldest first because the client inserts each row at the front). The client bails out silently on a short or over-cap body. Item records are 1589 bytes (see the item record layout below); the same record is used by recv_adventure_shop_item and the history push. Seeded on all three island 商店街 maps.

**Layout:**

```text
    ULong  {AllCount}
    String {Word}          // NUL-terminated, max 385 bytes incl. NUL
    UInt   {Filter}
    UInt   {Sort}
    UInt   {Index}
    ULong  {SearchCount}
    UInt   {ItemCount}     // max 50
    Item   {Items}[ItemCount]
    UInt   {RankSort}
    UInt   {RankingCount}  // max 5
    Item + UShort + UInt {Rankings}[RankingCount]
    UInt   {HistoryCount}  // max 50
    Item + Byte + UInt   {Historys}[HistoryCount]   // Byte ignored; UInt = purchase time (Unix seconds)
```

Item record (parser 0x799BC0, 1589 bytes; fixed-width strings carry their NUL inside the field). Meanings follow the client's row builder (0x5C6FF0), detail pane (0x5C3720) and buy check (0x5CF0BD):

```text
    Long      {ScriptId}
    Char[37]  {AuthorName}
    Char[121] {Title}
    Long      {Price}        // checked against the デレ purse and sent back by send_adventure_shop_buy with price type 0
    Long      {PriceAi}      // second (ニコニコポイント) price; shown instead of Price when non-zero. The emulator sends 0
    Char[61]  {Tags}[10]     // only Tags[GenreTagIndex] is read: its text is matched against the client's 10 genre names
    UShort    {TagFlags}     // parsed, never read
    Byte      {GenreTagIndex}
    Char[768] {Comment}
    Byte      {Official}     // 公式配信: copied into the client's download-list entry; the PC library's ribbon tab (verified live)
    Byte      {Reserved}     // never read
    UInt      {UploadedAt}   // Unix seconds; shown as a date on the rows
    UInt      {Reserved}     // stored, never read
    UInt      {Purchases}    // 購入数 on the rows and the detail card
    UInt      {Pages}        // ページ (manuscript sheets); copied into the client's download-list entry
    Long      {ContentBytes} // アップロード容量 on the detail card
```

The ranking rows' trailing UShort + UInt are ignored by the client. The genre names (message ids 0x640FC400-0x640FC409) are 総合, オフィシャル, 学園もの, ラブストーリー, ホラー, サスペンス, SF, ミステリー, テスト, その他; the emulator sends the listing's genre name as Tags[0].

## send_adventure_shop_end (AdventureShopEndRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xB34F
- **Packet Size:** 0
- **Description:** Sent when the drama disc shop window is closed. The client keeps the window open until the reply arrives.

**Layout:**

```text
    (empty)
```

## recv_adventure_shop_end_r (AdventureShopEndResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xC605
- **Packet Size:** 4
- **Description:** Acknowledges the close. On its own it does not close the window; recv_adventure_shop_ended follows.

**Layout:**

```text
    UInt {Result}
```

## recv_adventure_shop_ended (AdventureShopEndedNotify)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xAD2D
- **Packet Size:** 0
- **Description:** Sent right after recv_adventure_shop_end_r; the client tears the drama disc shop window down on this, the same pairing as recv_shop_end_r / recv_shop_ended.

**Layout:**

```text
    (empty)
```

## send_adventure_shop_genre_search (AdventureShopGenreSearchRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x157F
- **Packet Size:** 16
- **Description:** A genre tab click, a sort combo change or a page combo change in the drama disc shop window (wrapper 0x7A80F0). Filter is always 0 in this client; Genre is the tab index: 0 総合 (every genre), then オフィシャル, 学園もの, ラブストーリー, ホラー, サスペンス, SF, ミステリー, テスト, その他; Sort is the combo index 新着順 / ダウンロード数が多い順 / 購入数が多い順; Index is the 0-based page of 50. A sort change resets the page to 0. There is no keyword or tag search sender in the client, and send_adventure_shop_ranking_search (0xD861, one UInt) is dead code: its reply never releases the window.

**Layout:**

```text
    UInt {Genre}   // 0-9
    UInt {Filter}
    UInt {Sort}
    UInt {Index}   // page
```

## recv_adventure_shop_item (AdventureShopItemNotify)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x9B08
- **Packet Size:** variable (client buffer 0x13D0C)
- **Description:** One lineup page (case 0x7DE878). No result field. The client replaces its lineup with the items, derives the page combo from SearchCount (ceil / 50, min 1) and selects Sort and Index in its combos, so the emulator echoes the request. Sent in answer to a genre search, before recv_adventure_shop_genre_search_r.

**Layout:**

```text
    String {Word}         // NUL-terminated, max 385 bytes incl. NUL
    UInt   {Filter}
    UInt   {Sort}
    UInt   {Index}
    ULong  {SearchCount}  // total hits
    UInt   {ItemCount}    // max 50
    Item   {Items}[ItemCount]
```

## recv_adventure_shop_genre_search_r (AdventureShopGenreSearchResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x6DC0
- **Packet Size:** 4
- **Description:** Releases the window's busy state after a genre search (case 0x7D33C3); it refreshes the lineup from the page it holds, so recv_adventure_shop_item has to arrive first. Non-zero shows the error dialog with the code. recv_adventure_shop_tag_search_r (0x53D5), recv_adventure_shop_keyword_search_r (0x0AE8) and recv_adventure_shop_ranking_search_r (0x9EA9: UInt result, UInt count ≤ 5, ranking rows) exist in the client but nothing requests them.

**Layout:**

```text
    UInt {Result}
```

## send_adventure_shop_buy (AdventureShopBuyRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0289
- **Packet Size:** 17
- **Description:** The buy button (wrapper 0x7A7D00). The window sends the item's first price with price type 0 after checking it against the デレ (AI point) purse, and only the second price with type 1 (ニコニコポイント) when the first is 0. It refuses locally when a 購入履歴 entry for the disc is younger than 7 days or the history already holds 50 entries. The emulator accepts type 0 only and requires the price to match the listing.

**Layout:**

```text
    Long {ScriptId}
    Long {Price}
    Byte {PriceType}   // 0 = デレ
```

## recv_adventure_shop_buy_r (AdventureShopBuyResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xFAA8
- **Packet Size:** 4
- **Description:** Releases the window after a purchase (case 0x7F4C0E); 0 shows the completion dialog, non-zero the error dialog with the code. The client never fetches the disc on its own after buying and the reply carries no ticket, so on success the emulator pushes recv_adventure_shop_added_buy_history and recv_money_updated_aipoint, then this acknowledgement; the client then sends send_adventure_shop_download_request on its own (pushing a ticket reply before the acknowledgement made it download twice). Failure codes are the emulator's AdventureBuyOutcome values (1 unknown, 2 not for sale, 3 own listing, 4 bought within 7 days, 5 price mismatch or currency, 6 not enough デレ).

**Layout:**

```text
    UInt {Result}
```

## recv_adventure_shop_added_buy_history (AdventureShopAddedBuyHistoryNotify)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xEEE8
- **Packet Size:** 1594
- **Description:** One 購入履歴 row appended to the client's history (case 0x7F2F8D). The purchase time drives the client's own 7-day rule: it shows purchase + 7 days as the re-download deadline and refuses to buy the disc again before then.

**Layout:**

```text
    Item {Item}
    Byte {Flag}         // ignored
    UInt {PurchasedAt}  // Unix seconds
```

## send_adventure_shop_download_request (AdventureShopDownloadRequestRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x9F15
- **Packet Size:** 8
- **Description:** Re-download of a 購入履歴 entry (wrapper 0x7A7F40).

**Layout:**

```text
    Long {ScriptId}
```

## recv_adventure_shop_download_request_r (AdventureShopDownloadRequestResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x46BC
- **Packet Size:** 53
- **Description:** Fixed 53 bytes (case 0x7CCB89). On result 0 the client POSTs userid / scriptid / ticket to the download host (`/ai-sp/download.php`) synchronously inside the handler and expects the same XML shape as the upload reply, with the actor table in `datalist` and the script in `contents` (CDATA); it packs those two texts into dl/drama/ai{ScriptId}.txt itself (routine 0x4B1D10: "ADV0" header, UTF-16LE payloads, 20-byte jammer), adds the disc to its download list and, if missing, to its purchase history. A non-XML body or an HTTP failure shows 「接続、もしくはデータ解析に失敗しました。」. The client sends the request by itself right after recv_adventure_shop_buy_r. The emulator issues the ticket to buyers and to the author; it is single use and valid for 15 minutes.

**Layout:**

```text
    UInt     {Result}
    Long     {ScriptId}
    Char[41] {Ticket}
```

## send_adventure_upload_delete_request (AdventureUploadDeleteRequestRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xCB22
- **Packet Size:** 8
- **Description:** Taking a disc off sale from the upload window (wrapper 0x7A9460). send_adventure_download_delete_request (0x628C, wrapper 0x7AF480) and send_adventure_shop_remove_buy_history (0x454B, wrapper 0x7A8530) have the same body; send_adventure_shop_remove_all_buy_history (0xB7A0) is empty.

**Layout:**

```text
    Long {ScriptId}
```

## recv_adventure_upload_delete_request_r (AdventureUploadDeleteRequestResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xFEF7
- **Packet Size:** 12
- **Description:** Result 0 makes the client drop the entry from its upload list (case 0x7F61E6). The emulator delists the listing (buyers keep their copies), clears the work's Uploaded flag and then re-sends recv_adventure_upload_started for the 買取 clerk on the player's map: the open window never renders an unsolicited work list (it stores it, and it can release the window's wait early), but the upload-started push runs the window's open sequence, so the client re-requests and rebuilds both lists itself. recv_adventure_download_delete_request_r (0x35CA) and recv_adventure_shop_remove_buy_history_r (0x1915) have the same layout and drop the entry from the download list / history; recv_adventure_shop_remove_all_buy_history_r (0xB736) is a lone UInt result that clears the history. Removed downloads and hidden history rows only hide the purchase; the copy can still be downloaded through the other list.

**Layout:**

```text
    UInt {Result}
    Long {ScriptId}
```

## recv_adventure_upload_started (AdventureUploadStartedNotify)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x90BD
- **Packet Size:** 8
- **Description:** Pushed after the player talks to the drama disc shop's 買取担当 clerk (はっぴぃ・すとぉりぃ買取). The client looks up the NPC object by NpcObjectId (name and position go on the window), opens the drama upload window (ドラマショップ) and then sends get_adventure_work_list and get_adventure_upload_list. Seeded on all three island 商店街 maps.

**Layout:**

```text
    UInt {NpcObjectId}
    UInt {Value}  // second field, not read by the window
```

## send_adventure_upload_end (AdventureUploadEndRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xB592
- **Packet Size:** 0
- **Description:** Sent when the drama upload window is closed. The client keeps the window open until the reply arrives.

**Layout:**

```text
    (empty)
```

## recv_adventure_upload_end_r (AdventureUploadEndResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2562
- **Packet Size:** 4
- **Description:** Acknowledges the close.

**Layout:**

```text
    UInt {Result}
```

## send_adventure_upload_request (AdventureUploadRequestRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x89F8
- **Packet Size:** variable
- **Description:** Sent from the drama upload window's 説明事項 dialog (同意する). Listing metadata only; the manuscript itself is POSTed to upload.php over HTTP after a success reply. The emulator registers a pending listing for the work (script ids start at 10001 so they never collide with the legacy service's discs) and answers with a one-time upload ticket; an unknown work id is refused with result 1.

**Layout:**

```text
    UShort {WorkId}
    String {Title}       // NUL-terminated, max 121
    UInt   {Genre}
    String {Comment}     // NUL-terminated, max 769
    String {AuthorName}  // NUL-terminated, max 37
    Long   {Price}       // デレ
    Byte   {ContentsPublic} // 「ダウンロード時に内容を公開する」: buyers may open the manuscript; logger name publish
    Long   {ContentSize} // byte size of drama_N.csv + datalist_N.txt, the two parts of the HTTP upload
```

## recv_adventure_upload_request_r (AdventureUploadRequestResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xF857
- **Packet Size:** 55
- **Description:** Fixed 55-byte reply (client case 0x7F41D1). Result 0 makes the client POST the manuscript to the upload host (`/ai-sp/upload.php`, served by the emulator's Kestrel listener; see docs on the proxy for port 80) as multipart form fields userid / scriptid / ticket plus the uccadv and datalist file parts (the command script and the actor table as plain UTF-8 text; download.php returns the same texts and the client packs its own cache file), and then send send_adventure_upload_request_report. Non-zero shows an error dialog. The ticket is 40 characters plus the NUL, single use, valid for 15 minutes. The XML the client accepts back (parser 0x4B0F80, no NULL checks) is a root element with a `status` attribute and text-bearing children: `<result status="ok"><cms>ok</cms><scriptid>N</scriptid><contents>N</contents></result>`, or `<result status="fail"><error><code>N</code><description>…</description></error></result>`. A `<status>` child element instead of the attribute, or a child without text, crashes the client.

**Layout:**

```text
    UInt   {Result}
    UShort {WorkId}
    Long   {ScriptId}
    Char[41] {Ticket}    // one-time token for upload.php
```

## send_adventure_upload_request_report (AdventureUploadRequestReportRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2494
- **Packet Size:** 14
- **Description:** The client's verdict on the HTTP upload: the reply parser's boolean, 1 after 「アップロードに成功しました！」 (verified live; the first guess of 0 = ok was wrong). Sent right after the HTTP call; the client then re-requests the work and upload lists on its own.

**Layout:**

```text
    UInt   {Report}
    UShort {WorkId}
    Long   {ScriptId}
```

## recv_adventure_upload_request_report_r (AdventureUploadRequestReportResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x1F30
- **Packet Size:** 12
- **Description:** Report 1 puts the pending listing on sale, marks the work Uploaded and takes down any older listing of the same work; result 1 means upload.php never stored a manuscript for that script id. Any other report abandons the pending listing so the work can be uploaded again (result 0); the row is kept so its script id is never reused, because the client remembers a failed id and answers a later upload that gets the same one with 「原因不明のエラーが発生しました」 (seen live).

**Layout:**

```text
    UInt {Result}
    Long {ScriptId}
```

## send_get_adventure_upload_list (GetAdventureUploadListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xB6EE
- **Packet Size:** 0
- **Description:** Sent by the drama upload window for the right-hand アップロードドラマ list.

**Layout:**

```text
    (empty)
```

## recv_get_adventure_upload_list_r (GetAdventureUploadListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x49B5
- **Packet Size:** 8 + Count × 0x630
- **Description:** The account's discs currently on sale (case 0x7CD994, parser 0x7998A0). Count is at most 100; records are packed 1574 bytes on the wire (0x630 in memory). The field names after the title follow the upload request's order; the client only stores the record.

**Layout:**

```text
    UInt {Result}
    UInt {Count}
    Record[Count]:
        Long      {ScriptId}
        Char[37]  {AuthorName}
        Char[121] {Title}
        Long      {Price}
        Char[769] {Comment}
        Byte      {ContentsPublic}
        UInt      {Genre}
        Long      {FileSize}
        UInt      {Sales}       // unnamed in the client
        Char[61]  {Tags}[10]
        UInt      {UploadedAt}  // unnamed in the client
```

## send_get_adventure_download_list (GetAdventureDownloadListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x3FE2
- **Packet Size:** 0
- **Description:** Sent by the PC play window for the purchased-disc cache list.

**Layout:**

```text
    (empty)
```

## recv_get_adventure_download_list_r (GetAdventureDownloadListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xA39A
- **Packet Size:** 8 + Count × 17
- **Description:** The discs the account holds copies of (case 0x7DFF50, parser 0x799A80). Count is at most 1000; records are packed 17 bytes. The client never reads the two u32 fields; the u8 clears the PC library's lock (中身を見る) when non-zero, so it carries the listing's contents-public flag. A local entry missing from this list is erased by the client, so every disc the account may keep must be listed. Purchases removed with send_adventure_download_delete_request are left out.

**Layout:**

```text
    UInt {Result}
    UInt {Count}
    Record[Count]:
        Long {ScriptId}
        UInt {PurchasedAt}  // Unix seconds
        UInt {Pages}
        Byte {ContentsPublic} // 1 clears the lock in the PC library, 0 keeps it (verified live)
```

## send_get_adventure_work_list (GetAdventureWorkListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x32C8
- **Packet Size:** 0
- **Description:** Sent by the drama editor (opened from the 鉛筆とノート furniture) and by the drama upload window.

**Layout:**

```text
    (empty)
```

## recv_get_adventure_work_list_r (GetAdventureWorkListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xEA66
- **Packet Size:** 12 + Count × 13
- **Description:** The account's drama works and its 原稿用紙 (manuscript sheet) stock. Records are packed, 13 bytes each, at most 100; the client merges them by WorkId with its local `user/<uid>/<slot>/work/drama/list.csv`. Manuscripts live only on the client; the server keeps the registry.

**Layout:**

```text
    UInt {Result}
    UInt {SheetStock}
    UInt {Count}
    Record[Count]:
        UInt {WorkId}
        UInt {Sheets}
        UInt {Reserved}
        Byte {Uploaded}
```

## send_adventure_work_create (AdventureWorkCreateRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xB1D9
- **Packet Size:** 4
- **Description:** 新規作成 in the drama editor. Sheets is the number of 原稿用紙 to spend on the new work (the editor sends 1).

**Layout:**

```text
    UInt {Sheets}
```

## recv_adventure_work_create_r (AdventureWorkCreateResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x7CD2
- **Packet Size:** 10
- **Description:** On Result 0 the client creates the local files for WorkId (named 新規作成_NNN) and registers it in list.csv. WorkId is per account and must never be reused: the client overwrites whatever it already has under that id. Preceded by recv_adventure_updated_sheet_stack (stores CAdvMgr+0x1BC only; the editor caption does not paint on this recv).

**Layout:**

```text
    UInt {Result}
    UInt {Sheets}
    UShort {WorkId}
```

## send_adventure_work_delete (AdventureWorkDeleteRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2DA5
- **Packet Size:** 2
- **Description:** 削除 in the drama editor.

**Layout:**

```text
    UShort {WorkId}
```

## recv_adventure_work_delete_r (AdventureWorkDeleteResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2083
- **Packet Size:** 6
- **Description:** Removes the work from the local list (CAdvMgr+0x4ACA50) and rebuilds the list. Does not write sheet stock or paint the 原稿用紙 caption. Preceded by recv_adventure_updated_sheet_stack so +0x1BC already has the returned pages.

**Layout:**

```text
    UInt {Result}
    UShort {WorkId}
```

## send_adventure_work_add_sheet (AdventureWorkAddSheetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2FFF
- **Packet Size:** 6
- **Description:** Adds Count sheets from the account stock to a work. The editor sends it on 編集 when the local work has more sheets than the server record.

**Layout:**

```text
    UShort {WorkId}
    UInt {Count}
```

## recv_adventure_work_add_sheet_r (AdventureWorkAddSheetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xCE6A
- **Packet Size:** 10
- **Description:** Delta is the applied count: the client adds it to its local sheet count for WorkId rather than replacing it (`add [work+0x3C], delta` at 0x4A82F9). It does not touch CAdvMgr+0x1BC or +0x1C0 and does not paint the 原稿用紙 caption. Preceded by recv_adventure_updated_sheet_stack so +0x1BC is already the new stock. Adding a page in the editor increments +0x1C0 immediately (caption = 1BC−1C0) and redraws; deleting a page does not replenish stock until save, when sub_sheet returns pages and 0xABE0 writes +0x1BC. The caption stays stale across save and catches up on the next local add/remove. The editor sends add_sheet on save with the pages added since the last save, then sub_sheet with the pages deleted, never netted; the work-list window sends one of them before 編集 when its local page count differs from the server record.

**Layout:**

```text
    UInt {Result}
    UShort {WorkId}
    UInt {Delta}
```

## send_adventure_work_sub_sheet (AdventureWorkSubSheetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x4187
- **Packet Size:** 6
- **Description:** Returns Count sheets from a work to the account stock.

**Layout:**

```text
    UShort {WorkId}
    UInt {Count}
```

## recv_adventure_work_sub_sheet_r (AdventureWorkSubSheetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x203C
- **Packet Size:** 10
- **Description:** Same shape as recv_adventure_work_add_sheet_r.

**Layout:**

```text
    UInt {Result}
    UShort {WorkId}
    UInt {Delta}
```

## recv_adventure_updated_sheet_stack (AdventureUpdatedSheetStackNotify)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xABE0
- **Packet Size:** 4
- **Description:** Writes the account's 原稿用紙 stock to CAdvMgr+0x1BC (`mov [ecx+0x1BC], arg` at 0x4A7B40). Does not paint. The editor caption is `+0x1BC − +0x1C0` (getter 0x4A7DA0), redrawn only on local add/remove sheet. +0x1C0 is the reservation for unsaved new pages.

**Layout:**

```text
    UInt {SheetStock}
```

## send_sheet_shop_start (SheetShopStartRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x46EE
- **Packet Size:** 0
- **Description:** The drama editor's 通販 button (wrapper 0x7A8840). The editor stays open underneath, locked, until the sheet shop window closes.

**Layout:**

```text
    (empty)
```

## recv_sheet_shop_start_r (SheetShopStartResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x6F5C
- **Packet Size:** 12
- **Description:** Result 0 opens the 原稿用紙 shop window with the unit price (case 0x7D3B93). The remaining-sheet label comes from the last recv_adventure_updated_sheet_stack and the デレ balance from the money manager, so nothing else is needed. Served from `Server:AdventureSheetPriceAi`.

**Layout:**

```text
    UInt {Result}
    Long {SheetPriceAi}  // price of one sheet in デレ
```

## send_sheet_shop_buy (SheetShopBuyRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x1E92
- **Packet Size:** 12
- **Description:** The buy button after its confirm dialog (wrapper 0x7A8B00). The window clamps the count so the stock stays below 10000 and refuses a total above the デレ balance itself; the price field is the unit price it displayed, not the total.

**Layout:**

```text
    UInt {SheetCount}
    Long {SheetPriceAi}  // unit price echo
```

## recv_sheet_shop_buy_r (SheetShopBuyResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x92AB
- **Packet Size:** 4
- **Description:** Result only (case 0x7DD0D5). The window redraws its remaining-sheet label, the editor's and the work list's from the stock the last recv_adventure_updated_sheet_stack stored, in the tick this lands, so the emulator sends the stock push and recv_money_updated_aipoint first. Non-zero shows an error dialog (0xFFFFFF83 is the client's own "not enough デレ" code) and keeps the window open.

**Layout:**

```text
    UInt {Result}
```

## send_sheet_shop_end (SheetShopEndRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xAF54
- **Packet Size:** 0
- **Description:** The sheet shop window's close button (wrapper 0x7A89A0).

**Layout:**

```text
    (empty)
```

## recv_sheet_shop_end_r (SheetShopEndResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xAE06
- **Packet Size:** 4
- **Description:** Result 0 closes the window and the editor resumes (case 0x7E272B); unlike the drama disc shop there is no ended push. Non-zero leaves it open.

**Layout:**

```text
    UInt {Result}
```

## send_adventure_start (AdventureStartRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2939
- **Packet Size:** 0
- **Description:** 再生 in the drama editor (or the shop). Nothing on the wire names the scenario's map: before sending, the client parses the scenario's first CHANGEMAP and caches the resolved field in its adventure manager.

**Layout:**

```text
    (empty)
```

## recv_adventure_start_r (AdventureStartResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x7C69
- **Packet Size:** 4
- **Description:** Result 0 starts playback (HUD hidden, fade out). The server must then send recv_notify_change_map to the drama stage map 30000000: the client's transition code special-cases that id and loads its cached field in the visual-novel presentation, so the server never needs the scenario's map. Routing to the real map leaves the world scene under the VN layer and fails the CAM_SET preset check; a plain ack leaves the client waiting, because CHANGEMAP only builds the stage while the current map is 30000000. The client then sends send_enter_map for 30000000.

**Layout:**

```text
    UInt {Result}
```

## send_adventure_end (AdventureEndRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2125
- **Packet Size:** 0
- **Description:** Sent when the scenario finishes or is aborted.

**Layout:**

```text
    (empty)
```

## recv_adventure_end_r (AdventureEndResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xC1D1
- **Packet Size:** 4
- **Description:** Acknowledges the end; the emulator then routes the player back to the map they started from.

**Layout:**

```text
    UInt {Result}
```
