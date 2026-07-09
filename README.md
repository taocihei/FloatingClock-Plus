# Floating Clock+ · 悬浮时钟增强版

一个 Windows 桌面悬浮时钟：Alt+C 呼出 / 隐藏，可做成鼠标穿透的覆盖层（类似 NVIDIA 性能浮窗 / AIDA64 检测窗），也可作为普通可拖动的小挂件。WPF / C# / .NET Framework 4.7.2，绿色单文件（~115 KB）。

> 本项目基于 [BaalTech/Floating-Clock](https://github.com/BaalTech/Floating-Clock) 二次开发。原作核心（`MainWindow.xaml`、`HotKey.cs`、`MouseHook.cs`、`App.xaml`）版权归原作者 BaalTech，采用 **CC BY-NC-ND 4.0** 许可（见 `LICENSE`）。本仓库新增的设置系统、皮肤系统等为在此基础上的增强。

## 功能

**窗口**
- 浮窗模式：覆盖所有窗口最上层 + 鼠标穿透 + 钉住不动（显示层）
- 强制置顶：Win32 定时重申 `HWND_TOPMOST`，压过全屏 / 其它置顶窗口
- 锁定 / 解锁位置（优先级：解锁即恢复可拖动，自动关闭穿透）
- 鼠标拖动任意移动，记忆位置
- 热角唤出：鼠标撞屏幕右上 / 右下角自动弹出

**外观**
- 皮肤系统：8 套内置主题（终端绿 / 霓虹蓝 / 暖橙 / 樱粉 / 复古琥珀 …）+ 外部 `skins/*.json` 自定义皮肤
- 不透明度、宽度 / 高度缩放、背景底色明暗可调
- 玻璃（Aero）背景 / 纯色背景切换

**区域**
- 自定义时区（时区名跟随界面语言，中文内置常见时区译名）
- 显示位置：左下 / 左上 / 右上 / 右下 / 居中 / 自定义（拖动记忆）
- 多显示器：指定显示在某块屏幕，或跟随鼠标所在屏
- 界面语言：中文 / English / 跟随系统
- 日期格式：`yyyy-MM-dd`

**系统**
- 开机自启（写 `HKCU\...\Run`）
- 全部设置持久化到注册表 `HKCU\SOFTWARE\BaalTech\FloatingClock`
- 托盘图标：双击打开设置，右键菜单快捷开关

## 自定义皮肤

把 `*.json` 放到 exe 同级的 `skins\` 目录，重开设置窗口即可在「主题」下拉里看到：

```json
{
  "name": "我的紫色皮肤",
  "font": "Consolas",
  "bold": true,
  "timeColor": "#C08CFF",
  "dateColor": "#7B5CB0",
  "back": "#140A1E"
}
```

- `font` 字体名 · `bold` 是否加粗 · `timeColor` 时间色 · `dateColor` 日期色
- `back`：`""` 不改背景 / `"glass"` 玻璃 / `"#RRGGBB"` 纯色

## 时区说明

因为 Claude 等 AI 运营商政策导致本地时区只能使用美国时间，而使用此软件获取正常北京时区时间。

在「设置 → 时区」里选「(UTC+08:00) 北京、重庆、香港、乌鲁木齐」即可。

## 构建

需要 Visual Studio 2019+ 或 MSBuild + .NET Framework 4.7.2 开发包：

```powershell
msbuild FloatingClock.sln /t:Build /p:Configuration=Release
```

若无 4.7.2 Targeting Pack，可用 NuGet 参考程序集包编译（`Microsoft.NETFramework.ReferenceAssemblies.net472` + `FrameworkPathOverride`）。

产物：`FloatingClock\bin\Release\FloatingClock.exe`

## 使用

- **Alt+C**：呼出 / 隐藏时钟
- **双击托盘图标**：打开设置
- **右键托盘**：快捷开关（浮窗模式 / 置顶 / 锁定 / 显示秒 …）/ 退出

## 许可

原作核心代码：CC BY-NC-ND 4.0，作者 BaalTech，源自 <https://github.com/BaalTech/Floating-Clock> 。详见 `LICENSE`。
