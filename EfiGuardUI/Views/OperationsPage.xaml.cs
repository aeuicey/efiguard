using System.Windows;
using System.Windows.Controls;
using EfiGuardUI.Services;

namespace EfiGuardUI.Views;

public partial class OperationsPage : Page
{
    private readonly MainWindow _mainWindow;
    private readonly SystemQueryService _queryService = new();

    public OperationsPage(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeComponent();
        CheckBundledEfiGuard();
    }

    private void CheckBundledEfiGuard()
    {
        var info = _queryService.GetBundledEfiGuardInfo();
        bool zh = _mainWindow.IsChinese;
        if (info.Available)
        {
            EfiStatusText.Text = zh ? $"✓ EfiGuard {info.Version} 已集成" : $"✓ EfiGuard {info.Version} bundled";
            EfiStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 209, 88));
        }
        else
        {
            EfiStatusText.Text = zh ? "✗ 未找到本地 EfiGuard 文件" : "✗ Bundled EfiGuard files not found";
            EfiStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 69, 58));
            BtnInstallEfi.IsEnabled = false;
        }
    }

    private async void BtnDisableVbs_Click(object sender, RoutedEventArgs e)
    {
        bool zh = _mainWindow.IsChinese;
        var result = MessageBox.Show(
            zh ? "即将关闭 Virtualization-based Security 及相关 Hyper-V 功能。\n\n计算机需要重启才能使更改生效。是否继续？"
               : "About to disable Virtualization-based Security and related Hyper-V features.\n\nA reboot is required for changes to take effect. Continue?",
            zh ? "关闭 VBS" : "Disable VBS",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        BtnDisableVbs.IsEnabled = false;
        BtnDisableVbsText.Text = zh ? "执行中..." : "Processing...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                if (_mainWindow._logsPage != null)
                    _mainWindow._logsPage.AppendLog(msg);
            });

            var opResult = await _queryService.DisableVbsAsync(progress);

            foreach (var log in opResult.Logs)
            {
                _mainWindow._logsPage?.AppendLog(log);
            }

            MessageBox.Show(
                zh ? "VBS 关闭命令已执行。\n\n请重启计算机使更改生效。"
                   : "VBS disable commands executed.\n\nPlease reboot your computer.",
                zh ? "完成" : "Done",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _mainWindow._logsPage?.AppendLog($"[ERR] {ex.Message}");
        }
        finally
        {
            BtnDisableVbs.IsEnabled = true;
            BtnDisableVbsText.Text = zh ? "立即关闭 VBS" : "Disable VBS Now";
        }
    }

    private async void BtnInstallEfi_Click(object sender, RoutedEventArgs e)
    {
        bool zh = _mainWindow.IsChinese;
        var result = MessageBox.Show(
            zh ? "即将修改 EFI 系统分区和 BCD 启动配置。\n\n将创建新的启动项 \"EfiGuard Loader\"。是否继续？"
               : "About to modify the EFI System Partition and BCD store.\n\nA new boot entry \"EfiGuard Loader\" will be created. Continue?",
            zh ? "安装 EfiGuard" : "Install EfiGuard",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        BtnInstallEfi.IsEnabled = false;
        BtnInstallEfiText.Text = zh ? "安装中..." : "Installing...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                if (_mainWindow._logsPage != null)
                    _mainWindow._logsPage.AppendLog(msg);
            });

            var opResult = await _queryService.InstallEfiGuardAsync(progress);

            foreach (var log in opResult.Logs)
            {
                _mainWindow._logsPage?.AppendLog(log);
            }

            if (opResult.Success)
            {
                MessageBox.Show(
                    zh ? "EfiGuard 安装完成。\n\n重启后选择 \"EfiGuard Loader\" 启动。"
                       : "EfiGuard installation complete.\n\nSelect 'EfiGuard Loader' at boot.",
                    zh ? "完成" : "Done",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    opResult.Error ?? (zh ? "安装失败" : "Installation failed"),
                    zh ? "错误" : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _mainWindow._logsPage?.AppendLog($"[ERR] {ex.Message}");
        }
        finally
        {
            BtnInstallEfi.IsEnabled = true;
            BtnInstallEfiText.Text = zh ? "一键安装到 ESP" : "Install to ESP";
        }
    }

    public void RefreshLanguage()
    {
        bool zh = _mainWindow.IsChinese;
        OpsBadge.Text = zh ? "系统操作 / System Ops" : "System Ops / 系统操作";
        OpsTitle.Text = zh ? "操作中心" : "Operations";
        OpsSubtitle.Text = zh ? "管理 VBS 与 EfiGuard 启动配置" : "Manage VBS and EfiGuard boot configuration";
        VbsTitle.Text = zh ? "关闭 VBS" : "Disable VBS";
        VbsTag.Text = zh ? "高风险 / DANGER" : "DANGER / 高风险";
        VbsDesc.Text = zh
            ? "关闭基于虚拟化的安全功能和所有 Hyper-V 相关组件。将修改 Windows 可选功能和 BCD 配置，重启后生效。"
            : "Turn off Virtualization-based Security and all Hyper-V components. Will modify Windows Optional Features and BCD config. Reboot required.";
        BtnDisableVbsText.Text = zh ? "立即关闭 VBS" : "Disable VBS Now";
        EfiTitle.Text = zh ? "安装 EfiGuard" : "Install EfiGuard";
        EfiTag.Text = zh ? "高级 / ADVANCED" : "ADVANCED / 高级";
        EfiDesc.Text = zh
            ? "将预打包的 EfiGuard v1.3 部署到 EFI 系统分区并创建 BCD 启动项。EfiGuard 会在启动时禁用 PatchGuard 和驱动签名强制 (DSE)。"
            : "Deploy bundled EfiGuard v1.3 to the EFI System Partition and create a BCD boot entry. EfiGuard disables PatchGuard and DSE at boot.";
        BtnInstallEfiText.Text = zh ? "一键安装到 ESP" : "Install to ESP";
        WarnTitle.Text = zh ? "重要提示 / Important Notice" : "Important Notice / 重要提示";
        WarnHvci.Text = zh
            ? "EfiGuard 无法禁用 HVCI（内存完整性）。如果 HVCI 处于启用状态，EfiGuard 的 DSE 绕过将无效。"
            : "EfiGuard cannot disable HVCI (Memory Integrity). If HVCI is enabled, EfiGuard DSE bypass will be ineffective.";
        WarnReboot.Text = zh
            ? "所有系统级修改都需要重启计算机才能生效。"
            : "All system-level changes require a reboot to take effect.";
        CheckBundledEfiGuard();
    }
}
