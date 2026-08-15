# AI-Space Emulator

AI-Space Emulator is a fan-made server emulator for the discontinued Japanese MMO **AISp@ce**. The goal of this project is to recreate the original experience for educational purposes and to keep the game playable after its shutdown.

## Project scope

- Provides a replacement server stack for AISp@ce, implemented in .NET 10.0 with Entity Framework Core and NLog for persistence and logging.
- Serves as a learning resource for networking, game server architecture, and reverse engineering of legacy online games.
- Does **not** ship original game assets. For game data and metadata about AISp@ce itself, refer to the community-maintained archive at [Tricon2-Elf/AI-Space](https://github.com/Tricon2-Elf/AI-Space).

## Repository layout

- `aisp.Network/` — Wire format, TCP transport, and client-compatible encryption (no game logic or database code).
- `aisp.Common/` — Game logic, packet handlers, and Entity Framework Core persistence; references `aisp.Network`.
- `aisp.Server/` — Executable host; runs Auth, Msg, and Area servers in one process. Includes runtime configuration such as `appsettings.json` and `NLog.config`.
- `tests/aisp.Network.Tests/`, `tests/aisp.Common.Tests/`, `tests/aisp.Server.Tests/` — xUnit test projects for each layer.

Dependency order: `aisp.Network` → `aisp.Common` → `aisp.Server`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download) for building and running the server projects.
- Access to AISp@ce client metadata and information; see the [AI-Space archive](https://github.com/Tricon2-Elf/AI-Space) for historical game metadata.

## Building

From the repository root:

```bash
dotnet restore
dotnet build
```

This restores NuGet dependencies and compiles all projects in the solution.

## Testing

Run the full test suite from the repository root:

```bash
dotnet test aisp.sln
```

Run tests for a single project:

```bash
dotnet test aisp.Common.Tests
dotnet test aisp.Network.Tests
dotnet test aisp.Server.Tests
```

Tests use **xUnit v3** with in-memory SQLite for database integration tests. See `AGENTS.md` for more detail on test conventions and helpers.

## Running the server

After building, start the server project:

```bash
dotnet run --project aisp.Server
```

Configuration files such as `appsettings.json` and `NLog.config` are copied to the output directory during build. Adjust database providers or logging behavior there before launching.

## Contributing

Community contributions are welcome. Please ensure changes respect the educational, non-commercial nature of the project. If you contribute reverse-engineered insights or packet captures, avoid including any proprietary assets.

## Disclaimer

This emulator is an unofficial, community effort provided for educational and preservation purposes. It is not affiliated with the original AISp@ce developers or publishers.

## AI-Assisted Development

This project makes use of AI-assisted development tools to help accelerate certain tasks. This choice is driven by limited development time and other constraints that make traditional pacing difficult. AI assistance allows the project to maintain momentum despite these challenges.

**All AI-assisted code is reviewed, tested, and verified before being committed and all responsibility of said code is owned by the author and not the LLM** No code reaches the repository without human oversight. The use of AI is strictly assistive — it does not replace manual code review, testing, or architectural decision-making.
