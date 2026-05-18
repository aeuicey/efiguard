using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using EfiGuardUI.Models;
using EfiGuardUI.Services;

namespace EfiGuardUI.Views;

public partial class DashboardPage : Page
{
    private readonly MainWindow _mainWindow;
    private readonly SystemQueryService _queryService = new();
    private SecurityStatus? _lastStatus;
    private System.Windows.Threading.DispatcherTimer? _timer;

    private readonly List<StatusCardDef> _cardDefs = new()
    {
        new("vbs", "VBS", "基于虚拟化的安全", "Virtualization-based Security", "Virtualization-based Security", (s) => s.Vbs),
        new("hvci", "内存完整性", "HVCI / 强制代码完整性", "Memory Integrity", "Hypervisor-enforced Code Integrity", (s) => s.Hvci),
        new("cg", "Credential Guard", "凭证隔离保护", "Credential Guard", "Isolated credential storage", (s) => s.CredentialGuard),
        new("hyperv", "Hyper-V", "Hyper-V 虚拟化平台", "Hyper-V", "Hyper-V platform", (s) => s.HyperV),
        new("vt", "CPU 虚拟化", "VT-x / AMD-V 硬件支持", "CPU Virtualization", "VT-x / AMD-V support", (s) => s.Virtualization),
        new("slat", "SLAT", "二级地址转换", "SLAT", "Second Level Address Translation", (s) => s.Slat),
        new("dep", "DEP", "数据执行保护", "DEP", "Data Execution Prevention", (s) => s.Dep),
        new("64bit", "64-bit", "操作系统位数", "64-bit", "Operating System Architecture", (s) => s.Is64Bit),
        new("sb", "Secure Boot", "UEFI 安全启动", "Secure Boot", "UEFI Secure Boot", (s) => s.SecureBoot),
        new("tpm", "TPM", "可信平台模块", "TPM", "Trusted Platform Module", (s) => s.Tpm),
        new("hvlaunch", "Hypervisor 启动", "BCD 虚拟机监控程序启动类型", "HV Launch", "BCD hypervisorlaunchtype", (s) => s.HypervisorLaunchType),
        new("efiguard", "EfiGuard 启动项", "EfiGuard 引导项状态", "EfiGuard Entry", "EfiGuard bootloader entry", (s) => s.EfiGuard),
    };

    public DashboardPage(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeComponent();
        CreateCards();
        _ = RefreshAsync();
        _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += async (_, _) => await RefreshAsync();
        _timer.Start();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer?.Stop();
    }

    private void CreateCards()
    {
        // Unregister old names before clearing
        foreach (var def in _cardDefs)
        {
            try { UnregisterName($"Card_{def.Key}"); } catch { }
            try { UnregisterName($"Title_{def.Key}"); } catch { }
            try { UnregisterName($"Chip_{def.Key}"); } catch { }
            try { UnregisterName($"Desc_{def.Key}"); } catch { }
        }
        StatusCardsPanel.Children.Clear();
        foreach (var def in _cardDefs)
        {
            var card = CreateCardBorder(def);
            StatusCardsPanel.Children.Add(card);
        }
    }

    private Border CreateCardBorder(StatusCardDef def)
    {
        var border = new Border
        {
            Width = 280,
            Margin = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(29, 29, 31)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 44)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20),
            Opacity = 0,
            RenderTransform = new TranslateTransform(0, 20)
        };
        RegisterName($"Card_{def.Key}", border);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var title = new TextBlock
        {
            Text = _mainWindow.IsChinese ? def.TitleZh : def.TitleEn,
            Foreground = new SolidColorBrush(Color.FromRgb(245, 245, 247)),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        RegisterName($"Title_{def.Key}", title);
        Grid.SetColumn(title, 0);

        var chip = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(30, 110, 110, 115)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(8, 3, 8, 3),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new TextBlock
            {
                Text = _mainWindow.IsChinese ? "未知" : "UNKNOWN",
                Foreground = new SolidColorBrush(Color.FromRgb(110, 110, 115)),
                FontSize = 11,
                FontWeight = FontWeights.Bold
            }
        };
        RegisterName($"Chip_{def.Key}", chip);
        Grid.SetColumn(chip, 1);

        header.Children.Add(title);
        header.Children.Add(chip);
        Grid.SetRow(header, 0);

        // Description
        var desc = new TextBlock
        {
            Text = _mainWindow.IsChinese ? def.DescZh : def.DescEn,
            Foreground = new SolidColorBrush(Color.FromRgb(110, 110, 115)),
            FontSize = 12,
            Margin = new Thickness(0, 10, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        RegisterName($"Desc_{def.Key}", desc);
        Grid.SetRow(desc, 1);

        grid.Children.Add(header);
        grid.Children.Add(desc);
        border.Child = grid;

        // Entrance animation
        var sb = new Storyboard();
        var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(_cardDefs.IndexOf(def) * 50)
        };
        Storyboard.SetTarget(fadeAnim, border);
        Storyboard.SetTargetProperty(fadeAnim, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(fadeAnim);

        var transAnim = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(400))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            BeginTime = TimeSpan.FromMilliseconds(_cardDefs.IndexOf(def) * 50)
        };
        Storyboard.SetTarget(transAnim, border);
        Storyboard.SetTargetProperty(transAnim, new PropertyPath("RenderTransform.Y"));
        sb.Children.Add(transAnim);

        Loaded += (_, _) => sb.Begin();

        return border;
    }

    private async Task RefreshAsync()
    {
        try
        {
            _lastStatus = await _queryService.GetSystemStatusAsync();
            UpdateUI(_lastStatus);
        }
        catch (Exception ex)
        {
            // Log error silently
            System.Diagnostics.Debug.WriteLine($"Refresh error: {ex}");
        }
    }

    private void UpdateUI(SecurityStatus status)
    {
        if (Dispatcher.CheckAccess() == false)
        {
            Dispatcher.Invoke(() => UpdateUI(status));
            return;
        }

        OsInfoText.Text = $"{status.OsInfo?.OsName} ({status.OsInfo?.OsVersion})";
        UpdatedText.Text = DateTime.Now.ToString("HH:mm:ss");

        foreach (var def in _cardDefs)
        {
            var value = def.Getter(status);
            var (text, cls) = GetStatusDisplay(value, def.Key);
            UpdateCard(def.Key, text, cls);
        }
    }

    private void UpdateCard(string key, string text, string cls)
    {
        var card = FindName($"Card_{key}") as Border;
        var chip = FindName($"Chip_{key}") as Border;
        var chipText = chip?.Child as TextBlock;
        if (chipText is null) return;

        chipText.Text = text;

        var (bg, fg) = cls switch
        {
            "on" => (Color.FromArgb(30, 48, 209, 88), Color.FromRgb(48, 209, 88)),
            "off" => (Color.FromArgb(30, 255, 69, 58), Color.FromRgb(255, 69, 58)),
            "warn" => (Color.FromArgb(30, 255, 159, 10), Color.FromRgb(255, 159, 10)),
            _ => (Color.FromArgb(30, 110, 110, 115), Color.FromRgb(110, 110, 115))
        };

        chip.Background = new SolidColorBrush(bg);
        chipText.Foreground = new SolidColorBrush(fg);

        // Left border indicator
        var leftColor = cls switch
        {
            "on" => Color.FromRgb(48, 209, 88),
            "off" => Color.FromRgb(255, 69, 58),
            "warn" => Color.FromRgb(255, 159, 10),
            _ => Color.FromRgb(72, 72, 74)
        };

        // Create left indicator
        if (card?.Child is Grid grid)
        {
            var existing = grid.Children.OfType<Rectangle>().FirstOrDefault();
            if (existing is null)
            {
                var rect = new Rectangle
                {
                    Width = 3,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    RadiusX = 2,
                    RadiusY = 2,
                    Margin = new Thickness(-20, -20, 0, -20)
                };
                Grid.SetRowSpan(rect, 2);
                grid.Children.Add(rect);
                existing = rect;
            }
            existing.Fill = new SolidColorBrush(leftColor);
        }
    }

    private (string text, string cls) GetStatusDisplay(object? value, string key)
    {
        bool zh = _mainWindow.IsChinese;

        if (value is null)
            return (zh ? "未知" : "UNKNOWN", "unknown");

        if (key == "vbs")
        {
            return (int)value switch
            {
                0 => (zh ? "禁用" : "OFF", "off"),
                1 => (zh ? "启用" : "ON", "on"),
                2 => (zh ? "运行中" : "RUNNING", "on"),
                _ => (zh ? "未知" : "UNKNOWN", "unknown")
            };
        }

        if (key == "hvci" || key == "efiguard")
        {
            return (int)value switch
            {
                0 => (zh ? "禁用" : "OFF", "off"),
                1 => (zh ? "启用" : "ON", "on"),
                _ => (zh ? "未知" : "UNKNOWN", "unknown")
            };
        }

        if (key == "credentialGuard")
        {
            return (int)value switch
            {
                0 => (zh ? "禁用" : "OFF", "off"),
                1 => (zh ? "启用" : "ON", "on"),
                2 => (zh ? "审计" : "AUDIT", "warn"),
                _ => (zh ? "未知" : "UNKNOWN", "unknown")
            };
        }

        if (key == "hyperv" || key == "hvlaunch")
        {
            var s = value.ToString()?.ToLowerInvariant();
            if (s == "enabled" || s == "auto") return (zh ? "启用" : "ON", "on");
            if (s == "disabled" || s == "off") return (zh ? "禁用" : "OFF", "off");
            return (value.ToString() ?? (zh ? "未知" : "UNKNOWN"), "unknown");
        }

        if (key == "tpm")
        {
            if (value is TpmInfo tpm)
            {
                if (tpm.Present && tpm.Enabled) return (zh ? "就绪" : "READY", "on");
                if (tpm.Present && !tpm.Enabled) return (zh ? "存在" : "PRESENT", "warn");
                if (!tpm.Present) return (zh ? "缺失" : "MISSING", "off");
            }
            return (zh ? "未知" : "UNKNOWN", "unknown");
        }

        if (key == "dep")
        {
            if (value is bool depBool)
                return depBool ? (zh ? "可用" : "AVAILABLE", "on") : (zh ? "不可用" : "UNAVAILABLE", "off");
            return (zh ? "未知" : "UNKNOWN", "unknown");
        }

        if (key == "64bit")
        {
            if (value is bool bitBool)
                return bitBool ? ("64-bit", "on") : ("32-bit", "off");
            return (zh ? "未知" : "UNKNOWN", "unknown");
        }

        // bool
        if (value is bool b)
            return b ? (zh ? "启用" : "ON", "on") : (zh ? "禁用" : "OFF", "off");

        return (zh ? "未知" : "UNKNOWN", "unknown");
    }

    public void RefreshLanguage()
    {
        TitleText.Text = _mainWindow.IsChinese ? "系统安全中心" : "Security Center";
        SubtitleText.Text = _mainWindow.IsChinese
            ? "可视化管理系统虚拟化与安全功能状态"
            : "Visualize system virtualization and security features";
        LiveText.Text = _mainWindow.IsChinese ? "实时监控中 / Live Monitoring" : "Live Monitoring / 实时监控中";

        // Update card titles and descriptions
        foreach (var def in _cardDefs)
        {
            var title = FindName($"Title_{def.Key}") as TextBlock;
            var desc = FindName($"Desc_{def.Key}") as TextBlock;
            if (title != null) title.Text = _mainWindow.IsChinese ? def.TitleZh : def.TitleEn;
            if (desc != null) desc.Text = _mainWindow.IsChinese ? def.DescZh : def.DescEn;
        }

        if (_lastStatus != null) UpdateUI(_lastStatus);
    }

    
}
