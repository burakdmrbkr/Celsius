# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.3-beta] - 2026-10-03

### Added

- Official application logo: a red thermometer mark used as the app icon
  (`Celsius.exe`), the system tray icon, the installer wizard icon and the
  README. Source `src/Celsius.App/Assets/celsius.png`; the multi-resolution
  `.ico` is produced by `tools/make-icon.ps1`.

## [0.1.2-beta] - 2026-10-03

### Fixed

- **CPU temperature value rendered black** on the dashboard. Its `Foreground`
  used a binding against a `DataContext` that is never set, so WPF fell back to
  `TextBlock`'s default (black) instead of the theme colour.

### Changed

- The CPU temperature value now **adapts its colour to the live reading**:
  green below 70 °C, amber from 70 °C, red from 85 °C.
- Shared the temperature colour logic via
  `TemperatureBrushConverter.ResolveColor` so the XAML converter and code-behind
  stay consistent.
- Minor dashboard polish: `TempNormal/Warm/Hot` theme brushes, plus styles for
  metric units, separators, tooltips and a slimmer scrollbar.

## [0.1.1-beta] - 2026-10-03

### Added

- **Inno Setup installer** (`Celsius-<version>-win-x64-setup.exe`) that bundles
  and installs the PawnIO kernel driver. Before installing it checks the
  installed PawnIO version and warns when it is missing or outdated, then
  installs/updates it automatically. English and Turkish UI.
- `tools/Diag`, a read-only diagnostic that reports elevation, CPU identity,
  thermal profile and sensor availability.
- On-screen warning in the main window when running elevated but no temperature
  sensors are available (points at the PawnIO driver).

### Fixed

- **All sensors disabled on boards with an invalid BIOS date** (e.g.
  `00/00/0000`): LibreHardwareMonitor 0.9.6 crashed in `SMBios.GetDate`, which
  aborted the whole hardware tree so CPU/GPU temperature, clock, load, memory
  and the GPU list all came back empty. Celsius now consumes a patched
  `LibreHardwareMonitorLib 0.9.6-celsius1` (see `tools/lhm-patch/`).
- `HardwareMonitorService` now reports `HasTemperatureSensors` only when a
  temperature value is actually read, enables motherboard/storage sensors,
  widens CPU/GPU temperature detection, and surfaces backend errors
  (`IHardwareMonitor.LastError` / `CelsiusEngine.SensorError`).

## [0.1.0-beta] - 2026-10-02

### Added

- CPU monitoring: temperature, clock and load, with automatic sensor selection
  for Intel (`CPU Package`) and AMD (`Core (Tctl/Tdie)`).
- Memory usage and per-drive disk usage.
- GPU name, temperature and clock (NVIDIA / AMD / Intel) via LibreHardwareMonitor,
  with DXGI-based enumeration.
- Manufacturer-based thermal threshold resolution: hardware `TjMax` value →
  bundled Intel 6th–14th gen / AMD Zen–Zen5 lookup table → 90 °C default.
- Time-boxed CPU stress test with 1/2/3/5 min presets and a custom duration
  (1–60 min).
- Live CPU temperature graph during stress testing and an end-of-run summary
  (min / max / average temperature, duration, throttle events, stop reason).
- Thermal guard that stops the stress test at `TjMax − 5 °C`, independent of the
  remaining time.
- System tray icon with a live CPU-temperature tooltip and a Show / Stress Test /
  Exit menu.
- Settings window and `%AppData%\Celsius\settings.json` persistence (poll
  interval, default stress duration, thread count, close behaviour, start
  minimized, language).
- English and Turkish UI (resx localization, switchable at runtime).
- Error/critical file logging to `%AppData%\Celsius\logs`.
- GitHub Actions CI (build + test) and release workflow (self-contained
  `win-x64` single-file publish on `v*` tags), plus Dependabot.

[Unreleased]: https://github.com/burakdmrbkr/Celsius/compare/v0.1.3-beta...HEAD
[0.1.3-beta]: https://github.com/burakdmrbkr/Celsius/releases/tag/v0.1.3-beta
[0.1.2-beta]: https://github.com/burakdmrbkr/Celsius/releases/tag/v0.1.2-beta
[0.1.1-beta]: https://github.com/burakdmrbkr/Celsius/releases/tag/v0.1.1-beta
[0.1.0-beta]: https://github.com/burakdmrbkr/Celsius/releases/tag/v0.1.0-beta
