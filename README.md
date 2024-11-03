# GCMod
DMM游戏 【少女艺术绮谭】的简体中文翻译插件

改写自https://github.com/TSKModding/TSKHook

### 使用方法：
- 将GCMod目录下的所有文件解压放到游戏根目录下（GC.exe所在的目录）
- 第一次启动或者游戏更新之后启动会进行初始化的操作（可能需要一段时间）

### 配置文件：
- 位于: BepInEx\plugins\GCMod.json

- 默认提供活动主线和个人剧情的翻译（sakura 0.9）
- 默认请求的路径:
	- 角色名称: API地址 + /names/zh_Hans.json
	- 故事剧情: API地址 + /novels/ + 剧情ID（NovelId） + /zh_Hans.json

### 配置项
- `translation`: 是否开启翻译
- `translation_api`: 翻译请求的api地址
- `font_name`: 翻译使用的字体文件名，默认使用筑紫A丸Bold。<br>如有需要可自行用TextMeshPro打包，保证FontAsset的名称为文件名加上` SDF`即可
- `font_asset_name`: 字体AB包中TMPro_FontAsset的名称
- `normal_alpha`: 普通剧情文本框的透明度，0-1，0为完全透明
- `cg_mode_alpha`: CG模式下文本框的透明度，0-1，0为完全透明
- `change_text_color`: 是否改变文本颜色，用于文本框透明时提高文本的区分度，此项为true时下列配置才生效
- `name_text_color`: 对话人名文本的颜色，RGBA模式，各项取值0-255
- `message_text_color`: 对话内容文本的颜色，RGBA模式，各项取值0-255
- `outline_color`: 文本描边的颜色，RGBA模式，各项取值0-255
- `outline_width`: 文本描边的宽度
- `text_dilate_width`: 控制文本的外扩或收缩，即粗细，因为是居中描边，所以在开启描边时此项大于0效果会更好

### 快捷键
- `F5`: 开启/关闭改变文本颜色
- `F6`: 减小普通剧情文本框的不透明度，按住`alt`时为增大
- `F7`: 减小CG剧情文本框的不透明度，按住`alt`时为增大
- `F8`: 开启/关闭翻译
- `F10`: 重新读取（刷新）配置文件

**如需要关闭控制台窗口，请到`BepInEx\config\BepInEx.cfg`中更改以下内容：**

```
[Logging.Console]

## Enables showing a console for log output.
# Setting type: Boolean
# Default value: true
Enabled = false
```
