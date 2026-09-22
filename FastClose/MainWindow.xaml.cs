using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FastClose
{
    public class AppConfig
    {
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public bool MatchExactTitle { get; set; } = false;
        public double? WindowLeft { get; set; }
        public double? WindowTop { get; set; }
    }

    public partial class MainWindow : Window
    {
        private const uint WM_CLOSE = 0x0010;
        private const uint WM_QUERYENDSESSION = 0x0011;
        private const uint WM_ENDSESSION = 0x0016;

        // 记录上一次点击的时间戳（毫秒），用于防连击节流
        private long _lastClickTimestamp = 0;
        private const int ClickIntervalMilliseconds = 300;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpfn, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public MainWindow()
        {
            InitializeComponent();
            EnsureConfigFileExists();
            RestoreWindowPosition();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Windows 11 / 10 的标题栏占据了高度，通过调整 Window.Height 保证客户区保持 1:1 正方形
            if (RootGrid.ActualWidth > 0)
            {
                double targetHeight = RootGrid.ActualWidth;
                double extraHeight = this.ActualHeight - RootGrid.ActualHeight;
                this.Height = targetHeight + extraHeight;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveWindowPosition();
        }

        public static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fc_config.json");

        public static void EnsureConfigFileExists()
        {
            if (!File.Exists(ConfigPath))
            {
                var defaultConfig = new AppConfig
                {
                    ProcessName = "",
                    WindowTitle = "记事本",
                    MatchExactTitle = false
                };

                SaveConfig(defaultConfig);
            }
        }

        public static void SaveConfig(AppConfig config)
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigPath, json, Encoding.UTF8);
        }

        public static AppConfig LoadConfig()
        {
            EnsureConfigFileExists();
            var json = File.ReadAllText(ConfigPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }

        /// <summary>
        /// 右击窗口弹出设置对话框
        /// </summary>
        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// 程序启动时恢复窗口位置（若超出当前屏幕分辨率范围则保持居中）
        /// </summary>
        private void RestoreWindowPosition()
        {
            try
            {
                var config = LoadConfig();
                if (config.WindowLeft.HasValue && config.WindowTop.HasValue)
                {
                    double left = config.WindowLeft.Value;
                    double top = config.WindowTop.Value;
                    double width = Width > 0 ? Width : 120;
                    double height = Height > 0 ? Height : 150;

                    if (IsPositionOnScreen(left, top, width, height))
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual;
                        Left = left;
                        Top = top;
                    }
                }
            }
            catch
            {
                // 恢复位置失败时忽略，沿用默认居中
            }
        }

        /// <summary>
        /// 程序关闭时保存当前屏幕坐标到配置文件（若坐标未发生变化则跳过写入）
        /// </summary>
        private void SaveWindowPosition()
        {
            try
            {
                var config = LoadConfig();

                double currentLeft = WindowState == WindowState.Normal ? Left : RestoreBounds.Left;
                double currentTop = WindowState == WindowState.Normal ? Top : RestoreBounds.Top;

                // 坐标未变动（容差 0.5 像素以兼容 DPI 缩放微小偏差）时无需重复保存
                if (config.WindowLeft.HasValue && config.WindowTop.HasValue &&
                    Math.Abs(config.WindowLeft.Value - currentLeft) < 0.5 &&
                    Math.Abs(config.WindowTop.Value - currentTop) < 0.5)
                {
                    return;
                }

                config.WindowLeft = currentLeft;
                config.WindowTop = currentTop;
                SaveConfig(config);
            }
            catch
            {
                // 避免关闭程序因文件写入失败而崩溃
            }
        }

        /// <summary>
        /// 检查坐标和窗口是否完全在当前虚拟屏幕范围内
        /// </summary>
        private static bool IsPositionOnScreen(double left, double top, double width, double height)
        {
            double virtualLeft = SystemParameters.VirtualScreenLeft;
            double virtualTop = SystemParameters.VirtualScreenTop;
            double virtualWidth = SystemParameters.VirtualScreenWidth;
            double virtualHeight = SystemParameters.VirtualScreenHeight;

            return left >= virtualLeft &&
                   (left + width) <= (virtualLeft + virtualWidth) &&
                   top >= virtualTop &&
                   (top + height) <= (virtualTop + virtualHeight);
        }

        private void CloseWindowGracefully(IntPtr hWnd)
        {
            SendMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            SendMessage(hWnd, WM_QUERYENDSESSION, IntPtr.Zero, IntPtr.Zero);
            SendMessage(hWnd, WM_ENDSESSION, new IntPtr(1), IntPtr.Zero);
        }

        /// <summary>
        /// 动态创建独立的提示文本并播放动画，不会打断前一个动画；支持指定颜色
        /// </summary>
        private void ShowToast(string message, Brush textColor)
        {
            var transform = new TranslateTransform();

            var toast = new TextBlock
            {
                Text = message,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = textColor,
                Opacity = 0.0,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                RenderTransform = transform
            };

            // 将提示加入到容器中
            RootGrid.Children.Add(toast);

            double containerHeight = RootGrid.ActualHeight > 0 ? RootGrid.ActualHeight : 100.0;
            double startY = containerHeight * 0.30;
            double endY = -containerHeight * 0.30;
            var duration = TimeSpan.FromSeconds(1.3);

            // 1. 垂直位移动画（从 80% 到 20% 高度）
            var translateYAnim = new DoubleAnimation
            {
                From = startY,
                To = endY,
                Duration = duration,
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            // 2. 淡入淡出动画（0 -> 0.9 -> 0.9保持 -> 0）
            var opacityAnim = new DoubleAnimationUsingKeyFrames();
            opacityAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            opacityAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
            opacityAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.9))));
            opacityAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(duration)));

            // 动画播放完成后，自动从 RootGrid 中移除该 TextBlock，释放资源
            opacityAnim.Completed += (s, e) =>
            {
                RootGrid.Children.Remove(toast);
            };

            transform.BeginAnimation(TranslateTransform.YProperty, translateYAnim);
            toast.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        }

        private void BtnCloseTarget_Click(object sender, RoutedEventArgs e)
        {
            // 防连击限制：距离上次点击不足 300ms 则直接忽略
            long now = Environment.TickCount64;
            if (now - _lastClickTimestamp < ClickIntervalMilliseconds)
            {
                return;
            }
            _lastClickTimestamp = now;

            try
            {
                var config = LoadConfig();
                bool found = false;

                if (!string.IsNullOrWhiteSpace(config.ProcessName))
                {
                    var processes = Process.GetProcessesByName(config.ProcessName.Trim());
                    foreach (var proc in processes)
                    {
                        using (proc)
                        {
                            foreach (ProcessThread thread in proc.Threads)
                            {
                                EnumThreadWindows((uint)thread.Id, (hWnd, lParam) =>
                                {
                                    CloseWindowGracefully(hWnd);
                                    found = true;
                                    return true;
                                }, IntPtr.Zero);
                            }

                            try { proc.CloseMainWindow(); } catch { }
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(config.WindowTitle))
                {
                    EnumWindows((hWnd, lParam) =>
                    {
                        var titleBuilder = new StringBuilder(256);
                        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                        string title = titleBuilder.ToString();

                        if (string.IsNullOrWhiteSpace(title))
                            return true;

                        bool titleMatches = config.MatchExactTitle
                            ? title.Equals(config.WindowTitle, StringComparison.OrdinalIgnoreCase)
                            : title.Contains(config.WindowTitle, StringComparison.OrdinalIgnoreCase);

                        if (titleMatches)
                        {
                            CloseWindowGracefully(hWnd);
                            found = true;
                        }

                        return true;
                    }, IntPtr.Zero);
                }
                else
                {
                    ShowToast("请先配置规则", Brushes.White);
                    return;
                }

                if (!found)
                {
                    // 未找到程序：黄色
                    ShowToast("未找到目标", new SolidColorBrush(Color.FromRgb(255, 235, 59)));
                }
                else
                {
                    // 成功关闭：浅绿色
                    ShowToast("已成功关闭", new SolidColorBrush(Color.FromRgb(76, 175, 80)));
                }
            }
            catch (Exception)
            {
                ShowToast("执行失败", new SolidColorBrush(Color.FromRgb(255, 138, 128)));
            }
        }
    }
}