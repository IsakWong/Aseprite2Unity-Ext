Slice Collider Sample
=====================

演示如何通过继承 AsepriteImporter 创建一个「Override Importer」，
并在导入 .aseprite 时把 Aseprite slice 自动转换为 Unity 的 BoxCollider2D。

使用方法
--------
1. 导入此 Sample。SliceColliderAsepriteImporter 通过 overrideExts 登记，
   不会自动接管资源。
2. 打开菜单 Tools → Aseprite2Unity → Select Importer...，选中
   SliceColliderAsepriteImporter 并点击 "Apply To All"，把项目里
   所有 .ase / .aseprite 资源切到本子类。
   （或在单个资源 Inspector 顶部 "Importer" 下拉里就地切换。）
3. 本目录自带的 Idle.aseprite 已经包含若干 unity:collider 命名的 slice，
   可直接在 Inspector 中查看效果。
4. 如需在自己的资源中使用，请在 Aseprite 里新建若干 Slice，并以
   "unity:collider" 开头命名，例如：
     - unity:collider
     - unity:collider_body
     - unity:collider_feet
5. （可选）添加 "unity:pivot" slice 以指定 sprite pivot；collider 会以此 pivot
   作为本地坐标原点对齐。
6. 重新导入资源，主 GameObject 上会出现对应数量的 BoxCollider2D。
7. 如需切回默认 importer，再次打开 Select Importer 窗口选 AsepriteImporter
   并 Apply To All 即可。

注意事项
--------
- Slice 的位置以第一个 key（FrameNumber 最小者）为准，作为静态 collider 使用。
  如需逐帧动态 collider，可参考 SliceColliderAsepriteImporter.VisitSliceChunk
  中的 Entries 遍历方式自行扩展。
- Inspector 顶部可切换是否生成 collider、是否设为 Trigger。
