# Architecture

Celsius is split into a **UI-agnostic core** (`Celsius.Core`) and a **WPF front
end** (`Celsius.App`). The core owns all hardware access, thermal logic and the
stress engine; the app owns windows, the tray icon and localization. This keeps
the interesting logic unit-testable without a display or real hardware.

```
┌──────────────────────────────┐
│         Celsius.App          │  WPF, tray icon, resx localization
│  MainWindow / StressTestWindow│
│  SettingsWindow / Tray        │
└───────────────┬──────────────┘
                │  CelsiusEngine (facade)
┌───────────────▼──────────────┐
│         Celsius.Core         │
│  HardwareMonitorService      │  ← LibreHardwareMonitor, Vortice.DXGI
│  CpuInfoProvider             │  ← WMI / registry
│  ThermalThresholdResolver    │  ← TjMax: MSR parameter → JSON table → default
│  StressorService             │  ← TPL worker threads
│  ThermalGuard                │  ← stop-at-threshold safety
│  SettingsStore / FileLogger  │
└──────────────────────────────┘
```

## Projects

| Project | Target | Purpose |
|---|---|---|
| `src/Celsius.Core` | `net10.0-windows` | Hardware, thermal, stress, settings, logging |
| `src/Celsius.App` | `net10.0-windows` (WPF) | Dashboard, stress window, settings, tray |
| `tests/Celsius.Core.Tests` | `net10.0-windows` | xUnit unit tests for the core |
| `tools/SmokeTest` | `net10.0-windows` | Console harness for on-hardware verification |

## Key components

### `CelsiusEngine` (facade)

Composes the monitor, CPU provider, thermal resolver, settings and the stress
engine. The UI only ever talks to this object:

- `Initialize()` — opens hardware access, detects the CPU, resolves the thermal
  profile.
- `CaptureSnapshot()` — refreshes sensor values and returns a `SystemSnapshot`
  (should be called off the UI thread).
- `CreateThermalGuard()` — builds a `ThermalGuard` bound to the resolved profile.
- `Stressor` — the stress-test engine.

All hardware calls are wrapped in try/catch: a failure degrades gracefully (an
empty snapshot / fallback profile) rather than crashing the app, and the error is
logged.

### Hardware access — `HardwareMonitorService`

Wraps **LibreHardwareMonitor** (`Computer`) with CPU and GPU enabled.

- Sensors are polled on a background thread and pushed through an
  `UpdateVisitor`; `Computer.Close()` runs on dispose.
- CPU temperature is selected by vendor: **Intel → `CPU Package`**,
  **AMD → `Core (Tctl/Tdie)`**, with sensible fallbacks.
- Also reads CPU clock and load, memory usage, per-drive usage, and GPU
  temperature/clock.
- A whole-computer **`Mutex`/single-instance** guard around the low-level driver
  keeps concurrent readers from conflicting.

`HardwareMonitorService` implements `IHardwareMonitor`, so tests can substitute a
fake.

### CPU identity — `CpuInfoProvider`

Detects the CPU and normalizes its marketing name into a stable lookup key:

1. WMI `Win32_Processor` (`Name`, `Manufacturer`) — primary.
2. Registry `HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor\0\ProcessorNameString`
   — fallback.
3. Normalization strips noise ("12th Gen", "CPU @ 3.60GHz", "8-Core
   Processor", "(R)", "(TM)") and produces keys such as `i7-12700k` or
   `ryzen55600`.

### Thermal profile — `ThermalThresholdResolver`

Resolves the CPU's manufacturer-rated maximum (`TjMax`) in priority order:

1. **Hardware value** exposed by LibreHardwareMonitor as the `TjMax [°C]` sensor
   parameter (read from Intel MSR `0x1A2`, bits `[23:16]`).
2. **Bundled JSON table** (`Resources/cpu-tjmax.json`) — covers Intel 6th–14th
   gen and AMD Zen–Zen5, keyed by the normalized model token.
3. **Conservative default** of **90 °C**.

The result is a `ThermalProfile` carrying `TjMax`, the fixed **5 °C** margin, the
computed **stop threshold** (clamped to `[70 °C, TjMax]`), and the **source**
that produced it. The resolved profile is cached at startup.

### Stress engine — `StressorService`

Drives the CPU with `Task`/`Parallel`-based floating-point work.

- Configurable worker count (`0` = all logical processors).
- Runs for a caller-supplied `TimeSpan` (1–60 min) via a `CancellationToken`, and
  reports progress and an iteration count.
- Returns a **stop reason** (`Completed`, `Cancelled`, `ThermalLimit`) so the UI
  can present the right outcome.

### Safety — `ThermalGuard`

Given live temperature readings, the guard trips when the temperature reaches the
profile's stop threshold — **independently of the remaining time**. The epoch
that stopped the run becomes part of the resulting `StressTestSummary` (min / max
/ average temperature, duration, throttle events, stop reason).

### Settings & logging

- `SettingsStore` reads/writes `AppSettings` as JSON at
  `%AppData%\Celsius\settings.json`, with a `Clamp()` step that normalizes all
  values into supported ranges.
- `FileLogger` writes **errors and critical messages only** to
  `%AppData%\Celsius\logs`, so the app stays quiet during normal operation.

## UI layer (`Celsius.App`)

- **`MainWindow`** — a list of live metric cards (CPU, memory, disks, GPU,
  thermal threshold). The **Stress Test** button opens a separate window.
- **`StressTestWindow`** — duration presets (1/2/3/5 min) plus a custom minute
  box (validated 1–60), a live temperature chart (`SimpleChart`), running stats,
  and a summary panel on completion.
- **`SettingsWindow`** — poll interval, default stress duration, thread count and
  close behaviour.
- **`TrayIconManager`** — `H.NotifyIcon.Wpf` `TaskbarIcon` with a live tooltip
  and a Show / Stress Test / Exit menu.
- **Localization** — `Strings.resx` (English, neutral) and `Strings.tr.resx`
  (Turkish), switched at runtime via `LocalizationManager`.

## Threading model

- Sensor polling happens on a background thread / `DispatcherTimer` tick and is
  handed to the UI as an immutable `SystemSnapshot`.
- The stress engine runs on the thread pool; the UI observes it via events and a
  cancellation token.
- The thermal guard is evaluated on the same cadence as polling, so it reacts
  within roughly one poll interval.

## Dependency notes

- **LibreHardwareMonitorLib** (MPL-2.0) — all sensor access, including NVIDIA
  (NVAPI/NVML), AMD (ADL) and Intel (IGCL) GPUs.
- **Vortice.DXGI** (MIT) — GPU enumeration (correct VRAM/vendor IDs; WMI's
  `AdapterRAM` is a 32-bit field and unusable above 4 GB).
- **H.NotifyIcon.Wpf** (MIT) — tray icon.
- **System.Management** — WMI access.
