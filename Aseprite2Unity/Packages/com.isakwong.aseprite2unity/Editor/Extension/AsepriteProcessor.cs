using System;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite 导入处理器基类。
    /// 纯逻辑，不持有序列化数据——Per-Asset 配置通过关联的 AsepriteProcessorSettings 子类存储。
    ///
    /// 两种继承方式：
    /// 1. AsepriteProcessor&lt;TSettings&gt; —— 需要 Per-Asset 配置的处理器（推荐）
    /// 2. AsepriteProcessor —— 无需配置的轻量处理器
    ///
    /// 处理器支持三种互补的处理模式：
    /// 1. 重写 TryCreateImportedGameObject() —— 在导入早期参与主 GameObject 创建
    /// 2. 重写 OnImportAseprite() —— 基于导入结果（Sprites、Clips 等）
    /// 3. 重写 Visit*() 方法 —— Visitor 模式遍历原始 Aseprite 数据
    ///
    /// 执行顺序：OnImportAseprite() 先于 Visit*() 遍历。
    /// </summary>
    public abstract class AsepriteProcessor
    {
        /// <summary>Inspector 中显示的名称</summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>执行顺序，值越小越先执行。默认 0。</summary>
        public virtual int ProcessOrder => 0;

        /// <summary>关联的 Settings 类型。无配置的处理器返回 null。</summary>
        public virtual Type SettingsType => null;

        /// <summary>是否需要 Visitor 模式遍历 AseFile Chunks</summary>
        public virtual bool NeedVisitChunks => false;

        /// <summary>
        /// 在导入早期尝试创建主 GameObject。
        /// 返回 null 表示跳过，交由后续 Processor 或 Importer 自身兜底。
        /// </summary>
        public virtual GameObject TryCreateImportedGameObject(AssetImportContext ctx, AsepriteImporter importer)
        {
            return null;
        }

        /// <summary>
        /// 导入时调用。重写此方法添加自定义处理逻辑。
        /// </summary>
        public abstract void OnImportAseprite(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result);

        /// <summary>
        /// 在 OnImportAseprite 前调用，返回 false 跳过此处理器。
        /// </summary>
        public virtual bool ShouldProcess(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
        {
            return true;
        }

        /// <summary>
        /// 处理出错时的回调。
        /// </summary>
        public virtual void OnProcessError(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result, Exception exception)
        {
            Debug.LogError($"[{GetType().Name}] Error processing {ctx.assetPath}: {exception.Message}\n{exception.StackTrace}");
        }

        /// <summary>
        /// 前置创建主 GameObject 失败时的回调。
        /// </summary>
        public virtual void OnCreateGameObjectError(AssetImportContext ctx, AsepriteImporter importer, Exception exception)
        {
            Debug.LogError($"[{GetType().Name}] Error creating root GameObject for {ctx.assetPath}: {exception.Message}\n{exception.StackTrace}");
        }

        /// <summary>获取全局导入配置（可能为 null）</summary>
        protected static AsepriteImportConfig GlobalConfig => AsepriteImportConfig.Find();

        // ---- 内部：供 Registry 注入当前 Settings 实例 ----
        internal virtual void InjectSettings(AsepriteProcessorSettings settings) { }

        // ---- Visitor 回调 ----

        public virtual void BeginFileVisit(AseFile file, AsepriteImportResult result) { }
        public virtual void EndFileVisit(AseFile file, AsepriteImportResult result) { }
        public virtual void BeginFrameVisit(AseFrame frame, int frameIndex, AsepriteImportResult result) { }
        public virtual void EndFrameVisit(AseFrame frame, int frameIndex, AsepriteImportResult result) { }
        public virtual void VisitLayerChunk(AseLayerChunk layer, AsepriteImportResult result) { }
        public virtual void VisitCelChunk(AseCelChunk cel, AsepriteImportResult result) { }
        public virtual void VisitFrameTagsChunk(AseFrameTagsChunk frameTags, AsepriteImportResult result) { }
        public virtual void VisitPaletteChunk(AsePaletteChunk palette, AsepriteImportResult result) { }
        public virtual void VisitUserDataChunk(AseUserDataChunk userData, AsepriteImportResult result) { }
        public virtual void VisitSliceChunk(AseSliceChunk slice, AsepriteImportResult result) { }
        public virtual void VisitTilesetChunk(AseTilesetChunk tileset, AsepriteImportResult result) { }

        /// <summary>
        /// 执行 Visitor 遍历。由 Registry 在 OnImportAseprite() 之后调用。
        /// 通常不需要重写，重写各个 Visit*() 方法即可。
        /// </summary>
        internal void PerformChunkVisit(AsepriteImportResult result)
        {
            var aseFile = result.AseFile;
            if (aseFile == null) return;

            BeginFileVisit(aseFile, result);

            for (int frameIndex = 0; frameIndex < aseFile.Frames.Count; frameIndex++)
            {
                var frame = aseFile.Frames[frameIndex];
                BeginFrameVisit(frame, frameIndex, result);

                foreach (var chunk in frame.Chunks)
                {
                    switch (chunk)
                    {
                        case AseLayerChunk layer: VisitLayerChunk(layer, result); break;
                        case AseCelChunk cel: VisitCelChunk(cel, result); break;
                        case AseFrameTagsChunk frameTags: VisitFrameTagsChunk(frameTags, result); break;
                        case AsePaletteChunk palette: VisitPaletteChunk(palette, result); break;
                        case AseUserDataChunk userData: VisitUserDataChunk(userData, result); break;
                        case AseSliceChunk slice: VisitSliceChunk(slice, result); break;
                        case AseTilesetChunk tileset: VisitTilesetChunk(tileset, result); break;
                    }
                }

                EndFrameVisit(frame, frameIndex, result);
            }

            EndFileVisit(aseFile, result);
        }
    }

    /// <summary>
    /// 带 Per-Asset 配置的处理器泛型基类。
    /// TSettings 的实例通过 [SerializeReference] 存储在每个 .aseprite 资源的 .meta 文件中，
    /// 在 Inspector 中可逐资源编辑。
    ///
    /// 用法示例：
    /// <code>
    /// [Serializable]
    /// public class MySettings : AsepriteProcessorSettings
    /// {
    ///     public bool enabled = true;
    ///     public string sortingLayer = "Default";
    /// }
    ///
    /// public class MyProcessor : AsepriteProcessor&lt;MySettings&gt;
    /// {
    ///     public override void OnImportAseprite(...) {
    ///         if (!Settings.enabled) return;
    ///         // 使用 Settings.sortingLayer ...
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="TSettings">关联的配置类型</typeparam>
    public abstract class AsepriteProcessor<TSettings> : AsepriteProcessor
        where TSettings : AsepriteProcessorSettings, new()
    {
        /// <summary>
        /// 当前资源的配置实例。
        /// 由 Registry 在每次导入前注入，保证在 ShouldProcess / OnImportAseprite / Visit* 中可用。
        /// 如果 Importer 上尚无此类型的 Settings，则使用 new TSettings() 默认值。
        /// </summary>
        public TSettings Settings { get; private set; } = new TSettings();

        public sealed override Type SettingsType => typeof(TSettings);

        internal sealed override void InjectSettings(AsepriteProcessorSettings settings)
        {
            Settings = settings as TSettings ?? new TSettings();
        }
    }
}
