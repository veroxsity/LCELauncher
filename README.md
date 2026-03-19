# LCE Launcher

Phase-one Windows desktop launcher for the Minecraft Legacy Edition workspace.

## Current Scope

- local auth mode
- launcher-managed `username.txt`
- launcher-managed `servers.db`
- native LCE server entries
- Java bridge-backed server entries with stable localhost port mapping
- managed bridge startup before launching the client
- manual path configuration for the built client and bridge jar

Online auth, downloads, and update flow are intentionally deferred to later phases from `.local/launcher_plan.md`.

## Project

- Project path: `launcher`
- Framework: `.NET 8` WinForms

## Run

```powershell
cd launcher
dotnet run
```

## Build

```powershell
cd launcher
dotnet build
```

## Default Discovery

On first run, the launcher tries to auto-detect:

- the latest `Minecraft.Client.exe` under `client/`
- the latest built bridge jar under `bridge/scripts/output/` or `bridge/_build/bootstrap-standalone/libs/`

Launcher state is stored under:

```text
%LOCALAPPDATA%\Minecraft Legacy Edition\Launcher
```
