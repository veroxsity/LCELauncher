# LCE Launcher (New) — Architecture & Implementation Plan

## Overview

A cross-platform (Windows / macOS / Linux) game launcher for Minecraft Legacy Console Edition,
built with **Avalonia UI** on **.NET 8**. It replaces the WinForms launcher with a fresh UI while
reusing all business logic from the original.

---

## Technology Stack

| Layer | Technology |
|---|---|
| UI framework | Avalonia UI 11 (`Avalonia.Desktop`) |
| UI theme | Avalonia.Themes.Fluent (custom-styled) |
| UI pattern | MVVM via ReactiveUI (`Avalonia.ReactiveUI`) |
| Language | C# 12 / .NET 8 (`net8.0`, not `net8.0-windows`) |
| Auth | Microsoft.Identity.Client (already cross-platform) |
| Config / serialisation | System.Text.Json (same as original) |
| HTTP | System.Net.Http.HttpClient (same as original) |

---

## What Gets Reused from `launcher/`

The following files can be copied nearly verbatim — they have no WinForms dependencies:

```
Models/
    AuthMode.cs
    BridgeAuthContext.cs
    ClientServerEntry.cs
    LauncherConfig.cs
    ManagedBridgeInstallInfo.cs
    ManagedBridgeUpdateInfo.cs
    ManagedClientInstallInfo.cs
    ManagedClientStream.cs
    ManagedClientUpdateInfo.cs
    ManagedClientUpdateState.cs
    OnlineAccountProfile.cs
    ServerEntry.cs
    ServerType.cs

Services/
    BridgeConfigRenderer.cs
    BridgeInstallService.cs
    BridgeRuntimeManager.cs
    ClientInstallService.cs
    ClientProfileManager.cs
    LaunchCoordinator.cs
    LauncherAuthService.cs
    LauncherConfigService.cs
    LauncherLogger.cs
    ServerListWriter.cs
    ServerManager.cs
```

**What changes:**
- `AppPaths` — updated for cross-platform data directories (see below)
- `Program.cs` — Avalonia entry point instead of WinForms
- Everything in `UI/` — completely fresh Avalonia AXAML + ViewModels

---

## Cross-Platform Data Paths (`AppPaths`)

| OS | Data root |
|---|---|
| Windows | `%LOCALAPPDATA%\Minecraft Legacy Edition\Launcher` |
| macOS | `~/Library/Application Support/Minecraft Legacy Edition/Launcher` |
| Linux | `~/.local/share/minecraft-legacy-edition/launcher` |

All sub-paths (installs, downloads, auth, logs, etc.) stay the same relative to the data root.

---

## Project Structure

```
launcher-new/
├── LceLauncher.sln
├── LceLauncher/
│   ├── LceLauncher.csproj          # net8.0, Avalonia packages
│   ├── Program.cs                  # Avalonia AppBuilder entry point
│   ├── App.axaml / App.axaml.cs   # Application root, theme
│   │
│   ├── Models/                     # (copied from launcher/)
│   ├── Services/                   # (copied from launcher/, AppPaths updated)
│   │
│   ├── ViewModels/
│   │   ├── MainWindowViewModel.cs  # Navigation state, top-level VM
│   │   ├── PlayViewModel.cs        # Client install, update, launch
│   │   ├── ServersViewModel.cs     # Server list management
│   │   ├── SettingsViewModel.cs    # Auth, paths, bridge options
│   │   ├── AboutViewModel.cs       # Version info, links
│   │   └── ServerEditViewModel.cs  # Add/edit server dialog
│   │
│   └── Views/
│       ├── MainWindow.axaml/.cs    # Shell: sidebar + content area
│       ├── PlayView.axaml/.cs
│       ├── ServersView.axaml/.cs
│       ├── SettingsView.axaml/.cs
│       ├── AboutView.axaml/.cs
│       └── ServerEditDialog.axaml/.cs
```

---

## UI Layout

### Shell — `MainWindow`

```
┌──────────────────────────────────────────────────┐
│  [■ LCE Launcher]                  [— □ ×]       │  ← custom titlebar (Avalonia handles this)
├─────────┬────────────────────────────────────────┤
│         │                                        │
│  PLAY   │   <content area — swaps with nav>      │
│         │                                        │
│ SERVERS │                                        │
│         │                                        │
│ SETTINGS│                                        │
│         │                                        │
│  ABOUT  │                                        │
│         │                                        │
└─────────┴────────────────────────────────────────┘
```

Sidebar is a fixed-width panel with icon + label nav items. Selected item highlights.
Content area swaps the active View based on navigation state.

---

### Play Page — `PlayView`

Two panels side by side (or stacked on narrow):

**Client panel**
- Stream selector: `Release Nightly` / `Debug Nightly` (radio or tab)
- Installed version + date
- Status chip: Up to date / Update available / Not installed
- Button: Install / Update / Repair
- Progress bar (shown during download/extract)

**Bridge panel**
- Installed version + date
- Status chip
- Button: Install / Update
- Progress bar

**Bottom bar**
- Server dropdown (populated from server list)
- `PLAY` button (big, prominent)
- Launch status / error message area

---

### Servers Page — `ServersView`

- List of server cards: name, type badge (Native / Java Bridge), address:port
- `+ Add Server` button
- Each card: Edit ✎ / Delete 🗑 actions
- Add/Edit opens `ServerEditDialog` (modal)

**ServerEditDialog fields:**
- Display Name
- Type: Native LCE / Java Bridge (segmented control)
- Remote Address + Port
- [JavaBridge only] Requires Online Auth toggle
- Save / Cancel

---

### Settings Page — `SettingsView`

Sections:

**Account**
- Auth mode: Local / Microsoft
- Local: username text field
- Microsoft: Sign in button, signed-in account display, Sign out

**Client**
- Prefer managed install toggle
- Custom executable path (override, shown when managed is off)
- Extra launch arguments text field

**Bridge**
- Prefer managed bridge toggle
- Custom bridge jar path (override)
- Java executable path
- Log level dropdown (trace / debug / info / warn / error)
- Log packets toggle

**Advanced**
- First bridge port (number input)
- Close bridge on launcher exit toggle
- Microsoft Auth Client ID (override)

---

### About Page — `AboutView`

- Project name + version
- Links: GitHub repos (client, bridge, launcher)
- Open logs folder button
- Open data folder button

---

## Key Implementation Notes

### Auth (MSAL)
`LauncherAuthService` and `Microsoft.Identity.Client` are already cross-platform.
Token cache path comes from `AppPaths.MsalTokenCachePath`.

### Bridge process
`BridgeRuntimeManager` starts `java -jar bridge.jar config.yml` via `Process.Start`.
This works identically on all platforms — no changes needed.

### Client launch
`LaunchCoordinator` launches `Minecraft.Client.exe` via `Process.Start`.
On Windows this works natively. On Linux/Mac the user would need Wine/Proton — the launcher
just passes through whatever executable path is configured, so it stays flexible.

### Progress reporting
`ClientInstallService` and `BridgeInstallService` currently do not report progress granularly.
When porting, add `IProgress<double>` parameters to `DownloadFileAsync` so the UI progress
bar can bind to download progress. This is the main service-layer change.

### Reactive bindings
Use ReactiveUI `ReactiveObject` base for ViewModels, with `[Reactive]` properties and
`ReactiveCommand` for actions. Avalonia binds to these natively via its binding engine.

---

## NuGet Packages

```xml
<PackageReference Include="Avalonia" Version="11.x" />
<PackageReference Include="Avalonia.Desktop" Version="11.x" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.x" />
<PackageReference Include="Avalonia.ReactiveUI" Version="11.x" />
<PackageReference Include="ReactiveUI.Fody" Version="19.x" />        <!-- [Reactive] attribute -->
<PackageReference Include="Microsoft.Identity.Client" Version="4.x" />
```

---

## Implementation Order

1. **Scaffold** — `.sln`, `.csproj`, `Program.cs`, `App.axaml`, empty Views/VMs
2. **Copy Models** — verbatim from `launcher/`
3. **Copy + adapt Services** — verbatim except `AppPaths` cross-platform update
4. **Add progress to download methods** — `IProgress<(long, long)>` on `DownloadFileAsync`
5. **MainWindow shell** — sidebar nav, content routing
6. **PlayView + PlayViewModel** — install/update client and bridge, progress bars, launch
7. **ServersView + ServersViewModel** — list, add/edit/delete
8. **ServerEditDialog** — modal for server editing
9. **SettingsView + SettingsViewModel** — all config options
10. **AboutView** — static info + folder openers
11. **Crash handling** — global exception handler in `Program.cs`
12. **Polish** — styling, icons, transitions, error states

---

## Out of Scope (for now)

- Auto-updater for the launcher itself
- macOS `.app` bundle / code signing
- Linux `.AppImage` / `.deb` packaging
- Windows installer (MSIX / Inno Setup)
- Dark/light mode toggle (use system theme via Avalonia)
