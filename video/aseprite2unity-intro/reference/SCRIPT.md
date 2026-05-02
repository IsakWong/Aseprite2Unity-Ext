# Aseprite2Unity-Ext 介绍视频 — 脚本

> 视频规格：1920×1080, 30fps, 总时长约 90 秒
> 受众：Aseprite 像素动画师、2D 游戏开发者
> 风格：深色背景 + 产品界面演示 + 关键信息字幕，减少代码展示

---

## Scene 0 · 开场标题 | 0s – 6s (180 帧)

**画面**：深色渐变背景 + 像素风格彩色方块装饰 + 标题文字渐入
**旁白**：
> 「Aseprite2Unity-Ext —— 将 Aseprite 像素动画无缝导入 Unity 的编辑器插件。」

**视觉元素**：
- 彩色像素方块逐个弹入（紫 / 浅紫 / 青 / 粉）
- 大标题：Aseprite2Unity-Ext（88px, 白色, 发光阴影）
- 副标题：Unity 精灵动画导入工具（36px, 浅紫色）
- 底部标签：v2.0.0 · MIT License · 开源免费

---

## Scene 1 · 扩展能力演示：自动阴影 | 6s – 22s (480 帧)

**画面**：全屏播放自动阴影 DEMO 视频 (`scene1_shadow_demo.mp4`)
**旁白**：
> 「先来看一个实际例子。这是用 Aseprite2Unity-Ext 开发的一个扩展：在 Aseprite 中为角色划定阴影区域，导入 Unity 后自动计算并生成独立的阴影 Sprite。不需要手动处理，一切自动化。」

**视觉元素**：
- 全屏演示视频渐入
- 底部渐变遮罩保留字幕空间
- 演示内容：Aseprite 中标记阴影区域 → Unity 中自动生成阴影 Sprite，跟随角色动画自动更新

---

## Scene 2 · 工作流痛点 | 22s – 30s (240 帧)

**画面**：左右分屏——左侧播放 Aseprite 中正在创作的像素动画 (`scene2_aseprite.mp4`)，右侧 Unity 空项目（虚线框占位）
**旁白**：
> 「Aseprite 是像素动画师的首选工具。你在 Aseprite 中创作了精美的动画角色，但导入 Unity 却成了噩梦。
>  导出帧、创建 Sprite、拼 Animation Clip、搭 Animator Controller……每一步都是重复劳动。」

**视觉元素**：
- 左面板：Aseprite 中带逐帧动画的角色
- 右面板：虚线边框 + 问号占位，文字「逐帧动画导入流程复杂」
- 底部渐显步骤标签：导出帧 → Sprite → Animation Clip → Animator

---

## Scene 3 · 一键导入 | 30s – 43s (390 帧)

**画面**：全屏播放 Unity 导入演示视频 (`scene2_import_to_unity.mp4`)，展示将带动画的 Aseprite 资源导入 Unity 后自动创建 AnimatorController 的完整效果
**旁白**：
> 「Aseprite2Unity-Ext 解决了这一切。只需将 .aseprite 文件拖入 Unity，就能自动生成 Sprite、Animation Clip、Animator Controller 和 Prefab。
>  支持全部二十多种图层混合模式——正片叠底、叠加、颜色减淡等——通过 Burst 编译器实现高性能像素合成。」

**视觉元素**：
- 全屏导入演示视频，展示 .aseprite 拖入 Unity 后自动弹出 AnimatorController 等资源
- 右上角依次弹出生成物标签（带弹性动画）：Sprite → Animation Clip → Animator Controller → Prefab
- 底部渐变遮罩

---

## Scene 4 · Sprite Atlas 图集 | 43s – 51s (240 帧)

**画面**：上方图集网格可视化 + 下方 Draw Call 对比数字动画
**旁白**：
> 「性能方面，Sprite Atlas 可以将所有动画帧自动合并到一张纹理中，大幅减少 Draw Call。
>  系统自动计算最优行列布局使图集接近正方形，你也可以按需调整边距。」

**视觉元素**：
- 5×3 彩色方块网格，单元格逐一亮起
- Draw Call 数字动画：12 → 1（对比色展示）
- 底部简短配置提示（2 行，非大段代码）

---

## Scene 5 · 处理器管线 | 51s – 63s (360 帧)

**画面**：管道流程图 + 特性卡片
**旁白**：
> 「最强大的特性是可扩展的处理器管线。你可以在不修改插件源码的情况下，添加自己的导入处理逻辑。
>  系统通过反射自动发现所有处理器，按顺序依次执行，处理器之间还能通过 SharedData 共享数据。」

**视觉元素**：
- 横向管道动图：Aseprite 文件 → 处理器1 → 处理器2 → 处理器3 → 导入产物
- 三个特性卡片逐一滑入：反射自动发现（无需注册）· ProcessOrder（控制顺序）· SharedData（处理器间通信）
- 无代码展示（仅视觉示意图）

---

## Scene 6 · 灵活配置 + 自定义导入 | 63s – 75s (360 帧)

**画面**：上下布局——上方展示全局/单文件配置体系，下方展示覆写 Importer 自定义 Prefab 的流程
**旁白**：
> 「Aseprite2Unity-Ext 提供了灵活的配置系统。你可以创建全局 ScriptableObject 配置，统一控制项目中所有导入的 PPU、帧率、图集等参数；
>  也可以在单个 .aseprite 文件的 Inspector 面板中覆盖这些设置。
>  更进一步，你还可以继承 AsepriteImporter 基类，覆写导入逻辑，定制生成 Prefab 的结构——比如自动添加碰撞体、挂载脚本或调整组件布局。」

**视觉元素**：
- 上方：全局配置（ScriptableObject 图标 + "统一管理 PPU / 帧率 / 图集"）→ 单文件 Override（Inspector 面板图标 + "按需覆盖"），箭头连接表示优先级
- 下方：AsepriteImporter 基类 → 自定义 Importer → 定制化 Prefab 示意图（带自定义组件标注）
- 无大段代码

---

## Scene 7 · 实战：SliceCollider | 75s – 85s (300 帧)

**画面**：Aseprite 切片 → Unity 碰撞体的转化示意图 + 特性列表
**旁白**：
> 「来看一个实际的扩展示例。在 Aseprite 中命名为 'unity:collider' 的切片，
>  导入后自动转换为 Unity 的 BoxCollider2D 组件。
>  这就是 Aseprite2Unity-Ext 的扩展能力——覆写一个方法，加一点自定义 UI，就能在不影响原有逻辑的前提下增加新功能。」

**视觉元素**：
- 左侧卡片：Aseprite 切片（图标 + 名称 "unity:collider"）
- 箭头动画连接
- 右侧卡片：Unity 组件（图标 + BoxCollider2D）
- 下方三个 ✓ 特性标签：覆写 VisitSliceChunk · 自定义 Inspector UI · 保持原有逻辑不变
- 无大段代码

---

## Scene 8 · 结尾 | 85s – 94s (270 帧)

**画面**：深色背景 + GitHub 链接卡片 + 标签 + Logo
**旁白**：
> 「Aseprite2Unity-Ext 是开源项目，基于 MIT License。支持 Unity 2020.3 及以上版本。
>  欢迎访问 GitHub 获取最新版本，也欢迎提交 Issue 和 Pull Request。」

**视觉元素**：
- 标题：「开始使用 Aseprite2Unity-Ext」
- GitHub 链接卡片（带 SVG 图标）：isakwong/Aseprite2Unity-Ext
- 四个彩色标签：MIT License · Unity 2020.3+ · 开源免费 · 持续更新
- 底部 Logo 渐入

---

## 素材对应表

| 场景 | 视觉素材 | 类型 |
|------|----------|------|
| Scene 0 | 纯 CSS 动画 | 代码生成 |
| Scene 1 | `scene1_shadow_demo.mp4` | 视频 — 插件扩展自动阴影 DEMO |
| Scene 2 | `scene2_aseprite.mp4` | 视频 — Aseprite 中带动画的角色 |
| Scene 3 | `scene2_import_to_unity.mp4` | 视频 — 导入 Unity 自动创建 AnimatorController |
| Scene 4 | CSS 网格 + 数字动画 | 代码生成 |
| Scene 5 | CSS 管道流程图 | 代码生成 |
| Scene 6 | CSS 配置系统示意图 + Importer 继承流程图 | 代码生成 |
| Scene 7 | CSS 转化示意图 | 代码生成 |
| Scene 8 | `LOGO.png` | 图片素材 |
