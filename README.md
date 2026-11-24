# CS:GO / CS2 Config Replace

一个 Windows 桌面工具，用于将 CS:GO / CS2 的本地 config 文件从一个 Steam 账号快速迁移到另一个 Steam 账号。

## 文档索引

- [需求分析](docs/01-requirements-analysis.md)
- [可行性分析](docs/02-feasibility-analysis.md)
- [技术选型](docs/03-technology-selection.md)
- [架构设计](docs/04-architecture-design.md)
- [实现计划](docs/05-implementation-plan.md)
- [实现总结](docs/06-implementation-summary.md)

## 功能特性

- 自动扫描本机 Steam 安装路径（支持手动修改）。
- 列出所有登录过的 Steam 账号、昵称及其 CS2 配置状态。
- 将任意账号的 `cfg` 目录设为基准配置，也支持直接选择外部 `config` 文件夹作为基准。
- 一键将基准配置应用到其他账号，支持多选。
- **自动检测新登录账号**：开启后，程序运行期间新登录的 Steam 账号会自动应用基准配置。
- 目标账号从未运行过 CS2 时，自动创建 `730/local/cfg` 目录。
- 应用前自动备份目标账号原有配置。
- **自动关闭 CS2 云同步**：应用配置时自动修改 `sharedconfig.vdf`，防止 Steam 云端覆盖本地配置。
- 操作日志实时显示。

## 下载与运行

### 直接运行（推荐）

下载发布目录中的 `CsgoConfigReplace.App.exe`，双击即可运行。

- 平台：Windows 10 / 11
- 大小：**约 1004 KB**
- 依赖：Windows 系统自带的 **.NET Framework 4.8**，无需额外安装。

### 从源码构建

```bash
# 安装 .NET 8 SDK（用于构建，非运行时依赖）
# 克隆仓库后执行：
dotnet restore CsgoConfigReplace.sln
dotnet build CsgoConfigReplace.sln -p:Platform="Any CPU"
dotnet run --project src/CsgoConfigReplace.App/CsgoConfigReplace.App.csproj
```

### 发布单文件 exe

```bash
dotnet build src/CsgoConfigReplace.App/CsgoConfigReplace.App.csproj \
  -c Release -p:Platform="Any CPU"
```

构建完成后，exe 位于：

```text
src/CsgoConfigReplace.App/bin/Any CPU/Release/net48/CsgoConfigReplace.App.exe
```

项目使用 Costura.Fody 自动将所有依赖 DLL 合并进单个 exe。

## 使用说明

1. **启动程序**：程序会自动尝试定位 Steam 路径。如果失败，点击“浏览...”手动选择 Steam 安装目录。
2. **扫描账号**：点击“重新扫描”，左侧会列出所有在本机登录过的 Steam 账号。
   - “已配置”表示该账号存在 `cfg` 目录。
   - “未运行过 CS2”表示该账号还没有 `730/local/cfg` 目录。
3. **设置基准配置**：
   - 方式一：在左侧选中一个已有配置的账号，在右侧勾选需要复制的配置文件，点击“将选中账号设为基准”。
   - 方式二：点击“选择外部config文件夹”，选择任意本地 config 文件夹，勾选文件后点击“将选中账号设为基准”。
4. **应用到目标账号**：
   - 在左侧勾选一个或多个目标账号。
   - 点击“一键应用到勾选账号”。
   - 确认后程序会自动备份目标账号原有配置，并复制基准文件。
5. **开启自动监控（可选）**：
   - 勾选“自动检测新登录账号并应用基准配置”。
   - 保持程序运行，之后新登录的 Steam 账号会自动应用基准配置。
   - 操作日志会显示自动应用结果。
6. **验证**：启动 CS2，检查准星、灵敏度、按键等设置是否生效。

## 配置文件位置

程序操作的是 Steam 本地目录：

```text
<Steam安装目录>/userdata/<SteamID3>/730/local/cfg/
```

常见文件：

| 文件 | 说明 |
|------|------|
| `config.cfg` | 游戏设置、按键绑定、准星等 |
| `autoexec.cfg` | 自定义启动脚本 |
| `video.txt` | 画面设置 |

## 备份与恢复

每次应用配置前，程序会在目标账号的 `local` 目录下创建备份：

```text
userdata/<目标SteamID3>/730/local/cfg.backup_yyyyMMdd_HHmmss/
```

如需恢复，删除当前 `cfg` 目录，将备份目录重命名为 `cfg` 即可。

## 注意事项

- 目标账号必须在本机登录过 Steam，否则 `userdata/<SteamID3>` 不存在，程序无法识别。
- 如果目标账号正在运行 CS2，请先退出游戏再应用配置。
- 应用配置时会自动关闭该账号的 CS2 Steam 云同步；如果 Steam 客户端正在运行，建议重启 Steam 使设置生效。
- CS2 与 CS:GO 部分指令存在差异，本工具只做文件级迁移，不保证所有指令完全兼容。

## 开发

### 技术栈

- C# / .NET Framework 4.8
- WPF + MVVM（CommunityToolkit.Mvvm）
- Costura.Fody（单文件打包）
- xUnit + FluentAssertions

### 运行测试

```bash
dotnet test CsgoConfigReplace.sln -p:Platform="Any CPU"
```

### 项目结构

```text
src/CsgoConfigReplace.App/     # WPF 主程序
tests/CsgoConfigReplace.Tests/ # 单元测试
docs/                          # 过程文档
artifacts/                     # 发布产物
```

## 许可证

MIT
