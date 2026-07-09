using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace FloatingClock
{
    /// <summary>
    ///     简易本地化（中 / 英）。key -> [中文, English]
    /// </summary>
    internal static class Loc
    {
        public static string Lang
        {
            get
            {
                if (MainWindow.LanguageCode == "zh") return "zh";
                if (MainWindow.LanguageCode == "en") return "en";
                return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
            }
        }

        public static CultureInfo Culture
        {
            get { return Lang == "zh" ? new CultureInfo("zh-CN") : new CultureInfo("en-US"); }
        }

        private static readonly Dictionary<string, string[]> M = new Dictionary<string, string[]>
        {
            { "title",        new[] { "浮窗时钟 · 设置", "Floating Clock · Settings" } },
            // 分区标题
            { "sec_window",   new[] { "窗口", "WINDOW" } },
            { "sec_general",  new[] { "常规", "GENERAL" } },
            { "sec_appear",   new[] { "外观", "APPEARANCE" } },
            { "sec_region",   new[] { "区域与语言", "REGION & LANGUAGE" } },
            // 开关（完整描述）
            { "overlay",      new[] { "浮窗模式（覆盖显示 · 鼠标穿透 · 不可移动）", "Overlay mode (over all · click-through · fixed)" } },
            { "ontop",        new[] { "强制显示在所有窗口最前", "Always on top of all windows" } },
            { "lock",         new[] { "锁定位置（禁止拖动）", "Lock position (disable dragging)" } },
            { "hotcorner",    new[] { "启用热角唤出（撞屏幕右上 / 右下角弹出）", "Hot corner (mouse hits top / bottom-right)" } },
            { "seconds",      new[] { "显示秒（每秒刷新）", "Show seconds (refresh every second)" } },
            { "hidefocus",    new[] { "失去焦点时自动隐藏", "Auto-hide when focus is lost" } },
            { "autostart",    new[] { "开机自动启动", "Start with Windows" } },
            { "disableglass", new[] { "禁用玻璃（Aero）背景，改用纯色", "Disable glass (Aero) background" } },
            { "opacity",      new[] { "不透明度", "Opacity" } },
            { "scale_x",      new[] { "宽度", "Width" } },
            { "scale_y",      new[] { "高度", "Height" } },
            { "linkscale",    new[] { "等比联动（宽高一起缩放）🔗", "Link width & height 🔗" } },
            { "backshade",    new[] { "底色", "Shade" } },
            { "skin_label",   new[] { "主题", "Theme" } },
            // 开关提示（tooltip）
            { "overlay_tip",  new[] { "覆盖在所有窗口最上层 · 鼠标穿透 · 钉住不可移动（像 NVIDIA / AIDA64 检测浮窗）。关闭后为普通窗口，可自由拖动。", "Above all windows · click-through · fixed in place (like the NVIDIA / AIDA64 overlay). Off = a normal, draggable window." } },
            { "hotcorner_tip",new[] { "鼠标撞屏幕右上 / 右下角自动弹出时钟。", "Pop up the clock when the mouse hits the top / bottom-right corner." } },
            // 下拉标签
            { "tz_label",     new[] { "时区", "Time zone" } },
            { "tz_local",     new[] { "跟随系统本地时区", "System local time zone" } },
            { "pos_label",    new[] { "位置", "Position" } },
            { "screen_label", new[] { "屏幕", "Screen" } },
            { "screen_auto",  new[] { "跟随鼠标所在屏幕", "Follow mouse screen" } },
            { "pos_bl",       new[] { "左下角", "Bottom-left" } },
            { "pos_tl",       new[] { "左上角", "Top-left" } },
            { "pos_tr",       new[] { "右上角", "Top-right" } },
            { "pos_br",       new[] { "右下角", "Bottom-right" } },
            { "pos_center",   new[] { "屏幕居中", "Center" } },
            { "pos_custom",   new[] { "自定义（拖动记忆）", "Custom (remembered)" } },
            { "lang_label",   new[] { "语言", "Language" } },
            { "lang_sys",     new[] { "跟随系统", "Follow system" } },
            // 底部
            { "hint",         new[] { "Alt+C 唤出 / 隐藏时钟 · 双击托盘图标打开设置 · 改动即时保存", "Alt+C to toggle · double-click the tray icon for settings · changes save instantly" } },
            { "close",        new[] { "关闭", "Close" } },
            // 托盘菜单
            { "tray_hotcorner", new[] { "热角唤出", "Hot Corner" } },
            { "tray_seconds",   new[] { "显示秒", "Show Seconds" } },
            { "tray_hide",      new[] { "失焦自动隐藏", "Hide When Focus Lost" } },
            { "tray_glass",     new[] { "纯黑背景", "Solid Background" } },
            { "tray_lock",      new[] { "锁定位置", "Lock Position" } },
            { "tray_ontop",     new[] { "强制置顶", "Always On Top" } },
            { "tray_overlay",   new[] { "浮窗模式", "Overlay Mode" } },
            { "tray_options",   new[] { "设置...", "Settings..." } },
            { "tray_exit",      new[] { "退出", "Exit" } },
            { "balloon_hello",  new[] { "你好 {0}", "Hello {0}" } },
            { "balloon_body",   new[] { "按 Alt+C 显示时钟 · 双击托盘图标打开设置", "Press Alt+C to show the clock · double-click the tray icon for settings" } },
        };

        public static string T(string key)
        {
            string[] v;
            if (M.TryGetValue(key, out v)) return Lang == "zh" ? v[0] : v[1];
            return key;
        }
    }

    /// <summary>
    ///     设置窗口：无边框圆角、卡片分区、拨动开关、深色下拉。
    /// </summary>
    public class OptionsWindow : Window
    {
        private sealed class ComboOption
        {
            public string Text;
            public object Value;
            public override string ToString() { return Text; }
        }

        private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8));
        private static readonly Brush SubBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A));
        private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x9A, 0xFF));

        private static ResourceDictionary _styles;
        private static ResourceDictionary Styles
        {
            get { return _styles ?? (_styles = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(StyleXaml)); }
        }

        public OptionsWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = true;
            Title = Loc.T("title");
            FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI");

            var root = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x3A)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(14),
                Effect = new DropShadowEffect { BlurRadius = 26, ShadowDepth = 0, Opacity = 0.55, Color = Colors.Black }
            };

            var outer = new StackPanel { Width = 372 };
            root.Child = outer;

            outer.Children.Add(BuildTitleBar());

            var body = new StackPanel { Margin = new Thickness(16, 2, 16, 14) };
            outer.Children.Add(body);

            // 窗口
            body.Children.Add(Section(Loc.T("sec_window"),
                ToggleRow("overlay", "overlay_tip", MainWindow.ClickThrough, v => MainWindow.Current.SetOverlayMode(v)),
                ToggleRow("ontop", null, MainWindow.AlwaysOnTop, v => MainWindow.Current.SetAlwaysOnTop(v)),
                ToggleRow("lock", null, MainWindow.Locked, v => MainWindow.Current.SetLocked(v)),
                ToggleRow("hotcorner", "hotcorner_tip", MainWindow.HotCornerEnabled, v => MainWindow.Current.SetHotCorner(v))));

            // 常规
            body.Children.Add(Section(Loc.T("sec_general"),
                ToggleRow("seconds", null, MainWindow.SecondsEnabled, v => MainWindow.Current.SetSeconds(v)),
                ToggleRow("hidefocus", null, MainWindow.HideIfFocusLost, v => MainWindow.Current.SetHideIfFocusLost(v)),
                ToggleRow("autostart", null, MainWindow.IsAutoStartEnabled(), v => MainWindow.Current.SetAutoStart(v))));

            // 外观
            body.Children.Add(Section(Loc.T("sec_appear"),
                SkinRow(),
                ToggleRow("disableglass", null, MainWindow.DisableGlass, v => MainWindow.Current.SetDisableGlass(v)),
                SliderRow("opacity", 0.2, 1.0, MainWindow.ClockOpacity, 0.05, v => MainWindow.Current.SetOpacity(v)),
                ScaleRows(),
                SliderRow("backshade", 0.0, 1.0, MainWindow.ClockBackShade / 255.0, 0.02, v => MainWindow.Current.SetBackShade((int)Math.Round(v * 255)))));

            // 区域与语言
            body.Children.Add(Section(Loc.T("sec_region"),
                TimeZoneRow(),
                PositionRow(),
                ScreenRow(),
                LanguageRow()));

            body.Children.Add(new TextBlock
            {
                Text = Loc.T("hint"),
                Foreground = SubBrush,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 4, 2, 12),
                LineHeight = 17
            });

            var close = new Button
            {
                Content = Loc.T("close"),
                Style = (Style)Styles["FlatBtn"],
                Width = 88,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            close.Click += (s, e) => Close();
            body.Children.Add(close);

            Content = root;

            // 整窗可拖动：点击非交互控件的区域（标题栏、卡片、标签空白处）即可拖动窗口。
            // 开关 / 滑块 / 下拉 / 按钮会各自消费鼠标事件，不会触发拖动。
            MouseLeftButtonDown += (s, e) => { try { DragMove(); } catch { } };
        }

        private FrameworkElement BuildTitleBar()
        {
            var bar = new Grid { Height = 40, Margin = new Thickness(16, 6, 10, 0), Background = Brushes.Transparent };
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dot = new Border
            {
                Width = 9,
                Height = 9,
                CornerRadius = new CornerRadius(5),
                Background = AccentBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 9, 0)
            };
            var titleText = new TextBlock
            {
                Text = Loc.T("title"),
                Foreground = TextBrush,
                FontSize = 14.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            left.Children.Add(dot);
            left.Children.Add(titleText);
            Grid.SetColumn(left, 0);

            var closeX = new Button { Style = (Style)Styles["CloseBtn"], VerticalAlignment = VerticalAlignment.Center };
            closeX.Click += (s, e) => Close();
            Grid.SetColumn(closeX, 1);

            bar.Children.Add(left);
            bar.Children.Add(closeX);
            return bar;
        }

        private FrameworkElement Section(string title, params UIElement[] rows)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x27)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x32, 0x32, 0x35)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(13, 7, 13, 8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = AccentBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(1, 0, 0, 6)
            });
            foreach (var r in rows) sp.Children.Add(r);
            card.Child = sp;
            return card;
        }

        private FrameworkElement ToggleRow(string labelKey, string tipKey, bool init, Action<bool> onChange)
        {
            var grid = new Grid { MinHeight = 30, Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = new TextBlock
            {
                Text = Loc.T(labelKey),
                Foreground = TextBrush,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            if (tipKey != null) lbl.ToolTip = Loc.T(tipKey);
            Grid.SetColumn(lbl, 0);

            var cb = new CheckBox
            {
                Style = (Style)Styles["Toggle"],
                IsChecked = init,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (tipKey != null) cb.ToolTip = Loc.T(tipKey);
            cb.Checked += (s, e) => onChange(true);
            cb.Unchecked += (s, e) => onChange(false);
            Grid.SetColumn(cb, 1);

            grid.Children.Add(lbl);
            grid.Children.Add(cb);
            return grid;
        }

        private FrameworkElement SliderRow(string labelKey, double min, double max, double val, double tick, Action<double> onChange, Action<Slider> onCreated = null)
        {
            var grid = new Grid { Height = 34 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });

            var lbl = new TextBlock
            {
                Text = Loc.T(labelKey),
                Foreground = TextBrush,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 0);

            var slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Max(min, Math.Min(max, val)),
                TickFrequency = tick,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 10, 0)
            };
            Grid.SetColumn(slider, 1);

            var pct = new TextBlock
            {
                Foreground = SubBrush,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = ((int)Math.Round(slider.Value * 100)) + "%"
            };
            Grid.SetColumn(pct, 2);

            slider.ValueChanged += (s, e) =>
            {
                pct.Text = ((int)Math.Round(slider.Value * 100)) + "%";
                onChange(slider.Value);
            };

            grid.Children.Add(lbl);
            grid.Children.Add(slider);
            grid.Children.Add(pct);
            if (onCreated != null) onCreated(slider);
            return grid;
        }

        /// <summary>宽 / 高缩放两行 + 等比联动开关（🔗）</summary>
        private FrameworkElement ScaleRows()
        {
            var panel = new StackPanel();
            Slider sx = null, sy = null;
            var syncing = new bool[] { false };

            panel.Children.Add(ToggleRow("linkscale", null, MainWindow.LinkScale, on =>
            {
                MainWindow.Current.SetLinkScale(on);
                if (on && sx != null && sy != null && !syncing[0])
                {
                    syncing[0] = true; sy.Value = sx.Value; syncing[0] = false;
                    MainWindow.Current.SetScaleY(sx.Value);
                }
            }));

            panel.Children.Add(SliderRow("scale_x", 0.5, 3.0, MainWindow.ClockScaleX, 0.05,
                v =>
                {
                    MainWindow.Current.SetScaleX(v);
                    if (MainWindow.LinkScale && sy != null && !syncing[0]) { syncing[0] = true; sy.Value = v; syncing[0] = false; }
                }, s => sx = s));

            panel.Children.Add(SliderRow("scale_y", 0.5, 3.0, MainWindow.ClockScaleY, 0.05,
                v =>
                {
                    MainWindow.Current.SetScaleY(v);
                    if (MainWindow.LinkScale && sx != null && !syncing[0]) { syncing[0] = true; sx.Value = v; syncing[0] = false; }
                }, s => sy = s));

            return panel;
        }

        private FrameworkElement TimeZoneRow()
        {
            var opts = new List<ComboOption> { new ComboOption { Text = Loc.T("tz_local"), Value = "" } };
            foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                opts.Add(new ComboOption { Text = TzText(tz), Value = tz.Id });
            return ComboRow("tz_label", opts.ToArray(), MainWindow.TimeZoneId ?? "",
                v => MainWindow.Current.SetTimeZone((string)v));
        }

        /// <summary>
        ///     时区显示名跟随语言：英文用系统英文名；中文用「(UTC偏移) 中文译名」，冷门时区回退英文名。
        /// </summary>
        private static string TzText(TimeZoneInfo tz)
        {
            if (Loc.Lang != "zh") return tz.DisplayName;
            var off = tz.BaseUtcOffset;
            string sign = off < TimeSpan.Zero ? "-" : "+";
            string prefix = string.Format("(UTC{0}{1:00}:{2:00}) ", sign, Math.Abs(off.Hours), Math.Abs(off.Minutes));
            string cn;
            return ZhTz.TryGetValue(tz.Id, out cn) ? prefix + cn : tz.DisplayName;
        }

        private static readonly Dictionary<string, string> ZhTz = new Dictionary<string, string>
        {
            { "China Standard Time", "北京、重庆、香港、乌鲁木齐" },
            { "Taipei Standard Time", "台北" },
            { "Tokyo Standard Time", "东京、大阪、札幌" },
            { "Korea Standard Time", "首尔" },
            { "Singapore Standard Time", "吉隆坡、新加坡" },
            { "W. Australia Standard Time", "珀斯" },
            { "SE Asia Standard Time", "曼谷、河内、雅加达" },
            { "China Standard Time (Xinjiang)", "乌鲁木齐" },
            { "India Standard Time", "新德里、加尔各答、孟买" },
            { "Sri Lanka Standard Time", "斯里贾亚瓦德纳普拉" },
            { "Nepal Standard Time", "加德满都" },
            { "Bangladesh Standard Time", "达卡" },
            { "Myanmar Standard Time", "仰光" },
            { "West Asia Standard Time", "塔什干、阿什哈巴德" },
            { "Pakistan Standard Time", "伊斯兰堡、卡拉奇" },
            { "Afghanistan Standard Time", "喀布尔" },
            { "Iran Standard Time", "德黑兰" },
            { "Arabian Standard Time", "阿布扎比、迪拜" },
            { "Azerbaijan Standard Time", "巴库" },
            { "Georgian Standard Time", "第比利斯" },
            { "Caucasus Standard Time", "埃里温" },
            { "Russian Standard Time", "莫斯科、圣彼得堡" },
            { "Arab Standard Time", "科威特、利雅得" },
            { "E. Africa Standard Time", "内罗毕" },
            { "Turkey Standard Time", "伊斯坦布尔" },
            { "Israel Standard Time", "耶路撒冷" },
            { "Egypt Standard Time", "开罗" },
            { "South Africa Standard Time", "哈拉雷、比勒陀利亚" },
            { "GTB Standard Time", "雅典、布加勒斯特" },
            { "E. Europe Standard Time", "基希讷乌" },
            { "Central European Standard Time", "萨拉热窝、华沙" },
            { "Central Europe Standard Time", "布拉格、布达佩斯" },
            { "W. Europe Standard Time", "柏林、罗马、阿姆斯特丹" },
            { "Romance Standard Time", "巴黎、马德里" },
            { "GMT Standard Time", "伦敦、都柏林、里斯本" },
            { "Greenwich Standard Time", "蒙罗维亚、雷克雅未克" },
            { "UTC", "协调世界时 UTC" },
            { "Morocco Standard Time", "卡萨布兰卡" },
            { "Cape Verde Standard Time", "佛得角" },
            { "E. South America Standard Time", "巴西利亚" },
            { "Argentina Standard Time", "布宜诺斯艾利斯" },
            { "SA Eastern Standard Time", "卡宴" },
            { "Newfoundland Standard Time", "纽芬兰" },
            { "Atlantic Standard Time", "大西洋时间（加拿大）" },
            { "Eastern Standard Time", "东部时间（纽约）" },
            { "US Eastern Standard Time", "印第安纳（东部）" },
            { "Central Standard Time", "中部时间（芝加哥）" },
            { "Central Standard Time (Mexico)", "墨西哥城" },
            { "Mountain Standard Time", "山地时间（丹佛）" },
            { "US Mountain Standard Time", "亚利桑那" },
            { "Pacific Standard Time", "太平洋时间（洛杉矶）" },
            { "Alaskan Standard Time", "阿拉斯加" },
            { "Hawaiian Standard Time", "夏威夷" },
            { "SA Pacific Standard Time", "波哥大、利马、基多" },
            { "Central America Standard Time", "中美洲" },
            { "Venezuela Standard Time", "加拉加斯" },
            { "AUS Eastern Standard Time", "堪培拉、墨尔本、悉尼" },
            { "E. Australia Standard Time", "布里斯班" },
            { "AUS Central Standard Time", "达尔文" },
            { "Cen. Australia Standard Time", "阿德莱德" },
            { "Tasmania Standard Time", "霍巴特" },
            { "New Zealand Standard Time", "奥克兰、惠灵顿" },
            { "Fiji Standard Time", "斐济" },
            { "Tonga Standard Time", "努库阿洛法" },
            { "West Pacific Standard Time", "关岛、莫尔兹比港" },
            { "Ekaterinburg Standard Time", "叶卡捷琳堡" },
            { "N. Central Asia Standard Time", "新西伯利亚" },
            { "North Asia Standard Time", "克拉斯诺亚尔斯克" },
            { "North Asia East Standard Time", "伊尔库茨克" },
            { "Vladivostok Standard Time", "符拉迪沃斯托克" },
            { "Ulaanbaatar Standard Time", "乌兰巴托" },
        };

        private FrameworkElement PositionRow()
        {
            var opts = new[]
            {
                new ComboOption { Text = Loc.T("pos_bl"), Value = 0 },
                new ComboOption { Text = Loc.T("pos_tl"), Value = 1 },
                new ComboOption { Text = Loc.T("pos_tr"), Value = 2 },
                new ComboOption { Text = Loc.T("pos_br"), Value = 3 },
                new ComboOption { Text = Loc.T("pos_center"), Value = 4 },
                new ComboOption { Text = Loc.T("pos_custom"), Value = 5 },
            };
            return ComboRow("pos_label", opts, MainWindow.ClockPosition,
                v => MainWindow.Current.SetPosition((int)v));
        }

        private FrameworkElement ScreenRow()
        {
            var opts = new List<ComboOption> { new ComboOption { Text = Loc.T("screen_auto"), Value = -1 } };
            var screens = System.Windows.Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                string label = (Loc.Lang == "zh" ? "屏幕 " : "Display ") + (i + 1)
                    + "  " + s.Bounds.Width + "×" + s.Bounds.Height
                    + (s.Primary ? (Loc.Lang == "zh" ? " · 主" : " · Primary") : "");
                opts.Add(new ComboOption { Text = label, Value = i });
            }
            return ComboRow("screen_label", opts.ToArray(), MainWindow.TargetScreen,
                v => MainWindow.Current.SetTargetScreen((int)v));
        }

        private FrameworkElement SkinRow()
        {
            var skins = SkinLibrary.All();
            var opts = new ComboOption[skins.Count];
            for (int i = 0; i < skins.Count; i++)
                opts[i] = new ComboOption { Text = skins[i].Name, Value = skins[i].Name };
            return ComboRow("skin_label", opts, MainWindow.ClockSkin,
                v => MainWindow.Current.SetSkin((string)v));
        }

        private FrameworkElement LanguageRow()
        {
            var opts = new[]
            {
                new ComboOption { Text = Loc.T("lang_sys"), Value = "" },
                new ComboOption { Text = "中文", Value = "zh" },
                new ComboOption { Text = "English", Value = "en" },
            };
            return ComboRow("lang_label", opts, MainWindow.LanguageCode ?? "",
                v => MainWindow.Current.SetLanguage((string)v));
        }

        private FrameworkElement ComboRow(string labelKey, ComboOption[] options, object current, Action<object> onChange)
        {
            var grid = new Grid { Height = 35, Margin = new Thickness(0, 1, 0, 1) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text = Loc.T(labelKey),
                Foreground = SubBrush,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 0);

            var combo = new ComboBox
            {
                Style = (Style)Styles["Combo"],
                ItemContainerStyle = (Style)Styles["ComboItem"],
                VerticalAlignment = VerticalAlignment.Center
            };
            foreach (var o in options) combo.Items.Add(o);
            int sel = 0;
            for (int i = 0; i < options.Length; i++)
            {
                if (Equals(options[i].Value, current)) { sel = i; break; }
            }
            combo.SelectedIndex = sel;
            combo.SelectionChanged += (s, e) =>
            {
                var opt = combo.SelectedItem as ComboOption;
                if (opt != null) onChange(opt.Value);
            };
            Grid.SetColumn(combo, 1);

            grid.Children.Add(lbl);
            grid.Children.Add(combo);
            return grid;
        }

        // ---- 控件样式（XAML 字符串，运行时解析）----
        private const string StyleXaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

  <Style x:Key='Toggle' TargetType='CheckBox'>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='CheckBox'>
          <Grid Background='Transparent'>
            <Border x:Name='track' Width='38' Height='21' CornerRadius='11' Background='#4A4A4E'/>
            <Border x:Name='thumb' Width='15' Height='15' CornerRadius='8' Background='#EDEDED'
                    HorizontalAlignment='Left' Margin='3,0,0,0'/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property='IsChecked' Value='True'>
              <Setter TargetName='track' Property='Background' Value='#4C9AFF'/>
              <Setter TargetName='thumb' Property='HorizontalAlignment' Value='Right'/>
              <Setter TargetName='thumb' Property='Margin' Value='0,0,3,0'/>
              <Setter TargetName='thumb' Property='Background' Value='White'/>
            </Trigger>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='track' Property='Opacity' Value='0.85'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='CloseBtn' TargetType='Button'>
    <Setter Property='Foreground' Value='#B0B0B0'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Border x:Name='b' Width='30' Height='28' CornerRadius='7' Background='Transparent'>
            <TextBlock Text='&#x2715;' FontSize='12' Foreground='{TemplateBinding Foreground}'
                       HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='b' Property='Background' Value='#C0392B'/>
              <Setter Property='Foreground' Value='White'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='FlatBtn' TargetType='Button'>
    <Setter Property='Foreground' Value='#E8E8E8'/>
    <Setter Property='FontSize' Value='13'/>
    <Setter Property='Height' Value='31'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='Button'>
          <Border x:Name='b' CornerRadius='7' Background='#4C9AFF'>
            <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='b' Property='Background' Value='#5AA6FF'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='ComboItem' TargetType='ComboBoxItem'>
    <Setter Property='Foreground' Value='#E8E8E8'/>
    <Setter Property='Padding' Value='11,7'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ComboBoxItem'>
          <Border x:Name='b' Background='Transparent' Padding='{TemplateBinding Padding}'>
            <ContentPresenter/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsHighlighted' Value='True'>
              <Setter TargetName='b' Property='Background' Value='#3A3A3D'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style x:Key='Combo' TargetType='ComboBox'>
    <Setter Property='Foreground' Value='#E8E8E8'/>
    <Setter Property='Background' Value='#2D2D30'/>
    <Setter Property='Height' Value='30'/>
    <Setter Property='FontSize' Value='12.5'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ComboBox'>
          <Grid>
            <ToggleButton x:Name='tb' Focusable='False' ClickMode='Press' Background='#2D2D30'
                          IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>
              <ToggleButton.Template>
                <ControlTemplate TargetType='ToggleButton'>
                  <Border x:Name='bd' CornerRadius='7' Background='{TemplateBinding Background}'
                          BorderBrush='#3C3C40' BorderThickness='1'>
                    <Grid>
                      <Grid.ColumnDefinitions>
                        <ColumnDefinition Width='*'/>
                        <ColumnDefinition Width='24'/>
                      </Grid.ColumnDefinitions>
                      <Path Grid.Column='1' HorizontalAlignment='Center' VerticalAlignment='Center'
                            Data='M0,0 L4,4 L8,0 Z' Fill='#A8A8AC'/>
                    </Grid>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property='IsMouseOver' Value='True'>
                      <Setter TargetName='bd' Property='BorderBrush' Value='#4C9AFF'/>
                    </Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate>
              </ToggleButton.Template>
            </ToggleButton>
            <ContentPresenter IsHitTestVisible='False'
                              Content='{TemplateBinding SelectionBoxItem}'
                              ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'
                              Margin='11,0,26,0' VerticalAlignment='Center'
                              TextElement.Foreground='#E8E8E8'/>
            <Popup x:Name='pop' Placement='Bottom' Focusable='False' AllowsTransparency='True'
                   PopupAnimation='Fade'
                   IsOpen='{Binding IsDropDownOpen, RelativeSource={RelativeSource TemplatedParent}}'>
              <Border MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}'
                      MaxHeight='300' Background='#282829' BorderBrush='#3C3C40' BorderThickness='1'
                      CornerRadius='7' Margin='0,4,0,6'>
                <Border.Effect>
                  <DropShadowEffect BlurRadius='14' ShadowDepth='0' Opacity='0.5' Color='Black'/>
                </Border.Effect>
                <ScrollViewer Margin='2'>
                  <StackPanel IsItemsHost='True'/>
                </ScrollViewer>
              </Border>
            </Popup>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

</ResourceDictionary>";
    }
}
