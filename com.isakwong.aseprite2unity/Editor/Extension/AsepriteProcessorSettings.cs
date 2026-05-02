using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite Processor 的 Per-Asset 配置基类。
    /// 通过 [SerializeReference] 存储在 AsepriteImporter 上（.meta 文件），实现逐资源覆盖。
    ///
    /// 设计约定：
    /// - 每个 AsepriteProcessor 子类关联一个 AsepriteProcessorSettings 子类
    /// - Settings 只包含纯数据字段（[SerializeField]），不包含逻辑
    /// - Processor 通过泛型基类 AsepriteProcessor&lt;TSettings&gt; 自动获取对应的 Settings 实例
    /// </summary>
    [System.Serializable]
    public abstract class AsepriteProcessorSettings
    {
        /// <summary>
        /// Inspector 中显示的名称。
        /// </summary>
        public virtual string DisplayName => GetType().Name;
    }
}
