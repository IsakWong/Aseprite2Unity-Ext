using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite 导入全局配置（插件级）。
    /// 包含所有 Processor 共享的通用默认值。
    /// 游戏专属配置请使用业务层的 JasaImportConfig。
    ///
    /// 创建方式：Project Settings → Aseprite2Unity（自动创建）
    /// 或手动：Assets → Create → Aseprite2Unity → Import Config
    /// 推荐路径：Assets/Config/AsepriteImportConfig.asset
    /// </summary>
    [CreateAssetMenu(fileName = "AsepriteImportConfig", menuName = "Aseprite2Unity/Import Config")]
    public class AsepriteImportConfig : ScriptableObject
    {
        // ---- 渲染 ----

        [Header("渲染")]
        [Tooltip("默认材质（用于 SpriteRenderer）")]
        public Material DefaultMaterial;

        // ---- 导入默认值 ----

        [Header("导入默认值")]
        [Tooltip("默认每单位像素数")]
        public float DefaultPixelsPerUnit = 16f;

        [Tooltip("默认动画帧率")]
        public float DefaultFrameRate = 60f;

        // ---- Atlas ----

        [Header("Atlas")]
        [Tooltip("默认是否将所有帧合并为一张图集")]
        public bool DefaultCreateAtlas = true;

        [Tooltip("图集中帧之间的默认间距（像素）")]
        [Min(0)]
        public int DefaultAtlasPadding = 0;

        // ---- 动画 ----

        [Header("动画")]
        [Tooltip("默认是否创建 AnimationClip")]
        public bool DefaultCreateAnimations = true;

        // ================================================================
        //  单例查找
        // ================================================================

        private static AsepriteImportConfig _cachedInstance;

        private const string DefaultConfigPath = "Assets/Config/AsepriteImportConfig.asset";

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

        /// <summary>
        /// 获取全局配置，不存在时自动创建。
        /// </summary>
        public static AsepriteImportConfig GetOrCreate()
        {
            var config = Find();
            if (config != null)
                return config;

#if UNITY_EDITOR
            // 确保目录存在
            var directory = System.IO.Path.GetDirectoryName(DefaultConfigPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                UnityEditor.AssetDatabase.Refresh();
            }

            config = CreateInstance<AsepriteImportConfig>();
            UnityEditor.AssetDatabase.CreateAsset(config, DefaultConfigPath);
            UnityEditor.AssetDatabase.SaveAssets();
            _cachedInstance = config;
            Debug.Log($"[AsepriteImportConfig] Created global config at: {DefaultConfigPath}");
#endif

            return config;
        }

        public static void ClearCache()
        {
            _cachedInstance = null;
        }
    }
}
