# LCELauncher

<p align="center">
  <img src="https://img.shields.io/github/license/veroxsity/LCELauncher?style=for-the-badge" alt="License" />
  <img src="https://img.shields.io/github/last-commit/veroxsity/LCELauncher?style=for-the-badge" alt="Last Commit" />
  <img src="https://img.shields.io/github/repo-size/veroxsity/LCELauncher?style=for-the-badge" alt="Repo Size" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square&logo=dotnet" alt=".NET 8" />
  <img src="https://img.shields.io/badge/UI-WinForms-0C7D9D?style=flat-square" alt="WinForms" />
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Windows" />
  <img src="https://img.shields.io/badge/Status-Active%20Development-6BB941?style=flat-square" alt="Active Development" />
</p>

Windows desktop launcher for the Minecraft Legacy Console Edition workspace.

LCELauncher is the player-facing entry point for local client startup, managed client installs, bridge-backed Java server routing, and Microsoft-based online auth.

## Current Scope

- local auth mode and online auth mode
- Microsoft device-code sign-in with Xbox Live and Minecraft service token exchange
- launcher-managed `username.txt`
- launcher-managed `servers.db`
- native LCE server entries
- Java bridge-backed server entries with stable localhost port mapping
- managed bridge install and update flow from `veroxsity/LCEBridge`
- managed client install, repair, and update flow for release and debug streams
- managed bridge startup before launching the client
- runtime logs and bridge control from the launcher UI
- manual path configuration for the client executable, bridge jar, and Java runtime
- local launcher config persisted under `%LOCALAPPDATA%`

## Repository

- Repository: `https://github.com/veroxsity/LCELauncher`
- Project path in the hub workspace: `launcher/`
- Framework: `.NET 8`
- App model: WinForms

## Screens

- `Play`: launch the client, launch a selected route, and inspect bridge status
- `Servers`: manage native LCE entries and Java bridge-backed routes
- `Settings`: configure sign-in, client path, bridge jar path, Java path, and runtime behavior
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

It can also install and maintain managed client streams under AppData, including the public `LCEDebug` nightly stream and the release stream sourced from `smartcmd/MinecraftConsoles`.

If you use the launcher repo on its own, you can either configure the client and bridge paths manually in the launcher settings or rely on the managed install flows.

## Local Data

Launcher state is stored under:

```text
%LOCALAPPDATA%\Minecraft Legacy Edition\Launcher
```

That includes launcher config, managed downloads and installs, runtime bridge data, and related local state.

## Current Limitations

- Windows-only desktop workflow
- Managed client streams are separate from local source builds in `LCEClient`
- Native server hosting is still handled by `LCEServer`; the launcher manages client-side routes and startup flow
- Java routes that require online auth still depend on a Microsoft account that owns Minecraft Java Edition

## Related Repositories

- Hub: `https://github.com/veroxsity/MinecraftLCE`
- Bridge: `https://github.com/veroxsity/LCEBridge`
- Client: `https://github.com/veroxsity/LCEClient`
- Debug: `https://github.com/veroxsity/LCEDebug`
- Server: `https://github.com/veroxsity/LCEServer`
