# EfiGuard UI

A Windows system security management tool with an Apple-inspired design, providing visualization of system virtualization and security states, one-click VBS (Virtualization-based Security) disabling, and EfiGuard boot entry management.

This repository contains **two UI implementations**:

| Implementation | Path | Framework | Status |
|---|---|---|---|
| **EfiGuardUI (WPF)** | [`EfiGuardUI/`](EfiGuardUI/) | .NET 10 WPF | Active |
| **EfiGuard UI (Electron)** | [`efiguard-ui/`](efiguard-ui/) | Electron 35 | Active |

Both versions share the same core functionality and visual design language.

---

## Features

- **Real-time Dashboard**
  - VBS (Virtualization-based Security)
  - Memory Integrity / HVCI (Hypervisor-enforced Code Integrity)
  - Credential Guard
  - Hyper-V Platform Status
  - CPU Virtualization (VT-x / AMD-V)
  - SLAT (Second Level Address Translation)
  - Secure Boot
  - TPM Status
  - Hypervisor Launch Type
  - EfiGuard Boot Entry Detection

- **One-click VBS Disabling**
  - Automatically executes DISM to disable Hyper-V features
  - Configures BCD boot entries (`hypervisorlaunchtype off`)
  - Deploys `SecConfig.efi` to ESP for complete VBS disablement

- **EfiGuard Management**
  - Bundles EfiGuard v1.3
  - Deploys `EfiGuardDxe.efi` and `Loader.efi` to the EFI System Partition (ESP)
  - Creates a UEFI firmware boot entry (`{bootmgr}`-based, compatible with Mattiwatti/EfiGuard)
  - Adds entry to the top of the firmware boot menu

---

## Requirements

- Windows 10/11 x64
- **Must run as Administrator** (all operation buttons are disabled otherwise)

---

## Usage

### Portable Build (Recommended)

Pre-built binaries are available in:

- [`publish/`](publish/) — .NET WPF build
- [`efiguard-ui/dist/win-unpacked/`](efiguard-ui/dist/win-unpacked/) — Electron build

Double-click `RunAsAdmin.bat` to launch with elevated privileges, or right-click the `.exe` and select **Run as administrator**.

### Building from Source

#### .NET WPF (EfiGuardUI)

```bash
cd EfiGuardUI
dotnet publish -c Release -o ../publish
```

#### Electron (efiguard-ui)

```bash
cd efiguard-ui
npm install
npm run dist:dir    # Unpacked directory
npm run dist        # Portable exe (requires NSIS, may fail in restricted networks)
```

> **Note:** In restricted network environments, the portable NSIS build may fail. Distribute the `dist/win-unpacked/` folder directly instead.

---

## Installation Steps (EfiGuard)

The EfiGuard installation follows the official [Mattiwatti/EfiGuard](https://github.com/Mattiwatti/EfiGuard) guidelines:

1. Mount the EFI System Partition (`mountvol X: /S`)
2. Copy `EfiGuardDxe.efi` and `Loader.efi` to `X:\EFI\Boot\`
3. Create a new boot entry by copying `{bootmgr}`
4. Set the path to `\EFI\Boot\Loader.efi`
5. Add the entry to the top of the firmware boot menu (`{fwbootmgr} displayorder`)

This ensures EfiGuard boots correctly and Windows starts without recovery errors.

---

## Architecture

```
EfiGuard UI
├── EfiGuardUI/           .NET 10 WPF Application
│   ├── Views/
│   ├── Services/
│   ├── Models/
│   └── Assets/efiguard/  Bundled EfiGuard v1.3 EFI files
│
├── efiguard-ui/          Electron Application
│   ├── main.js           Main process (IPC, PowerShell, BCDEdit)
│   ├── preload.js
│   ├── src/
│   │   ├── index.html
│   │   ├── renderer.js
│   │   └── styles.css
│   └── assets/efiguard/  Bundled EfiGuard v1.3 EFI files
│
└── publish/              Pre-built WPF output
```

---

## Warnings

- Disabling VBS and installing EfiGuard are **system-level modifications** that may reduce system security.
- EfiGuard **cannot bypass HVCI / Memory Integrity**. If HVCI is enabled, EfiGuard's DSE patch will be ineffective.
- All system-level changes **require a reboot** to take effect.
- Please use this tool only if you fully understand the risks.

---

## License

GPLv3 (consistent with EfiGuard)
