# GCMod
DMM游戏 【少女艺术绮谭】的简体中文翻译插件

改写自https://github.com/TSKModding/TSKHook

### 使用方法：
- 将GCMod目录下的所有文件解压放到游戏根目录下（GC.exe所在的目录）
- 第一次启动或者游戏更新之后启动会进行初始化的操作（可能需要一段时间）

### 配置文件：
- 位于: BepInEx\plugins\GCMod.json

- 默认提供的现在只有艺术家剧情的翻译（sakura 0.9）
- 默认请求的路径：
	- 角色名称: API地址 + /gc/names/zh_Hans.json
	- 故事剧情: API地址 + /gc/novels/ + 剧情ID（NovelId） + /zh_Hans.json

### 配置项
- `translation`：是否开启翻译
- `translation_api`：翻译请求的api地址
- `tanslation_font`：翻译使用的字体，默认使用筑紫A丸Bold。<br>如有需要可自行用TextMeshPro打包，保证FontAsset的名称为文件名加上` SDF`即可
