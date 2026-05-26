# ActionTimelineReborn 维护交接文档

本文档给后续维护者快速接手用，重点记录项目结构、常用命令、发布流程和这次处理过的坑。

## 当前维护状态

- 当前发布版本：`7.5.0.5`
- 主分支：`main`
- 当前远端：`origin https://github.com/anmili2022/ActionTimelineReborn`
- 插件框架：Dalamud 插件
- Dalamud API 级别：`15`
- 目标框架：`net10.0-windows10.0.26100.0`
- 默认构建输出目录：仓库根目录下的 `output/`
- 本地发布产物：`output/ActionTimelineReborn.zip`

## 重要文件

| 文件 | 用途 |
| --- | --- |
| `ActionTimelineReborn/ActionTimelineReborn.csproj` | 项目配置、版本号、输出目录、依赖 |
| `ActionTimelineReborn/ActionTimelineReborn.json` | 插件 manifest，构建时会复制进输出包 |
| `manifest.json` | 仓库根目录 manifest，和插件内 manifest 保持一致 |
| `ActionTimelineReborn/Plugin.cs` | 插件入口、命令、窗口注册、UiBuilder 回调 |
| `ActionTimelineReborn/Windows/SettingsWindow.cs` | 设置窗口 UI |
| `ActionTimelineReborn/Windows/TimelineWindow.cs` | 时间轴窗口绘制 |
| `ActionTimelineReborn/Timeline/TimelineManager.cs` | 技能、施法、状态、ActionEffect 采集逻辑 |
| `.github/workflows/publish.yaml` | tag 发布 workflow |
| `output/` | 本地构建输出目录，已加入 `.gitignore` |

## 开发环境要求

需要：

- Windows
- .NET SDK 10
- XIVLauncher / Dalamud dev 文件
- Git
- GitHub CLI：`gh`

确认 .NET：

```powershell
dotnet --info
```

确认 GitHub CLI 登录：

```powershell
gh auth status
```

确认 Dalamud dev DLL 存在：

```powershell
Test-Path "$env:APPDATA\XIVLauncher\addon\Hooks\dev\Dalamud.dll"
```

> 注意：如果本地 `Hooks\dev` 里的 Dalamud DLL 和游戏实际运行的 Dalamud API 不一致，可能出现 `MissingMethodException`。遇到 API 相关问题优先查官方文档：<https://dalamud.dev/api/>。

## 常用开发命令

在仓库根目录执行：

```powershell
dotnet build ActionTimelineReborn.sln
```

Release 构建：

```powershell
dotnet build ActionTimelineReborn.sln -c Release
```

构建成功后主要输出：

```text
output/ActionTimelineReborn.dll
output/ActionTimelineReborn.json
output/ActionTimelineReborn/ActionTimelineReborn.json
output/ActionTimelineReborn/latest.zip
```

其中 `output/ActionTimelineReborn/latest.zip` 是 DalamudPackager 生成的插件包，发布时可以复制/重命名成：

```text
output/ActionTimelineReborn.zip
```

## 本地加载测试

Dalamud 开发插件路径指向：

```text
E:\git\ActionTimelineReborn\output\ActionTimelineReborn.dll
```

常用命令：

```text
/atl
/atl lock
/atl unlock
/atl <时间轴名称>
```

行为：

- `/atl`：打开/关闭设置窗口。
- `/atl lock`：锁定所有时间轴窗口。
- `/atl unlock`：解锁所有时间轴窗口。
- `/atl <时间轴名称>`：显示/隐藏指定名称的时间轴；名称带空格时可使用引号。

示例：

```text
/atl 主要
/atl "Burst Line"
```

## 发布前检查清单

发布前建议按这个顺序做：

1. 确认工作区状态：

    ```powershell
    git status --short --branch
    ```

2. 更新版本号：

    文件：

    ```text
    ActionTimelineReborn/ActionTimelineReborn.csproj
    ```

    修改：

    ```xml
    <Version>x.y.z.w</Version>
    ```

3. 如有说明变更，同步修改两个 manifest：

    ```text
    manifest.json
    ActionTimelineReborn/ActionTimelineReborn.json
    ```

4. Release 构建：

    ```powershell
    dotnet build ActionTimelineReborn.sln -c Release
    ```

5. 检查输出 manifest 版本：

    ```powershell
    Get-Content output\ActionTimelineReborn.json -Encoding UTF8
    ```

    确认包含：

    ```json
    "AssemblyVersion": "x.y.z.w"
    ```

6. 确认插件包内容：

    ```powershell
    tar -tf output\ActionTimelineReborn\latest.zip
    ```

    正常应包含：

    ```text
    ActionTimelineReborn.deps.json
    ActionTimelineReborn.dll
    ActionTimelineReborn.json
    ECommons.dll
    Microsoft.Windows.SDK.NET.dll
    WinRT.Runtime.dll
    ```

7. 本地 Dalamud 加载测试。

8. 提交、打 tag、发布。

## 快速发布流程

下面用 `7.4.0.2` 举例。下次发布时替换成实际版本号即可。

### 1. 修改版本号

编辑：

```text
ActionTimelineReborn/ActionTimelineReborn.csproj
```

把：

```xml
<Version>7.4.0.1</Version>
```

改为：

```xml
<Version>7.4.0.2</Version>
```

### 2. 构建

```powershell
dotnet build ActionTimelineReborn.sln -c Release
```

### 3. 准备发布 zip

DalamudPackager 会生成：

```text
output/ActionTimelineReborn/latest.zip
```

复制成 GitHub Release 资产名：

```powershell
Copy-Item -Path output\ActionTimelineReborn\latest.zip -Destination output\ActionTimelineReborn.zip -Force
```

检查 zip 内容：

```powershell
tar -tf output\ActionTimelineReborn.zip
```

### 4. 提交

```powershell
git status --short --branch
git add .gitignore ActionTimelineReborn/ActionTimelineReborn.csproj ActionTimelineReborn/ActionTimelineReborn.json ActionTimelineReborn/Configurations/DrawingSettings.cs ActionTimelineReborn/Plugin.cs ActionTimelineReborn/Timeline/TimelineManager.cs ActionTimelineReborn/Windows/SettingsWindow.cs ActionTimelineReborn/Windows/TimelineWindow.cs manifest.json HANDOFF.md README.md
git commit -m "Release 7.4.0.2"
```

如果只有版本号变化，可以只 add 实际改动文件。

### 5. 创建 tag

优先使用普通 lightweight tag：

```powershell
git tag 7.4.0.2
```

如果 `git tag` 被本机 GPG 或其他配置卡住，可以直接写 ref：

```powershell
git update-ref refs/tags/7.4.0.2 HEAD
```

确认 tag：

```powershell
git rev-parse HEAD refs/tags/7.4.0.2
```

两行 hash 应一致。

### 6. 推送 main 和 tag

```powershell
git push origin main
git push origin refs/tags/7.4.0.2
```

### 7. 发布 GitHub Release

仓库已有 `.github/workflows/publish.yaml`，理论上推送四段版本 tag，例如 `7.4.0.2`，会触发 GitHub Actions 自动发布：

```yaml
on:
  push:
    tags:
      - '*.*.*.*'
```

检查 Actions：

```powershell
gh run list --workflow Publish --limit 5
```

如果 Actions 没有启动或想更快发布，可以手动创建 Release：

```powershell
gh release create 7.4.0.2 output\ActionTimelineReborn.zip --title "7.4.0.2" --notes "- 更新说明 1`n- 更新说明 2"
```

确认发布：

```powershell
gh release view 7.4.0.2 --json tagName,url,isDraft,isPrerelease,assets,publishedAt
```

## 一条龙发布命令模板

确认版本号已经改好并构建测试通过后，可以用这个模板快速发布：

```powershell
dotnet build ActionTimelineReborn.sln -c Release
Copy-Item -Path output\ActionTimelineReborn\latest.zip -Destination output\ActionTimelineReborn.zip -Force
git status --short --branch
git add .
git commit -m "Release 7.4.0.2"
git update-ref refs/tags/7.4.0.2 HEAD
git push origin main
git push origin refs/tags/7.4.0.2
gh release create 7.4.0.2 output\ActionTimelineReborn.zip --title "7.4.0.2" --notes "- 更新到 7.4.0.2"
gh release view 7.4.0.2 --json tagName,url,assets
```

> 注意：`git add .` 只适合确认 `.gitignore` 正常、没有多余文件时使用。发布前务必看 `git status --short --branch`。

## 已知坑和处理方式

### 1. `Window.TitleBarButtons` 缺失

现象：

```text
MissingMethodException: Window.get_TitleBarButtons()
```

原因：本地编译 API 和运行时 Dalamud API 不一致，或者运行时接口已变化。

当前处理：已移除标题栏 Ko-fi 按钮，不再调用 `TitleBarButtons`。

### 2. `WindowSystem.AddWindow(Window)` 缺失

现象：

```text
MissingMethodException: WindowSystem.AddWindow(Dalamud.Interface.Windowing.Window)
```

原因：编译时硬绑定的 `AddWindow(Window)` 和运行时实际签名不一致。

当前处理：`Plugin.cs` 里使用反射查找运行时 `AddWindow`，避免硬绑定精确签名。

### 3. 主 UI 回调警告

现象：

```text
The plugin does not register a main UI callback.
```

当前处理：已注册：

```csharp
Svc.PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;
```

主 UI 和配置 UI 都打开设置窗口。

### 4. 构造失败导致 Hook 泄露

现象：

```text
is leaking hooks
```

当前处理：`Plugin` 构造函数里已经加异常清理逻辑，构造失败会反注册 UiBuilder 回调、Dispose `TimelineManager`、Dispose ECommons。

### 5. GitHub Actions 没有触发发布

这次 `7.4.0.1` 推送 tag 后没有看到 Actions run，因此使用 `gh release create` 手动发布。

后续如果 Actions 仍不触发：

1. 检查 workflow 是否启用：

    ```powershell
    gh workflow list --all
    ```

2. 手动发布：

    ```powershell
    gh release create <版本号> output\ActionTimelineReborn.zip --title "<版本号>" --notes "<更新说明>"
    ```

## 当前 7.5.0.5 发布内容

Release 链接：

```text
https://github.com/anmili2022/ActionTimelineReborn/releases/tag/7.5.0.5
```

下载地址：

```text
https://github.com/anmili2022/ActionTimelineReborn/releases/download/7.5.0.5/ActionTimelineReborn.zip
```

主要变更：

- UI 汉化。
- 插件说明和简介改为中英双语。
- 新增 `/atl <时间轴名称>` 显示/隐藏指定时间轴。
- 注册 `UiBuilder.OpenMainUi`，处理插件校验警告。
- 移除不兼容的 `TitleBarButtons` 调用。
- 对 `WindowSystem.AddWindow` 做运行时兼容处理。
- 构造失败时清理 hooks，减少泄露。
- 构建输出目录改为 `output/`。
- 同步上游 `7.5.0.4` 更新并保留汉化。
- 修复部分技能显示名和图标回退。
- 兼容状态列表结构变化。
- 帮助页补充 `/atl <时间轴名称>` 用法。
