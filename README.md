# GCMod

**首先，这是一个适用于 Fanza Game 少女艺术绮谭 R 的翻译项目**

本仓库的文件主要用于 **Windows 平台 DMM Game Player 端**，如果你需要安卓版，请移步[DMM-Mod](https://github.com/anosu/DMM-Mod)

### 插件功能

-   为游戏提供剧情翻译，包括主线活动角色剧情以及艺术馆和地下城小剧场，还有主页语音台词
-   修改对话文本（框）样式，包括更改文本颜色、间距和描边，修改文本框透明度

### 使用方法

-   首先确保你已经安装了游戏的客户端（DMM Game Player 版）并且知道游戏的可执行文件（`GC.exe`）所在的文件夹路径

-   从本仓库的[Releases](https://github.com/anosu/GCMod/releases)页面（← 如果你不知道在哪儿那就直接点这里）找到最新发布的版本（带有绿色的`Latest`标识），展开`Assets`选项卡（默认应该就是展开的），下载名为`GCMod.7z`或类似的压缩包，不要下载`Source code`，那是源码

-   将下载的压缩包解压你会得到`winhttp.dll`，`BepInEx`等文件和文件夹，将所有这些文件复制（或直接解压）到与游戏的可执行文件（`GCMod.exe`）相同的目录，你的`BepInEx`文件夹、`winhttp.dll`文件以及`GCMod.exe`应该在同一个目录下。如果你之前下载过旧版本可以先将旧版本删除或者直接全部覆盖（如果后面没问题的话）

-   正常启动游戏。注意：首次启动或者游戏更新之后，插件会有一个初始化的过程，此时你只会看到一个控制台窗口，等待其初始化完成游戏才会正常启动，此过程中 BepInEx 会从其官网下载对应游戏 Unity 版本的补丁来对游戏进行修改以支持插件的运行，如果你使用 ACGP 之类的加速器并且在此过程中看到了控制台窗口出现了红色的报错那么说明你可能无法直连其官网，请打开梯子来解决此问题。

-   当插件初始化完成并且游戏正常启动后（控制台窗口没有出现红色的报错），那么此时应该已经可以正常使用了。插件首次运行之后会在`BepInEx\config`目录下生成 BepInEx 和 mod 本身的配置文件，分别为`BepInEx.cfg`和`GCMod.cfg`，如果你需要修改插件的设置（如关闭翻译或者修改文本样式的功能），请修改`GCMod.cfg`之后按`F10`重载配置文件或者重新启动游戏。如果你需要隐藏控制台窗口，请在`BepInEx.cfg`中找到`[Logging.Console]`选项，并将`Enabled`的值设置为`false`

### 配置项

-   `[Translation]`
    -   `Enabled`: 是否开启游戏内剧情翻译
    -   `CDN`: 翻译加载的 CDN
    -   `AsyncMode`: 异步请求翻译（不会造成加载界面卡顿，但翻译可能延迟显示）
-   `[Translation.Font]`
    -   `AssetBundlePath`: TMP 字体 AssetBundle 的路径，默认相对于插件目录，也可使用绝对路径
    -   `AssetName`: AssetBundle 中`TMP_FontAsset`的名称
-   `[Message.Window]`
    -   `NormalAlpha`: 普通剧情中的对话框透明度，默认完全透明
    -   `CgModeAlpha`: 寝室剧情中的对话框透明度，默认完全透明
-   `[Message.Text]`
    -   `Modified`: 是否更改对话框文本样式（用于对话框透明时提高对比度）
    -   `NameColor`: 对话框人物名文本颜色，仅支持十六进制格式，可带可不带前置`#`
    -   `MessageColor`: 对话框消息文本颜色，仅支持十六进制格式，可带可不带前置`#`
    -   `FaceDilate`: 字体粗细，不建议过大，取值范围：`[-1, 1]`
    -   `OutlineColor`: 文本描边颜色，仅支持十六进制格式，可带可不带前置`#`
    -   `OutlineWidth`: 文本描边宽度，不建议过大，取值范围：`[0, 1]`
    -   `OutlineSoftness`: 文本描边羽化程度，建议极小，取值范围：`[0, 1]`
    -   `CharacterSpacing`: 字间距，仅对消息文本设置，不应用于人物名。游戏默认值`-2.1`，插件默认值`1`

### 快捷键

-   `F5`: 开启/关闭文本样式修改
-   `F6`: 减小普通剧情文本框的不透明度，按住`Alt`时为增大
-   `F7`: 减小 CG 剧情文本框的不透明度，按住`Alt`时为增大
-   `F8`: 开启/关闭翻译
-   `F10`: 重载配置文件

### 注意事项

-   6.0.0 版本之后更换了配置文件，不在使用原来的 GCMod.json，改为使用 BepInEx 提供的
-   如果你在使用旧版，请看[README](https://github.com/anosu/GCMod/blob/main/README_OLD.md)

### 其他

-   翻译使用 DeepSeek-V3
-   翻译文件见[girlscreaionr-translation](https://github.com/anosu/girlscreaionr-translation)
-   正常使用过程中如果遇到问题可以提交 issue 或者直接在群里@我（如果可以的话）
