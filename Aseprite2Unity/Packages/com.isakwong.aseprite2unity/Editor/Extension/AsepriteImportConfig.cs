using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite 导入全局配置（插件级）。
    /// 仅包含所有 Processor 共享的通用默认值。
    /// 游戏专属配置请使用业务层的 JasaImportConfig。
    /// 
    /// 创建方式：Assets → Create → Aseprite2Unity → Import Config
    /// 推荐路径：Assets/Config/AsepriteImportConfig.asset
    /// </summary>
    [CreateAssetMenu(fileName = "AsepriteImportConfig", menuName = "Aseprite2Unity/Import Config")]
    public class AsepriteImportConfig : ScriptableObject
    {
        [Tooltip("默认材质（用于 SpriteRenderer）")]
        public Material DefaultMaterial;

        // ---- 单例查找 ----

        private static AsepriteImportConfig _cachedInstance;

        /// <summary>
        /// 获取全局配置实例。
        /// 自动从项目中查找唯一的 AsepriteImportConfig 资源。
        /// 找不到时返回 null。
        /// </summary>
        public static AsepriteImportConfig Find()
        {
            if (_cachedInstance != null)
                return _cachedInstance;

#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:AsepriteImportConfig");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                _cachedInstance = UnityEditor.AssetDatabase.LoadAssetAtPath<AsepriteImportConfig>(path);

                if (guids.Length > 1)
                {
                    Debug.LogWarning($"[AsepriteImportConfig] Found {guids.Length} AsepriteImportConfig assets. Using: {path}");
                }
            }
#endif

            return _cachedInstance;
        }

        public static void ClearCache()
        {
            _cachedInstance = null;
        }
    }
}
