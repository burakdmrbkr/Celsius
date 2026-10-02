# PawnIO kernel driver (required for temperatures)

Celsius reads CPU/GPU temperatures through
[LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).
Since version 0.9.6, LibreHardwareMonitor no longer uses the legacy **WinRing0**
driver, because Microsoft added it to the
[Vulnerable Driver Blocklist](https://learn.microsoft.com/windows/security/application-security/application-control/app-control-for-business/design/microsoft-recommended-driver-block-rules).
It now relies on **[PawnIO](https://pawnio.eu/)**, a signed kernel driver.

## Symptom when PawnIO is missing

You are running Celsius **as administrator**, and yet:

- CPU temperature shows `—`
- GPU temperature shows `—`
- **but** CPU model, CPU clock/load, memory and disk all work fine
- and the GPU *name* is shown (from DXGI) with no temperature

This is the exact signature of a missing or blocked PawnIO driver. CPU/GPU
temperatures need low-level MSR/PCI access, which only the kernel driver can
provide; everything else comes from WMI, performance counters or DXGI, which
need no driver.

## Fix

### 1. Install PawnIO (the actual fix)

1. Download the latest installer from **<https://pawnio.eu/>**.
2. Run the installer **as administrator**.
3. Reboot if the installer asks you to.
4. Start Celsius again (also as administrator).

> The driver is signed, but some antivirus products / "core isolation" (Memory
> Integrity / HVCI) settings can still block it. If the installer fails, check
> your security software and the Windows "Vulnerable driver blocklist" setting.

### 2. Verify it loaded

After installing, the Celsius log (`%AppData%\Celsius\logs`) no longer reports a
sensor error, and the dashboard shows real temperatures.

## Why this is not bundled

Celsius deliberately does not ship the PawnIO driver in its release archive:

- The driver is GPL-2.0 (PawnIO) with an LGPL-2.1 exception for modules; the
  kernel driver itself is not redistributable the same way as the app's MIT
  code without carrying its own license obligations.
- Driver installation requires elevation and a reboot window, which is best
  handled by PawnIO's own installer so that updates and revocation lists stay
  current.

## Alternative: run without the driver

Without PawnIO, Celsius still works as a *non-thermal* monitor: CPU model,
clock and load, memory usage, disk usage and GPU names remain available. Only
temperature (and TjMax-based features such as the thermal guard) are disabled.
