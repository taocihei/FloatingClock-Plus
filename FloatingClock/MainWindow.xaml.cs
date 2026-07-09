using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace FloatingClock
{
    using System.Windows.Media;
    using Microsoft.Win32;

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow Current;
        public static bool WindowIsVisible;
        public static bool HotCornerEnabled;
        public static bool SecondsEnabled;
        public static bool HideIfFocusLost = true;
        public static bool DisableGlass = false;
        public static string TimeZoneId = "";     // 空 = 跟随系统本地时区
        public static int ClockPosition = 0;      // 0=左下 1=左上 2=右上 3=右下 4=居中
        public static string LanguageCode = "";   // "" 跟随系统, "zh", "en"
        public static bool Locked = false;        // 锁定位置，禁止拖动
        public static double ClockOpacity = 0.9;  // 目标不透明度 0.2 ~ 1.0
        public static double CustomLeft = double.NaN;  // 手动拖动记忆坐标
        public static double CustomTop = double.NaN;
        public static bool AlwaysOnTop = true;    // 强制显示在所有窗口最前
        public static bool ClickThrough = false;  // 点击穿透（显示模式，不拦截鼠标）
        public static double ClockScaleX = 1.0;   // 横向缩放
        public static double ClockScaleY = 1.0;   // 纵向缩放
        public static int ClockBackShade = 17;    // 背景底色明暗 0(黑) ~ 255(白)
        public static int TargetScreen = -1;      // -1=跟随鼠标所在屏；0..N=指定屏幕
        public static string ClockSkin = "默认（玻璃白）";  // 当前皮肤名

        private NotifyIcon notifyIcon;
        private DispatcherTimer refreshDispatcher;
        private DispatcherTimer topmostTimer;
        private OptionsWindow optionsWindow;

        /// <summary>
        ///     写入设置到注册表
        /// </summary>
        private static void SaveReg(string name, bool value)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BaalTech\FloatingClock", true)
                                      ?? Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BaalTech\FloatingClock");
            registryKey.SetValue(name, Convert.ToInt32(value));
            registryKey.Close();
        }

        private static void SaveReg(string name, string value)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BaalTech\FloatingClock", true)
                                      ?? Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BaalTech\FloatingClock");
            registryKey.SetValue(name, value ?? "");
            registryKey.Close();
        }

        private static void SaveReg(string name, int value)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BaalTech\FloatingClock", true)
                                      ?? Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BaalTech\FloatingClock");
            registryKey.SetValue(name, value);
            registryKey.Close();
        }

        private static double ParseDouble(object val, double fallback)
        {
            double d;
            if (val != null && double.TryParse(Convert.ToString(val),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out d))
                return d;
            return fallback;
        }

        /// <summary>
        ///     统一设置入口（托盘菜单与设置窗口共用），负责应用效果 + 持久化
        /// </summary>
        internal void SetHotCorner(bool v)
        {
            EnableHotCorner(v);
            SaveReg(nameof(HotCornerEnabled), v);
        }

        internal void SetSeconds(bool v)
        {
            EnableSeconds(v);
            SaveReg(nameof(SecondsEnabled), v);
        }

        internal void SetHideIfFocusLost(bool v)
        {
            HideIfFocusLost = v;
            SaveReg(nameof(HideIfFocusLost), v);
        }

        internal void SetLocked(bool v)
        {
            Locked = v;
            SaveReg(nameof(Locked), v);
            // 优先级：解锁即恢复可拖动 —— 若还在浮窗穿透，一并关掉
            if (!v && ClickThrough)
            {
                SetClickThrough(false);
                RebuildTrayMenu();
                RefreshOptionsWindow();
            }
        }

        internal void SetDisableGlass(bool v)
        {
            DisableGlass = v;
            ApplyBackground();
            SaveReg(nameof(DisableGlass), v);
        }

        /// <summary>
        ///     按当前设置应用背景：玻璃 或 指定明暗的纯色底
        /// </summary>
        private void ApplyBackground()
        {
            if (SystemParameters.IsGlassEnabled && !DisableGlass)
                ClockWindow.Background = SystemParameters.WindowGlassBrush;
            else
            {
                byte g = (byte)ClockBackShade;
                ClockWindow.Background = new SolidColorBrush(Color.FromArgb(255, g, g, g));
            }
        }

        /// <summary>
        ///     背景底色明暗（0=黑 ~ 255=白）。调整即切到纯色底以便预览。
        /// </summary>
        internal void SetBackShade(int shade)
        {
            ClockBackShade = Math.Max(0, Math.Min(255, shade));
            DisableGlass = true;
            SaveReg(nameof(ClockBackShade), ClockBackShade);
            SaveReg(nameof(DisableGlass), true);
            ApplyBackground();
        }

        /// <summary>
        ///     横向 / 纵向缩放时钟（LayoutTransform，窗口随内容自适应大小）
        /// </summary>
        private void ApplyScale()
        {
            grid.LayoutTransform = new ScaleTransform(ClockScaleX, ClockScaleY);
        }

        internal void SetScaleX(double v)
        {
            ClockScaleX = v;
            SaveReg(nameof(ClockScaleX), v.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ApplyScale();
            if (WindowIsVisible) SetPositionOnCurrentDisplay();
        }

        internal void SetScaleY(double v)
        {
            ClockScaleY = v;
            SaveReg(nameof(ClockScaleY), v.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ApplyScale();
            if (WindowIsVisible) SetPositionOnCurrentDisplay();
        }

        internal void SetTimeZone(string id)
        {
            TimeZoneId = id ?? "";
            SaveReg(nameof(TimeZoneId), TimeZoneId);
            Refresh();
        }

        internal void SetPosition(int pos)
        {
            ClockPosition = pos;
            SaveReg(nameof(ClockPosition), pos);
            if (WindowIsVisible) SetPositionOnCurrentDisplay();
        }

        internal void SetTargetScreen(int idx)
        {
            TargetScreen = idx;
            SaveReg(nameof(TargetScreen), idx);
            if (WindowIsVisible) SetPositionOnCurrentDisplay();
        }

        internal void SetSkin(string name)
        {
            var s = SkinLibrary.Find(name);
            if (s == null) return;
            ClockSkin = name;
            SaveReg(nameof(ClockSkin), name);
            ApplySkin(s);
        }

        /// <summary>
        ///     应用皮肤：字体 + 时间色 + 日期色 + 背景（Back 为空则不动背景）
        /// </summary>
        private void ApplySkin(Skin s)
        {
            if (s == null) return;
            var timeBrush = BrushFrom(s.TimeColor, Brushes.White);
            var dateBrush = BrushFrom(s.DateColor, timeBrush);
            var ff = new FontFamily(s.Font);
            var fw = s.Bold ? FontWeights.Bold : FontWeights.Normal;

            foreach (var t in new[] { Hours, Dots, Minutes, Seconds, SecondDots })
            {
                t.Foreground = timeBrush;
                t.FontFamily = ff;
                t.FontWeight = fw;
            }
            DayOfTheWeek.Foreground = dateBrush;
            DayOfTheWeek.FontFamily = ff;
            DayOfTheMonth.Foreground = dateBrush;
            DayOfTheMonth.FontFamily = ff;

            if (s.Back == "glass")
            {
                if (SystemParameters.IsGlassEnabled)
                    ClockWindow.Background = SystemParameters.WindowGlassBrush;
            }
            else if (!string.IsNullOrEmpty(s.Back))
            {
                var bg = BrushFrom(s.Back, null);
                if (bg != null) ClockWindow.Background = bg;
            }
            // Back 为 "" 时不动背景，沿用玻璃 / 底色设置
        }

        private static SolidColorBrush BrushFrom(string hex, SolidColorBrush fallback)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch { return fallback; }
        }

        internal void SetLanguage(string code)
        {
            LanguageCode = code ?? "";
            SaveReg(nameof(LanguageCode), LanguageCode);
            Refresh();
            RebuildTrayMenu();
            RefreshOptionsWindow();
        }

        /// <summary>
        ///     重开设置窗口，让其中控件重新读取最新状态 / 文本
        /// </summary>
        private void RefreshOptionsWindow()
        {
            if (optionsWindow != null)
            {
                optionsWindow.Close();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    optionsWindow = new OptionsWindow();
                    optionsWindow.Closed += (s, a) => optionsWindow = null;
                    optionsWindow.Show();
                }));
            }
        }

        internal void SetOpacity(double v)
        {
            ClockOpacity = v;
            SaveReg(nameof(ClockOpacity), v.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (WindowIsVisible) ClockWindow.Opacity = v;
        }

        internal void SetAlwaysOnTop(bool v)
        {
            AlwaysOnTop = v;
            Topmost = v;
            SaveReg(nameof(AlwaysOnTop), v);
            if (v && WindowIsVisible) BringToTop();
        }

        internal void SetClickThrough(bool v)
        {
            ClickThrough = v;
            SaveReg(nameof(ClickThrough), v);
            ApplyClickThrough();
        }

        /// <summary>
        ///     浮窗模式总开关。
        ///     开启 = 覆盖在所有窗口上 + 鼠标穿透 + 不可移动（类似 NVIDIA 性能浮窗 / AIDA64 检测窗）；
        ///     关闭 = 普通窗口，可自由拖动移动。
        /// </summary>
        internal void SetOverlayMode(bool v)
        {
            if (v)
            {
                SetAlwaysOnTop(true);
                SetClickThrough(true);
                Locked = true;               // 浮窗模式即固定
                SaveReg(nameof(Locked), true);
            }
            else
            {
                SetClickThrough(false);
                Locked = false;              // 关闭浮窗即恢复可拖动
                SaveReg(nameof(Locked), false);
            }
            RebuildTrayMenu();
            RefreshOptionsWindow();
        }

        // ---- 点击穿透（显示模式）：WS_EX_TRANSPARENT 让鼠标点击穿过时钟到下层窗口 ----
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        /// <summary>
        ///     应用 / 取消点击穿透。开启后时钟不再拦截鼠标，点击直接落到下面的界面上。
        /// </summary>
        private void ApplyClickThrough()
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero) return;
                int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
                if (ClickThrough) ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
                else ex &= ~WS_EX_TRANSPARENT;
                SetWindowLong(hwnd, GWL_EXSTYLE, ex);
            }
            catch { }
        }

        // ---- 开机自启：写 HKCU\...\Run 键 ----
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "FloatingClock";

        internal static bool IsAutoStartEnabled()
        {
            using (var run = Registry.CurrentUser.OpenSubKey(RunKey, false))
                return run != null && run.GetValue(RunValueName) != null;
        }

        internal void SetAutoStart(bool v)
        {
            try
            {
                using (var run = Registry.CurrentUser.OpenSubKey(RunKey, true)
                                 ?? Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (v)
                    {
                        string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        run.SetValue(RunValueName, "\"" + exe + "\"");
                    }
                    else if (run.GetValue(RunValueName) != null)
                    {
                        run.DeleteValue(RunValueName, false);
                    }
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001, SWP_NOACTIVATE = 0x0010;

        /// <summary>
        ///     用 Win32 强制把窗口提到最前（对付其它置顶 / 全屏窗口）
        /// </summary>
        private void BringToTop()
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
            catch { }
        }

        /// <summary>
        ///     周期性重申置顶（仅在可见且开启时），压过后来出现的置顶窗口
        /// </summary>
        private void TopmostReassert(object sender, EventArgs e)
        {
            if (WindowIsVisible && AlwaysOnTop) BringToTop();
        }

        /// <summary>
        ///     拖动结束后保存自定义位置，并把位置模式切到"自定义"
        /// </summary>
        private void SaveCustomPosition()
        {
            CustomLeft = Left;
            CustomTop = Top;
            ClockPosition = 5;
            SaveReg("CustomLeft", CustomLeft.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SaveReg("CustomTop", CustomTop.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SaveReg(nameof(ClockPosition), 5);
        }

        /// <summary>
        ///     鼠标左键拖动窗口（锁定时禁止）
        /// </summary>
        private void Window_DragMove(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Locked) return;
            try
            {
                DragMove();
                SaveCustomPosition();
            }
            catch { }
        }

        /// <summary>
        ///     取当前时间（考虑自定义时区）
        /// </summary>
        private static DateTime GetNow()
        {
            if (string.IsNullOrEmpty(TimeZoneId)) return DateTime.Now;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch { return DateTime.Now; }
        }

        /// <summary>
        ///     Initialize Application and Main Window
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Current = this;
            MouseLeftButtonDown += Window_DragMove;
            SourceInitialized += (s, e) => ApplyClickThrough();

            RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BaalTech\FloatingClock");
            HotCornerEnabled = Convert.ToBoolean(registryKey.GetValue(nameof(HotCornerEnabled), Convert.ToInt32(HotCornerEnabled)));
            SecondsEnabled = Convert.ToBoolean(registryKey.GetValue(nameof(SecondsEnabled), Convert.ToInt32(SecondsEnabled)));
            HideIfFocusLost = Convert.ToBoolean(registryKey.GetValue(nameof(HideIfFocusLost), Convert.ToInt32(HideIfFocusLost)));
            DisableGlass = Convert.ToBoolean(registryKey.GetValue(nameof(DisableGlass), Convert.ToInt32(DisableGlass)));
            TimeZoneId = Convert.ToString(registryKey.GetValue(nameof(TimeZoneId), TimeZoneId));
            ClockPosition = Convert.ToInt32(registryKey.GetValue(nameof(ClockPosition), ClockPosition));
            LanguageCode = Convert.ToString(registryKey.GetValue(nameof(LanguageCode), LanguageCode));
            Locked = Convert.ToBoolean(registryKey.GetValue(nameof(Locked), Convert.ToInt32(Locked)));
            ClockOpacity = ParseDouble(registryKey.GetValue(nameof(ClockOpacity)), ClockOpacity);
            CustomLeft = ParseDouble(registryKey.GetValue("CustomLeft"), CustomLeft);
            CustomTop = ParseDouble(registryKey.GetValue("CustomTop"), CustomTop);
            AlwaysOnTop = Convert.ToBoolean(registryKey.GetValue(nameof(AlwaysOnTop), Convert.ToInt32(AlwaysOnTop)));
            ClickThrough = Convert.ToBoolean(registryKey.GetValue(nameof(ClickThrough), Convert.ToInt32(ClickThrough)));
            ClockScaleX = ParseDouble(registryKey.GetValue(nameof(ClockScaleX)), ClockScaleX);
            ClockScaleY = ParseDouble(registryKey.GetValue(nameof(ClockScaleY)), ClockScaleY);
            ClockBackShade = Convert.ToInt32(registryKey.GetValue(nameof(ClockBackShade), ClockBackShade));
            TargetScreen = Convert.ToInt32(registryKey.GetValue(nameof(TargetScreen), TargetScreen));
            ClockSkin = Convert.ToString(registryKey.GetValue(nameof(ClockSkin), ClockSkin));
            registryKey.Close();

            Refresh();

            ApplyBackground();
            ApplyScale();
            ApplySkin(SkinLibrary.Find(ClockSkin) ?? SkinLibrary.Builtin()[0]);

            ShowClock();
            InitializeRefreshDispatcher();

            EnableSeconds(SecondsEnabled);

            new HotKey(Key.C, KeyModifier.Alt, key => ShowClock());
            EnableHotCorner(HotCornerEnabled);

            TrayIcon();

            Topmost = AlwaysOnTop;
            topmostTimer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 2) };
            topmostTimer.Tick += TopmostReassert;
            topmostTimer.Start();
        }

        private void EnableHotCorner(bool enable)
        {
            if (enable)
                MouseHook._hookID = MouseHook.SetHook(MouseHook._proc);
            else MouseHook.UnhookWindowsHookEx(MouseHook._hookID);
            HotCornerEnabled = enable;
        }

        private void EnableSeconds(bool enable)
        {
            SecondsEnabled = enable;
            OptionalSeconds.Visibility = enable ? Visibility.Visible : Visibility.Hidden;
            Refresh();
            InitializeRefreshDispatcher();

            if (enable)
            {
                refreshDispatcher.Start();
            }
            else
            {
                WaitToFullMinuteAndRefresh();

            }
        }

        /// <summary>
        ///     Prepare Clock to Show 
        /// </summary>
        public void ShowClock()
        {
            if (!WindowIsVisible)
            {
                Refresh();
                InitializeAnimationIn();
                WaitToFullMinuteAndRefresh();
            }
            else
            {
                HideWindow();
            }
        }

        /// <summary>
        ///     Load Current Data to Controls
        /// </summary>
        private void LoadCurrentClockData()
        {
            var timeNow = GetNow();
            var ci = Loc.Culture;
            Hours.Text = timeNow.ToString("HH");
            Minutes.Text = timeNow.ToString("mm");
            Seconds.Text = timeNow.ToString("ss");
            DayOfTheWeek.Text = ci.TextInfo.ToTitleCase(timeNow.ToString("dddd", ci));
            DayOfTheMonth.Text = timeNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Initialize Refresh Dispatcher
        /// </summary>
        private void InitializeRefreshDispatcher()
        {
            refreshDispatcher = new DispatcherTimer();
            refreshDispatcher.Tick += Refresh;
            if(SecondsEnabled)
                refreshDispatcher.Interval = new TimeSpan(0,0,1);
            else
                refreshDispatcher.Interval = new TimeSpan(0, 1, 0);
        }

        /// <summary>
        ///     Wait to full minute refresh data and start refresh Dispatcher
        /// </summary>
        private async void WaitToFullMinuteAndRefresh()
        {
            await Task.Delay((60 - DateTime.Now.Second) * 1000);
            Refresh();
            refreshDispatcher.Start();
        }

        /// <summary>
        ///     DispatcherTimer Refresh Event
        /// </summary>
        /// <param name="sender">Dispatcher</param>
        /// <param name="e">Dispatcher Arg</param>
        private void Refresh(object sender = null, EventArgs e = null)
        {
            LoadCurrentClockData();
        }

        /// <summary>
        ///     Set position on current Display 
        /// </summary>
        private void SetPositionOnCurrentDisplay()
        {
            var screens = Screen.AllScreens;
            Screen activeScreen = (TargetScreen >= 0 && TargetScreen < screens.Length)
                ? screens[TargetScreen]
                : Screen.FromPoint(Control.MousePosition);
            int resHeight = Screen.PrimaryScreen.Bounds.Height;
            double actualHeight = SystemParameters.PrimaryScreenHeight;
            double dpi = resHeight / actualHeight;

            double waX = activeScreen.WorkingArea.X / dpi;
            double waY = activeScreen.WorkingArea.Y / dpi;
            double waW = activeScreen.WorkingArea.Width / dpi;
            double waH = activeScreen.WorkingArea.Height / dpi;

            double w = Application.Current.MainWindow.ActualWidth;
            double h = Application.Current.MainWindow.ActualHeight;
            if (w < 1) w = 420;
            if (h < 1) h = 180;
            const double margin = 50;

            double left, top;
            switch (ClockPosition)
            {
                case 1: // 左上
                    left = waX + margin; top = waY + margin; break;
                case 2: // 右上
                    left = waX + waW - w - margin; top = waY + margin; break;
                case 3: // 右下
                    left = waX + waW - w - margin; top = waY + waH - h - margin; break;
                case 4: // 居中
                    left = waX + (waW - w) / 2; top = waY + (waH - h) / 2; break;
                case 5: // 自定义（手动拖动记忆）—— 绝对坐标，跨屏也能回到原位
                    if (!double.IsNaN(CustomLeft) && !double.IsNaN(CustomTop))
                    {
                        // 用整个虚拟桌面（所有屏幕合并）范围钳制，而不是鼠标所在的单块屏，
                        // 否则开机鼠标在主屏时，保存在副屏的坐标会被拉回主屏。
                        double vX = SystemParameters.VirtualScreenLeft;
                        double vY = SystemParameters.VirtualScreenTop;
                        double vW = SystemParameters.VirtualScreenWidth;
                        double vH = SystemParameters.VirtualScreenHeight;
                        Application.Current.MainWindow.Left = Math.Max(vX, Math.Min(CustomLeft, vX + vW - w));
                        Application.Current.MainWindow.Top = Math.Max(vY, Math.Min(CustomTop, vY + vH - h));
                        return;
                    }
                    left = waX + margin; top = waY + waH - h - margin;
                    break;
                default: // 0 左下
                    left = waX + margin; top = waY + waH - h - margin; break;
            }
            Application.Current.MainWindow.Left = left;
            Application.Current.MainWindow.Top = top;
        }

        /// <summary>
        ///     Initialize Tray Icon and BaloonTip
        /// </summary>
        private void TrayIcon()
        {
            notifyIcon = new NotifyIcon();
            RebuildTrayMenu();
            notifyIcon.DoubleClick += OpenOptionWindow;   // 双击托盘图标直接打开设置

            var streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/clock.ico"));
            if (streamResourceInfo != null)
                notifyIcon.Icon = new Icon(streamResourceInfo.Stream);

            notifyIcon.Visible = true;

            notifyIcon.ShowBalloonTip(5, string.Format(Loc.T("balloon_hello"), Environment.UserName),
                Loc.T("balloon_body"), ToolTipIcon.Info);
        }

        /// <summary>
        ///     构建 / 重建托盘右键菜单（语言切换时重建）
        /// </summary>
        private void RebuildTrayMenu()
        {
            var menu = new ContextMenu();
            var hot = new MenuItem(Loc.T("tray_hotcorner"), ChangeHotCornerActiveState) { Checked = HotCornerEnabled };
            var sec = new MenuItem(Loc.T("tray_seconds"), ChangeSecondsState) { Checked = SecondsEnabled };
            var hide = new MenuItem(Loc.T("tray_hide"), ChangeHideIfFocusLostState) { Checked = HideIfFocusLost };
            var glass = new MenuItem(Loc.T("tray_glass"), ChangeDisableGlassState) { Checked = DisableGlass };
            var lockItem = new MenuItem(Loc.T("tray_lock"), ChangeLockedState) { Checked = Locked };
            var ontop = new MenuItem(Loc.T("tray_ontop"), ChangeAlwaysOnTopState) { Checked = AlwaysOnTop };
            var through = new MenuItem(Loc.T("tray_overlay"), ChangeOverlayModeState) { Checked = ClickThrough };
            var opt = new MenuItem(Loc.T("tray_options"), OpenOptionWindow) { Enabled = true };
            var exit = new MenuItem(Loc.T("tray_exit"), CloseWindow);
            menu.MenuItems.Add(hot);
            menu.MenuItems.Add(sec);
            menu.MenuItems.Add(hide);
            menu.MenuItems.Add(glass);
            menu.MenuItems.Add(lockItem);
            menu.MenuItems.Add(ontop);
            menu.MenuItems.Add(through);
            menu.MenuItems.Add(opt);
            menu.MenuItems.Add(exit);
            notifyIcon.ContextMenu = menu;
        }

        private void ChangeDisableGlassState(object sender, EventArgs e)
        {
            SetDisableGlass(!DisableGlass);
            (sender as MenuItem).Checked = DisableGlass;
        }

        private void ChangeSecondsState(object sender, EventArgs e)
        {
            SetSeconds(!SecondsEnabled);
            (sender as MenuItem).Checked = SecondsEnabled;
        }


        private void ChangeHideIfFocusLostState(object sender, EventArgs e)
        {
            SetHideIfFocusLost(!HideIfFocusLost);
            (sender as MenuItem).Checked = HideIfFocusLost;
        }

        private void OpenOptionWindow(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (optionsWindow == null)
                {
                    optionsWindow = new OptionsWindow();
                    optionsWindow.Closed += (s, a) => optionsWindow = null;
                    optionsWindow.Show();
                }
                else
                {
                    optionsWindow.Activate();
                }
            });
        }

        private void ChangeHotCornerActiveState(object sender, EventArgs e)
        {
            SetHotCorner(!HotCornerEnabled);
            (sender as MenuItem).Checked = HotCornerEnabled;
        }

        private void ChangeLockedState(object sender, EventArgs e)
        {
            SetLocked(!Locked);
            (sender as MenuItem).Checked = Locked;
        }

        private void ChangeAlwaysOnTopState(object sender, EventArgs e)
        {
            SetAlwaysOnTop(!AlwaysOnTop);
            (sender as MenuItem).Checked = AlwaysOnTop;
        }

        private void ChangeOverlayModeState(object sender, EventArgs e)
        {
            SetOverlayMode(!ClickThrough);
            (sender as MenuItem).Checked = ClickThrough;
        }

        private void CloseWindow(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Closing app after Right Click
        /// </summary>
        /// <param name="sender">NotifyIcon Click Event</param>
        /// <param name="e">MouseEventArg (Left Right Mouse button)</param>
        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            var mouseEventArgs = e as MouseEventArgs;
            if (mouseEventArgs != null && mouseEventArgs.Button == MouseButtons.Right)
                Close();
        }

        /// <summary>
        ///     Start Animation FadeIN
        /// </summary>
        private void InitializeAnimationIn()
        {
            var win = Application.Current.MainWindow;
            win.Opacity = 0;                       // 先全透明
            win.Visibility = Visibility.Visible;
            WindowIsVisible = true;
            win.Activate();
            if (AlwaysOnTop) BringToTop();

            // 等窗口完成首次布局、拿到真实 ActualWidth/Height 后再定位并淡入，
            // 避免在尺寸还是 0（用兜底 420x180）时定位，导致每次启动错位。
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetPositionOnCurrentDisplay();
                var fade = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 5) };
                fade.Tick += OpacityFadeIn;
                fade.Start();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        ///     Animation Fade In Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeIn(object sender, EventArgs e)
        {
            double target = MainWindow.ClockOpacity;
            if (Application.Current.MainWindow.Opacity < target - 0.001)
                Application.Current.MainWindow.Opacity = Math.Min(target, Application.Current.MainWindow.Opacity + 0.05);
            else
            {
                Application.Current.MainWindow.Opacity = target;
                ((DispatcherTimer)sender).Stop();
            }
        }

        /// <summary>
        ///     Call HideWindow if Window Deactivated
        /// </summary>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if(HideIfFocusLost)
                HideWindow();
        }
        /// <summary>
        /// Start Fade out Animation and stop time Dispatchers
        /// </summary>
        private void HideWindow()
        {
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += OpacityFadeOut;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 15);

            dispatcherTimer.Start();
            WindowIsVisible = false;
            refreshDispatcher.Stop();

        }

        /// <summary>
        ///     Animation Fade Out Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpacityFadeOut(object sender, EventArgs e)
        {
            if (Application.Current.MainWindow.Opacity > 0)
                Application.Current.MainWindow.Opacity -= 0.1;
            else
            {
                ((DispatcherTimer)sender).Stop();
                Application.Current.MainWindow.Visibility = Visibility.Collapsed;
            }
        }

    }
}