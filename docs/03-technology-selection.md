# 技术选型

## 最终方案：C# + WPF + .NET Framework 4.8

### 变更说明

最初选择 .NET 8 + WPF，但在实际发布时发现两个无法同时满足的问题：

1. **自包含单文件 exe 体积过大**：约 155 MB，远超用户要求的 5 MB。
2. **双击启动不稳定**：.NET 8 单文件 exe 启动时需要解压到临时目录，容易受权限或杀毒软件影响。

因此最终切换为 **.NET Framework 4.8 + WPF**。

### 选择理由

1. **Windows 系统自带**
   - Windows 10 / 11 默认已安装 .NET Framework 4.8，用户无需安装任何运行时。
   - 编译后的程序可直接双击运行，稳定性高。
2. **体积极小**
   - 配合 Costura.Fody 将依赖 DLL 合并后，单文件 exe 仅约 **858 KB**。
   - 完全满足 5 MB 以内的要求。
3. **Windows 原生支持最好**
   - 直接调用 Windows 注册表 API 读取 Steam 路径。
   - 文件系统操作稳定，长路径、权限处理成熟。
4. **开发效率高**
   - WPF 提供数据绑定、样式、模板，适合做账号列表和配置预览界面。
   - 现有 .NET 8 代码仅需少量兼容性修改即可迁移到 .NET Framework 4.8。

### 技术栈明细

| 层级 | 技术 | 说明 |
|------|------|------|
| 语言 | C#（LangVersion latest） | 使用现代语法，编译到 .NET Framework 4.8 |
| 运行时 | .NET Framework 4.8 | Windows 10/11 自带 |
| UI 框架 | WPF | Windows 原生桌面 UI |
| 架构模式 | MVVM | 使用 CommunityToolkit.Mvvm 简化 ViewModel |
| 依赖注入 | Microsoft.Extensions.DependencyInjection 8.0.1 | 支持 .NET Standard 2.0 |
| 日志 | Microsoft.Extensions.Logging 8.0.1 | 记录操作日志 |
| JSON | System.Text.Json 8.0.5 | 设置持久化 |
| VDF 解析 | ValveKeyValue 0.3.1.152 | 读取 loginusers.vdf 获取 Steam 昵称 |
| 文件操作 | System.IO | 复制、备份、目录创建 |
| 注册表 | Microsoft.Win32.Registry | 读取 Steam 安装路径 |
| 单文件打包 | Costura.Fody 6.2.0 | 将依赖 DLL 嵌入 exe |
| 单元测试 | xUnit + FluentAssertions | 测试路径扫描、文件迁移逻辑 |

### 项目结构建议

```text
CsgoConfigReplace/
├── src/
│   └── CsgoConfigReplace.App/          # WPF 主程序
│       ├── Views/                      # 窗口与页面
│       ├── ViewModels/                 # MVVM ViewModel
│       ├── Models/                     # 数据模型
│       ├── Services/                   # 业务服务
│       │   ├── ISteamLocator.cs
│       │   ├── SteamLocator.cs
│       │   ├── IAccountScanner.cs
│       │   ├── AccountScanner.cs
│       │   ├── IConfigService.cs
│       │   └── ConfigService.cs
│       │   ├── ICloudSyncService.cs
│       │   └── CloudSyncService.cs
│       ├── FodyWeavers.xml             # Costura 配置
│       └── Resources/                  # 样式、图标
├── tests/
│   └── CsgoConfigReplace.Tests/        # 单元测试
├── docs/                               # 过程文档
└── README.md
```

## 备选方案对比

| 方案 | 优点 | 缺点 | 适用场景 |
|------|------|------|----------|
| **C# + WPF + .NET Framework 4.8** | Windows 自带、体积极小、双击即运行 | 仅 Windows、技术栈较旧 | **最终选择**，最匹配需求 |
| C# + WPF + .NET 8 | 现代、跨平台潜力 | 自包含体积大、启动需解压 | 对体积不敏感的大型工具 |
| C# + WinForms + .NET Framework 4.8 | 更简单、控件直接拖放 | 界面较老旧、MVVM 支持弱 | 快速原型 |
| Python + PySide6 | 开发快、语法简洁 | 打包体积大、需处理依赖 | 个人小工具 |
| Electron + Node.js | 跨平台、前端技术栈 | 体积大、内存占用高 | 已有前端团队 |
| Rust + Tauri | 性能高、体积小 | 学习曲线陡、Windows 桌面生态不如 C# 成熟 | 追求极致性能 |

## 关键实现方案

### Steam 路径定位

按优先级尝试：

1. 读取注册表 `HKCU\Software\Valve\Steam` 的 `SteamPath`。
2. 读取注册表 `HKLM\SOFTWARE\WOW6432Node\Valve\Steam` 的 `InstallPath`。
3. 读取注册表 `HKLM\SOFTWARE\Valve\Steam` 的 `InstallPath`。
4. 检查默认路径 `C:\Program Files (x86)\Steam`。
5. 以上均失败则提示用户手动选择。

### 账号扫描

遍历 `userdata` 下的所有数字目录（SteamID3），检查是否存在 `730/local/cfg`。每个账号展示：

- SteamID3
- cfg 目录是否存在
- 配置文件列表
- 最后修改时间

### 基准配置管理

- 用户右键某个账号，选择“设为基准配置”。
- 程序将基准配置的**来源路径**记录到本地设置文件（JSON）。
- 应用时直接从该来源路径复制文件，不额外复制一份到应用目录，减少状态不一致。
- 可选：允许用户手动选择一个外部 cfg 文件夹作为基准。

### 一键应用流程

1. 用户勾选目标账号（支持多选）。
2. 检查目标是否正在运行 CS2，若运行则提示。
3. 对每个目标账号：
   - 若 `730/local/cfg` 不存在则创建。
   - 若已有文件则备份到 `cfg.backup_yyyyMMdd_HHmmss/`。
   - 复制基准配置文件到目标 `cfg`。
4. 记录操作日志并显示结果。

### 需要复制的文件

默认复制整个 `cfg` 目录内容。允许用户取消勾选特定文件，例如：

- `config.cfg`
- `autoexec.cfg`
- `video.txt`
- `videodefaults.txt`

## 发布方案

- 使用 .NET 8 SDK 构建 .NET Framework 4.8 项目（需引用 `Microsoft.NETFramework.ReferenceAssemblies.net48`）。
- 使用 Costura.Fody 在编译时将所有依赖 DLL 嵌入 exe。
- 最终产物为单个 `CsgoConfigReplace.App.exe`，约 858 KB。
- 用户只需 Windows 10/11 系统，无需安装任何额外运行时。
