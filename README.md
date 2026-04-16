# LCE Launcher

![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-blue?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![Avalonia](https://img.shields.io/badge/UI-Avalonia%2012-blueviolet?style=flat-square)
![License](https://img.shields.io/github/license/veroxsity/LCELauncher?style=flat-square)
![Last Commit](https://img.shields.io/github/last-commit/veroxsity/LCELauncher?style=flat-square)

A cross-platform desktop launcher for **Minecraft Legacy Console Edition**, built as part of the [LCE dedicated server project](https://github.com/veroxsity/MinecraftLCE). It handles everything you need to get in-game — authentication, client installs, bridge runtime management, and server connections — wrapped in a clean interface inspired by the official Minecraft Java launcher.

<p align="center">
  <img src="assets/screenshot.png" alt="LCE Launcher screenshot" width="780" />
</p>

---

## Features

- **Microsoft account sign-in** — authenticates via Xbox Live / XSTS, syncs your gamertag and profile picture automatically
- **Managed installs** — downloads and updates the LCE client and LCEBridge runtime with one click; no manual file management
- **Stream selection** — choose between Release Nightly and Debug Nightly builds independently for the client and bridge
- **Server launcher** — pick a server from your list and connect automatically on launch
- **Cross-platform** — runs natively on Windows, macOS, and Linux via Avalonia UI
- **Offline / local mode** — play without a Microsoft account using a local username

---

## Requirements

| Dependency | Version |
|---|---|
| [.NET Runtime](https://dotnet.microsoft.com/download) | 9.0 or newer |
| [Java](https://adoptium.net/) | 17 or newer (for LCEBridge) |

---

## Getting Started

### Download

Grab the latest release from the [Releases](https://github.com/veroxsity/LCELauncher/releases) page for your platform.

### Build from source

```bash
git clone https://github.com/veroxsity/LCELauncher.git
cd LCELauncher
dotnet build -c Release
dotnet run --project LceLauncher
```

---

## Usage

1. **Sign in** — go to Settings → Account and sign in with Microsoft, or enter a local username for offline play.
2. **Install the client** — go to the Downloads tab and install the Release Nightly client build.
3. **Install the bridge** — install the LCEBridge runtime from the same Downloads tab (requires Java 17+).
4. **Play** — select a server if desired and hit **PLAY**.

---

## Project Structure

```
LceLauncher/
├── Assets/          # Icons, hero image, logo
├── Models/          # Data models (config, auth, profiles)
├── Services/        # Auth, download, client/bridge management
├── ViewModels/      # MVVM view models (CommunityToolkit.Mvvm)
└── Views/           # Avalonia XAML views
```

---

## Related Projects

| Project | Description |
|---|---|
| [LCEClient](https://github.com/veroxsity/LCEClient) | The Minecraft Legacy Console Edition client |
| [LCEBridge](https://github.com/veroxsity/LCEBridge) | Java-to-LCE protocol bridge |
| [LCEServer](https://github.com/veroxsity/LCEServer) | Dedicated server |
| [MinecraftLCE](https://github.com/veroxsity/MinecraftLCE) | Parent monorepo |

---

## Contributing

Pull requests are welcome. For significant changes, please open an issue first to discuss what you'd like to change.

---

## License

See [LICENSE](LICENSE) for details.

> *This project is not affiliated with Mojang Studios or Microsoft.*
