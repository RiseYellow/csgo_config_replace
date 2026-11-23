# 实现计划

## 阶段划分

### Phase 1：项目初始化与核心服务（MVP 基础）

目标：搭建解决方案，实现 Steam 路径定位和账号扫描。

- [ ] 创建 .NET 8 WPF 项目 `CsgoConfigReplace.App`。
- [ ] 创建 xUnit 测试项目 `CsgoConfigReplace.Tests`。
- [ ] 引入 CommunityToolkit.Mvvm、Microsoft.Extensions.DependencyInjection、Microsoft.Extensions.Logging。
- [ ] 实现 `SteamLocator`：注册表读取 + 默认路径回退 + 手动路径验证。
- [ ] 实现 `AccountScanner`：遍历 `userdata`，识别 SteamID3 和 cfg 目录。
- [ ] 编写单元测试：路径解析、账号扫描逻辑（使用临时目录模拟）。

### Phase 2：UI 与基准配置管理

目标：让用户能看到账号列表并设置基准配置。

- [ ] 设计主窗口 XAML：路径栏、账号列表、基准配置面板。
- [ ] 实现 `MainViewModel`：加载路径、扫描账号、处理选中状态。
- [ ] 实现“设为基准配置”：记录来源账号和需要复制的文件。
- [ ] 实现设置持久化：保存 Steam 路径和基准配置到本地 JSON。
- [ ] 实现简单的配置文件预览（只读文本）。

### Phase 3：一键应用与备份

目标：完成核心迁移功能。

- [ ] 实现 `BackupService`：复制目标 cfg 目录到带时间戳的备份目录。
- [ ] 实现 `ConfigService.ApplyToAccountsAsync`：创建目录、备份、复制文件。
- [ ] 实现多目标账号批量应用。
- [ ] 实现操作结果反馈：成功/失败列表、日志记录。
- [ ] 添加游戏运行检测（可选）：检测 `cs2.exe` / `csgo.exe` 进程并提示。

### Phase 4：测试、打包与文档

目标：发布可用版本。

- [ ] 集成测试：在真实 Steam 目录上测试扫描和应用。
- [ ] 边界测试：新账号无 cfg 目录、目标文件只读、路径含空格等。
- [ ] 配置单文件发布：`dotnet publish` 生成绿色版 exe。
- [ ] 编写 README：使用说明、截图、下载方式。
- [ ] 编写变更日志。

## 后续迭代（Backlog）

- [ ] 显示 Steam 昵称（读取 `userdata/<id>/config/localconfig.vdf` 或调用 Steam Web API）。
- [ ] 支持多个 Steam 安装目录/库目录。
- [ ] 支持配置差异对比与合并。
- [ ] 支持导入/导出基准配置包。
- [ ] 自动检测 Steam 云同步状态并提示。
- [ ] 支持 CS:GO 与 CS2 配置指令兼容性检查。

## 验收标准

1. 启动程序后能自动识别本机 Steam 路径。
2. 账号列表能正确显示所有登录过的 SteamID3 及其 cfg 状态。
3. 用户能指定一个账号为基准配置。
4. 用户能选择一个或多个目标账号，一键复制基准配置。
5. 目标账号从未运行过游戏时，程序能自动创建目录并完成复制。
6. 应用前自动备份目标账号原有配置。
7. 操作完成后显示明确的成功/失败信息，并写入日志。
8. 发布为单文件 exe，在干净 Windows 环境中可直接运行。
