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

### 🌐 剧情翻译

- 主线剧情、活动剧情、角色剧情的完整翻译
- 艺术馆与地下城小剧场翻译
- 主页台词/语音翻译
- 基于清单（Manifest）的增量更新缓存系统
- 支持同步/异步两种翻译加载模式

### 🎨 视觉增强

- 自定义 TMP 字体（通过 AssetBundle 加载）
- 对话框文本样式修改：颜色、粗细、描边、字间距
- 对话框透明度独立调节（普通剧情 / CG 剧情）
- 文本描边效果，透明背景下提高可读性

### ⚡ 游戏优化

- 自定义帧率
- 跳过大招动画（变身与释放 Cut-in）

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

修改 `GCMod.cfg` 后按 **`F10`** 即可热重载，无需重启游戏。

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
| `Enabled`   | `true`                                                                              | 是否开启游戏内剧情翻译                             |
| `CDN`       | `https://raw.githubusercontent.com/anosu/girlscreaionr-translation/refs/heads/main` | 翻译数据 CDN 地址                                  |
| `Language`  | `zh_Hans`                                                                           | 翻译语言（目前仅支持 `zh_Hans`）                   |
| `AsyncMode` | `false`                                                                             | 异步加载翻译（不阻塞加载界面，但翻译可能延迟显示） |

### `[Translation.Font]`

| 配置项            | 默认值                                | 说明                                                |
| ----------------- | ------------------------------------- | --------------------------------------------------- |
| `AssetBundlePath` | `GCMod/fonts/TsukuARdGothic-Std-Bold` | TMP 字体 AssetBundle 路径（相对插件目录或绝对路径） |
| `AssetName`       | `TsukuARdGothic-Std-Bold SDF`         | AssetBundle 中 TMP_FontAsset 的名称                 |

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
| `CharacterSpacing` | `0`        | 字间距（仅消息文本，不影响人物名；游戏原值为 `-2.1`） |

---

## ⌨️ 快捷键

| 快捷键 | 功能                                  |
| ------ | ------------------------------------- |
| `F3`   | 开启/关闭大招动画跳过                 |
| `F5`   | 开启/关闭文本样式修改                 |
| `F6`   | 降低普通对话框透明度（`Alt+F6` 升高） |
| `F7`   | 降低 CG 对话框透明度（`Alt+F7` 升高） |
| `F8`   | 开启/关闭翻译                         |
| `F10`  | 热重载配置文件                        |

---

## 🏗️ 项目架构

```
GCMod/
├── Core/                       # 核心模块
│   ├── Plugin.cs               # BepInEx 插件入口，装配依赖
│   ├── Mod.cs                  # 轻量服务定位器
│   ├── Config.cs               # 全局配置管理器
│   ├── InputHandler.cs         # 快捷键处理（MonoBehaviour）
│   └── ModLogger.cs            # 统一日志封装
├── Patches/                    # Harmony 补丁层
│   ├── PatchManager.cs         # 补丁管理器 & 共享工具
│   ├── TranslationPatch.cs     # 剧情翻译注入
│   ├── VisualPatch.cs          # 字体替换 & 文本样式 & 透明度
│   ├── EnhancementPatch.cs     # 帧率修改 & 大招跳过
│   └── HomeWordPatch.cs        # 主页台词翻译 & 字体
├── Services/                   # 服务层
│   ├── TranslationManager.cs   # 翻译数据协调管理
│   ├── TranslationCache.cs     # 基于 Manifest 的缓存系统（支持增量更新）
│   ├── TranslationPaths.cs     # 远程/本地路径构建
│   ├── FontLoader.cs           # AssetBundle 字体加载器
│   └── AlphaController.cs      # 透明度控制工具
└── Models/
    └── ManifestData.cs         # 翻译清单数据结构
```

---

## 📦 翻译数据

翻译语料由 DeepSeek-V4 生成，托管在独立仓库中：

👉 [girlscreaionr-translation](https://github.com/anosu/girlscreaionr-translation)

翻译缓存系统通过 Manifest 文件的哈希校验实现增量更新，避免每次重复下载未变更的翻译数据。

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
