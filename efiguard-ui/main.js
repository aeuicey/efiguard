const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const { exec, execFile } = require('child_process');
const path = require('path');
const fs = require('fs');
const os = require('os');

let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 860,
    minWidth: 960,
    minHeight: 640,
    frame: false,
    backgroundColor: '#000000',
    titleBarOverlay: {
      color: '#0d0d0d',
      symbolColor: '#f5f5f7',
      height: 40
    },
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  });

  mainWindow.loadFile(path.join(__dirname, 'src', 'index.html'));
}

function isAdmin() {
  try {
    execSync('net session', { timeout: 5000, windowsHide: true });
    return true;
  } catch {
    return false;
  }
}

const { execSync } = require('child_process');

app.whenReady().then(() => {
  if (!isAdmin()) {
    dialog.showMessageBoxSync(null, {
      type: 'warning',
      buttons: ['OK'],
      title: '需要管理员权限 / Administrator Required',
      message: 'EfiGuard UI 需要管理员权限运行。\nEfiGuard UI requires Administrator privileges.',
      detail: '请使用 RunAsAdmin.bat 启动，或右键选择"以管理员身份运行"。\nPlease use RunAsAdmin.bat, or right-click and select "Run as administrator".'
    });
  }

  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});

// =====================
// 窗口控制 IPC
// =====================
ipcMain.handle('window-minimize', () => { if (mainWindow) mainWindow.minimize(); });
ipcMain.handle('window-maximize', () => { if (mainWindow) { mainWindow.isMaximized() ? mainWindow.unmaximize() : mainWindow.maximize(); } });
ipcMain.handle('window-close', () => { if (mainWindow) mainWindow.close(); });
ipcMain.handle('is-admin', async () => isAdmin());

// =====================
// PowerShell 执行器 (使用临时 ps1 文件, 避免 shell 转义问题)
// =====================

function runPsScript(script, timeout = 30000) {
  return new Promise((resolve, reject) => {
    const tmpFile = path.join(os.tmpdir(), `eg-${Date.now()}-${Math.random().toString(36).slice(2)}.ps1`);
    fs.writeFileSync(tmpFile, script, 'utf8');

    execFile('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', tmpFile], {
      timeout,
      windowsHide: true
    }, (error, stdout, stderr) => {
      try { fs.unlinkSync(tmpFile); } catch {}

      const out = stdout ? stdout.trim() : '';
      const err = stderr ? stderr.trim() : '';

      // 忽略 PowerShell 进度条 CLIXML
      const cleanErr = err.replace(/#< CLIXML[\s\S]*?<\/Objs>/gi, '').trim();

      if (error && !out) {
        reject(cleanErr || error.message);
      } else {
        resolve(out);
      }
    });
  });
}

function runCmd(command, timeout = 60000) {
  return new Promise((resolve, reject) => {
    exec(command, { timeout, windowsHide: true }, (error, stdout, stderr) => {
      if (error && !stdout) {
        reject(stderr || error.message);
      } else {
        resolve(stdout ? stdout.trim() : '');
      }
    });
  });
}

// =====================
// 系统状态批量查询
// =====================

const BATCH_QUERY_SCRIPT = `
$ErrorActionPreference = 'SilentlyContinue'
$r = @{}

# === OS & System ===
$os = Get-CimInstance Win32_OperatingSystem
$r['OsName'] = $os.Caption
$r['OsVersion'] = $os.Version

$comp = Get-CimInstance Win32_ComputerSystem
$r['HyperVisorPresent'] = $comp.HypervisorPresent

# === CPU ===
$cpu = Get-WmiObject -Class Win32_Processor | Select-Object -First 1
$r['Virtualization'] = $cpu.VirtualizationFirmwareEnabled
$r['Slat'] = $cpu.SecondLevelAddressTranslationExtensions

# === DeviceGuard WMI ===
$dg = Get-CimInstance -ClassName Win32_DeviceGuard -Namespace 'root\Microsoft\Windows\DeviceGuard'
if ($dg) {
    $r['VbsWmi'] = $dg.VirtualizationBasedSecurityStatus
    $r['CredentialGuardWmi'] = $dg.CredentialGuardStatus
}

# === Registry fallbacks ===
$regVbs = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard' -Name 'EnableVirtualizationBasedSecurity'
$r['VbsReg'] = if ($regVbs) { $regVbs.EnableVirtualizationBasedSecurity } else { $null }

$regHvci = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name 'Enabled'
$r['HvciReg'] = if ($regHvci) { $regHvci.Enabled } else { $null }

$regCg = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard' -Name 'LsaCfgFlags'
$r['CgReg'] = if ($regCg) { $regCg.LsaCfgFlags } else { $null }

# === TPM ===
try {
    $tpm = Get-Tpm
    $r['TpmPresent'] = $tpm.TpmPresent
    $r['TpmReady'] = $tpm.TpmReady
    $r['TpmEnabled'] = $tpm.TpmEnabled
} catch {
    $r['TpmPresent'] = $null
    $r['TpmReady'] = $null
    $r['TpmEnabled'] = $null
}

# === Secure Boot (needs admin) ===
try {
    $r['SecureBoot'] = Confirm-SecureBootUEFI
} catch {
    $r['SecureBoot'] = $null
}

# === BCD (needs admin) ===
try {
    $bcd = bcdedit /enum
    $m = [regex]::Match($bcd, 'hypervisorlaunchtype\s+(\w+)')
    $r['HypervisorLaunchType'] = if ($m.Success) { $m.Groups[1].Value } else { $null }
    $r['EfiGuard'] = [bool]($bcd -match 'EfiGuard|DebugTool|SecConfig\.efi')
} catch {
    $r['HypervisorLaunchType'] = $null
    $r['EfiGuard'] = $false
}

# === systeminfo parsing ===
$si = systeminfo
$vbsMatch = [regex]::Match($si, 'Virtualization-based security:\s*Status:\s*(\w+)')
$r['VbsSystemInfo'] = if ($vbsMatch.Success) { $vbsMatch.Groups[1].Value } else { $null }

$sbMatch = [regex]::Match($si, 'Available Security Properties:[\s\S]*?Secure Boot')
$r['SecureBootSystemInfo'] = $sbMatch.Success

$cgMatch = [regex]::Match($si, 'Services Running:[\s\S]*?Credential Guard')
$r['CredentialGuardSystemInfo'] = $cgMatch.Success

$hvMatch = [regex]::Match($si, 'A hypervisor has been detected')
$r['HypervisorDetectedSystemInfo'] = $hvMatch.Success

# === Hyper-V feature state (needs admin) ===
try {
    $hv = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All
    $r['HyperVState'] = $hv.State
} catch {
    $r['HyperVState'] = $null
}

# === HVCI via WMI (Defender namespace) ===
try {
    $def = Get-CimInstance -Namespace 'root\Microsoft\Windows\Defender' -ClassName MSFT_MpComputerStatus
    # MemoryIntegrityStatus may not exist on all builds; check multiple property names
    $mi = $def.MemoryIntegrityStatus
    $r['HvciWmi'] = if ($mi -ne $null) { $mi } else { $null }
} catch {
    $r['HvciWmi'] = $null
}

# === Additional registry for HVCI (Windows 11 alternate path) ===
$regHvci2 = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name 'HVCIPolicy'
$r['HvciReg2'] = if ($regHvci2) { $regHvci2.HVCIPolicy } else { $null }

# Output JSON
$r | ConvertTo-Json -Compress
`;

function parseBatchResult(raw) {
  try {
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

function resolveVbs(r) {
  // WMI: 0=disabled, 1=enabled, 2=running
  if (r.VbsWmi !== undefined && r.VbsWmi !== null) return parseInt(r.VbsWmi);
  // Registry: 0=disabled, 1=enabled
  if (r.VbsReg !== undefined && r.VbsReg !== null) return parseInt(r.VbsReg);
  // systeminfo: Running -> 2
  if (r.VbsSystemInfo === 'Running') return 2;
  if (r.VbsSystemInfo === 'Enabled') return 1;
  if (r.VbsSystemInfo === 'Not enabled') return 0;
  return null;
}

function resolveHvci(r) {
  // Registry primary path
  if (r.HvciReg !== undefined && r.HvciReg !== null) {
    const v = parseInt(r.HvciReg);
    return v === 1 ? 1 : 0;
  }
  // Registry alternate path
  if (r.HvciReg2 !== undefined && r.HvciReg2 !== null) {
    const v = parseInt(r.HvciReg2);
    return v === 1 ? 1 : 0;
  }
  // WMI Defender
  if (r.HvciWmi !== undefined && r.HvciWmi !== null) {
    const v = r.HvciWmi;
    if (v === true || v === 1 || v === '1') return 1;
    if (v === false || v === 0 || v === '0') return 0;
  }
  return null;
}

function resolveCredentialGuard(r) {
  // WMI: 0=disabled, 1=enabled, 2=audit
  if (r.CredentialGuardWmi !== undefined && r.CredentialGuardWmi !== null) return parseInt(r.CredentialGuardWmi);
  // Registry
  if (r.CgReg !== undefined && r.CgReg !== null) return parseInt(r.CgReg);
  // systeminfo
  if (r.CredentialGuardSystemInfo === true) return 1;
  return null;
}

function resolveHyperV(r) {
  if (r.HyperVState) return r.HyperVState;
  // systeminfo hypervisor detection
  if (r.HypervisorDetectedSystemInfo === true) return 'Enabled';
  if (r.HyperVisorPresent === true) return 'Enabled';
  return null;
}

function resolveSecureBoot(r) {
  if (r.SecureBoot !== undefined && r.SecureBoot !== null) {
    return r.SecureBoot === true || r.SecureBoot === 'True' || r.SecureBoot === 1;
  }
  if (r.SecureBootSystemInfo === true) return true;
  return null;
}

function resolveTpm(r) {
  if (r.TpmPresent === undefined || r.TpmPresent === null) return null;
  return {
    Present: r.TpmPresent === true || r.TpmPresent === 'True',
    Ready: r.TpmReady === true || r.TpmReady === 'True',
    Enabled: r.TpmEnabled === true || r.TpmEnabled === 'True'
  };
}

ipcMain.handle('get-system-status', async () => {
  const raw = await runPsScript(BATCH_QUERY_SCRIPT, 35000);
  const r = parseBatchResult(raw);

  return {
    osInfo: {
      OsName: r.OsName || null,
      OsVersion: r.OsVersion || null,
      HyperVisorPresent: r.HyperVisorPresent || null
    },
    vbs: resolveVbs(r),
    hvci: resolveHvci(r),
    credentialGuard: resolveCredentialGuard(r),
    hyperV: resolveHyperV(r),
    virtualization: r.Virtualization === true || r.Virtualization === 'True',
    slat: r.Slat === true || r.Slat === 'True',
    secureBoot: resolveSecureBoot(r),
    tpm: resolveTpm(r),
    hypervisorLaunchType: r.HypervisorLaunchType || null,
    efiGuard: r.EfiGuard === true,
    timestamp: Date.now()
  };
});

// =====================
// VBS 关闭功能
// =====================

ipcMain.handle('disable-vbs', async () => {
  const logs = [];
  const push = (msg) => { logs.push(msg); return msg; };

  const commands = [
    'dism /Online /Disable-Feature:microsoft-hyper-v-all /NoRestart',
    'dism /Online /Disable-Feature:IsolatedUserMode /NoRestart',
    'dism /Online /Disable-Feature:Microsoft-Hyper-V-Hypervisor /NoRestart',
    'dism /Online /Disable-Feature:Microsoft-Hyper-V-Online /NoRestart',
    'dism /Online /Disable-Feature:HypervisorPlatform /NoRestart',
    'bcdedit /set hypervisorlaunchtype off'
  ];

  for (const cmd of commands) {
    try {
      const out = await runCmd(cmd);
      push(`[OK] ${cmd}`);
      if (out) push(out.slice(0, 800));
    } catch (err) {
      push(`[ERR] ${cmd}`);
      push(String(err).slice(0, 500));
    }
  }

  // SecConfig.efi 逻辑
  try {
    const windir = process.env.WINDIR || 'C:\\Windows';
    const secConfig = path.join(windir, 'System32', 'SecConfig.efi');
    if (fs.existsSync(secConfig)) {
      await runCmd('mountvol X: /s');
      await runCmd(`copy "${secConfig}" "X:\\EFI\\Microsoft\\Boot\\SecConfig.efi" /Y`);
      try {
        await runCmd('bcdedit /create {0cb3b571-2f2e-4343-a879-d86a476d7215} /d "DebugTool" /application osloader');
      } catch (e) { /* 可能已存在 */ }
      await runCmd('bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} path "\\EFI\\Microsoft\\Boot\\SecConfig.efi"');
      await runCmd('bcdedit /set {bootmgr} bootsequence {0cb3b571-2f2e-4343-a879-d86a476d7215}');
      await runCmd('bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions DISABLE-LSA-ISO,DISABLE-VBS');
      await runCmd('bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} device partition=X:');
      await runCmd('mountvol X: /d');
      push('[OK] SecConfig.efi 已部署到 ESP 并创建 BCD 启动项');
    } else {
      push(`[WARN] 未找到 SecConfig.efi: ${secConfig}`);
    }
  } catch (err) {
    push(`[ERR] SecConfig 部署失败: ${err}`);
  }

  push('\n[!] 请重启计算机使更改生效。\n[!] Please restart your computer for changes to take effect.');
  return logs;
});

// =====================
// EfiGuard 本地安装
// =====================

const EFIGUARD_ASSETS = path.join(__dirname, 'assets', 'efiguard');

ipcMain.handle('get-efiguard-bundled', async () => {
  try {
    const files = fs.readdirSync(EFIGUARD_ASSETS);
    const hasDxe = files.some(f => f.toLowerCase().includes('efiguarddxe'));
    const hasLoader = files.some(f => f.toLowerCase() === 'loader.efi');
    return {
      available: hasDxe && hasLoader,
      path: EFIGUARD_ASSETS,
      version: 'v1.3',
      files
    };
  } catch {
    return { available: false, path: null, version: null, files: [] };
  }
});

ipcMain.handle('install-efiguard-bundled', async () => {
  const logs = [];
  const push = (msg) => { logs.push(msg); return msg; };

  try {
    const files = fs.readdirSync(EFIGUARD_ASSETS);
    const guardDxe = files.find(f => f.toLowerCase().includes('efiguarddxe'));
    const loader = files.find(f => f.toLowerCase() === 'loader.efi');

    if (!guardDxe || !loader) {
      throw new Error('本地 EfiGuard 文件不完整');
    }

    await runCmd('mountvol X: /s');
    const espBoot = 'X:\\EFI\\Boot';
    if (!fs.existsSync(espBoot)) fs.mkdirSync(espBoot, { recursive: true });

    fs.copyFileSync(path.join(EFIGUARD_ASSETS, guardDxe), path.join(espBoot, 'EfiGuardDxe.efi'));
    push(`[OK] 已复制 EfiGuardDxe.efi 到 ESP`);

    fs.copyFileSync(path.join(EFIGUARD_ASSETS, loader), path.join(espBoot, 'Loader.efi'));
    push(`[OK] 已复制 Loader.efi 到 ESP`);

    // BCD 启动项
    try {
      const copyOut = await runCmd('bcdedit /copy {current} /d "EfiGuard Loader"');
      const guidMatch = copyOut.match(/\{([^}]+)\}/);
      if (guidMatch) {
        const guid = guidMatch[1];
        await runCmd(`bcdedit /set {${guid}} device partition=X:`);
        await runCmd(`bcdedit /set {${guid}} path \\EFI\\Boot\\Loader.efi`);
        await runCmd(`bcdedit /set {${guid}} description "EfiGuard Loader"`);
        push(`[OK] 已创建 BCD 启动项 {${guid}}`);
      }
    } catch (bcdErr) {
      push(`[WARN] BCD 配置: ${bcdErr.message || bcdErr}`);
    }

    await runCmd('mountvol X: /d');
    push('[!] EfiGuard 安装完成。重启后选择 "EfiGuard Loader" 启动。');
    return { success: true, logs };
  } catch (err) {
    push(`[ERR] 安装失败: ${err.message || err}`);
    return { success: false, error: err.message || err, logs };
  }
});

// =====================
// 对话框
// =====================

ipcMain.handle('show-confirm', async (event, options) => {
  const result = await dialog.showMessageBox(mainWindow, {
    type: 'warning',
    buttons: [options.cancelText || '取消 / Cancel', options.okText || '确认 / Confirm'],
    defaultId: 1,
    cancelId: 0,
    title: options.title || 'Confirm',
    message: options.message || '',
    detail: options.detail || ''
  });
  return result.response === 1;
});

ipcMain.handle('show-message', async (event, options) => {
  await dialog.showMessageBox(mainWindow, {
    type: options.type || 'info',
    title: options.title || 'Info',
    message: options.message || '',
    detail: options.detail || ''
  });
});
