using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EfiGuardUI.Views;

public partial class LogsPage : Page
{
    private readonly MainWindow _mainWindow;
    private bool _hasContent = false;

    public LogsPage(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        InitializeComponent();
    }

    public void AppendLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            if (!_hasContent)
            {
                LogsTextBox.Text = "";
                _hasContent = true;
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var color = message switch
            {
                _ when message.StartsWith("[OK]") => "#30d158",
                _ when message.StartsWith("[ERR]") => "#ff453a",
                _ when message.StartsWith("[WARN]") => "#ff9f0a",
                _ when message.StartsWith("[!]") => "#ff9f0a",
                _ => "#86868b"
            };

            // For simplicity, append as plain text with color not possible in TextBox
            // Use Runs in a RichTextBox for colors, but TextBox is simpler
            LogsTextBox.AppendText($"[{timestamp}] {message}\n");
            LogsTextBox.ScrollToEnd();
            LogsScroll.ScrollToEnd();
        });
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        LogsTextBox.Text = _mainWindow.IsChinese ? "等待操作执行..." : "Waiting for operations...";
        _hasContent = false;
    }

    public void RefreshLanguage()
    {
        bool zh = _mainWindow.IsChinese;
        LogsBadge.Text = zh ? "系统日志 / System Logs" : "System Logs / 系统日志";
        LogsTitle.Text = zh ? "操作日志" : "Operation Logs";
        LogsSubtitle.Text = zh ? "命令执行输出与状态记录" : "Command output and status records";
        LiveLabel.Text = zh ? "实时输出 / Live Output" : "Live Output / 实时输出";
        if (!_hasContent)
            LogsTextBox.Text = zh ? "等待操作执行..." : "Waiting for operations...";
    }
}
