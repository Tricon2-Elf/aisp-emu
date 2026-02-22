
## Area Server

### Ping (PingRequest / PingResponse)

- **Server:** Area (also Auth, Msg)
- **Direction:** ClientToServer / ServerToClient (same packet type both ways)
- **Packet ID (hex):** 0xC202
- **Packet ID (int):** 49666
- **Packet Size:** 4
- **Description:** Keep-alive / latency check; client and server echo a timestamp.

**Layout:**

```
    UInt {Time}
```

### send_enter_areasv (AreasvEnterRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x4646
- **Packet ID (int):** 17990
- **Packet Size:** 24 (4 + 20)
- **Description:** Client enters the area server with user ID and OTP.

**Layout:**

```
    UInt {UserID}
    FixedString(20, ASCII) {OTP}
```

### recv_enter_areasv_r (AreasvEnterResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x0149
- **Packet ID (int):** 329
- **Packet Size:** 8
- **Description:** Result of area enter and assigned object ID.

**Layout:**

```
    UInt {Result}
    UInt {ObjID}
```

### send_leave_areasv (AreasvLeaveRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xF7B9
- **Packet ID (int):** 63417
- **Packet Size:** 0
- **Description:** Client requests to leave the area server.

**Layout:**

```
    (empty)
```

### recv_leave_areasv_r (AreasvLeaveResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xE31D
- **Packet ID (int):** 58141
- **Packet Size:** 4
- **Description:** Result of area leave.

**Layout:**

```
    UInt {Result}
```

### send_move_avatar (AvatarMoveRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x9483
- **Packet ID (int):** 38019
- **Packet Size:** 28 (2 × 14)
- **Description:** Client sends avatar movement (two MovementData entries).

**Layout:**

```
    2 × (Float {X}, Float {Y}, Float {Z}, SByte {Rotation}, Byte {Animation})  // MovementData, 14 bytes each
```

### recv_notify_move_chara (AvatarNotifyMove)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xAADB
- **Packet ID (int):** 43739
- **Packet Size:** 22 (4 + 4 + 14)
- **Description:** Server notifies about an avatar’s movement (result, avatar ID, movement data).

**Layout:**

```
    UInt {Result}
    UInt {AvatarId}
    Float {X}, Float {Y}, Float {Z}, SByte {Rotation}, Byte {Animation}  // MovementData
```

### send_get_ai_upload_rate (AiUploadRateGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xE30D
- **Packet ID (int):** 58125
- **Packet Size:** 0
- **Description:** Request AI upload rate info.

**Layout:**

```
    (empty)
```

### recv_get_ai_upload_rate_r (AiUploadRateGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB2BC
- **Packet ID (int):** 45756
- **Packet Size:** 4
- **Description:** AI upload rate result.

**Layout:**

```
    UInt {Result}
```

### send_get_ai_download_list (AiDownloadListGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x1D3F
- **Packet ID (int):** 7487
- **Packet Size:** 0
- **Description:** Request AI download list.

**Layout:**

```
    (empty)
```

### recv_get_ai_download_list_r (AiDownloadListGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xBEE1
- **Packet ID (int):** 48865
- **Packet Size:** 8
- **Description:** AI download list result and count.

**Layout:**

```
    UInt {Result}
    UInt {Downs}
```

### send_get_emotion_base_list (EmotionGetBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x7FCD
- **Packet ID (int):** 32717
- **Packet Size:** 0
- **Description:** Request base emotion list.

**Layout:**

```
    (empty)
```

### recv_get_emotion_base_list_r (EmotionGetBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x28E3
- **Packet ID (int):** 10467
- **Packet Size:** 8
- **Description:** Base emotion list result and array length placeholder.

**Layout:**

```
    UInt {Result}
    UInt {ArrayLength}  // 0 in current impl
```

### send_get_obtained_emotion_list (EmotionGetObtainedListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xFD42
- **Packet ID (int):** 64834
- **Packet Size:** 0
- **Description:** Request obtained emotion list.

**Layout:**

```
    (empty)
```

### recv_get_obtained_emotion_list_r (EmotionGetObtainedListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xC3D7
- **Packet ID (int):** 50135
- **Packet Size:** 8
- **Description:** Obtained emotion list result.

**Layout:**

```
    UInt {Result}
    UInt {EmotionIds}  // count or placeholder, 0 in current impl
```

### send_get_equip_order_list (EquipOrderListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xF74C
- **Packet ID (int):** 63308
- **Packet Size:** 0
- **Description:** Request equip order list.

**Layout:**

```
    (empty)
```

### recv_get_equip_order_list_r (EquipOrderListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2DAE
- **Packet ID (int):** 11694
- **Packet Size:** 12
- **Description:** Equip order list (result, chara_order, job_order placeholders).

**Layout:**

```
    UInt {Result}
    UInt {CharaOrder}
    UInt {JobOrder}
```

### send_get_friend_list_data (FriendGetListDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x805F
- **Packet ID (int):** 32863
- **Packet Size:** 0
- **Description:** Request friend list data.

**Layout:**

```
    (empty)
```

### recv_get_friend_list_data_r (FriendGetListDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2411
- **Packet ID (int):** 9233
- **Packet Size:** 16
- **Description:** Friend list result and placeholders (friend_data, already_in, comment).

**Layout:**

```
    UInt {Result}
    UInt {FriendData}
    UInt {AlreadyIn}
    UInt {Comment}
```

### send_get_friend_link_tag_data (FriendLinkTagGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0F97
- **Packet ID (int):** 3991
- **Packet Size:** 0
- **Description:** Request friend link tag data.

**Layout:**

```
    (empty)
```

### recv_get_friend_link_tag_data_r (FriendLinkTagGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x239E
- **Packet ID (int):** 9118
- **Packet Size:** 24
- **Description:** Friend link tag result and placeholders.

**Layout:**

```
    UInt {Result}
    UInt {AvatarId}
    UInt {TagData}
    UInt {Slot}
    UInt {QuestionnaireTagData}
    UInt {QuestionnaireSlot}
```

### send_get_furniture_base_list (FurnitureGetBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2FDA
- **Packet ID (int):** 12250
- **Packet Size:** 0
- **Description:** Request furniture base list.

**Layout:**

```
    (empty)
```

### recv_get_furniture_base_list_r (FurnitureGetBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xA0D1
- **Packet ID (int):** 41169
- **Packet Size:** 8
- **Description:** Furniture base list result.

**Layout:**

```
    UInt {Result}
    UInt {Count}
```

### send_heroine_ticket_get_base (HeroineGetTicketBaseRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x25CE
- **Packet ID (int):** 9678
- **Packet Size:** 0
- **Description:** Request heroine ticket base.

**Layout:**

```
    (empty)
```

### recv_heroine_ticket_get_base_r (HeroineGetTicketBaseResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x16E6
- **Packet ID (int):** 5862
- **Packet Size:** 4
- **Description:** Heroine ticket base (count or placeholder).

**Layout:**

```
    UInt {HeroineTickets}
```

### send_get_item_list (ItemGetListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2A9A
- **Packet ID (int):** 10906
- **Packet Size:** 0
- **Description:** Request item list.

**Layout:**

```
    (empty)
```

### recv_get_item_list_r (ItemGetListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xA522
- **Packet ID (int):** 42274
- **Packet Size:** 4
- **Description:** Item list result.

**Layout:**

```
    UInt {Result}
```

### send_enter_map_data_request_end (MapDataEnterEndRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x04B4
- **Packet ID (int):** 1204
- **Packet Size:** 0
- **Description:** Notify map data enter end.

**Layout:**

```
    (empty)
```

### recv_enter_map_data_request_end_r (MapDataEnterEndResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xBE02
- **Packet ID (int):** 48642
- **Packet Size:** 4
- **Description:** Map data enter end result.

**Layout:**

```
    UInt {Result}
```

### send_enter_map (MapEnterRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x2810
- **Packet ID (int):** 10256
- **Packet Size:** 0
- **Description:** Request to enter map.

**Layout:**

```
    (empty)
```

### recv_enter_map_r (MapEnterResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x1DCD
- **Packet ID (int):** 7629
- **Packet Size:** 4
- **Description:** Map enter result.

**Layout:**

```
    UInt {Result}
```

### send_get_maplink_data (MapLinkGetDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x30C8
- **Packet ID (int):** 12488
- **Packet Size:** 0
- **Description:** Request map link data.

**Layout:**

```
    (empty)
```

### recv_get_maplink_data_r (MapLinkGetDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x6C4E
- **Packet ID (int):** 27726
- **Packet Size:** 4
- **Description:** Map link data result.

**Layout:**

```
    UInt {Result}
```

### send_get_mascot_count (MascotGetCountRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0CBC
- **Packet ID (int):** 3260
- **Packet Size:** 0
- **Description:** Request mascot count.

**Layout:**

```
    (empty)
```

### recv_get_mascot_count_r (MascotGetCountResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x7790
- **Packet ID (int):** 30608
- **Packet Size:** 16
- **Description:** Mascot count result and placeholders.

**Layout:**

```
    UInt {Result}
    UInt {Count}
    UInt {SerialId}
    UInt {Name}
```

### send_get_money_data (MoneyDataGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x61E7
- **Packet ID (int):** 25063
- **Packet Size:** 0
- **Description:** Request money data.

**Layout:**

```
    (empty)
```

### recv_get_money_data_r (MoneyDataGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xDC19
- **Packet ID (int):** 56345
- **Packet Size:** 4
- **Description:** Money data result.

**Layout:**

```
    UInt {Result}
```

### send_get_mission_data (MissionDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x7D29
- **Packet ID (int):** 32041
- **Packet Size:** 0
- **Description:** Request mission data.

**Layout:**

```
    (empty)
```

### recv_get_mission_data_r (MissionDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x47F9
- **Packet ID (int):** 18425
- **Packet Size:** 4
- **Description:** Mission data result.

**Layout:**

```
    UInt {Result}
```

### send_get_myroom_furniture (MyRoomGetFurnitureRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xE868
- **Packet ID (int):** 59496
- **Packet Size:** 0
- **Description:** Request my room furniture list.

**Layout:**

```
    (empty)
```

### recv_get_myroom_furniture_r (MyRoomGetFurnitureResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x943D
- **Packet ID (int):** 37949
- **Packet Size:** 4
- **Description:** My room furniture result.

**Layout:**

```
    UInt {Result}
```

### send_get_niconi_commons_base_list (NiconiCommonsBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x97B7
- **Packet ID (int):** 38839
- **Packet Size:** 0
- **Description:** Request Niconi commons base list.

**Layout:**

```
    (empty)
```

### recv_get_niconi_commons_base_list_r (NiconiCommonsBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xE60C
- **Packet ID (int):** 58892
- **Packet Size:** 8
- **Description:** Niconi commons base list result.

**Layout:**

```
    UInt {Result}
    UInt {CommonsBase}
```

### send_get_monster_data (NpcGetDataRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x461B
- **Packet ID (int):** 17947
- **Packet Size:** 0
- **Description:** Request NPC/monster data.

**Layout:**

```
    (empty)
```

### recv_get_monster_data_r (NpcGetDataResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x4403
- **Packet ID (int):** 17411
- **Packet Size:** 4
- **Description:** NPC data result.

**Layout:**

```
    UInt {Result}
```

### send_get_robo_list (RoboGetListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x44CE
- **Packet ID (int):** 17614
- **Packet Size:** 0
- **Description:** Request robo list.

**Layout:**

```
    (empty)
```

### recv_get_robo_list_r (RoboGetListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xF606
- **Packet ID (int):** 62982
- **Packet Size:** 8
- **Description:** Robo list result and count.

**Layout:**

```
    UInt {Result}
    UInt {RoboCount}
```

### send_robo_voice_type_update (RoboVoiceTypeUpdateRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x9305
- **Packet ID (int):** 37637
- **Packet Size:** 0
- **Description:** Request robo voice type update.

**Layout:**

```
    (empty)
```

### recv_robo_voice_type_update_r (RoboVoiceTypeUpdateResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x8F10
- **Packet ID (int):** 36624
- **Packet Size:** 5 (4 + 1)
- **Description:** Robo voice type update result.

**Layout:**

```
    UInt {Result}
    Byte {VoiceType}
```

### send_get_timezone (TimeZoneGetRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x5F53
- **Packet ID (int):** 24403
- **Packet Size:** 0
- **Description:** Request timezone info.

**Layout:**

```
    (empty)
```

### recv_get_timezone_r (TimeZoneGetResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xCD38
- **Packet ID (int):** 52536
- **Packet Size:** 17 (4 + 4 + 4 + 4 + 1)
- **Description:** Timezone, time, max and flag.

**Layout:**

```
    UInt {Result}
    UInt {Timezone}
    UInt {Time}
    UInt {TimeZoneMax}
    Byte {Flag}
```

### send_get_ucc_adv_figure_base_list (UccAdvFigureBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x86DD
- **Packet ID (int):** 34525
- **Packet Size:** 0
- **Description:** Request UCC advance figure base list.

**Layout:**

```
    (empty)
```

### recv_get_ucc_adv_figure_base_list_r (UccAdvFigureBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x878A
- **Packet ID (int):** 34698
- **Packet Size:** 8
- **Description:** UCC advance figure base list result.

**Layout:**

```
    UInt {Result}
    UInt {AdvFigures}
```

### send_get_ucc_voice_base_list (UccVoiceBaseListRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x1149
- **Packet ID (int):** 4425
- **Packet Size:** 0
- **Description:** Request UCC voice base list.

**Layout:**

```
    (empty)
```

### recv_get_ucc_voice_base_list_r (UccVoiceBaseListResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xBB8F
- **Packet ID (int):** 48015
- **Packet Size:** 8
- **Description:** UCC voice base list result.

**Layout:**

```
    UInt {Result}
    UInt {VoiceData}
```

### send_update_option (UpdateOptionRequest)

- **Server:** Area
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x79A1
- **Packet ID (int):** 31137
- **Packet Size:** 0
- **Description:** Request option update.

**Layout:**

```
    (empty)
```

### recv_update_option_r (UpdateOptionResponse)

- **Server:** Area
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB314
- **Packet ID (int):** 45844
- **Packet Size:** 4
- **Description:** Option update result.

**Layout:**

```
    UInt {Result}  // 1 = success in current impl
```

---