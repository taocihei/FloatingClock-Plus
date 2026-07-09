using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FloatingClock
{
    /// <summary>
    ///     一套皮肤（主题）：字体 + 颜色 + 背景。纯数据，无 UI。
    /// </summary>
    public class Skin
    {
        public string Name = "";
        public string Font = "Segoe UI Light";
        public bool Bold = false;
        public string TimeColor = "#FFFFFF";   // 时:分:秒 颜色
        public string DateColor = "#B0B0B0";   // 星期 / 日期 颜色
        public string Back = "";               // "" 不改背景 | "glass" 玻璃 | "#RRGGBB" 纯色
    }

    /// <summary>
    ///     皮肤库：内置皮肤 + 扫描 exe 同级 skins\ 目录里的 *.json 外部皮肤。
    /// </summary>
    internal static class SkinLibrary
    {
        public static List<Skin> Builtin()
        {
            return new List<Skin>
            {
                new Skin { Name = "默认（玻璃白）", Font = "Segoe UI Light",     TimeColor = "#FFFFFF", DateColor = "#DDDDDD", Back = "" },
                new Skin { Name = "极简白",         Font = "Segoe UI Light",     TimeColor = "#FFFFFF", DateColor = "#9AA0A6", Back = "#111214" },
                new Skin { Name = "终端绿",         Font = "Consolas",           TimeColor = "#33FF6A", DateColor = "#1E9E45", Back = "#050805", Bold = true },
                new Skin { Name = "霓虹蓝",         Font = "Segoe UI",           TimeColor = "#4CC3FF", DateColor = "#2C7FB0", Back = "#0B1420" },
                new Skin { Name = "暖橙",           Font = "Segoe UI Semibold",  TimeColor = "#FFB347", DateColor = "#C0863A", Back = "#1A1206" },
                new Skin { Name = "樱粉",           Font = "Segoe UI",           TimeColor = "#FF9BC2", DateColor = "#C86E92", Back = "#1A0F14" },
                new Skin { Name = "纯黑白大字",     Font = "Segoe UI Black",     TimeColor = "#FFFFFF", DateColor = "#DDDDDD", Back = "#000000", Bold = true },
                new Skin { Name = "复古琥珀",       Font = "Consolas",           TimeColor = "#FFB000", DateColor = "#B37A00", Back = "#000000", Bold = true },
            };
        }

        /// <summary>皮肤目录：exe 同级 skins\</summary>
        public static string SkinDir()
        {
            try
            {
                string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                return Path.Combine(Path.GetDirectoryName(exe), "skins");
            }
            catch { return null; }
        }

        /// <summary>加载外部皮肤（skins\*.json）</summary>
        public static List<Skin> LoadExternal()
        {
            var list = new List<Skin>();
            try
            {
                var dir = SkinDir();
                if (dir == null || !Directory.Exists(dir)) return list;
                foreach (var f in Directory.GetFiles(dir, "*.json"))
                {
                    try
                    {
                        var s = Parse(File.ReadAllText(f));
                        if (s != null && !string.IsNullOrEmpty(s.Name)) list.Add(s);
                    }
                    catch { }
                }
            }
            catch { }
            return list;
        }

        public static List<Skin> All()
        {
            var all = Builtin();
            all.AddRange(LoadExternal());
            return all;
        }

        public static Skin Find(string name)
        {
            foreach (var s in All())
                if (s.Name == name) return s;
            return null;
        }

        // ---- 极简 JSON 解析（扁平对象，够皮肤用，无需第三方库）----
        private static string Str(string json, string key, string def)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : def;
        }

        private static bool Bool(string json, string key, bool def)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.ToLowerInvariant() == "true" : def;
        }

        public static Skin Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var s = new Skin();
            s.Name = Str(json, "name", "");
            s.Font = Str(json, "font", s.Font);
            s.Bold = Bool(json, "bold", false);
            s.TimeColor = Str(json, "timeColor", s.TimeColor);
            s.DateColor = Str(json, "dateColor", s.DateColor);
            s.Back = Str(json, "back", "");
            return s;
        }
    }
}
