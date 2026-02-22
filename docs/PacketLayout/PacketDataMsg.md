

## Msg Server

### send_login (LoginRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x34EF
- **Packet ID (int):** 13551
- **Packet Size:** 24 (4 + 20)
- **Description:** Client sends user ID and OTP after world select to log into the Msg server.

**Layout:**

```
    UInt {UserId}
    Bytes(20) {OTP}
```

### recv_login_r (LoginResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x1FEA
- **Packet ID (int):** 8170
- **Packet Size:** 4
- **Description:** Result of login (success or failure e.g. invalid credentials).

**Layout:**

```
    UInt {Result}  // AuthResponseResult: 0=Success, ...
```

### send_logout (LogoutRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0AD0
- **Packet ID (int):** 2768
- **Packet Size:** 0
- **Description:** Client requests to log out from the Msg server.

**Layout:**

```
    (empty)
```

### recv_logout_r (LogoutResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB7B9
- **Packet ID (int):** 47033
- **Packet Size:** 4
- **Description:** Server confirms logout.

**Layout:**

```
    UInt {Result}  // 0
```

### send_avatar_create (AvatarCreateRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x29A4
- **Packet ID (int):** 10660
- **Packet Size:** Variable (CString + 4 + 19 + 4)
- **Description:** Client creates a new avatar (name, model, visual, slot).

**Layout:**

```
    CString(ASCII) {AvatarName}
    UInt {ModelId}
    Bytes(19) {Visual}  // CharaVisual: UInt BloodType, Byte Month, Byte Day, UInt Gender, UInt CharacterID, Byte Face, UInt Hairstyle
    UInt {SlotId}
```

### recv_avatar_create_r (AvatarCreateResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x788F
- **Packet ID (int):** 30863
- **Packet Size:** 4
- **Description:** Result of avatar creation.

**Layout:**

```
    UInt {Result}
```

### recv_avatar_data (AvatarDataResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x6747
- **Packet ID (int):** 26439
- **Packet Size:** Variable. 4 + CString + 4 + 19 + 4 + 4 + (30 × 8) = 4 + name + 35 + 240
- **Description:** Full avatar data (result, name, model, visual, island, slot, 30 equip slots).

**Layout:**

```
    UInt {Result}
    CString(ASCII) {Name}
    UInt {ModelId}
    Bytes(19) {Visual}  // CharaVisual
    UInt {IslandId}
    UInt {SlotId}
    30 × (UInt {EquipId}, UInt {Socket})  // ItemSlotInfo
```

### send_get_enquete (EnqueteGetRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xC578
- **Packet ID (int):** 50552
- **Packet Size:** 0
- **Description:** Request survey/enquete questions.

**Layout:**

```
    (empty)
```

### recv_get_enquete_r (EnqueteGetResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x24EE
- **Packet ID (int):** 9454
- **Packet Size:** Variable. 4 + 4 + (Count × 795). EnqueteData: 4 + FixedString(181) + 10×FixedString(61) = 795.
- **Description:** List of survey questions and answers.

**Layout:**

```
    UInt {Result}
    UInt {QuestionCount}
    foreach question:
        UInt {Id}
        FixedString(181, Shift_JIS) {Question}
        10 × FixedString(61, Shift_JIS) {Answer}
```

### send_enquete_answer (EnqueteAnswerRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0352
- **Packet ID (int):** 850
- **Packet Size:** Variable. 4 + (N×4) + 4 + (N×4) for question/answer IDs.
- **Description:** Client submits survey answers (question IDs and answer IDs).

**Layout:**

```
    UInt {QuestionCount}
    QuestionCount × UInt {QuestionId}
    UInt {AnswerCount}
    AnswerCount × UInt {AnswerId}
```

### recv_enquete_answer_r (EnqueteAnswerResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x615A
- **Packet ID (int):** 24922
- **Packet Size:** 4
- **Description:** Result of submitting survey answers.

**Layout:**

```
    UInt {Result}
```

### send_select_avatar (AvatarSelectRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x113D
- **Packet ID (int):** 4413
- **Packet Size:** 4
- **Description:** Client selects an avatar by slot ID.

**Layout:**

```
    UInt {SlotId}
```

### recv_select_avatar_r (AvatarSelectResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2C5F
- **Packet ID (int):** 11359
- **Packet Size:** 4
- **Description:** Result of avatar selection.

**Layout:**

```
    UInt {Result}
```

### send_get_channellist (ChannelListGetRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x0300
- **Packet ID (int):** 768
- **Packet Size:** 0
- **Description:** Request list of channels.

**Layout:**

```
    (empty)
```

### recv_get_channellist_r (ChannelListGetResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xF27F
- **Packet ID (int):** 62079
- **Packet Size:** Variable. 4 + 4 + (Count × 79). ChannelInfo: 4 + 4 + 4 + 67 (ServerInfo: 2 + 65).
- **Description:** List of channels with server info.

**Layout:**

```
    UInt {Result}
    UInt {ChannelCount}
    foreach channel:
        UInt {ChannelId}
        UInt {_0x0004}
        UInt {_0x0008}
        UShort {Port}
        FixedString(65, ASCII) {IP}
```

### send_select_channel (ChannelSelectRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xFFE1
- **Packet ID (int):** 65505
- **Packet Size:** 4
- **Description:** Client selects a channel by ID.

**Layout:**

```
    UInt {ChannelID}
```

### recv_select_channel_r (ChannelSelectResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xFFEA
- **Packet ID (int):** 65514
- **Packet Size:** 4 + 67 + 4 + 4 = 79
- **Description:** Result and server info for the selected channel (IP, port, map IDs).

**Layout:**

```
    UInt {Result}
    UShort {Port}
    FixedString(65, ASCII) {IP}
    UInt {MapID}
    UInt {MapSerialID}
```

### recv_get_avatar_create_info_r (AvatarGetCreateInfoResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xA5AD
- **Packet ID (int):** 42413
- **Packet Size:** Variable. Multiple count-prefixed arrays (male/female builds, faces, hair, equipment).
- **Description:** Default creation options (builds, faces, hairstyles, colours, equipment) for avatar creation.

**Layout:**

```
    UInt {MaleBuildCount}; MaleBuildCount × UInt
    UInt {MaleFaceCount}; MaleFaceCount × Byte
    UInt {MaleHairStyleCount}; MaleHairStyleCount × UInt
    UInt {MaleHairColourCount}; MaleHairColourCount × Byte
    UInt {MaleEquipCount}; MaleEquipCount × (UInt Id, UInt Socket)
    (same for Female)
```

### recv_get_avatar_data_r (AvatarGetDataResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB055
- **Packet ID (int):** 45141
- **Packet Size:** 4 + 4 = 8 (min)
- **Description:** Result of get-avatar-data request (payload may vary).

**Layout:**

```
    UInt {Result}
```

### send_talk_post (PostTalkRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xEB2E
- **Packet ID (int):** 60206
- **Packet Size:** Variable (4 + 4 + CString + 4)
- **Description:** Client sends a chat/talk message (message ID, distribution ID, text, balloon type).

**Layout:**

```
    UInt {MessageID}
    UInt {DistID}
    CString(ASCII) {Message}
    UInt {BalloonID}
```

### send_get_item_base_list (ItemGetBaseListRequest)

- **Server:** Msg
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xC8EA
- **Packet ID (int):** 51434
- **Packet Size:** 0
- **Description:** Request base item list.

**Layout:**

```
    (empty)
```

### recv_get_item_base_list_r (ItemGetBaseListResponse)

- **Server:** Msg
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xC7A9
- **Packet ID (int):** 51113
- **Packet Size:** Variable. 4 + 4 + (Count × item size). ItemData: 4+4+4+4+97+4+4+4+769+193+4+2+4+4+4+4 = 1109 bytes.
- **Description:** Base item catalog (key, item ID, name, sockets, description, etc.).

**Layout:**

```
    UInt {Result}
    UInt {ItemCount}
    foreach item:
        UInt {Key}, UInt {SortedListPriority}, UInt {ItemId}, UInt {SkillId}
        FixedString(97, Shift_JIS) {Name}
        UInt {Category}, UInt {Socket1}, UInt {Socket2}
        FixedString(769, Shift_JIS) {Description}
        FixedString(193, Shift_JIS) {LimitDesc}
        UInt {Flags}, UShort {_0x0448}, UInt {_0x044c}, UInt {_0x0450}, UInt {EmotionId}, UInt {_0x0458}
```

---