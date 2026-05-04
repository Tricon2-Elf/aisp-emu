# VCE Encryption: Key Exchange & Custom Camellia-128 Cipher

This document explains the proprietary encryption protocol used by AISp@ce (VCE), as reimplemented in this emulator. The protocol has two layers:

1. **RSA key exchange** — establishes two ephemeral Camellia-128 session keys
2. **Custom Camellia-128 bulk cipher** — encrypts all subsequent traffic with a per-block key mutation

## RSA Key Exchange

Unlike a standard TLS handshake, the VCE protocol uses an unusually small RSA key with a fixed public exponent, and the key exchange is one-way.

### Parameters

| Parameter                       | Value                                      |
|---------------------------------|--------------------------------------------|
| Modulus N                       | 16 bytes (128-bit), little-endian unsigned |
| Public exponent e               |             65537 (0x10001), fixed         |
| Private exponent d              | known only to the key generator            |
| Key pairs generated per session | 2 (one for each direction)                 |

### Flow

```text
Client                              Server
  |                                                  |
  |-- RSA public modulus N (16 B) -->                |
  |                                                  |  Generate random S2C key (16 B)
  |                                                  |  Generate random C2S key (16 B)
  |                                                  |  Encrypt both with RSA: c = m^65537 mod N
  |                          <-- S2C_enc + C2S_enc --|
  |                                                  |
  |  Decrypt with private key d                      |
  |  Init Camellia with plain keys                   |
```

1. The **client** generates an RSA keypair (N, d) and sends the 16-byte modulus N (little-endian) to the server immediately upon connecting.
2. The **server** uses `CryptoUtils.CreateEncryptedKey()` to:
   - Generate a random 16-byte plaintext key for the Server-to-Client direction
   - Generate a random 16-byte plaintext key for the Client-to-Server direction
   - Encrypt each with `c = BigInteger.ModPow(m, 65537, N)`
   - Concatenate both encrypted keys (32 bytes total) and send them back
3. The **client** decrypts both with its private exponent d to recover the two Camellia keys.
4. The **server** already holds the plaintext keys and initializes both cipher instances.

Because RSA with a 128-bit modulus provides no real security (it can be factored trivially), the protocol relies on the ephemeral nature of the keypair: a new key is generated per connection, and the encrypted keys are only useful for that single session.

### Key generation details (`CryptoUtils.cs`)

```text
CreateEncryptedKey(rsaNLe):
  1. n = FromLeUnsigned(rsaNLe)          # convert LE bytes to BigInteger
  2. plainLe = CreatePlainKeyLe16(n)     # random 16 bytes, forced positive
  3. m = FromLeUnsigned(plainLe)
  4. c = BigInteger.ModPow(m, 65537, n)   # RSA encryption
  5. cipherLe = ToFixedLe(c, 16)          # convert back to 16-byte LE
  6. return (plainLe, cipherLe)
```

`CreatePlainKeyLe16` loops until it finds a random 16-byte value that, when interpreted as an unsigned LE integer, is less than N. The high byte's MSB is forced to zero (`key[15] = 0`) to guarantee a positive BigInteger regardless of endianness.

### Direction-specific keys

Two independent keys are always generated — one per direction:

- **S2C** (`S2C` field on `ClientConnection`): Server-to-Client, used by `EncryptBlock` / `EncryptBlocks`
- **C2S** (`C2S` field on `ClientConnection`): Client-to-Server, used by `DecryptBlock` / `DecryptBlocks`

This means each direction has its own cipher state with its own key schedule and its own mutation counter, keeping them fully independent.

## Custom Camellia-128 Cipher

The bulk cipher is based on the standard Camellia-128 block cipher but with one critical modification.

### Standard Camellia-128 (refresher)

Camellia is a 128-bit block cipher designed by NTT and Mitsubishi, jointly submitted to NESSIE and AES competition. It uses a Feistel structure with 18 rounds for the 128-bit key variant:

- **Key schedule**: The 128-bit key KL is expanded through the SIGMA constants (defined in the Camellia specification, based on fractional parts of square roots of small primes) and rotations to produce 26 subkeys.
- **F function**: A 64-bit non-linear function using four 8×8 S-boxes combined with byte multiplication by sparse constants (0x0101..., etc.) — this is the "Camellia-style" diffusion.
- **FL / FLINV functions**: Key-dependent logic layers inserted every 6 rounds (after rounds 6 and 12) to add additional non-linearity.
- **Feistel rounds**: 18 rounds total, with the FL/FLINV layers at rounds 6 and 12.

### The VCE key schedule (`GenSubkeys128`)

The subkey generation follows RustCrypto's `camellia v0.1.0` implementation exactly:

1. Compute `KA` from `KL` and `KR` using the SIGMA constants through the F function (four Feistel-like operations)
2. For 128-bit keys, `KR = (0, 0)`, so `KA` is derived solely from `KL`
3. 26 subkeys are generated by rotating `KL` and `KA` by specific bit offsets:

| Subkey    | Source  | Rotation |
|-----------|---------|----------|
| k[0..1]   | KL      | 0        |
| k[2..3]   | KA      | 0        |
| k[4..5]   | KL      | 15       |
| k[6..7]   | KA      | 15       |
| k[8..9]   | KA      | 30       |
| k[10..11] | KL      | 45       |
| k[12..13] | KA / KL | 45 / 60  |
| k[14..15] | KA      | 60       |
| k[16..17] | KL      | 77       |
| k[18..19] | KL      | 94       |
| k[20..21] | KA      | 94       |
| k[22..23] | KL      | 111      |
| k[24..25] | KA      | 111      |

Rotations use the full 128-bit value split across two 64-bit halves, so a rotation of e.g. 77 bits wraps across the two halves.

### VCE-unique modification: per-block key mutation via `IncK0`

**This is the single deviation from standard Camellia-128.**

After every `EncryptBlock()` or `DecryptBlock()` call, the cipher calls `IncK0()`:

```csharp
public void IncK0()
{
    uint hi = (uint)(_k[0] >> 32);
    uint lo = (uint)_k[0];
    hi++;
    _k[0] = ((ulong)hi << 32) | lo;
}
```

This increments the **upper 32 bits** of subkey k[0] by 1 (modulo 2^32), leaving the lower 32 bits unchanged.

**Why this matters:**

- Standard Camellia (ECB mode): `c[i] = E_k(p[i])` — same key for every block
- Standard Camellia (CBC mode): `c[i] = E_k(p[i] XOR c[i-1])` — same key, chained input
- VCE Camellia: `k' = mutate(k); c[i] = E_k'(p[i])` — the key itself changes per block

This means:

- Two identical plaintext blocks will produce different ciphertexts (even in "ECB-like" usage)
- Decryption must be strictly sequential — each block mutates the key for the next
- Encryption and decryption are symmetric in mutation (both call `IncK0`), keeping them in sync
- The cipher state is **tied to the connection** — you cannot encrypt/decrypt blocks out of order

**Impact on security:**

- This is a **proprietary mode** not found in any standard (NIST, ISO, etc.)
- It prevents cut-and-paste attacks on individual blocks
- It does NOT provide authentication (no MAC, no integrity check)
- The mutation only affects k[0]; all other subkeys remain static for the session
- The mutation space is 2^32 (32-bit counter), so after ~4 billion blocks the counter wraps

### Feistel structure

The round function implementation is standard Camellia:

```text
Encryption per block:
  1. XOR block halves with k[0], k[1] (pre-whitening)
  2. For each round pair (i = 2, 4, 6, ..., 22):
     - Every 8th pair (i % 8 == 0): apply FL/FLINV instead of F
     - Otherwise: d2 ^= F(d1, k[i]); d1 ^= F(d2, k[i+1])
  3. XOR with k[24], k[25] (post-whitening), swap halves
  4. Call IncK0()
```

The FL/FLINV insertion at rounds 6 and 12 (which is at i=8 and i=16 in the 0-indexed loop, hence `i % 8 == 0` fires at i=8 and i=16) matches the standard Camellia-128 structure exactly.

### F function

```csharp
F(input, key):
  x = (input ^ key) as 8 bytes big-endian
  z1 = 0x0101_0100_0100_0001 * S1[x[0]]
  z2 = 0x0001_0101_0101_0000 * S2[x[1]]
  z3 = 0x0100_0101_0001_0100 * S3[x[2]]
  z4 = 0x0101_0001_0000_0101 * S4[x[3]]
  z5 = 0x0001_0101_0001_0101 * S2[x[4]]
  z6 = 0x0100_0101_0100_0101 * S3[x[5]]
  z7 = 0x0101_0001_0101_0001 * S4[x[6]]
  z8 = 0x0101_0100_0101_0100 * S1[x[7]]
  return z1 ^ z2 ^ z3 ^ z4 ^ z5 ^ z6 ^ z7 ^ z8
```

Each S-box output (an 8-bit value) is multiplied by a sparse 64-bit constant. Due to the carefully chosen constants, each multiplication places the S-box output byte at specific positions in the 64-bit result. Eight such terms are XORed together to produce the final 64-bit F-function output, providing the nonlinear diffusion that gives Camellia its resistance to differential and linear cryptanalysis.

S-box design: S1 is the base 8×8 substitution table. S2, S3, and S4 are derived from S1 through GF(2^8) linear transformations (bitwise rotations of the lookup input/output). All four S-boxes produce distinct lookup tables.

## Wire framing

### Encrypted frame structure

After the RSA key exchange, all traffic is encrypted in frames:

```text
[4 bytes LE: plaintext length] [N*16 bytes: ciphertext]
```

- The 4-byte prefix is the **unpadded plaintext size** in little-endian
- The ciphertext is padded to the next 16-byte boundary (Camellia block size) with zeros
- Max plaintext chunk before padding: 1392 bytes (configured as `MaxChunkSize`)
- Frames larger than 1392 bytes are split into multiple chunks; each chunk is independently padded and gets its own length prefix, but the cipher state (k[0] counter) continues across chunks within the same direction

### VCE Codec multiplexing

Within a single decrypted frame, multiple logical messages can be packed using the VCE codec header:

```text
[codec byte] [variable-length size] [payload] [codec byte] [variable-length size] [payload] ...
```

The codec byte's upper nibble is the `VceCodecHeaderType`:

| Type          | Value | Description                                         |
|---------------|-------|-----------------------------------------------------|
| PacketData    | 0     | Game packet: size field + packet type + body        |
| Ping          | 1     | Keep-alive ping, 9 bytes total, skip in dispatch    |
| Pong          | 2     | Keep-alive pong, 9 bytes total, skip in dispatch    |
| Terminated    | 3     | Session terminated, 5 bytes total, skip in dispatch |
| DirectContact | 4     | Direct contact control message                      |

For PacketData, the lower nibble encodes the number of additional size bytes (0-3; nibble values 4-15 are clamped to 3, giving 1-4 total size bytes):

```text
PacketData layout:
  [byte 0: codec (0x0N where N = size_bytes-1)]
  [bytes 1..1+N: payload length (LE, variable)]
  [bytes 2+N..: payload (UShort PacketType + body)]
```

### Fallback: legacy single-packet mode

If the first byte of a decrypted frame does not look like a valid codec header, the entire frame is treated as a single legacy packet:

```text
[UShort LE: PacketType] [body...]
```

This backward-compatible mode allows the same decryption path to handle both codec-multiplexed and simple packets.

## Direction cipher states

Each `ClientConnection` holds two independent cipher instances:

```csharp
public VCECamellia128 C2S = new();   // decrypt incoming (client→server)
public VCECamellia128 S2C = new();   // encrypt outgoing (server→client)
```

- `DecryptBlock` / `DecryptBlocks` operates on `C2S`
- `EncryptBlock` / `EncryptBlocks` operates on `S2C`

They are initialized with different keys at connection setup (`SetCamelliaKeys`), so their subkey arrays and mutation counters evolve separately. This means a corrupted or reordered block in one direction does not affect the other direction's cipher state.

## Comparison: VCE Camellia vs Standard Camellia

| Aspect                 | Standard Camellia-128                                       | VCE Camellia-128                                    |
|------------------------|-------------------------------------------------------------|-----------------------------------------------------|
| Key schedule           | Identical (SIGMAS, rotations)                               | Identical                                           |
| S-boxes                | S1 base; S2/S3/S4 derived via GF(2^8) rotations             | Identical                                           |
| F function             | 4 S-box lookups × 8 sparse multiplies                       | Identical                                           |
| FL/FLINV               | Inserted at rounds 6, 12                                    | Identical                                           |
| Rounds                 | 18 Feistel rounds                                           | Identical                                           |
| Per-block key mutation | None                                                        | **Upper 32 bits of k[0] incremented**               |
| Cipher mode            | Stateless primitive; needs external mode (ECB/CBC/CTR/etc.) | Built-in stateful mutation (k[0] evolves per block) |
| Counterpart required   | No (stateless)                                              | Yes (state tracks block count)                      |
| Auth/integrity         | None (standard modes need MAC)                              | None (same)                                         |

The only difference is `IncK0()` — every other aspect of the cipher (key schedule, F function, S-boxes, FL/FLINV, rotation constants, number of rounds) is byte-for-byte standard Camellia-128.

## OTP generation

`CryptoUtils.GenerateOTP()` generates a 20-character hex string from SHA-256 of a GUID seed. This is used for authentication challenges (one-time passwords) for between server transfers and is unrelated to the encryption layer.
