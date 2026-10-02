# Security Policy

## Supported Versions

Celsius is currently in early (pre-release) development. Only the latest release
receives security updates.

| Version | Supported |
| ------- | --------- |
| 0.1.x (beta) | ✅ |

## Reporting a Vulnerability

If you discover a security issue, please **do not open a public issue**. Instead,
report it privately using GitHub's
[Report a vulnerability](https://github.com/burakdmrbkr/Celsius/security/advisories/new)
feature, or contact the maintainer directly.

Please include:

- A description of the issue and its impact
- Steps to reproduce
- Affected version(s) and Windows version
- Any proof-of-concept or suggested fix, if available

You can expect an acknowledgement within a few days. Once the issue is confirmed
and fixed, it will be disclosed in the release notes.

## Scope and Known Considerations

Celsius is a system monitor that:

- **Runs with administrator privileges** and loads a low-level hardware driver
  (LibreHardwareMonitor / PawnIO) to read CPU temperature.
- **Is distributed unsigned**, so Windows SmartScreen and some antivirus products
  may flag it. This is expected for an unsigned beta.
- **Loads native vendor libraries** (NVAPI/NVML, ADL, IGCL) shipped with your GPU
  drivers.

These are inherent to what the app does. Reports about the *expected* behaviour
above (e.g. "it asks for admin" or "SmartScreen warned me") are not security
vulnerabilities. Reports about the app misusing its privileges, reading data it
should not, or loading untrusted code **are** in scope.
