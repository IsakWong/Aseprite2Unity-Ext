using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite 导入结果上下文对象。
    /// 封装导入过程产生的所有数据，供 AsepriteProcessor 在扩展流程中使用。
    /// 提供 SharedData 字典，允许不同 Processor 之间共享自定义中间数据。
    /// </summary>
    public class AsepriteImportResult
    {
        /// <summary>
        /// Unity 资源导入上下文，Processor 可通过此对象调用 AddObjectToAsset 添加子资源。
        /// </summary>
        public AssetImportContext Context { get; }

        /// <summary>
        /// Aseprite 导入器实例，可读取 PixelsPerUnit、FrameRate 等导入设置。
        /// </summary>
        public AsepriteImporter Importer { get; }

        /// <summary>
        /// 完整的 Aseprite 文件解析结果。
        /// 可通过 AseFile.Frames 遍历所有帧，每帧的 Chunks 包含所有原始 Chunk 数据。
        /// </summary>
        public AseFile AseFile { get; }

        /// <summary>
        /// 导入生成的所有 Sprite（按帧顺序排列）。
        /// </summary>
        public IReadOnlyList<Sprite> Sprites { get; }

        /// <summary>
        /// 导入生成的所有 AnimationClip。
        /// </summary>
        public IReadOnlyList<AnimationClip> AnimationClips { get; }

        /// <summary>
        /// 导入过程中解析的所有帧数据。
        /// </summary>
        public IReadOnlyList<AseFrame> Frames { get; }

        /// <summary>
        /// 导入过程中解析的所有图层数据。
        /// </summary>
        public IReadOnlyList<AseLayerChunk> LayerChunks { get; }

        /// <summary>
        /// 帧标签数据（动画标记），可能为 null（如果 Aseprite 文件中未定义 Tag）。
        /// </summary>
        public AseFrameTagsChunk FrameTags { get; }

        /// <summary>
        /// 导入生成的主 GameObject。
        /// </summary>
        public GameObject GameObject { get; }

        /// <summary>
        /// Processor 之间共享的自定义数据字典。
        /// 可用于在不同 Processor 之间传递中间结果。
        /// </summary>
        public Dictionary<string, object> SharedData { get; } = new Dictionary<string, object>();

        public AsepriteImportResult(
            AssetImportContext context,
            AsepriteImporter importer,
            AseFile aseFile,
            IReadOnlyList<Sprite> sprites,
            IReadOnlyList<AnimationClip> animationClips,
            IReadOnlyList<AseFrame> frames,
            IReadOnlyList<AseLayerChunk> layerChunks,
            AseFrameTagsChunk frameTags,
            GameObject gameObject)
        {
            Context = context;
            Importer = importer;
            AseFile = aseFile;
            Sprites = sprites;
            AnimationClips = animationClips;
            Frames = frames;
            LayerChunks = layerChunks;
            FrameTags = frameTags;
            GameObject = gameObject;
        }

        /// <summary>
        /// 获取共享数据（泛型便捷方法）。
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键名</param>
        /// <returns>数据值，不存在则返回 default</returns>
        public T GetSharedData<T>(string key)
        {
            if (SharedData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// 设置共享数据。
        /// </summary>
        /// <param name="key">数据键名</param>
        /// <param name="value">数据值</param>
        public void SetSharedData(string key, object value)
        {
            SharedData[key] = value;
        }
    }
}

