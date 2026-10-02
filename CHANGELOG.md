# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/burakdmrbkr/Celsius/compare/v0.1.0-beta...HEAD
[0.1.0-beta]: https://github.com/burakdmrbkr/Celsius/releases/tag/v0.1.0-beta
