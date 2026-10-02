# Celsius

[![CI](https://github.com/burakdmrbkr/Celsius/actions/workflows/ci.yml/badge.svg)](https://github.com/burakdmrbkr/Celsius/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/burakdmrbkr/Celsius?include_prereleases)](https://github.com/burakdmrbkr/Celsius/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

> A lightweight Windows system monitor and CPU stress tester that lives in your
> system tray. Watch your CPU temperature, clock and load, memory, disks and GPU —
> then push your CPU to its limits with a time-boxed stress test and watch the
> thermal curve live.

<!-- Screenshot placeholder — replace with a real screenshot of the main window -->
![Celsius dashboard](docs/screenshots/dashboard.png)

<!-- GIF placeholder — replace with a short recording of a stress test -->
![Stress test](docs/screenshots/stress-test.gif)

---

## ⚠️ Important — please read first

- **Administrator rights are required.** Reading CPU package temperature on
  Windows needs low-level sensor access via the **LibreHardwareMonitor** kernel
  driver (**PawnIO**). Without elevation, temperature sensors are unavailable
  (other metrics still work). The app ships an `app.manifest` with
  `requireAdministrator`, so **Windows will show a UAC prompt on every launch**.
- **Unsigned build.** Releases are **not code-signed**. Windows SmartScreen (and
  some antivirus products) may warn you on first launch. Choose "More info" →
  "Run anyway" if you trust the source, or build from source yourself.
- **Kernel driver warning.** Because Celsius loads a low-level hardware driver,
  **some anti-cheat software (e.g. Vanguard, FACEIT) may flag or block it.** Do
  not run Celsius alongside competitive online games.
- **Stress testing generates real heat.** The stress test intentionally drives
  your CPU to 100%. Celsius stops automatically **5 °C below your CPU's
  manufacturer-rated maximum temperature** (`TjMax − 5 °C`), but you are still
  responsible for adequate cooling. Use at your own risk.

## Features

- 🌡️ **CPU temperature** with automatic, manufacturer-based safety threshold
- ⏱️ **CPU clock** and **CPU load**
- 🧠 **Memory** usage (%)
- 💾 **Disk** usage for every drive
- 🎮 **GPU** name, temperature and clock (NVIDIA / AMD / Intel)
- 🔥 **Time-boxed CPU stress test** — 1 / 2 / 3 / 5 min presets or a custom
  duration (1–60 min), with a **live temperature graph** and an end-of-run summary
- 🛡️ **Thermal guard** — the stress test stops automatically if the CPU gets too
  hot, regardless of the remaining time
- 📌 **System tray** — live CPU temperature in the tooltip, quick access to the
  dashboard and stress test
- 🌍 **English and Turkish** UI
- ⚙️ **Settings** — poll interval, default stress duration, thread count,
  close-to-tray behaviour

## Requirements

- **Windows 10 or 11 (x64)**
- **Administrator rights** (for temperature sensors)
- No .NET installation needed — release builds are **self-contained**

## Install

### From GitHub Releases (recommended)

1. Go to the [Releases](https://github.com/burakdmrbkr/Celsius/releases) page.
2. Download `Celsius-<version>-win-x64.zip`.
3. Extract the archive.
4. Run **`Celsius.exe`** and accept the UAC prompt.

> The build is self-contained, so the .NET runtime does not need to be installed.

## Build from source

Requires the **.NET 10 SDK**.

```powershell
git clone https://github.com/burakdmrbkr/Celsius.git
cd Celsius

# Build the patched LibreHardwareMonitorLib NuGet package (see tools/lhm-patch/).
# Required: Celsius consumes it from the local-nuget feed.
pwsh -File tools/lhm-patch/build-patched-lhm.ps1

dotnet build Celsius.slnx -c Release
dotnet test  Celsius.slnx -c Release

# Run the app (will prompt for elevation)
dotnet run --project src/Celsius.App

# Produce a self-contained single-file build
dotnet publish src/Celsius.App/Celsius.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o publish
```

> **Why the patched LibreHardwareMonitorLib?** Upstream 0.9.6 throws while
> parsing an invalid BIOS date (some boards report `00/00/0000`), which aborts
> hardware enumeration and disables **every** sensor. See
> [`tools/lhm-patch/README.md`](tools/lhm-patch/README.md). Remove the step once
> upstream fixes it.

## How the CPU thermal threshold works

Windows has no official API for CPU core temperature, so Celsius uses
[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
and derives a **safe stop threshold** from your CPU's own manufacturer-published
maximum temperature (`TjMax`), resolved in this order:

1. **Hardware value** — LibreHardwareMonitor exposes a `TjMax [°C]` sensor
   parameter (read from Intel MSR `0x1A2`, bits `[23:16]`). This is the
   authoritative, hardware-reported value.
2. **Bundled lookup table** — for CPUs where the hardware value is unavailable
   (notably **AMD**, which does not use that MSR), Celsius ships a curated table
   covering **Intel 6th–14th gen** and **AMD Zen–Zen5**.
3. **Conservative default** — **90 °C** if neither source resolves.

The stress test then **stops automatically at `TjMax − 5 °C`**, clamped to a
sane range (never below 70 °C, never above `TjMax`). The source used is written
to the log so problems are diagnosable.

> `TjMax` is the throttle/shutdown point, **not** a safe everyday temperature.

## Usage

- **Main window** shows a list of live metrics. Click **Stress Test** to open the
  stress test window.
- **Stress test**: pick a preset (1/2/3/5 min) or type a custom duration in
  minutes (1–60), then press Start. The live graph plots CPU temperature; the run
  ends when the timer expires **or** the thermal guard trips. A summary (min /
  max / average temperature, duration, throttle events) is shown at the end.
- **Tray icon**: left-click to show the window, right-click for the menu
  (Show / Stress Test / Exit). The tooltip shows the current CPU temperature.
- **Closing the window**: by default Celsius **minimises to the tray**. Change
  this in **Settings** if you prefer closing to exit.

## Configuration

Settings are stored at:

```
%AppData%\Celsius\settings.json
```

| Setting | Description | Default |
|---|---|---|
| `PollIntervalSeconds` | Sensor polling interval (0.5–10) | `1` |
| `LastStressDurationMinutes` | Default stress duration (1–60) | `1` |
| `StressWorkerCount` | Stress worker threads (`0` = all logical cores) | `0` |
| `CloseBehavior` | `MinimizeToTray` or `Exit` | `MinimizeToTray` |
| `StartMinimized` | Start hidden in the tray | `false` |
| `Language` | UI language (`en` / `tr`), `null` = system | system |

Logs (errors/critical only) are written to:

```
%AppData%\Celsius\logs
```

## Troubleshooting / FAQ

**Temperature shows "—" or is missing.**
Two things are required:

1. Launch `Celsius.exe` **as administrator**. Without elevation the kernel
   driver cannot be used and temperature sensors stay unavailable.
2. Install the **PawnIO** kernel driver (see [docs/PAWNIO.md](docs/PAWNIO.md)).
   LibreHardwareMonitor 0.9.6+ no longer uses the old WinRing0 driver (Windows
   blocks it), so CPU/GPU temperatures now require PawnIO. If elevation is on
   but temperatures are still missing while CPU model/clock/load, memory and
   disk work, the driver is the cause — Celsius shows an on-screen warning in
   that case.

If **no** sensor at all works (temperature, clock, load, memory and GPU list all
empty) even when elevated and with PawnIO installed, the machine likely reports
an invalid BIOS date (e.g. `00/00/0000`), which crashes the stock
LibreHardwareMonitor 0.9.6 SMBios parser. This repo ships a patched build — see
[`tools/lhm-patch/README.md`](tools/lhm-patch/README.md). Run
`tools/Diag` to check: it prints elevation, the CPU/thermal profile and whether
temperature sensors are available.

CPU load, clock, memory, disk and GPU *names* work without the driver.

**Some sensors are unavailable on my laptop.**
Discrete laptop GPUs are often powered down when idle. Celsius shows "—" instead
of an error in that case; the sensors become available once the GPU is in use.

**Windows SmartScreen or antivirus blocked the app.**
Release builds are unsigned. This is expected. Build from source if you prefer,
or use "More info" → "Run anyway".

**My game's anti-cheat complains.**
Celsius loads a low-level hardware driver. Don't run it alongside competitive
online games; close Celsius first.

**The stress test stopped before the timer ended.**
That's the thermal guard: the CPU reached `TjMax − 5 °C`. Improve cooling or
lower your ambient temperature before retrying.

## Contributing

Contributions are welcome! Please read [`CONTRIBUTING.md`](CONTRIBUTING.md) and
our [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md) first.

## License

Licensed under the [MIT License](LICENSE).

This project depends on [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor)
(MPL-2.0) and [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows)
(MIT). See [`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt) for details.
