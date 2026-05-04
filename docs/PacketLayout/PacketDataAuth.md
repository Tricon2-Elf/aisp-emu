# Auth Server

## recv_authenticate_r (AuthenticateResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xD4AB
- **Packet ID (int):** 54443
- **Packet Size:** 4
- **Description:** Sent after successful authentication; contains the user ID for the session.

**Layout:**

```text
    UInt {UserId}
```

## recv_authenticate_r_failure (AuthenticateFailureResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xD845
- **Packet ID (int):** 55365
- **Packet Size:** 4
- **Description:** Sent when authentication fails (invalid credentials, banned, etc.).

**Layout:**

```text
    UInt {Result}  // AuthResponseResult: 0=Success, 1=Failure, 2=InvalidCredentials, 3=AccountBanned, ...
```

## recv_check_version_r (VersionCheckResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xB6B4
- **Packet ID (int):** 46772
- **Packet Size:** 12
- **Description:** Response to client version check; returns result and server version.

**Layout:**

```text
    UInt {Result}
    UInt {Major}
    UInt {Minor}
    UInt {Ver}
```

## recv_get_worldlist_r (WorldListResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0xEE7E
- **Packet ID (int):** 61054
- **Packet Size:** Variable. 4 + 4 + (WorldCount × 867) + 4 bytes. World entry: 4 + 97 + 766 = 867 bytes.
- **Description:** List of available game worlds/servers (id, name, description).

**Layout:**

```text
    Int {Result}
    UInt {WorldCount}
    foreach world:
        UInt {WorldId}
        FixedString(97, ASCII) {Name}
        FixedString(766, ASCII) {Description}
    UInt {Padding}  // 0
```

## recv_notify_logout (LogoutNotify)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x2D66
- **Packet ID (int):** 11622
- **Packet Size:** 0
- **Description:** Notifies client that the session has been logged out (e.g. from another location).

**Layout:**

```text
    (empty)
```

## recv_select_world_r (WorldSelectResponse)

- **Server:** Auth
- **Direction:** ServerToClient
- **Packet ID (hex):** 0x3491
- **Packet ID (int):** 13457
- **Packet Size:** 95 (4 + 4 + 2 + 65 + 20)
- **Description:** Response to world selection; contains connection info (IP, port, OTP) for the chosen world.

**Layout:**

```text
    UInt {Result}
    UInt {WorldCount}  // 1
    UShort {Port}
    FixedString(65, ASCII) {IpAddress}
    FixedString(20, ASCII) {OTP}
```

## send_authenticate (AuthenticateRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0xF24B
- **Packet ID (int):** 62027
- **Packet Size:** Variable (CStrings only)
- **Description:** Client login request with username and password.

**Layout:**

```text
    CString(ASCII) {Username}
    CString(ASCII) {Password}
```

## send_check_version (VersionCheckRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x62BC
- **Packet ID (int):** 25276
- **Packet Size:** 12
- **Description:** Client sends client version for compatibility check.

**Layout:**

```text
    UInt {Major}
    UInt {Minor}
    UInt {Version}
```

## send_get_worldlist (Auth_WorldListRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x6676
- **Packet ID (int):** 26230
- **Packet Size:** 0
- **Description:** Request the list of available worlds/servers.

**Layout:**

```text
    (empty)
```

## send_select_world (WorldSelectRequest)

- **Server:** Auth
- **Direction:** ClientToServer
- **Packet ID (hex):** 0x7FE7
- **Packet ID (int):** 32743
- **Packet Size:** 4
- **Description:** Client selects a world to connect to by ID.

**Layout:**

```text
    UInt {WorldID}
```
