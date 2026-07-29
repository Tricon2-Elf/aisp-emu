# AGENTS.md

## Quick reference

| Task | Command |
| ------ | --------- |
| Restore + build | `dotnet restore AISpace.sln && dotnet build AISpace.sln` |
| Run tests (all) | `dotnet test AISpace.sln` |
| Run single test project | `dotnet test AISpace.Common.Tests` |
| Collect coverage | `./scripts/run-coverage.sh` |
| Coverage HTML report | `./scripts/reportgenerator.sh` |
| Run the server | `dotnet run --project AISpace.Server` |
| Format staged files | `git config core.hooksPath .githooks` (once), then commit |
| Check format (CI style) | `dotnet tool restore && dotnet tool run csharpier check .` |
| Format everything | `dotnet tool restore && dotnet tool run csharpier format .` |
| EF migration | `./scripts/generate-migration.sh <Name>` |
| Docker build | `docker compose up -d` |

## Toolchain

- **.NET 10 SDK** required (all projects target `net10.0` — this is a preview SDK).
- **CSharpier** for formatting: `dotnet tool restore` installs it locally. Config: `printWidth: 400` (intentionally wide).
- **EF Core 10.0**: migrations in `AISpace.Common/DAL/Migrations/`, tool `dotnet-ef` managed in `dotnet-tools.json`.
- **Coverage**: Coverlet collector on test projects; `./scripts/run-coverage.sh` then `./scripts/reportgenerator.sh` (ReportGenerator via `dotnet-tools.json`).
- **Tests use xunit.v3** (not v2). Test projects have `OutputType Exe` and global `using Xunit`. Moq for mocking, SQLite in-memory for DB integration tests. Use `TestContext.Current.CancellationToken` for test cancellation.

## Project dependency graph (strict — no cycles)

```text
AISpace.Network  (no project deps — wire format + transport only)
       ↑
AISpace.Common   (game logic, EF Core DAL, packet handlers)
       ↑
AISpace.Server   (executable; ASP.NET Core host + BackgroundServices)
```

Each layer must not reference anything above it. `AISpace.Network` has zero game logic or DB entities.

## Architecture

### Single process, three domain servers

All three game servers run as `BackgroundService` instances inside one process:

| Server | Port | ServerType enum |
| -------- | ------ | ----------------- |
| Auth | 50050 | `ServerType.Auth` (1) |
| Msg | 50052 | `ServerType.Msg` (3) |
| Area | 50054 | `ServerType.Area` (2) |

Each derives from `GameServerBase<T>` (`GameServerBase.cs:50`) which owns a TCP `VceListener`, a `Channel<Packet>` dispatch loop, and a 60 Hz tick timer.

### Packet handler pattern

Packet handlers implement `IPacketHandler` (`PacketHandlerBase.cs:21`) with three properties:

- `RequestType` (`PacketType` enum)
- `ResponseType` (`PacketType` enum)
- `ServerType` (`ServerType` enum)

They are auto-discovered by Scrutor at startup (no manual registration needed). Place new handlers in `AISpace.Common/Handlers/<Domain>/`.

The generic base class `PacketHandlerBase<TRequest, TResponse>` deserializes the request from bytes and serializes the response automatically. If a handler returns `null`, no response is sent.

### Packet types

`PacketType.cs` in `AISpace.Network/` — a master enum (~600 entries) annotated with `[PacketMetadata]` attributes specifying domain, ID, and name.

### Adding a new packet/feature

1. Packet DTOs in `AISpace.Network/Packets/<Domain>/` (implement `IIncomingPacket<T>` / `IOutgoingPacket`)
2. Entry in `AISpace.Network/PacketType.cs`
3. Handler class in `AISpace.Common/Handlers/<Domain>/` (auto-discovered)
4. Persistence in `AISpace.Common/DAL/` if needed, then generate migration

## Database

- **Default**: SQLite at `db/main.db` (relative to the process working directory). Override via `Server:DbOptions` in config or `Server__DbOptions__ConnectionString` (Docker: `/data/main.db` with compose volume `aisp-data` → `/data`).
- **Also supports**: SQL Server (packages are referenced).
- **Migrations auto-applied** at startup via `db.Database.MigrateAsync()` in `Program.cs:193`.
- **Seeding**: `Program.cs` calls `Seed*IfEmptyAsync` helpers on repositories for maps, map links, worlds, channels. Items are seeded once at startup in `Program.cs` (after migrations) via `ItemRepository.SeedItemsIfEmptyAsync` from `seedData/baseItems.json` (under `AISpace.Common/`, copied to output). Runtime code reads items from the database only.
- **Integration tests**: Use `TestDb.CreateInMemoryMainContext()` (`Support/TestDb.cs:10`) for a disposable SQLite in-memory context.
- **Migration command** (from repo root):

  ```bash
  dotnet ef migrations add <Name> --project AISpace.Common/AISpace.Common.csproj --startup-project AISpace.Server/AISpace.Server.csproj --context MainContext --output-dir DAL/Migrations
  ```

  Or use `./scripts/generate-migration.sh <Name>`.

## Configuration

- `appsettings.json` in `AISpace.Server/` — copied to output on build (`PreserveNewest`).
- `IP_OVERRIDE` env var (or `Server__IPOverride` config key) replaces `localhost` in server addresses (required for Docker).
- `ApiSettings.ApiKey` — API endpoints under `/api/` require `X-Api-Key` header matching this value.
- Per-server enable/disable and port via `Server:AuthServer`, `Server:MsgServer`, `Server:AreaServer` config sections.
- Maintenance mode configured via `Maintenance` section; `ScheduledMaintenanceService` handles timed server shutdown.

## CI / CD

- **Build + test** on push to `master` (`.github/workflows/build.yml`)
- **Format check** with CSharpier on PR/push to `master` (`.github/workflows/format.yml`)
- **Docker publish** to `ghcr.io` on push to `master` (`docker-publish.yml`)
- **Manual deploy** via `workflow_dispatch` (`deploy.yml`) — SSHs to VPS and runs `docker compose pull && up -d`

## Formatting

- CSharpier (via `dotnet tool run csharpier format`). Config: `printWidth: 400`.
- Two equivalent hook setups:
  - **Shell** (recommended): `git config core.hooksPath .githooks` — formats only staged C#/MSBuild files.
  - **Python pre-commit**: `pre-commit install` (uses `.pre-commit-config.yaml`).
- CI (`format.yml`) runs `csharpier check .` (not staged-only). Make sure local formatting passes before push.

## Logging

- NLog with console + rolling file targets. Config in `AISpace.Server/NLog.config`.
- `AISpace.MissingPackets` logger captures unhandled packet types with hex dumps (`PacketDispatcher.cs:23`).

## Encryption

- Custom RSA-16 key exchange + Camellia-128 block cipher in `AISpace.Network/Crypto/`. These are reverse-engineered from the original client.

## Testing notes

- `xunit.v3` with `OutputType Exe` — test projects are standalone executables.
- Integration tests that need a DB use SQLite in-memory via `TestDb.CreateInMemoryMainContext()`.
- `AISpace.Common.Tests` has a `Support/CapturingPlayerSession.cs` helper for capturing handler responses without a real network connection.
- Tests with `Distributed` in the name exercise the `SessionPresenceRepository` / `PendingMapTransferRepository` singletons (used for cross-server state sharing).

## Environment / Docker

- `Dockerfile` at `AISpace.Server/Dockerfile` (build context is repo root). Multi-stage Debian/glibc build.
- `docker-compose.yml` includes an `autoheal` sidecar to restart unhealthy containers.
- Healthcheck hits `/healthz` which returns 503 if any game server is unhealthy.

## Docs

See `docs/Project-Layout.md` for a deeper architecture walkthrough and `docs/Encryption.md` / `docs/PacketLayout/` for packet-level details.

## Decompiled client reference (`localDocs/aisp-decompiled.c`)

The 27 MB / 925k-line Hex-Rays decompile is the primary source for packet opcodes, field layouts, and client-side protocol logic. See `localDocs/aisp-decompiled-packet-llm-guide.md` for search strategies.

### Search acceleration indices

Run `python3 scripts/index-decompiled.py` to generate three assistive files:

| File | What | Size |
| --- | --- | --- |
| `localDocs/aisp-func-index.json` | Function name → `{addr, start, end}` for all ~21k functions | ~2 MB |
| `localDocs/aisp-protocol.c` | Protocol-relevant subset (13x smaller than original) | ~3 MB |
| `localDocs/aisp-dispatch.json` | 581 recv dispatch entries with opcode + alloc size | ~84 KB |

**Suggested agent workflow**:
1. Grep the smaller `aisp-protocol.c` for a packet name or opcode
2. Or look up the function in `aisp-func-index.json` → read exact line range with `sed`
3. For recv packets, check `aisp-dispatch.json` for alloc size (complexity hint) and exact line
4. After extracting the layout, map into `AISpace.Network/PacketType.cs` + handlers
