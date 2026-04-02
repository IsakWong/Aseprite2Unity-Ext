using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite 导入全局配置。
    /// 提供所有 AsepriteProcessor 共享的默认资源引用。
    /// 
    /// 创建方式：Assets → Create → Aseprite2Unity → Import Config
    /// 推荐路径：Assets/Config/AsepriteImportConfig.asset
    /// </summary>
    [CreateAssetMenu(fileName = "AsepriteImportConfig", menuName = "Aseprite2Unity/Import Config")]
    public class AsepriteImportConfig : ScriptableObject
    {
        [Tooltip("默认材质（用于 SpriteRenderer）")]
        public Material DefaultMaterial;

        [Tooltip("不可破坏障碍物基础预制体（新建障碍物时基于此创建 Prefab Variant）")]
        public GameObject ObstacleBasePrefab;
        
        [Tooltip("可破坏障碍物基础预制体（新建障碍物时基于此创建 Prefab Variant）")]
        public GameObject BreakBasePrefab;

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
