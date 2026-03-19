# LCE Launcher

<p align="center">
  <img src="https://img.shields.io/github/license/veroxsity/LCELauncher?style=for-the-badge" alt="License" />
  <img src="https://img.shields.io/github/last-commit/veroxsity/LCELauncher?style=for-the-badge" alt="Last Commit" />
  <img src="https://img.shields.io/github/repo-size/veroxsity/LCELauncher?style=for-the-badge" alt="Repo Size" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/UI-WinForms-0C7D9D?style=flat-square" alt="WinForms" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Windows" />
  <img src="https://img.shields.io/badge/Status-Phase%201-6BB941?style=flat-square" alt="Phase 1" />
</p>

Windows desktop launcher for the Minecraft Legacy Console Edition workspace.

The launcher is the player-facing entry point for local client startup, launcher-managed server entries, and bridge-backed Java server routing.

## Current Scope

- local auth mode
- launcher-managed `username.txt`
- launcher-managed `servers.db`
- native LCE server entries
- Java bridge-backed server entries with stable localhost port mapping
- managed bridge startup before launching the client
- runtime logs and bridge control from the launcher UI
- manual path configuration for the client, bridge jar, and Java runtime
- local launcher config persisted under `%LOCALAPPDATA%`

Online auth, download/install flow, and lightweight update flow are intentionally deferred to later phases from the workspace launcher plan.

## Repository

- Repository: `https://github.com/veroxsity/LCELauncher`
- Project path in the hub workspace: `launcher/`
- Framework: `.NET 8`
- App model: WinForms

## Screens

- `Play`: launch the client, launch a selected route, and inspect bridge status
- `Servers`: manage native LCE entries and Java bridge-backed routes
- `Settings`: configure client path, bridge jar path, Java path, and runtime behavior
- `Logs`: inspect launcher and bridge output

## Running

```powershell
git clone https://github.com/veroxsity/LCELauncher.git
cd LCELauncher
dotnet run
```

## Building

```powershell
dotnet build
```

## Workspace Usage

This launcher is designed to work especially well inside the full hub workspace:

```powershell
git clone --recurse-submodules https://github.com/veroxsity/MinecraftLCE.git
cd MinecraftLCE/launcher
dotnet run
```

In the hub workspace, the launcher can auto-detect:

- the latest `Minecraft.Client.exe` under `client/`
- the latest built bridge jar under `bridge/scripts/output/`
- the latest built bridge jar under `bridge/_build/bootstrap-standalone/libs/`

If you use the launcher repo on its own, configure the client and bridge paths manually in the launcher settings.

## Local Data

Launcher state is stored under:

```text
%LOCALAPPDATA%\Minecraft Legacy Edition\Launcher
```

That includes launcher config, runtime bridge data, and related local state.

## Current Limitations

- online auth is still a planned mode
- Microsoft sign-in is not implemented yet
- launcher-managed client download/install is not implemented yet
- lightweight client updates are not implemented yet

## Related Repositories

- Hub: `https://github.com/veroxsity/MinecraftLCE`
- Bridge: `https://github.com/veroxsity/LCEBridge`
- Client: `https://github.com/veroxsity/LCEClient`
- Server: `https://github.com/veroxsity/LCEServer`
