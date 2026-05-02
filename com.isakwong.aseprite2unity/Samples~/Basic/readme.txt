Basic Sample
============

最小可用示例：使用插件自带的默认 AsepriteImporter 直接导入 .aseprite。
不需要写任何代码，也无需子类。

使用方法
--------
1. 通过 Package Manager 导入此 Sample。
2. 选中目录里自带的 Idle.aseprite，Inspector 顶部即为默认 AsepriteImporter。
3. 双击或拖到场景中即可看到导入产物：
     - 主 GameObject（带 SpriteRenderer，已绑定第一帧）
     - AnimatorController + AnimationClip（按 Aseprite 中的 Tag 自动生成）
     - 切片好的 Sprite 列表（在资源子项下展开可见）
4. 在 Inspector 内可调整：
     - Pixels Per Unit / Frame Rate
     - Sorting Layer / Sorting Order
     - Atlas / Material / Animation 相关开关
   修改后点 Apply 重新导入。

进阶
----
- 在 Aseprite 中新建名为 "unity:pivot" 的 Slice，可指定 sprite pivot
  （取该 slice 的中心作为 pivot，支持半像素精度）。
- 想要自动生成 BoxCollider2D，请改用 "Slice Collider" Sample。
