# Client Messages


## Data Types:

All multi-byte numeric types are **little-endian**.

- **Byte** (1 byte): unsigned byte
- **SByte** (1 byte): signed byte
- **Short** (2 bytes): signed 16-bit integer
- **UShort** (2 bytes): unsigned 16-bit integer
- **Int** (4 bytes): signed 32-bit integer
- **UInt** (4 bytes): unsigned 32-bit integer
- **ULong** (8 bytes): unsigned 64-bit integer
- **Float** (4 bytes): single-precision float
- **Bytes(n)**: raw bytes, `n` is a fixed count (no length prefix)

- **CString**: Null-terminated string  
  - Default encoding: ASCII
  - Document as `CString` or `CString(encoding)` e.g. `CString(Shift_JIS)`

- **FixedString(length)**: Fixed-length string (exactly `length` bytes in the packet)  
  - Default encoding: Shift_JIS
  - Writer: `WriteFixedString(value, length)`; default encoding Shift_JIS; padded with zeros  
  - Document as `FString(n)` or `FString(n, encoding)` e.g. `FString(32, Shift_JIS)`


## Encrypted wire and multi-packet layout (VCE codec)

After the initial key exchange (RSA + Camellia), client and server send **encrypted blocks**. Each block is framed as:

- **UInt** (4 bytes, little-endian): raw message size in bytes (before block cipher padding).
- **Bytes(padded)**: ciphertext; length is the next multiple of 16 ≥ size. The whole block is decrypted at once.

Inside one decrypted block there can be **multiple logical messages**. The **VCE codec** is used to multiplex them.

### Codec byte and what it’s for

The first byte of each logical message in the decrypted buffer is a **codec byte**:

- **High nibble** = message kind:
  - `0` = **PacketData** — normal game packet (e.g. CmdExec, PostTalk). Rest of the message is `[length][payload]` with payload = `[packet type 2][body]`.
  - `1` = **Ping** (e.g. 9 bytes total) — keep-alive; skip in packet loop.
  - `2` = **Pong** (e.g. 9 bytes) — keep-alive response; skip.
  - `3` = **Terminated** (e.g. 5 bytes) — session control; skip.
  - `4` = **DirectContact** — other control; skip in normal packet handling.

- **Low nibble** (for PacketData only): number of **extra size bytes** for the length field. Total size bytes = 1 + low nibble (capped at 4). So:
  - `0` → 1-byte length (small packets),
  - `1` → 2 bytes,
  - `2` → 3 bytes,
  - `3` → 4 bytes (allows large payloads, e.g. CmdExec 3944 bytes in one message).

So the codec lets one encrypted block carry several game packets plus Ping/Pong/Terminated without splitting the stream per packet, and supports variable-length framing so big packets fit in a single message.

### PacketData (type 0) layout

For codec type 0 (PacketData):

- **Byte 0**: codec byte (0x0N, N = 0..3 for 1..4 size bytes).
- **Bytes 1..(1+N)**: payload length, little-endian (1, 2, 3, or 4 bytes).
- **From byte (2 + N)** onward: **payload** = `UShort {PacketType}` then the packet body (same as unencrypted: type + body; handlers receive only the payload, with type already consumed for routing).

So `data_start = 2 + (codec & 0xF)` (codec byte + length bytes). The server parses repeated codec messages until the decrypted block ends; each PacketData payload is pushed as one `Packet` to the channel.

### Single-message fallback

If the decrypted block does not look like a valid codec (e.g. first message is a legacy single packet with no codec byte), the server treats the **entire** block as one packet: **UShort {PacketType}** then body. So unencrypted-style `[type 2][body]` is supported inside an encrypted block when no codec prefix is used.


### PacketTemplate

- **Server:** Auth | Msg | Area
- **Direction:** ServerToClient | ClientToServer
- **Packet ID (hex):** 0x00
- **Packet ID (int):** 0000
- **Packet Size:** Variable (e.g. 10–100; varies with CStrings)
- **Description:** Description of what this packet is for

**Layout:**

```
    {PacketID}
    {Result}
    CString(ASCII) {username}
```


## Auth Server

### recv_authenticate_r (AuthenticateResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xD4AB
- **Packet ID (int):** 54443
- **Packet Size:** 4
- **Description:** Sent after successful authentication; contains the user ID for the session.

**Layout:**

```
    UInt {UserId}
```

### recv_authenticate_r_failure (AuthenticateFailureResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xD845
- **Packet ID (int):** 55365
- **Packet Size:** 4
- **Description:** Sent when authentication fails (invalid credentials, banned, etc.).

**Layout:**

```
    UInt {Result}  // AuthResponseResult: 0=Success, 1=Failure, 2=InvalidCredentials, 3=AccountBanned, ...
```

### recv_check_version_r (VersionCheckResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB6B4
- **Packet ID (int):** 46772
- **Packet Size:** 12
- **Description:** Response to client version check; returns result and server version.

**Layout:**

```
    UInt {Result}
    UInt {Major}
    UInt {Minor}
    UInt {Ver}
```

### recv_get_worldlist_r (WorldListResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xEE7E
- **Packet ID (int):** 61054
- **Packet Size:** Variable. 4 + 4 + (WorldCount × 867) + 4 bytes. World entry: 4 + 97 + 766 = 867 bytes.
- **Description:** List of available game worlds/servers (id, name, description).

**Layout:**

```
    Int {Result}
    UInt {WorldCount}
    foreach world:
        UInt {WorldId}
        FixedString(97, ASCII) {Name}
        FixedString(766, ASCII) {Description}
    UInt {Padding}  // 0
```

### recv_notify_logout (LogoutNotify)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2D66
- **Packet ID (int):** 11622
- **Packet Size:** 0
- **Description:** Notifies client that the session has been logged out (e.g. from another location).

**Layout:**

```
    (empty)
```

### recv_select_world_r (WorldSelectResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x3491
- **Packet ID (int):** 13457
- **Packet Size:** 95 (4 + 4 + 2 + 65 + 20)
- **Description:** Response to world selection; contains connection info (IP, port, OTP) for the chosen world.

**Layout:**

```
    UInt {Result}
    UInt {WorldCount}  // 1
    UShort {Port}
    FixedString(65, ASCII) {IpAddress}
    FixedString(20, ASCII) {OTP}
```

### send_authenticate (AuthenticateRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xF24B
- **Packet ID (int):** 62027
- **Packet Size:** Variable (CStrings only)
- **Description:** Client login request with username and password.

**Layout:**

```
    CString(ASCII) {Username}
    CString(ASCII) {Password}
```

### send_check_version (VersionCheckRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x62BC
- **Packet ID (int):** 25276
- **Packet Size:** 12
- **Description:** Client sends client version for compatibility check.

**Layout:**

```
    UInt {Major}
    UInt {Minor}
    UInt {Version}
```

### send_get_worldlist (Auth_WorldListRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x6676
- **Packet ID (int):** 26230
- **Packet Size:** 0
- **Description:** Request the list of available worlds/servers.

**Layout:**

```
    (empty)
```

### send_select_world (WorldSelectRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x7FE7
- **Packet ID (int):** 32743
- **Packet Size:** 4
- **Description:** Client selects a world to connect to by ID.

**Layout:**

```
    UInt {WorldID}
```


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

### Not yet implemented (Msg Server)

### recv_notify_circle_member_logout
### recv_get_mail_box_data_r
### recv_resign_circle_r
### recv_notify_circle_message_change
### recv_post_mail_r
### recv_notify_circle_leader_change
### recv_notify_circle_kick
### recv_circle_request_join_member_cancel_r
### recv_notify_circle_member_login
### recv_circle_chat_out_r
### recv_create_new_circle_r
### recv_get_placard_comment_log_r
### recv_get_channellist_map_r
### recv_notify_placard_comment_log
### recv_circle_member_kick_r
### recv_circle_message_change_r
### recv_cancel_protect_mail_r
### recv_notify_new_mail
### recv_avatar_destroy_r
### recv_cmd_exec_r
### send_avatar_destroy
### recv_notify_circle_resign_member
### recv_notify_circle_change_core_authority
### recv_circle_chat_in_r
### recv_notify_circle_request_join_result
### recv_get_circle_data_r
### recv_circle_chat_post_r
### recv_notify_circle_member
### recv_circle_change_core_authority_r
### recv_protect_mail_r
### recv_notify_circle_chat_in
### recv_circle_mark_change_r
### recv_circle_request_join_member_r
### recv_open_mail_r
### recv_notify_circle_add_member
### recv_delete_mail_r
### recv_notify_live_contest_comment_forward
### recv_check_channel_r
### recv_circle_chat_forward
### recv_circle_leader_change_r
### recv_get_live_contest_all_comment_r
### recv_notify_circle_chat_out
### recv_notify_circle_mark_change
### recv_notify_circle_request_join
### recv_notify_live_contest_comment_delete
### recv_select_channel_r_myroom
### recv_talk_forward
### send_cancel_protect_mail
### send_circle_change_core_authority
### send_circle_chat_in
### send_circle_chat_out
### send_circle_chat_post
### send_circle_mark_change
### send_circle_member_kick
### send_circle_message_change
### send_circle_request_join_answer
### send_circle_request_join_member
### send_circle_request_join_member_cancel
### send_cmd_exec
### send_create_new_circle
### send_delete_mail
### send_get_avatar_create_info
### send_get_avatar_data
### send_get_channellist_map
### send_get_circle_data
### send_get_mail_box_data
### send_get_placard_comment_log
### send_open_mail
### send_post_mail
### send_protect_mail
### send_resign_circle


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

### Not yet implemented (Area Server)

### recv_exe_type_get_request
### send_exe_type_get_request_r
### recv_adventure_create_tag_r
### recv_adventure_delete_tag_r
### recv_adventure_download_delete_request_r
### recv_adventure_end_r
### recv_adventure_lock_tag_r
### recv_adventure_shop_added_buy_history
### recv_adventure_shop_attn_tag
### recv_adventure_shop_buy_r
### recv_adventure_shop_buys_r
### recv_adventure_shop_download_request_r
### recv_adventure_shop_end_r
### recv_adventure_shop_ended
### recv_adventure_shop_genre_search_r
### recv_adventure_shop_item
### recv_adventure_shop_keyword_search_r
### recv_adventure_shop_ranking_search_r
### recv_adventure_shop_remove_all_buy_history_r
### recv_adventure_shop_remove_buy_history_r
### recv_adventure_shop_removed_buy_history
### recv_adventure_shop_started
### recv_adventure_shop_tag_search_r
### recv_adventure_start_r
### recv_adventure_unlock_tag_r
### recv_adventure_updated_sheet_stack
### recv_adventure_upload_delete_request_r
### recv_adventure_upload_end_r
### recv_adventure_upload_request_r
### recv_adventure_upload_request_report_r
### recv_adventure_upload_started
### recv_adventure_work_add_sheet_r
### recv_adventure_work_create_r
### recv_adventure_work_delete_r
### recv_adventure_work_sub_sheet_r
### recv_get_adventure_download_list_r
### recv_get_adventure_upload_list_r
### recv_get_adventure_upload_rate_r
### recv_get_adventure_work_list_r
### send_adventure_download_delete_request
### send_adventure_end
### send_adventure_shop_buy
### send_adventure_shop_download_request
### send_adventure_shop_end
### send_adventure_shop_genre_search
### send_adventure_shop_ranking_search
### send_adventure_shop_remove_all_buy_history
### send_adventure_shop_remove_buy_history
### send_adventure_start
### send_adventure_upload_delete_request
### send_adventure_upload_end
### send_adventure_upload_request
### send_adventure_upload_request_report
### send_adventure_work_add_sheet
### send_adventure_work_create
### send_adventure_work_delete
### send_adventure_work_sub_sheet
### send_get_adventure_download_list
### send_get_adventure_upload_list
### send_get_adventure_upload_rate
### send_get_adventure_work_list
### recv_edit_avatar_myprofile_r
### recv_get_my_avatar_myprofile_data_r
### recv_get_other_avatar_myprofile_data_r
### send_edit_avatar_myprofile
### send_get_avatar_data
### send_get_my_avatar_myprofile_data
### send_get_other_avatar_myprofile_data
### recv_battle_attack_blaze_r
### recv_battle_attack_cancel_r
### recv_battle_attack_exec_r
### recv_battle_attack_start_r
### recv_battle_dash_exec_r
### recv_battle_dash_finish_r
### recv_battle_kill_shot_ready_r
### recv_battle_target_lock_r
### send_battle_attack_blaze
### send_battle_attack_cancel
### send_battle_attack_exec
### send_battle_attack_start
### send_battle_dash_exec
### send_battle_dash_finish
### send_battle_kill_shot_ready
### send_battle_target_lock
### send_battle_target_unlock
### recv_voice_chara_r
### send_voice_chara
### recv_live_contest_audience_ready_r
### recv_live_contest_entry_audience_r
### recv_live_contest_entry_participant_r
### recv_live_contest_open_r
### recv_live_contest_play_r
### recv_live_contest_start_r
### recv_debug_add_item_r
### recv_ai_download_delete_request_r
### recv_ai_upload_delete_request_r
### recv_ai_upload_end_r
### recv_ai_upload_request_r
### recv_ai_upload_request_report_r
### recv_ai_upload_started
### recv_emotion_chara_r
### recv_emotion_obtain
### send_emotion_chara
### recv_event_access_npc_r
### recv_event_areamap_select_close
### recv_event_areamap_select_exec
### recv_event_bbs_select_exec
### recv_event_bgm_play
### recv_event_board_open
### recv_event_channel_select_exec
### recv_event_chara_hide
### recv_event_chara_show
### recv_event_end
### recv_event_fade_in
### recv_event_fade_out
### recv_event_get_tps_mode
### recv_event_island_select_exec
### recv_event_message
### recv_event_message_close
### recv_event_message_pos
### recv_event_notice
### recv_event_notice_close
### recv_event_quest_select_exec
### recv_event_script_play
### recv_event_se_play
### recv_event_select_exec
### recv_event_select_init
### recv_event_select_push
### recv_event_set_motion
### recv_event_sleep
### recv_event_start
### recv_event_sync
### recv_event_trade_item_reset_r
### recv_event_trade_item_set_r
### recv_event_trade_open
### recv_event_variable_datas
### recv_event_voice_message
### send_event_access_npc
### send_event_areamap_select_exec_r
### send_event_bbs_select_exec_r
### send_event_board_close
### send_event_channel_select_exec_r
### send_event_fade_in_r
### send_event_fade_out_r
### send_event_get_tps_mode_r
### send_event_island_select_exec_r
### send_event_quest_select_exec_r
### send_event_script_play_r
### send_event_select_exec_r
### send_event_set_motion_r
### send_event_sync_r
### send_event_trade_close
### send_event_trade_item_reset
### send_event_trade_item_set
### recv_change_friend_list_comment_r
### recv_close_myprofile_r
### recv_delete_friend_list_r
### recv_get_free_friend_link_tag_r
### recv_request_add_friend_list_r
### send_close_myprofile
### send_delete_friend_list
### send_friend_link_tag_change
### send_get_free_friend_link_tag
### send_request_add_friend_list_cancel
### send_request_friend_list_answer
### recv_gacha_buy_r
### recv_gacha_end_r
### recv_gacha_ended
### recv_gacha_started
### recv_gachaticket_exchange_item_add_r
### recv_gachaticket_exchange_item_del_r
### recv_gachaticket_exchange_open
### send_gacha_buy
### send_gacha_end
### send_gachaticket_exchange_close
### send_gachaticket_exchange_item_add
### send_gachaticket_exchange_item_del
### recv_heroine_ticket_check_r
### send_heroine_ticket_check
### send_heroine_ticket_select
### recv_select_init_island_start
### send_select_init_island_end
### recv_expire_item_create
### recv_expire_item_delete
### recv_expire_item_limit_select
### recv_expire_item_notify_next_state
### recv_expire_item_select_continue_result
### recv_expire_item_select_return_result
### recv_expire_item_update_expire_time
### recv_get_cosplay_list_r
### recv_get_item_base_list_r
### recv_get_item_list_r
### recv_get_itembox_item_list_r
### recv_get_tps_use_item_list_r
### recv_item_create
### recv_item_delete
### recv_item_discard_r
### recv_item_discard_sum_r
### recv_item_equip_end_r
### recv_item_equip_ended
### recv_item_equip_force_started
### recv_item_equip_r
### recv_item_equip_replace_r
### recv_item_equip_replaced
### recv_item_equip_start_r
### recv_item_equip_started
### recv_item_equipped
### recv_item_move_r
### recv_item_remove_r
### recv_item_removed
### recv_item_try_equip_fix_r
### recv_item_try_equip_r
### recv_item_try_equip_replace_r
### recv_item_try_equip_replaced
### recv_item_try_equip_reset_r
### recv_item_try_equipped
### recv_item_try_remove_r
### recv_item_try_removed
### recv_item_update_list
### recv_item_update_num
### recv_item_update_price
### recv_item_use_r
### recv_itembox_close_r
### recv_itembox_item_create
### recv_itembox_item_delete
### recv_itembox_opened
### recv_itembox_takeout_r
### recv_itemcode_open
### send_expire_item_limit_select_r
### send_get_cosplay_list
### send_get_item_list
### send_get_tps_use_item_list
### send_item_discard
### send_item_equip_end
### send_item_equip_start
### send_item_move
### send_item_try_equip_fix
### send_item_try_equip_replace
### send_item_try_equip_reset
### send_item_use
### send_itembox_takeout
### send_itemcode_close
### recv_pickup_mascot_r
### send_pickup_mascot
### recv_get_mission_data_r
### recv_get_mission_list_r
### recv_get_mission_result_item_r
### recv_mission_outmap_choice_open
### recv_mission_party_create_enter_r
### recv_mission_party_list_cancel_r
### recv_mission_party_list_enter_r
### recv_mission_party_list_quick_enter_r
### recv_mission_raise_choice_open
### recv_mission_shop_choice_open
### recv_request_mission_outmap_r
### recv_request_mission_party_breakup_r
### recv_request_mission_party_change_mission_r
### recv_request_mission_party_kick_r
### recv_request_mission_party_mission_start_r
### recv_request_mission_party_remove_r
### recv_request_mission_party_start_cancel_r
### recv_request_mission_party_start_ok_r
### recv_request_mission_shop_open_r
### recv_request_mission_watch_player_r
### send_get_mission_result_item
### send_mission_outmap_choice_open_r
### send_mission_party_create_cancel
### send_mission_party_create_enter
### send_mission_party_list_cancel
### send_mission_party_list_enter
### send_mission_raise_choice_open_r
### send_mission_result_close
### send_mission_shop_choice_open_r
### send_request_mission_outmap
### send_request_mission_party_breakup
### send_request_mission_party_change_mission
### send_request_mission_party_mission_start
### send_request_mission_party_mission_start_force
### send_request_mission_party_remove
### send_request_mission_party_start_cancel
### send_request_mission_party_start_ok
### send_request_mission_shop_open
### send_request_mission_watch_player
### recv_money_updated_aipoint
### recv_money_updated_nicopoint
### recv_nps_point_get_r
### recv_support_aipower_aipoint_r
### recv_support_aipower_nicopoint_r
### send_nps_point_get
### recv_get_myhouse_list_r
### recv_myhouse_payment_rent_r
### recv_myhouse_replacement_r
### recv_myroom_end_furniture_r
### recv_myroom_remove_furniture_r
### recv_myroom_set_furniture_r
### recv_myroom_start_furniture_r
### recv_myroom_throwout_others_r
### recv_myroom_update_furniture_r
### recv_myroom_update_name_r
### recv_myroom_update_security_r
### recv_myroom_use_furniture_r
### send_myroom_end_furniture
### send_myroom_remove_furniture
### send_myroom_set_furniture
### send_myroom_start_furniture
### send_myroom_update_furniture
### send_myroom_update_name
### send_myroom_update_security
### send_myroom_use_furniture
### recv_room_list_close_r
### send_room_list_close
### recv_niconi_commons_obtain
### recv_niconi_commons_shop_buy_r
### recv_niconi_commons_shop_end_r
### recv_niconi_commons_shop_ended
### recv_niconi_commons_shop_item
### recv_niconi_commons_shop_started
### recv_nicotv_close_r
### recv_nicotv_get_info_by_furniture_r
### recv_nicotv_get_playhead_time_r
### recv_nicotv_get_playhead_time_request
### recv_nicotv_open_r
### recv_nicotv_play_r
### recv_nicotv_set_channel_r
### recv_nicotv_set_comment_visible_r
### recv_nicotv_set_movie_r
### recv_nicotv_set_playhead_time_r
### send_nicolive_reload
### send_niconi_commons_shop_buy_commons
### send_niconi_commons_shop_buy_figure
### send_niconi_commons_shop_buy_voice
### send_niconi_commons_shop_end
### send_nicotv_close
### send_nicotv_get_info_by_furniture
### send_nicotv_get_playhead_time
### send_nicotv_get_playhead_time_request_r
### send_nicotv_open_by_furniture
### send_nicotv_play
### send_nicotv_set_channel
### send_nicotv_set_movie
### recv_notify_add_cosplay
### recv_notify_add_friend_list_result
### recv_notify_avatar_data
### recv_notify_battle_kill_shot_ready_end
### recv_notify_battle_raise_end
### recv_notify_battle_raise_start
### recv_notify_battle_report_target_obj
### recv_notify_battle_report_target_pos
### recv_notify_battle_target_lock
### recv_notify_battle_target_unlock
### recv_notify_bgm_play
### recv_notify_change_avatar_job
### recv_notify_change_friend_list_comment
### recv_notify_change_map
### recv_notify_change_map_failed
### recv_notify_change_myroom
### recv_notify_change_robo_job
### recv_notify_change_tag
### recv_notify_complete_state_close
### recv_notify_complete_state_open
### recv_notify_complete_state_update_num
### recv_notify_cosplay_level_up
### recv_notify_debug_add_object
### recv_notify_debug_motion
### recv_notify_debug_remove_object
### recv_notify_delete_friend_list_avatar
### recv_notify_disappear_chara
### recv_notify_effectobj_data
### recv_notify_emotion_chara
### recv_notify_end_effect_object
### recv_notify_end_effect_pos
### recv_notify_end_use_item_effect
### recv_notify_friend_list_avatar_login
### recv_notify_friend_list_avatar_logout
### recv_notify_heroine_ticket_heroine
### recv_notify_hide_chara
### recv_notify_item_base
### recv_notify_itemcode_obtain_item
### recv_notify_level_up
### recv_notify_live_contest_add_audience
### recv_notify_live_contest_add_participant
### recv_notify_live_contest_close
### recv_notify_live_contest_end
### recv_notify_live_contest_leave_audience
### recv_notify_live_contest_leave_participant
### recv_notify_live_contest_play
### recv_notify_live_contest_tag_info
### recv_notify_live_end_position
### recv_notify_live_start_position
### recv_notify_login_message
### recv_notify_maplink_data
### recv_notify_mascot_data
### recv_notify_milestone_hide
### recv_notify_milestone_show
### recv_notify_mission_action
### recv_notify_mission_data
### recv_notify_mission_list_end
### recv_notify_mission_list_pack
### recv_notify_mission_list_start
### recv_notify_mission_party_breakup
### recv_notify_mission_party_change_mission
### recv_notify_mission_party_create_open
### recv_notify_mission_party_info
### recv_notify_mission_party_join
### recv_notify_mission_party_kick
### recv_notify_mission_party_list_open_end
### recv_notify_mission_party_list_open_start
### recv_notify_mission_party_list_pack
### recv_notify_mission_party_list_update_r
### recv_notify_mission_party_remove
### recv_notify_mission_party_remove_host
### recv_notify_mission_party_start_ok_update
### recv_notify_mission_result_open
### recv_notify_mission_situation_message
### recv_notify_mission_start_data
### recv_notify_mission_watch_player_end
### recv_notify_monster_data
### recv_notify_myhouse_appear
### recv_notify_myhouse_auction_list
### recv_notify_myhouse_auction_list_end
### recv_notify_myhouse_auction_open
### recv_notify_myhouse_change_looks
### recv_notify_myhouse_change_security
### recv_notify_myhouse_disappear
### recv_notify_myhouse_list
### recv_notify_myhouse_relocate_failed
### recv_notify_myhouse_relocate_succeed
### recv_notify_myroom_furniture
### recv_notify_myroom_remove_furniture
### recv_notify_myroom_set_furniture
### recv_notify_myroom_update_furniture
### recv_notify_myroom_use_furniture
### recv_notify_nicolive_close
### recv_notify_nicolive_reload
### recv_notify_nicotv_close
### recv_notify_nicotv_play
### recv_notify_nicotv_set_channel
### recv_notify_nicotv_set_comment_visible
### recv_notify_nicotv_set_movie
### recv_notify_nicotv_set_playhead_time
### recv_notify_npc_data
### recv_notify_option_data
### recv_notify_placard_in_map
### recv_notify_placard_remove
### recv_notify_placard_setting
### recv_notify_placard_update_popular
### recv_notify_remove_ai_palette
### recv_notify_request_friend_list
### recv_notify_request_friend_list_cancel
### recv_notify_robo_data
### recv_notify_robo_furnact_end
### recv_notify_robo_furnact_start
### recv_notify_robo_job_work
### recv_notify_robo_job_work_result
### recv_notify_robo_jobs_pack
### recv_notify_room_list_open_end
### recv_notify_room_list_open_start
### recv_notify_room_list_pack
### recv_notify_se_play
### recv_notify_select_map
### recv_notify_shot_skill_change
### recv_notify_show_chara
### recv_notify_skill_obtain
### recv_notify_start_effect
### recv_notify_start_use_item_effect
### recv_notify_supply_npc_exec
### recv_notify_timelimit_hide
### recv_notify_timelimit_show
### recv_notify_tps_use_item_end
### recv_notify_tps_use_item_start
### recv_notify_tps_use_item_update
### recv_notify_update_battle_ability
### recv_notify_update_buff_battle_ability
### recv_notify_update_buff_hitpoint_max
### recv_notify_update_buff_stamina_costcut
### recv_notify_update_buff_tank_max
### recv_notify_update_cosplay_exp
### recv_notify_update_exp
### recv_notify_update_heart
### recv_notify_update_heart_max
### recv_notify_update_hitpoint
### recv_notify_update_hitpoint_max
### recv_notify_update_move_speed
### recv_notify_update_nameplate
### recv_notify_update_now_cosplay
### recv_notify_update_pickup_mascot_count
### recv_notify_update_robo_equip
### recv_notify_update_robo_state
### recv_notify_update_stamina
### recv_notify_update_status_point
### recv_notify_update_tank
### recv_notify_update_tank_max
### recv_notify_user_status_update
### recv_notify_voice_chara
### send_notify_mission_party_list_update
### recv_close_npc_rank_windor_r
### recv_get_npc_data_r
### recv_npc_rank_list
### recv_present_npc_started
### send_get_npc_data
### recv_exec_ai_palette_r
### recv_get_ai_palette_list_r
### recv_open_ai_palette_r
### recv_set_ai_palette_r
### send_close_ai_palette
### send_exec_ai_palette
### send_get_ai_palette_list
### send_open_ai_palette
### send_set_ai_palette
### recv_party_chat_talk_forward
### recv_party_chat_talk_post_r
### send_party_chat_talk_post
### recv_placard_remove_r
### recv_placard_setting_r
### send_placard_remove
### send_placard_setting
### recv_present_cancel_r
### recv_present_fix_r
### recv_quest_add_history
### recv_quest_ended
### recv_quest_get_history_r
### recv_quest_get_work_r
### recv_quest_remove_target
### recv_quest_set_target
### recv_quest_started
### recv_quest_update_target
### recv_quest_updated_chapter
### recv_quest_updated_restsec
### send_quest_get_history
### send_quest_get_work
### send_request_add_friend_list
### recv_edit_robo_myprofile_r
### recv_get_my_robo_myprofile_data_r
### recv_get_other_robo_myprofile_data_r
### recv_get_robo_create_info_r
### recv_get_robo_job_list_r
### recv_present_robo_start_r
### recv_robo_aiscript_start_r
### recv_robo_attach_r
### recv_robo_attach_request
### recv_robo_call_r
### recv_robo_create_r
### recv_robo_destroy_r
### recv_robo_detach_notice_from_avatar
### recv_robo_detach_notice_from_robo
### recv_robo_grant_next_message_notice
### recv_robo_job_giveup_r
### recv_robo_rest_r
### recv_robo_squire_r
### recv_robo_talk_forward
### recv_update_robo_voice_type_r
### send_edit_robo_myprofile
### send_get_my_robo_myprofile_data
### send_get_other_robo_myprofile_data
### send_get_robo_create_info
### send_move_robo
### send_robo_aiscript_end
### send_robo_aiscript_start
### send_robo_attach
### send_robo_attach_request_r
### send_robo_call
### send_robo_create
### send_robo_destroy
### send_robo_detach_from_avatar
### send_robo_detach_from_robo
### send_robo_furnact_end
### send_robo_furnact_start
### send_robo_rest
### send_robo_squire
### send_robo_talk_post
### send_robo_update_aiscript
### recv_ai_shop_added_buy_history
### recv_ai_shop_buy_r
### recv_ai_shop_download_request_r
### recv_ai_shop_end_r
### recv_ai_shop_ended
### recv_ai_shop_genre_search_r
### recv_ai_shop_item
### recv_ai_shop_ranking_search_r
### recv_ai_shop_remove_all_buy_history_r
### recv_ai_shop_remove_buy_history_r
### recv_ai_shop_removed_buy_history
### recv_ai_shop_started
### recv_auction_bid_pay_r
### recv_hair_shop_buy_r
### recv_hair_shop_end_r
### recv_hair_shop_ended
### recv_hair_shop_item
### recv_hair_shop_started
### recv_sheet_shop_buy_r
### recv_sheet_shop_end_r
### recv_sheet_shop_start_r
### recv_shop_buy_r
### recv_shop_chara_equip
### recv_shop_end_r
### recv_shop_ended
### recv_shop_item
### recv_shop_started
### recv_shop_update_chara_r
### send_ai_shop_buy
### send_ai_shop_download_request
### send_ai_shop_end
### send_ai_shop_genre_search
### send_ai_shop_ranking_search
### send_ai_shop_remove_all_buy_history
### send_ai_shop_remove_buy_history
### send_sheet_shop_buy
### send_sheet_shop_end
### send_sheet_shop_start
### send_shop_buy
### send_shop_end
### recv_get_obtained_skill_list_r
### recv_skill_exec_r
### recv_skill_start_cast_r
### send_get_obtained_skill_list
### send_shot_skill_cancell
### recv_stall_close_r
### recv_stall_closed
### recv_stall_comment_r
### recv_stall_open_r
### recv_stall_opened
### recv_stall_price_r
### recv_stall_shop_buy_r
### recv_stall_shop_chara_equip
### recv_stall_shop_end_r
### recv_stall_shop_ended
### recv_stall_shop_item
### recv_stall_shop_start_r
### recv_stall_shop_started
### recv_stall_shop_update_chara_r
### recv_distribute_status_point_add_r
### recv_distribute_status_point_finish_r
### send_distribute_status_point_add
### send_distribute_status_point_finish
### recv_storage_close_r
### recv_storage_deposit_r
### recv_storage_furn_close_r
### recv_storage_furn_open_r
### recv_storage_opened
### recv_storage_updated_deposit
### recv_storage_withdraw_r
### send_storage_close
### send_storage_deposit
### send_storage_withdraw
### recv_get_time_zone_r
### send_get_time_zone
### recv_trade_canceled
### recv_trade_commit
### recv_trade_decided
### recv_trade_dele_update_r
### recv_trade_dele_updated
### recv_trade_item_add_r
### recv_trade_item_added
### recv_trade_item_remove_r
### recv_trade_item_removed
### recv_trade_ok_start
### recv_trade_oked
### recv_trade_refused
### recv_trade_request_r
### recv_trade_requested
### recv_trade_respond_r
### recv_trade_start
### send_trade_cancel
### send_trade_decide
### send_trade_dele_update
### send_trade_item_add
### send_trade_item_remove
### send_trade_ok
### send_trade_request
### send_trade_respond
### recv_trashbox_close_r
### recv_trashbox_discard_item_r
### recv_trashbox_open_r
### send_trashbox_close
### send_trashbox_discard_item
### send_trashbox_open
### recv_ucc_adv_figure_obtain
### recv_ucc_voice_obtain
### recv_get_ai_upload_list_r
### send_get_ai_upload_list
### recv_user_status_update_r
### send_user_status_update
### send_check_version
### recv_check_version_r
### recv_aipower_data
### recv_cam_set_relative_avatar
### recv_catalog_list
### recv_close_aipower_window_r
### send_ai_download_delete_request
### send_ai_upload_delete_request
### send_ai_upload_end
### send_ai_upload_request
### send_ai_upload_request_report