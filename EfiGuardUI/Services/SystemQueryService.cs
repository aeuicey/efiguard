using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using EfiGuardUI.Models;

namespace EfiGuardUI.Services;

public class SystemQueryService
{
    private static readonly string BatchQueryScript = @"
$ErrorActionPreference = 'SilentlyContinue'
$r = @{}

$os = Get-CimInstance Win32_OperatingSystem
$r['OsName'] = $os.Caption
$r['OsVersion'] = $os.Version
$r['DepAvailable'] = $os.DataExecutionPrevention_Available
$r['DepSupportPolicy'] = $os.DataExecutionPrevention_SupportPolicy
$comp = Get-CimInstance Win32_ComputerSystem
$r['HyperVisorPresent'] = $comp.HypervisorPresent

$cpu = Get-WmiObject -Class Win32_Processor | Select-Object -First 1
$r['Virtualization'] = $cpu.VirtualizationFirmwareEnabled
$r['Slat'] = $cpu.SecondLevelAddressTranslationExtensions
$r['ProcessorDataWidth'] = $cpu.DataWidth
$r['Is64BitOs'] = [Environment]::Is64BitOperatingSystem

$dg = Get-CimInstance -ClassName Win32_DeviceGuard -Namespace 'root\Microsoft\Windows\DeviceGuard'
if ($dg) {
    $r['VbsWmi'] = $dg.VirtualizationBasedSecurityStatus
    $r['CredentialGuardWmi'] = $dg.CredentialGuardStatus
}

$regVbs = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard' -Name 'EnableVirtualizationBasedSecurity'
$r['VbsReg'] = if ($regVbs) { $regVbs.EnableVirtualizationBasedSecurity } else { $null }

$regHvci = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity' -Name 'Enabled'
$r['HvciReg'] = if ($regHvci) { $regHvci.Enabled } else { $null }

$regCg = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard' -Name 'LsaCfgFlags'
$r['CgReg'] = if ($regCg) { $regCg.LsaCfgFlags } else { $null }

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

try {
    $r['SecureBoot'] = Confirm-SecureBootUEFI
} catch {
    $r['SecureBoot'] = $null
}

try {
    $bcd = bcdedit /enum
    $m = [regex]::Match($bcd, 'hypervisorlaunchtype\s+(\w+)')
    $r['HypervisorLaunchType'] = if ($m.Success) { $m.Groups[1].Value } else { $null }
    $r['EfiGuard'] = [bool]($bcd -match 'EfiGuard|DebugTool|SecConfig\.efi')
} catch {
    $r['HypervisorLaunchType'] = $null
    $r['EfiGuard'] = $false
}

$si = systeminfo
$vbsMatch = [regex]::Match($si, 'Virtualization-based security:\s*Status:\s*(\w+)')
$r['VbsSystemInfo'] = if ($vbsMatch.Success) { $vbsMatch.Groups[1].Value } else { $null }
$sbMatch = [regex]::Match($si, 'Available Security Properties:[\s\S]*?Secure Boot')
$r['SecureBootSystemInfo'] = $sbMatch.Success
$cgMatch = [regex]::Match($si, 'Services Running:[\s\S]*?Credential Guard')
$r['CredentialGuardSystemInfo'] = $cgMatch.Success
$hvMatch = [regex]::Match($si, 'A hypervisor has been detected')
$r['HypervisorDetectedSystemInfo'] = $hvMatch.Success

try {
    $hv = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All
    $r['HyperVState'] = $hv.State
} catch {
    $r['HyperVState'] = $null
}

try {
    $def = Get-CimInstance -Namespace 'root\Microsoft\Windows\Defender' -ClassName MSFT_MpComputerStatus
    $mi = $def.MemoryIntegrityStatus
    $r['HvciWmi'] = if ($mi -ne $null) { $mi } else { $null }
} catch {
    $r['HvciWmi'] = $null
}

$regHvci2 = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name 'HVCIPolicy'
$r['HvciReg2'] = if ($regHvci2) { $regHvci2.HVCIPolicy } else { $null }

$r | ConvertTo-Json -Compress
";

    public async Task<SecurityStatus> GetSystemStatusAsync()
    {
        var raw = await RunPowerShellScriptAsync(BatchQueryScript);
        var r = ParseJson(raw);
        return ResolveStatus(r);
    }

    public async Task<OperationResult> DisableVbsAsync(IProgress<string>? progress)
    {
        var logs = new List<string>();
        var commands = new[]
        {
            "dism /Online /Disable-Feature:microsoft-hyper-v-all /NoRestart",
            "dism /Online /Disable-Feature:IsolatedUserMode /NoRestart",
            "dism /Online /Disable-Feature:Microsoft-Hyper-V-Hypervisor /NoRestart",
            "dism /Online /Disable-Feature:Microsoft-Hyper-V-Online /NoRestart",
            "dism /Online /Disable-Feature:HypervisorPlatform /NoRestart",
            "bcdedit /set hypervisorlaunchtype off"
        };

        foreach (var cmd in commands)
        {
            try
            {
                var output = await RunCmdAsync(cmd);
                logs.Add($"[OK] {cmd}");
                if (!string.IsNullOrWhiteSpace(output))
                    logs.Add(output.Length > 500 ? output[..500] : output);
                progress?.Report($"Executed: {cmd}");
            }
            catch (Exception ex)
            {
                logs.Add($"[ERR] {cmd}");
                logs.Add(ex.Message);
                progress?.Report($"Failed: {cmd}");
            }
        }

        try
        {
            var windir = Environment.GetEnvironmentVariable("WINDIR") ?? @"C:\Windows";
            var secConfig = Path.Combine(windir, "System32", "SecConfig.efi");
            if (File.Exists(secConfig))
            {
                await RunCmdAsync("mountvol X: /s");
                await RunCmdAsync($"copy \"{secConfig}\" \"X:\\EFI\\Microsoft\\Boot\\SecConfig.efi\" /Y");
                try { await RunCmdAsync("bcdedit /create {0cb3b571-2f2e-4343-a879-d86a476d7215} /d \"DebugTool\" /application osloader"); } catch { }
                await RunCmdAsync("bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} path \"\\EFI\\Microsoft\\Boot\\SecConfig.efi\"");
                await RunCmdAsync("bcdedit /set {bootmgr} bootsequence {0cb3b571-2f2e-4343-a879-d86a476d7215}");
                await RunCmdAsync("bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions DISABLE-LSA-ISO,DISABLE-VBS");
                await RunCmdAsync("bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} device partition=X:");
                await RunCmdAsync("mountvol X: /d");
                logs.Add("[OK] SecConfig.efi deployed and BCD entry created");
            }
            else
            {
                logs.Add($"[WARN] SecConfig.efi not found at {secConfig}");
            }
        }
        catch (Exception ex)
        {
            logs.Add($"[ERR] SecConfig deployment failed: {ex.Message}");
        }

        logs.Add("\n[!] Please restart your computer for changes to take effect.");
        return new OperationResult { Success = true, Logs = logs };
    }

    public EfiGuardBundleInfo GetBundledEfiGuardInfo()
    {
        var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "efiguard");
        try
        {
            var files = Directory.GetFiles(assetsPath).Select(Path.GetFileName).ToList()!;
            var hasDxe = files.Any(f => f.Contains("EfiGuardDxe", StringComparison.OrdinalIgnoreCase));
            var hasLoader = files.Any(f => f.Equals("Loader.efi", StringComparison.OrdinalIgnoreCase));
            return new EfiGuardBundleInfo
            {
                Available = hasDxe && hasLoader,
                Path = assetsPath,
                Version = "v1.3",
                Files = files
            };
        }
        catch
        {
            return new EfiGuardBundleInfo { Available = false };
        }
    }

    public async Task<OperationResult> InstallEfiGuardAsync(IProgress<string>? progress)
    {
        var logs = new List<string>();
        var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "efiguard");

        try
        {
            var files = Directory.GetFiles(assetsPath);
            var guardDxe = files.FirstOrDefault(f => Path.GetFileName(f).Contains("EfiGuardDxe", StringComparison.OrdinalIgnoreCase));
            var loader = files.FirstOrDefault(f => Path.GetFileName(f).Equals("Loader.efi", StringComparison.OrdinalIgnoreCase));

            if (guardDxe is null || loader is null)
                throw new Exception("Bundled EfiGuard files are incomplete");

            await RunCmdAsync("mountvol X: /s");
            var espBoot = @"X:\EFI\Boot";
            if (!Directory.Exists(espBoot)) Directory.CreateDirectory(espBoot);

            File.Copy(guardDxe, Path.Combine(espBoot, "EfiGuardDxe.efi"), true);
            logs.Add("[OK] Copied EfiGuardDxe.efi to ESP");
            progress?.Report("Copied EfiGuardDxe.efi");

            File.Copy(loader, Path.Combine(espBoot, "Loader.efi"), true);
            logs.Add("[OK] Copied Loader.efi to ESP");
            progress?.Report("Copied Loader.efi");

            try
            {
                var copyOut = await RunCmdAsync("bcdedit /copy {current} /d \"EfiGuard Loader\"");
                var guidMatch = System.Text.RegularExpressions.Regex.Match(copyOut, @"\{([^}]+)\}");
                if (guidMatch.Success)
                {
                    var guid = guidMatch.Groups[1].Value;
                    await RunCmdAsync($"bcdedit /set {{{guid}}} device partition=X:");
                    await RunCmdAsync($"bcdedit /set {{{guid}}} path \\EFI\\Boot\\Loader.efi");
                    await RunCmdAsync($"bcdedit /set {{{guid}}} description \"EfiGuard Loader\"");
                    logs.Add($"[OK] Created BCD entry {{{guid}}}");
                }
            }
            catch (Exception bcdErr)
            {
                logs.Add($"[WARN] BCD config: {bcdErr.Message}");
            }

            await RunCmdAsync("mountvol X: /d");
            logs.Add("[!] EfiGuard installed. Select 'EfiGuard Loader' at boot.");
            return new OperationResult { Success = true, Logs = logs };
        }
        catch (Exception ex)
        {
            logs.Add($"[ERR] Installation failed: {ex.Message}");
            return new OperationResult { Success = false, Error = ex.Message, Logs = logs };
        }
    }

    private static async Task<string> RunPowerShellScriptAsync(string script, int timeoutMs = 35000)
    {
        var tmpFile = Path.Combine(Path.GetTempPath(), $"eg-{Guid.NewGuid()}.ps1");
        await File.WriteAllTextAsync(tmpFile, script, Encoding.UTF8);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tmpFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            var delayTask = Task.Delay(timeoutMs);
            var completed = await Task.WhenAny(Task.WhenAll(stdoutTask, stderrTask), delayTask);

            if (completed == delayTask)
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException("PowerShell script timed out");
            }

            await process.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            var cleanStderr = System.Text.RegularExpressions.Regex.Replace(stderr, @"#<\s*CLIXML[\s\S]*?</Objs>", "").Trim();

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
                throw new Exception(cleanStderr);

            return stdout.Trim();
        }
        finally
        {
            try { File.Delete(tmpFile); } catch { }
        }
    }

    private static async Task<string> RunCmdAsync(string command, int timeoutMs = 60000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var delayTask = Task.Delay(timeoutMs);
        var completed = await Task.WhenAny(Task.WhenAll(stdoutTask, stderrTask), delayTask);

        if (completed == delayTask)
        {
            try { process.Kill(); } catch { }
            throw new TimeoutException("Command timed out");
        }

        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new Exception(stderr.Trim());

        return stdout.Trim();
    }

    private static Dictionary<string, object?> ParseJson(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(raw) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static SecurityStatus ResolveStatus(Dictionary<string, object?> r)
    {
        return new SecurityStatus
        {
            OsInfo = new OsInfo
            {
                OsName = GetString(r, "OsName"),
                OsVersion = GetString(r, "OsVersion"),
                HyperVisorPresent = GetBool(r, "HyperVisorPresent")
            },
            Vbs = ResolveVbs(r),
            Hvci = ResolveHvci(r),
            CredentialGuard = ResolveCredentialGuard(r),
            HyperV = ResolveHyperV(r),
            Virtualization = GetBool(r, "Virtualization"),
            Slat = GetBool(r, "Slat"),
            Dep = GetBool(r, "DepAvailable"),
            Is64Bit = GetBool(r, "Is64BitOs"),
            SecureBoot = ResolveSecureBoot(r),
            Tpm = ResolveTpm(r),
            HypervisorLaunchType = GetString(r, "HypervisorLaunchType"),
            EfiGuard = GetBool(r, "EfiGuard") ?? false,
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };
    }

    private static int? ResolveVbs(Dictionary<string, object?> r)
    {
        if (TryGetInt(r, "VbsWmi", out var v)) return v;
        if (TryGetInt(r, "VbsReg", out var reg)) return reg;
        if (GetString(r, "VbsSystemInfo") is string s)
        {
            return s switch { "Running" => 2, "Enabled" => 1, "Not enabled" => 0, _ => null };
        }
        return null;
    }

    private static int? ResolveHvci(Dictionary<string, object?> r)
    {
        if (TryGetInt(r, "HvciReg", out var v)) return v == 1 ? 1 : 0;
        if (TryGetInt(r, "HvciReg2", out var v2)) return v2 == 1 ? 1 : 0;
        var wmi = r.GetValueOrDefault("HvciWmi");
        if (wmi is not null)
        {
            var s = wmi.ToString();
            if (s == "True" || s == "1") return 1;
            if (s == "False" || s == "0") return 0;
        }
        return null;
    }

    private static int? ResolveCredentialGuard(Dictionary<string, object?> r)
    {
        if (TryGetInt(r, "CredentialGuardWmi", out var v)) return v;
        if (TryGetInt(r, "CgReg", out var reg)) return reg;
        if (GetBool(r, "CredentialGuardSystemInfo") == true) return 1;
        return null;
    }

    private static string? ResolveHyperV(Dictionary<string, object?> r)
    {
        var state = GetString(r, "HyperVState");
        if (!string.IsNullOrWhiteSpace(state)) return state;
        if (GetBool(r, "HypervisorDetectedSystemInfo") == true) return "Enabled";
        if (GetBool(r, "HyperVisorPresent") == true) return "Enabled";
        return null;
    }

    private static bool? ResolveSecureBoot(Dictionary<string, object?> r)
    {
        var sb = r.GetValueOrDefault("SecureBoot");
        if (sb is not null)
        {
            var s = sb.ToString();
            return s == "True" || s == "true" || s == "1";
        }
        return GetBool(r, "SecureBootSystemInfo");
    }

    private static TpmInfo? ResolveTpm(Dictionary<string, object?> r)
    {
        var present = r.GetValueOrDefault("TpmPresent");
        if (present is null) return null;
        return new TpmInfo
        {
            Present = ToBool(present),
            Ready = ToBool(r.GetValueOrDefault("TpmReady")),
            Enabled = ToBool(r.GetValueOrDefault("TpmEnabled"))
        };
    }

    private static string? GetString(Dictionary<string, object?> r, string key)
        => r.GetValueOrDefault(key)?.ToString();

    private static bool? GetBool(Dictionary<string, object?> r, string key)
    {
        var val = r.GetValueOrDefault(key);
        return val is null ? null : ToBool(val);
    }

    private static bool ToBool(object? val)
    {
        var s = val?.ToString()?.ToLowerInvariant();
        return s == "true" || s == "1";
    }

    private static bool TryGetInt(Dictionary<string, object?> r, string key, out int value)
    {
        value = 0;
        var val = r.GetValueOrDefault(key);
        if (val is null) return false;
        return int.TryParse(val.ToString(), out value);
    }
}
