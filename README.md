# GCMod

> 🎮 适用于 **Fanza Game「少女艺术绮谭 R」** 的 BepInEx 插件模组

本仓库适用于 **Windows 平台 DMM Game Player 端**。安卓用户请移步 [DMM-Mod](https://github.com/anosu/DMM-Mod)。

---

## 📋 目录

- [功能特性](#-功能特性)
- [快速开始](#-快速开始)
- [配置项](#-配置项)
- [快捷键](#-快捷键)
- [项目架构](#-项目架构)
- [开发指南](#-开发指南)
- [翻译数据](#-翻译数据)
- [常见问题](#-常见问题)
- [致谢](#-致谢)

---

## ✨ 功能特性

### 🌐 游戏翻译

- 主线剧情、活动剧情、角色剧情的完整翻译
- 艺术馆与地下城小剧场翻译
- 按数据表启用主数据翻译（包含主页台词）
- 基于清单（Manifest）的增量更新缓存系统
- 剧情翻译支持同步/异步加载，主数据翻译在消费前替换

### 🎨 视觉增强

- 自定义 TMP 后备字体（通过 AssetBundle 加载）
- 对话框文本样式修改：颜色、粗细、描边、字间距
- 对话框透明度独立调节（普通剧情 / CG 剧情）
- 文本描边效果，透明背景下提高可读性

### ⚡ 游戏优化

- 自定义帧率
- 跳过大招动画（变身与释放 Cut-in）
- 启动时后台检查 GitHub 最新正式版本，发现更新后显示提示

---

## 🚀 快速开始

### 1. 安装游戏客户端

确保已安装 DMM Game Player 版游戏，并知晓游戏可执行文件 `GC.exe` 所在的目录。

### 2. 下载插件

前往 [Releases](https://github.com/anosu/GCMod/releases) 页面，找到最新版本（带有绿色 `Latest` 标识），展开 `Assets` 下载 `GCMod.7z` 压缩包。

> ⚠️ 不要下载 `Source code`，那是源码。

### 3. 安装

将压缩包解压到与 `GC.exe` 相同的目录。解压后目录结构应如下：

```
游戏根目录/
├── GC.exe
├── winhttp.dll
└── BepInEx/
    ├── core/
    ├── plugins/
    │   └── GCMod/
    └── config/
```

> 如果你之前安装过旧版本，建议先删除旧文件或直接覆盖。

### 4. 启动游戏

正常启动游戏。首次启动或游戏更新后，BepInEx 会自动从其官网下载适配当前 Unity 版本的补丁，此时只会显示一个控制台窗口，等待初始化完成即可。

> ⚠️ 如果使用加速器（如 ACGP），控制台窗口可能出现红色错误，说明无法直连 BepInEx 官网，请开启代理后重试。

### 5. 配置文件

首次运行后，`BepInEx\config\` 目录下会生成两个配置文件：

| 文件          | 用途                                 |
| ------------- | ------------------------------------ |
| `BepInEx.cfg` | BepInEx 框架配置（如隐藏控制台窗口） |
| `GCMod.cfg`   | 插件功能配置（翻译、字体、样式等）   |

修改 `GCMod.cfg` 后按 **`F10`** 可重载配置并刷新翻译会话。CDN、语言和翻译文件会重新加载；已经反序列化的主数据仍需重新加载或重启游戏才会改变。

---

## ⚙️ 配置项

### `[General]`

| 配置项      | 默认值 | 说明               |
| ----------- | ------ | ------------------ |
| `FrameRate` | `60`   | 游戏帧率（正整数） |

### `[Battle]`

| 配置项        | 默认值  | 说明                                   |
| ------------- | ------- | -------------------------------------- |
| `IsSkipCutin` | `false` | 是否跳过大招动画（包括变身和释放动画） |

### `[Translation]`

| 配置项      | 默认值                                                                              | 说明                                               |
| ----------- | ----------------------------------------------------------------------------------- | -------------------------------------------------- |
| `Enabled`   | `true`                                                                              | 是否开启剧情和主数据翻译                             |
| `CDN`       | `https://raw.githubusercontent.com/anosu/girlscreation-translation/refs/heads/main` | 翻译数据 CDN 地址                                  |
| `Language`  | `zh-Hans`                                                                           | 翻译语言（旧配置需改为 `zh-Hans`）                   |
| `AsyncMode` | `false`                                                                             | 异步加载剧情翻译；同步入口和主数据入口最多等待 10 秒 |

### `[Translation.MasterData]`

`EnabledTables` 是 `string[]` 配置项，默认 `*`（启用翻译表中的所有数据表，含后续新增的表）。也可用逗号分隔具体表名以限定范围，留空则全部关闭。数据表名就是 TextAsset 名称及 `MasterAttribute.Object` 的值，区分大小写。

本地测试示例（在翻译仓库运行 `npm start`）：

```ini
[Translation]
Enabled = true
CDN = http://localhost:12315
Language = zh-Hans

[Translation.MasterData]
EnabledTables = *
```

例如 `EnabledTables = mUnitWords, mSubunitWords` 只启用这两张表。旧配置若已保存空名单，需手动改为 `*` 才会全部启用。F10 重载配置后，设置影响后续主数据加载；已经反序列化的数据不会自动还原或重译，通常需要重启游戏。原 Home Words 补丁及 `words.json` 已移除，主页台词由这些主数据表提供。

### `[Translation.Font]`

| 配置项            | 默认值                                | 说明                                                |
| ----------------- | ------------------------------------- | --------------------------------------------------- |
| `AssetBundlePath` | `GCMod/fonts/tsukuardgothic-std-medium` | TMP 后备字体 AssetBundle 路径（相对插件目录或绝对路径），自动选取字体资源 |

### `[Message.Window]`

| 配置项        | 默认值 | 说明                                         |
| ------------- | ------ | -------------------------------------------- |
| `NormalAlpha` | `0`    | 普通剧情对话框透明度（0-1，0 为完全透明）    |
| `CgModeAlpha` | `0`    | CG/寝室剧情对话框透明度（0-1，0 为完全透明） |

### `[Message.Text]`

| 配置项             | 默认值     | 说明                                                  |
| ------------------ | ---------- | ----------------------------------------------------- |
| `Modified`         | `true`     | 是否修改文本样式（对话框透明时建议开启以提高对比度）  |
| `NameColor`        | `FFFFFFFF` | 人物名颜色（十六进制，可选前置 `#`）                  |
| `MessageColor`     | `FFFFFFFF` | 消息文本颜色（十六进制，可选前置 `#`）                |
| `FaceDilate`       | `0.3`      | 字体粗细，范围 `[-1, 1]`                              |
| `OutlineColor`     | `3A3A3AFF` | 文本描边颜色（十六进制，可选前置 `#`）                |
| `OutlineWidth`     | `0.3`      | 描边宽度，范围 `[0, 1]`                               |
| `OutlineSoftness`  | `0.01`     | 描边羽化程度，范围 `[0, 1]`                           |
| `CharacterSpacing` | `0.1`      | 字间距（仅消息文本，不影响人物名） |

---

## ⌨️ 快捷键

| 快捷键 | 功能                                  |
| ------ | ------------------------------------- |
| `F3`   | 开启/关闭大招动画跳过                 |
| `F5`   | 开启/关闭文本样式修改                 |
| `F6`   | 降低普通对话框透明度（`Alt+F6` 升高） |
| `F7`   | 降低 CG 对话框透明度（`Alt+F7` 升高） |
| `F8`   | 开启/关闭翻译                         |
| `F10`  | 重载配置并刷新翻译（含 CDN、语言和翻译文件） |

---

## 🏗️ 项目架构

```
GCMod/
├── Core/                       # 核心模块
│   ├── Plugin.cs               # BepInEx 插件入口，装配依赖
│   ├── Config.cs               # 全局配置管理器
│   ├── Hotkey.cs               # 快捷键处理（MonoBehaviour）
│   └── Logger.cs               # 统一日志封装
├── Patches/                    # Harmony 与原生补丁
│   ├── PatchManager.cs         # 补丁管理器 & 共享工具
│   ├── TranslationPatch.cs     # 剧情翻译注入
│   ├── VisualPatch.cs          # 文本样式与透明度
│   ├── EnhancePatch.cs         # 帧率修改与大招跳过
│   ├── DebugPatch.cs           # 调试补丁（仅 Debug 构建）
│   └── MasterDataPatch.cs      # 解压后、消费前替换主数据 JSON
├── Services/                   # 服务层
│   ├── TranslationManager.cs   # 翻译数据协调管理
│   ├── TranslationCache.cs     # 基于 Manifest 的缓存系统（支持增量更新）
│   ├── TranslationSession.cs   # 单次语言会话、请求合并与取消
│   ├── TranslationPaths.cs     # 远程/本地路径构建
│   ├── MasterDataTranslator.cs # 三种字段格式的主数据翻译
│   ├── TranslationHash.cs      # 与翻译仓库一致的嵌套映射哈希
│   ├── UpdateChecker.cs        # GitHub 新版本检查与提示
│   └── AlphaController.cs      # 透明度控制工具
└── Models/
    └── Manifest.cs             # 翻译清单数据结构
```

---

## 🛠️ 开发指南

开发环境、Utility 源码引用和 CSharpier 格式化见 [构建说明](https://github.com/anosu/ModEngineering/blob/main/docs/CONVENTIONS.md)。

---

## 📦 翻译数据

翻译语料由 DeepSeek-V4 生成，托管在独立仓库中：

👉 [girlscreation-translation](https://github.com/anosu/girlscreation-translation)

翻译缓存系统通过 Manifest 文件的哈希校验实现增量更新，避免每次重复下载未变更的翻译数据。

同一会话复用已加载资源，并发请求共用一次下载。损坏的缓存会重新下载，写入通过同目录临时文件完成后再替换；磁盘写入失败不会丢弃已经取得的翻译。无可用数据的请求冷却 30 秒后允许后续请求重试，F10 刷新可立即重试。

翻译在启动时预加载。主数据消费优先使用已就绪的最新译文；网络更新尚未完成时先使用本地可用副本，后台更新继续执行。没有可用缓存时才等待下载，单次最多 10 秒，超时保留原文。最新版本就绪后，后续主数据加载会优先使用它；已经消费的数据不会自动重译。

剧情同步模式最多等待 10 秒。当前剧本 ID 不受翻译开关影响：F8 重新开启翻译会加载当前剧本，F10 刷新 CDN、语言或翻译文件时也会重新加载当前剧本。F10 会取消旧会话请求，防止旧语言的异步结果混入新会话。

默认缓存目录为 `BepInEx/plugins/GCMod/translations/<Language>/`。

资源按新仓库结构读取：`<CDN>/translations/<Language>/manifest.json`、`names.json`、`master.json` 和 `novels/<id>.json`。

`master.json` 按「数据表名 → 字段名 → 原文到译文映射」组织。例如：

```json
{
  "mItems": { "ml_name[]": { "AP回復薬（小）": "AP恢复药（小）" } },
  "mAthenesRecordAreas": { "ml_name|": { "プランタン地区": "春日地区" } },
  "mActionPatterns": { "name": { "AI：攻撃的": "AI：攻击型" } }
}
```

`[]` 仅替换数组第 0 项；`|` 仅替换第一个分隔符前的文本；无后缀则替换完整字符串。其他语言、未声明字段、未命中原文、空译文均保持不变。字段规则在加载翻译表时预处理一次。修改翻译表后需用翻译仓库的 `scripts/build.py` 更新清单，再按 F10 刷新；已经加载的主数据通常仍需重启游戏。

不依赖游戏环境的翻译与缓存测试：`dotnet test tests/GCMod.Tests/GCMod.Tests.csproj`。

---

## ❓ 常见问题

<details>
<summary><b>控制台窗口出现红色报错？</b></summary>
通常是 BepInEx 无法连接其官网下载 Unity 补丁。请开启代理/梯子后重启游戏。
</details>

<details>
<summary><b>如何隐藏控制台窗口？</b></summary>
编辑 <code>BepInEx\config\BepInEx.cfg</code>，找到 <code>[Logging.Console]</code>，将 <code>Enabled</code> 设为 <code>false</code>。
</details>

<details>
<summary><b>翻译没有生效？</b></summary>
<ol>
<li>检查 <code>GCMod.cfg</code> 中 <code>[Translation] Enabled</code> 是否为 <code>true</code></li>
<li>确认网络可访问翻译 CDN（GitHub Raw）</li>
<li>按 <code>F8</code> 切换翻译开关</li>
</ol>
</details>

<details>
<summary><b>修改配置后没有变化？</b></summary>
修改配置文件后按 <code>F10</code> 热重载，或重启游戏。
</details>

---

> 💬 遇到问题？欢迎提交 [Issue](https://github.com/anosu/GCMod/issues) 或直接在群里 @Jitsu。

## 开发

源码位于 `src/`，测试位于 `tests/`。项目配置由 `.csproj` 管理，依赖版本由 Git 子模块记录。构建、VS 联调和发布命令见[公共工程说明](https://github.com/anosu/ModEngineering/blob/main/docs/CONVENTIONS.md)。
