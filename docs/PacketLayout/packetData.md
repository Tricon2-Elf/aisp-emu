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
