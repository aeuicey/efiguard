using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EfiGuardUI.Views;

namespace EfiGuardUI;

public partial class MainWindow : Window
{
    private bool _isChinese = true;
    private DashboardPage? _dashboardPage;
    private OperationsPage? _operationsPage;
    public LogsPage? _logsPage;

    public MainWindow()
    {
        InitializeComponent();
        ShowDashboard();
        CheckAdmin();
    }

    private void CheckAdmin()
    {
        try
        {
            Process.Start(new ProcessStartInfo("net", "session")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })?.WaitForExit();
            // If we get here, admin check passed
        }
        catch
        {
            AdminBanner.Visibility = Visibility.Visible;
        }
    }

    public bool IsChinese => _isChinese;

    public void ToggleLanguage()
    {
        _isChinese = !_isChinese;
        var ci = _isChinese ? new CultureInfo("zh-CN") : new CultureInfo("en-US");
        Thread.CurrentThread.CurrentUICulture = ci;
        Thread.CurrentThread.CurrentCulture = ci;
        _dashboardPage?.RefreshLanguage();
        _operationsPage?.RefreshLanguage();
        _logsPage?.RefreshLanguage();
    }

    private void ShowDashboard()
    {
        _dashboardPage ??= new DashboardPage(this);
        ContentFrame.Navigate(_dashboardPage);
        SetNavActive(NavDashboard);
    }

    private void ShowOperations()
    {
        _operationsPage ??= new OperationsPage(this);
        ContentFrame.Navigate(_operationsPage);
        SetNavActive(NavOperations);
    }

    private void ShowLogs()
    {
        _logsPage ??= new LogsPage(this);
        ContentFrame.Navigate(_logsPage);
        SetNavActive(NavLogs);
    }

    private void SetNavActive(Button active)
    {
        NavDashboard.Tag = null;
        NavOperations.Tag = null;
        NavLogs.Tag = null;
        active.Tag = "Active";
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
    private void NavOperations_Click(object sender, RoutedEventArgs e) => ShowOperations();
    private void NavLogs_Click(object sender, RoutedEventArgs e) => ShowLogs();

    private void LangToggle_Click(object sender, RoutedEventArgs e) => ToggleLanguage();
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }
}
