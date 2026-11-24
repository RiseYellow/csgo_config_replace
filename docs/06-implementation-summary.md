# 实现总结

## 完成状态

MVP 已完成，核心功能全部实现并通过单元测试。根据用户反馈，已将技术栈从 .NET 8 切换为 .NET Framework 4.8，解决了启动稳定性和体积问题。

## 已实现功能

### 1. Steam 路径自动定位

- 读取注册表 `HKCU\Software\Valve\Steam\SteamPath`。
- 读取注册表 `HKLM\SOFTWARE\WOW6432Node\Valve\Steam\InstallPath`。
- 读取注册表 `HKLM\SOFTWARE\Valve\Steam\InstallPath`。
- 回退到默认路径 `C:\Program Files (x86)\Steam`。
- 支持手动浏览选择 Steam 目录。

### 2. 账号扫描

- 遍历 `userdata` 下的纯数字目录（SteamID3）。
- 排除 `0` 和非数字目录。
- 检测 `730/local/cfg` 是否存在。
- 展示配置文件列表、文件数量、最后修改时间。
- 对未运行过 CS2 的账号显示“未运行过 CS2”。

### 3. 基准配置管理

- 选中账号后可将其设为基准配置。
- 支持勾选需要复制的配置文件（默认全选）。
- 基准配置持久化到 `%LocalAppData%/CsgoConfigReplace/settings.json`。

### 4. 一键应用

- 支持多选目标账号。
- 自动创建缺失的 `730/local/cfg` 目录。
- 应用前自动备份目标账号原有配置到 `cfg.backup_yyyyMMdd_HHmmss`。
- 跳过源账号自身。
- 单账号失败不影响其他账号。
- 应用完成后自动重新扫描并显示结果。

### 5. 日志与反馈

- 界面底部实时显示操作日志。
- 状态栏显示当前操作状态。
- 忙碌时显示进度条。

## 技术栈调整记录

### 第一版：.NET 8 + WPF

- 优点：现代化、开发体验好。
- 问题：
  - 自包含单文件 exe 约 155 MB，远超 5 MB 要求。
  - 单文件 exe 启动需解压到临时目录，存在双击无法启动的风险。

### 第二版：.NET Framework 4.8 + WPF（当前）

- 优点：
  - Windows 10/11 系统自带，无需安装运行时。
  - Costura.Fody 合并后单文件 exe 仅约 858 KB。
  - 双击直接运行，稳定性高。
- 兼容性修改：
  - 目标框架改为 `net48`，`LangVersion` 设为 `latest`。
  - 添加 `Microsoft.NETFramework.ReferenceAssemblies.net48` 引用。
  - `File.ReadAllTextAsync/WriteAllTextAsync` 改为同步版本。
  - `OpenFolderDialog` 改为 `System.Windows.Forms.FolderBrowserDialog`。
  - 解决 `Application`、`MessageBox` 命名空间歧义。

### 第三版：修复启动闪退

- 问题：`App.xaml` 中定义的 `BooleanToVisibilityConverter` 资源未能正确加载，导致 `MainWindow` 初始化时抛出 `XamlParseException`，双击后直接闪退。
- 修复：
  - 将 `BooleanToVisibilityConverter` 移到 `MainWindow.xaml` 的 `Window.Resources` 中。
  - 在 `App.xaml.cs` 中添加全局异常处理（`DispatcherUnhandledException`、`AppDomain.UnhandledException`），启动失败时弹出详细错误信息。

### 第四版：增加 Steam 昵称列

- 读取 `Steam/config/loginusers.vdf`，解析出 SteamID64 与昵称（PersonaName）的映射。
- 将 SteamID64 转换为 SteamID3（`SteamID3 = SteamID64 - 76561197960265728`）。
- 账号列表新增“昵称”列，未获取到昵称时显示 `-`。
- 使用 `ValveKeyValue 0.3.1.152` 解析 VDF 文件，支持 .NET Framework 4.8。

### 第五版：新账号自动检测与自动应用

- 新增 `AccountMonitorService`，使用 `FileSystemWatcher` 监控 `userdata` 目录。
- 发现新的 SteamID3 目录时，延迟 3 秒（等待 Steam 完成文件写入）后触发事件。
- 主界面新增开关：“自动检测新登录账号并应用基准配置”。
- 开启后，程序运行期间新登录的 Steam 账号会自动应用当前基准配置。
- 启动时已存在的账号不会被误认为是新账号。
- 自动应用结果写入操作日志。

### 第六版：修复自动监控跨线程异常

- 问题：`FileSystemWatcher` 事件在线程池线程触发，直接修改绑定到 UI 的 `ObservableCollection` 会抛出 `NotSupportedException`。
- 修复：
  - `OnNewAccountDetected` 使用 `App.Current.Dispatcher.InvokeAsync` 将逻辑切换到 UI 线程。
  - `AddLog` 增加 `Dispatcher.CheckAccess()` 判断，非 UI 线程时自动封送。

### 第七版：自动关闭 CS2 Steam 云同步

- 问题：应用本地 config 后，启动游戏时 Steam 云同步可能从云端恢复旧配置，导致迁移失效。
- 方案：修改 `Steam/userdata/<SteamID3>/7/remote/sharedconfig.vdf`，将 `apps/730/cloudenabled` 设为 `"0"`。
- 实现：
  - 新增 `CloudSyncService`，支持读取和写入 `sharedconfig.vdf`。
  - 如果文件不存在则自动创建基本结构。
  - 在 `ConfigService.ApplyToSingleAccountAsync` 中，复制 config 后自动调用禁用云同步。
  - 禁用失败不会中断配置复制，但会在结果消息中提示。
- 注意：如果 Steam 客户端正在运行，建议重启 Steam 使设置生效。

### 第八版：界面布局调整与图标更换

- 将“将选中账号设为基准”、“一键应用到勾选账号”按钮和“自动检测”开关移到 Steam 路径下方的操作栏，小窗口下也能直接看到。
- 右侧基准配置面板保留摘要和文件勾选列表。
- 使用用户提供的 PNG 图片作为窗口图标。
- 将 PNG 转换为 ICO 并设置为 exe 文件图标。
- 图标文件存放在 `Resources/` 目录，随项目一起构建。

### 第九版：支持外部 config 文件夹作为基准

- 新增“选择外部config文件夹”按钮，可直接选择任意本地文件夹作为基准来源。
- 选择后自动列出文件夹内的文件，支持勾选需要复制的文件。
- 提供“清除”按钮恢复为从 Steam 账号选择基准的模式。
- 基准摘要会区分显示“源账号”或“外部文件夹”。
- 应用逻辑复用现有 `ConfigService`，无需改动核心复制流程。

## 测试情况

- 测试项目：`tests/CsgoConfigReplace.Tests`
- 测试框架：xUnit + FluentAssertions
- 测试数量：8 个
- 测试结果：**全部通过**

测试覆盖：

- `AccountScannerTests`：账号扫描、cfg 检测、非账号目录过滤。
- `ConfigServiceTests`：自动创建目录、备份、跳过源账号。
- `BackupServiceTests`：备份目录创建、空目录处理。

## 发布产物

- 路径：`artifacts/win-anycpu/CsgoConfigReplace.App.exe`
- 类型：单文件 exe（Costura.Fody 合并所有依赖）
- 大小：**约 1004 KB**
- 运行时：Windows 系统自带 .NET Framework 4.8，无需额外安装。

## 已知限制

- 仅支持单个 Steam 安装目录（后续可扩展多库目录）。
- 不显示 Steam 昵称，仅显示 SteamID3。
- 不解析 CS:GO 与 CS2 指令差异。
- 未做游戏进程运行检测（后续可增加）。

## 后续建议

1. 在真实环境中运行一次，确认界面交互符合预期。
2. 测试目标账号从未运行过 CS2 的场景。
3. 测试目标账号已有配置时的备份和恢复。
4. 如需发布到 GitHub，可添加 CI 工作流自动构建 Release。
