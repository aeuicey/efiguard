namespace EfiGuardUI.Models;

public class SecurityStatus
{
    public OsInfo? OsInfo { get; set; }
    public int? Vbs { get; set; }
    public int? Hvci { get; set; }
    public int? CredentialGuard { get; set; }
    public string? HyperV { get; set; }
    public bool? Virtualization { get; set; }
    public bool? Slat { get; set; }
    public bool? SecureBoot { get; set; }
    public TpmInfo? Tpm { get; set; }
    public string? HypervisorLaunchType { get; set; }
    public bool? EfiGuard { get; set; }
    public bool? Dep { get; set; }
    public bool? Is64Bit { get; set; }
    public long Timestamp { get; set; }
}

public class OsInfo
{
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public bool? HyperVisorPresent { get; set; }
}

public class TpmInfo
{
    public bool Present { get; set; }
    public bool Ready { get; set; }
    public bool Enabled { get; set; }
}

public class EfiGuardBundleInfo
{
    public bool Available { get; set; }
    public string? Path { get; set; }
    public string? Version { get; set; }
    public List<string> Files { get; set; } = new();
}

public class OperationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<string> Logs { get; set; } = new();
}
