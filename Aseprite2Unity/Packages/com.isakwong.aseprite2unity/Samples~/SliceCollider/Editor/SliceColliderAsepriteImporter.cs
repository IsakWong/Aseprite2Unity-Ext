using System;
using System.Collections.Generic;
using Aseprite2Unity.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Samples.SliceCollider
{
    /// <summary>
    /// Sample 子类 Importer：演示如何继承 AsepriteImporter 并把它登记为「Override Importer」。
    /// 在导入时把名为 <c>unity:collider</c>（可加任意后缀，如 <c>unity:collider_feet</c>）
    /// 的 Aseprite slice 转换成 <see cref="BoxCollider2D"/> 挂到主 GameObject 上。
    ///
    /// 注册方式：
    /// 使用 <c>overrideExts</c>（ScriptedImporter 第三参数）登记子类，
    /// 不会与基类的默认 [ScriptedImporter] 冲突。
    /// 安装本 Sample 后，请通过菜单
    ///   Tools → Aseprite2Unity → Select Importer...
    /// 选中 SliceColliderAsepriteImporter 并点击 "Apply To All"，
    /// 项目中所有 .ase / .aseprite 资源会被批量切到本子类。
    /// 单个资源也可在 Inspector 顶部 "Importer" 下拉就地切换。
    ///
    /// 关键覆写：
    /// - <see cref="VisitSliceChunk"/>：收集 collider slices；调用 base 保留 unity:pivot 解析逻辑。
    /// - <see cref="EndFileVisit"/>：在 base 处理完 SpriteRenderer / Animator 后，
    ///   根据已解析的 pivot 与 PixelsPerUnit 把 slice 转成本地坐标的 BoxCollider2D。
    /// </summary>
    [ScriptedImporter(1, null, new[] { "ase", "aseprite" })]
    public class SliceColliderAsepriteImporter : AsepriteImporter
    {
        /// <summary>Slice 名称前缀（不区分大小写）。匹配的 slice 会被转成 BoxCollider2D。</summary>
        public const string ColliderSlicePrefix = "unity:collider";

        [Header("Slice Collider")]
        [Tooltip("启用后会扫描所有以 'unity:collider' 开头的 slice 并创建 BoxCollider2D。")]
        public bool m_CreateColliders = true;

        [Tooltip("生成的 BoxCollider2D 是否设为 Trigger。")]
        public bool m_IsTrigger = false;

        private readonly List<(string name, Rect rectAse)> m_PendingColliders = new List<(string, Rect)>();

        public override void BeginFileVisit(AseFile file)
        {
            base.BeginFileVisit(file);
            m_PendingColliders.Clear();
        }

        public override void VisitSliceChunk(AseSliceChunk slice)
        {
            // 保留基类对 unity:pivot 等的处理
            base.VisitSliceChunk(slice);

            if (!m_CreateColliders) return;
            if (slice == null || string.IsNullOrEmpty(slice.Name)) return;
            if (slice.Entries == null || slice.Entries.Count == 0) return;
            if (!slice.Name.StartsWith(ColliderSlicePrefix, StringComparison.OrdinalIgnoreCase)) return;

            // 取第一个 key 作为静态 collider；如需逐帧 collider 可扩展遍历 Entries
            var entry = slice.Entries[0];
            m_PendingColliders.Add((slice.Name,
                new Rect(entry.OriginX, entry.OriginY, entry.Width, entry.Height)));
        }

        public override void EndFileVisit(AseFile file)
        {
            // 先让基类完成 SpriteRenderer / Animator / 子物体等装配
            base.EndFileVisit(file);

            if (!m_CreateColliders || m_PendingColliders.Count == 0) return;
            if (ImportedGameObject == null) return;

            float ppu = EffectivePixelsPerUnit;
            if (ppu <= 0f) ppu = 1f;

            int cw = CanvasWidth;
            int ch = CanvasHeight;

            // pivot 默认中心；与基类 BuildSprite 保持一致
            var pivotNorm = ResolvedPivot ?? new Vector2(0.5f, 0.5f);

            // pivot 在 Aseprite 像素坐标系（原点左上、Y 朝下）下的位置
            float pivotPxX = pivotNorm.x * cw;
            float pivotPxYTop = (1f - pivotNorm.y) * ch;

            foreach (var (name, rect) in m_PendingColliders)
            {
                // slice 中心（Aseprite 像素坐标）
                float cxAse = rect.x + rect.width * 0.5f;
                float cyAse = rect.y + rect.height * 0.5f;

                // 以 pivot 为原点、转换到 Unity 本地坐标（Y 朝上），再除以 PPU
                var offset = new Vector2(
                    (cxAse - pivotPxX) / ppu,
                    (pivotPxYTop - cyAse) / ppu);
                var size = new Vector2(rect.width / ppu, rect.height / ppu);

                var box = ImportedGameObject.AddComponent<BoxCollider2D>();
                box.offset = offset;
                box.size = size;
                box.isTrigger = m_IsTrigger;
            }

            m_PendingColliders.Clear();
        }
    }
}
