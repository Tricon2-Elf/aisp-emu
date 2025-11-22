# AI-Space Private Server

AI-Space Private Server is a fan-made emulator for the discontinued Japanese MMO **AISp@ce**. The goal of this project is to recreate the original experience for educational purposes and to keep the game playable after its shutdown.

## Project scope

- Provides a replacement server stack for AISp@ce, implemented in .NET 9.0 with Entity Framework Core and NLog for persistence and logging. 
- Serves as a learning resource for networking, game server architecture, and reverse engineering of legacy online games.
- Does **not** ship original game assets. For game data and metadata about AISp@ce itself, refer to the community-maintained archive at [Tricon2-Elf/AI-Space](https://github.com/Tricon2-Elf/AI-Space).

## Repository layout

- `AISpace.Server/` — Executable server project; references `AISpace.Common` and includes runtime configuration such as `appsettings.json` and `NLog.config`.
- `AISpace.Common/` — Shared library that contains cryptography, hosting abstractions, and Entity Framework Core components shared by server modules.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download) for building and running the server projects.
- Access to AISp@ce client metadata and information; see the [AI-Space archive](https://github.com/Tricon2-Elf/AI-Space) for historical game metadata.

## Building

From the repository root:

```bash
dotnet restore
dotnet build
```

This restores NuGet dependencies and compiles both the server and shared library projects in the solution.

## Running the server

After building, start the server project:

```bash
dotnet run --project AISpace.Server
```

Configuration files such as `appsettings.json` and `NLog.config` are copied to the output directory during build. Adjust database providers or logging behavior there before launching.

## Contributing

Community contributions are welcome. Please ensure changes respect the educational, non-commercial nature of the project. If you contribute reverse-engineered insights or packet captures, avoid including any proprietary assets.

## Disclaimer

This emulator is an unofficial, community effort provided for educational and preservation purposes. It is not affiliated with the original AISp@ce developers or publishers.
