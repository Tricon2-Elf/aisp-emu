# Project Layout

This document explains the structure of the aisp repository, how the runtime is wired together, and how contributors should approach changes.

## Repository map

```text
aisp-emu/
├── aisp.sln                          # Solution file
├── docker-compose.yml                   # Container orchestration
├── dotnet-tools.json                    # Local .NET tools (CSharpier, dotnet-ef)
├── .pre-commit-config.yaml              # Pre-commit hooks
├── .github/workflows/                   # CI, format check, Docker publish, deploy
├── docs/                                # Reverse-engineering notes & packet references
│   ├── PacketLayout/                    #  Packet notes by domain
│   ├── aisp-decompiled.c                # Decompiled original client code
│   ├── aisp-decompiled-packet-llm-guide.md
│   └── *.frida.js                       # Frida instrumentation scripts
├── scripts/                             # Utility scripts
│   └── generate-migration.sh
│
├── aisp.Server/                      # Entry point (executable)
├── aisp.Common/                      # Server logic & game domain code
├── aisp.Network/                     # Wire protocol & transport
└── tests/                            # xUnit test projects
    ├── aisp.Common.Tests/
    ├── aisp.Network.Tests/
    └── aisp.Server.Tests/
```

## Project breakdown

### `aisp.Server/` — Host & orchestration

The executable project. Runs all three game domains as `BackgroundService` instances in a single process.

| File | Role |
| --- | --- |
| `Program.cs` | Host builder, DI registration, DB migration, domain startup |
| `GameServerBase.cs` | Abstract base: owns the TCP listener, packet dispatch loop, 60 Hz tick |
| `AuthServer.cs` | Auth domain (port 50050): login, world selection |
| `MsgServer.cs` | Msg domain (port 50052): avatars, channels, circles, chat |
| `AreaServer.cs` | Area domain (port 50054): in-world gameplay, movement, items, NPCs |
| `GameServerHealthRegistry.cs` | Health check tracking for Kestrel endpoint |
| `appsettings.json` | Runtime configuration (ports, DB provider, IP override) |
| `NLog.config` | Structured logging configuration |

### `aisp.Common/` — Server logic

All game logic, persistence, and packet handling.

| Path | Contents |
| --- | --- |
| `PacketDispatcher.cs` | Routes incoming packets to handlers via DI-resolved `IPacketHandler` |
| `PacketHandlerBase.cs` | Generic request/response handler base class |
| `PasswordHasher.cs` | Password hashing utilities |
| `Config/` | `ServerOptions`, `DbOptions`, `NetworkOptions` bound from config |
| `DAL/` | EF Core persistence layer |
| `DAL/MainContext.cs` | DbContext |
| `DAL/Entities/` | `User`, `Character`, `Item`, `Map`, `MapLink`, `World`, `Circle`, `GameChannel`, `Avatar`, `CharacterEquipment`, `CharacterInventory`, `PendingAreaTransition`, `SessionPresence`, `UserSession` |
| `DAL/Repositories/` | Data access & startup seed helpers |
| `DAL/Migrations/` | EF Core migrations |
| `Game/` | Runtime state: `PlayerSession`, `SharedState`, `SessionStore`, `AreaPresenceIndex`, `PendingTransitionStore`, `MapLinkGeometry`, `TimeZoneService` |
| `Handlers/Auth/` | Authentication & world-list handlers |
| `Handlers/Msg/` | Avatar CRUD, channel, circle, chat, mail, enquete handlers |
| `Handlers/Area/` | ~49 in-world gameplay handlers |
| `Handlers/Common/` | `PingHandler`, `VersionCheckHandler` |

### `aisp.Network/` — Protocol & transport

Wire-format definitions with zero dependency on game logic.

| Path | Contents |
| --- | --- |
| `Packet.cs` | `IOutgoingPacket`, `IIncomingPacket`, `Packet` record |
| `PacketType.cs` | Master enum (~600 entries with `[PacketMetadata]` attributes) |
| `PacketReader.cs` / `PacketWriter.cs` | Binary serialization helpers |
| `ClientConnection.cs` | TCP connection + Camellia encryption |
| `VceListener.cs` | TCP listener + VCE protocol decoder |
| `Crypto/` | `CryptoUtils` (RSA key exchange), `VCECamellia128` (custom cipher) |
| `Data/` | Shared DTOs: `AvatarData`, `ChannelInfo`, `CharaData`, `CircleData`, `ItemData`, etc. |
| `Packets/Auth/` | 7 packet types |
| `Packets/Msg/` | 38 packet types |
| `Packets/Area/` | 79 packet types |
| `Packets/Common/` | 7 packet types |

### `tests/` — Test projects

- **aisp.Common.Tests**: handler tests (login, authenticate, ping, world, msg, area map), packet dispatcher, password hasher, repository integration, shared state, migrations, map link geometry
- **aisp.Network.Tests**: packet read/write roundtrips, crypto, world list response, parser tests
- **aisp.Server.Tests**: host/API and health registry tests

## How the runtime works

### Startup flow (`Program.cs`)

1. Create host builder, apply `IP_OVERRIDE` env var into config
2. Configure NLog
3. Register `MainContext`, repositories, `SharedState`, `PacketDispatcher`
4. Scan `aisp.Common` for `IPacketHandler` implementations via Scrutor
5. Start three hosted services: Auth (:50050), Msg (:50052), Area (:50054)
6. Run EF Core migrations (`Database.MigrateAsync()`)
7. Seed maps, map links, worlds, channels if tables are empty

### Packet flow

```text
Client TCP socket
  → VceListener (raw bytes → Camellia-128 decrypt → Packet)
  → Channel<Packet> (System.Threading.Channels)
  → GameServerBase.RunPacketLoop()
  → PacketDispatcher.DispatchAsync() (resolves IPacketHandler from DI)
  → Handler reads request, applies logic, sends response via session.SendAsync()
```

### Domain responsibilities

| Domain | Port | Responsibility |
| --- | --- | --- |
| Auth | 50050 | Authentication, version check, world list/selection |
| Msg | 50052 | Avatar CRUD/select, channel list/select, circles, chat, mail, enquete |
| Area | 50054 | In-world gameplay: maps, movement, items, equipment, NPCs, emotions, missions, trading, UCC |

## Where to make changes

### Add a new packet/feature

1. Define packet class(es) in `aisp.Network/Packets/<Domain>/`
2. Add entry in `aisp.Network/PacketType.cs`
3. Create handler in `aisp.Common/Handlers/<Domain>/` (auto-discovered by Scrutor)
4. Add persistence in `aisp.Common/DAL/` if needed

### Add database-backed game data

1. Add/update entity in `aisp.Common/DAL/Entities/`
2. Update `MainContext.cs`
3. Add repository methods in `aisp.Common/DAL/Repositories/`
4. Run `dotnet ef migrations add` to generate a migration

### Change startup or server topology

`aisp.Server/Program.cs`, `*Server.cs`, `appsettings.json`, `docker-compose.yml`

### Change shared session/world state

`aisp.Common/Game/PlayerSession.cs`, `aisp.Common/Game/SharedState.cs`

## Key technical details

- **Language**: C# — all projects target `net10.0`
- **ORM**: Entity Framework Core 10.0 with SQLite (default), SQL Server supported
- **Logging**: NLog — console + rolling file + dedicated missing-packets logger
- **Encryption**: Custom RSA-16 key exchange + Camellia-128 block cipher (reverse-engineered)
- **Container**: Multi-stage Alpine Dockerfile + auto-heal sidecar in compose
- **Formatting**: CSharpier via pre-commit hook and CI
- **DB strategy**: `Database.MigrateAsync()` at startup, with repo-level seed helpers

## Boundaries to preserve

- `aisp.Server/` — orchestration only (no protocol parsing, no game logic)
- `aisp.Common/` — game logic and persistence (no transport, no socket code)
- `aisp.Network/` — wire format and transport (no game logic, no DB entities)
