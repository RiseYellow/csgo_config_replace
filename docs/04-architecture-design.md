# 架构设计

## 总体架构

采用经典三层 + MVVM：

```text
┌─────────────────────────────────────┐
│  Views (WPF XAML)                   │
│  - MainWindow                       │
│  - AccountListView                  │
│  - ConfigPreviewDialog              │
└──────────────┬──────────────────────┘
               │ DataBinding
┌──────────────▼──────────────────────┐
│  ViewModels                         │
│  - MainViewModel                    │
│  - AccountItemViewModel             │
└──────────────┬──────────────────────┘
               │ 调用服务
┌──────────────▼──────────────────────┐
│  Services                           │
│  - SteamLocator                     │
│  - AccountScanner                   │
│  - ConfigService                    │
│  - BackupService                    │
│  - SettingsService                  │
└──────────────┬──────────────────────┘
               │ 操作系统 API
┌──────────────▼──────────────────────┐
│  Infrastructure                     │
│  - Registry / FileSystem / Logger   │
└─────────────────────────────────────┘
```

## 核心模型

### SteamAccount

```csharp
public class SteamAccount
{
    public string SteamId3 { get; set; } = string.Empty;
    public string UserDataPath { get; set; } = string.Empty;
    public string CfgPath { get; set; } = string.Empty;
    public bool HasCfgDirectory { get; set; }
    public List<ConfigFileInfo> ConfigFiles { get; set; } = new();
    public DateTime? LastWriteTime { get; set; }
}
```

### ConfigFileInfo

```csharp
public class ConfigFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastWriteTime { get; set; }
    public bool IsSelected { get; set; } = true;
}
```

### BaselineConfig

```csharp
public class BaselineConfig
{
    public string SourceSteamId3 { get; set; } = string.Empty;
    public string SourceCfgPath { get; set; } = string.Empty;
    public List<string> IncludedFiles { get; set; } = new();
}
```

## 服务接口设计

### ISteamLocator

```csharp
public interface ISteamLocator
{
    Task<SteamLocationResult> LocateAsync(CancellationToken ct = default);
    Task<SteamLocationResult> LocateFromPathAsync(string path, CancellationToken ct = default);
}
```

### IAccountScanner

```csharp
public interface IAccountScanner
{
    Task<IReadOnlyList<SteamAccount>> ScanAsync(string steamPath, CancellationToken ct = default);
}
```

### IConfigService

```csharp
public interface IConfigService
{
    Task SetBaselineAsync(SteamAccount account, IEnumerable<string> includedFiles);
    Task<BaselineConfig?> GetBaselineAsync();
    Task<ApplyResult> ApplyToAccountsAsync(
        BaselineConfig baseline,
        IEnumerable<SteamAccount> targets,
        IProgress<ApplyProgress>? progress = null,
        CancellationToken ct = default);
}
```

### IBackupService

```csharp
public interface IBackupService
{
    Task<string> CreateBackupAsync(string targetCfgPath, CancellationToken ct = default);
}
```

## 目录与文件操作策略

### 扫描策略

- 只扫描 `userdata` 下名称为纯数字的目录。
- 排除 `0`（通常代表本地/匿名）和 `ac` 等非账号目录。
- 对每个账号检查 `730/local/cfg` 是否存在。
- 如果 `cfg` 存在，枚举其中的文件，忽略子目录。

### 应用策略

- 目标目录不存在时，使用 `Directory.CreateDirectory` 递归创建。
- 备份采用“整个 cfg 目录复制”方式，备份目录命名为 `cfg.backup_yyyyMMdd_HHmmss`，放在目标账号的 `local` 目录下。
- 复制时先备份，再覆盖写入。
- 遇到文件占用或权限错误时，记录错误并继续处理其他账号，最后汇总结果。

### 日志策略

- 日志文件保存在 `%LocalAppData%/CsgoConfigReplace/logs/`。
- 每次操作记录：时间、源账号、目标账号、复制文件、备份路径、结果。
- UI 上提供“查看日志”入口。

## 界面布局

主窗口采用左右分栏：

- **顶部**：Steam 路径显示 + 重新扫描 + 手动选择路径。
- **左侧**：账号列表（SteamID3、配置状态、最后修改时间），支持多选。
- **右侧**：
  - 基准配置信息（来源账号、文件列表、勾选）。
  - “设为基准”按钮。
  - “应用到选中账号”按钮。
  - 操作结果/日志预览。

## 设置持久化

使用 JSON 文件保存到 `%LocalAppData%/CsgoConfigReplace/settings.json`：

```json
{
  "steamPath": "C:\\Program Files (x86)\\Steam",
  "baseline": {
    "sourceSteamId3": "123456789",
    "sourceCfgPath": "...",
    "includedFiles": ["config.cfg", "autoexec.cfg"]
  },
  "lastSelectedTargets": ["987654321"]
}
```
