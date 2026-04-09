using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// 在 Project Settings 中注册 Aseprite2Unity 设置面板。
    /// 自动查找或创建 AsepriteImportConfig 资源并以 SerializedObject 方式编辑。
    /// </summary>
    public class Aseprite2UnitySettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedConfig;
        private AsepriteImportConfig _config;
        private Vector2 _processorScrollPos;

        private Aseprite2UnitySettingsProvider(string path, SettingsScope scope)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            _config = AsepriteImportConfig.GetOrCreate();
            if (_config != null)
                _serializedConfig = new SerializedObject(_config);
        }

        public override void OnDeactivate()
        {
            if (_serializedConfig != null && _serializedConfig.hasModifiedProperties)
            {
                _serializedConfig.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        public override void OnGUI(string searchContext)
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox("无法找到或创建 AsepriteImportConfig 资源。", MessageType.Error);
                if (GUILayout.Button("重新创建配置"))
                {
                    AsepriteImportConfig.ClearCache();
                    _config = AsepriteImportConfig.GetOrCreate();
                    if (_config != null)
                        _serializedConfig = new SerializedObject(_config);
                }
                return;
            }

            // 如果外部替换了 config 资产，需要刷新
            if (_serializedConfig == null || _serializedConfig.targetObject == null)
            {
                _serializedConfig = new SerializedObject(_config);
            }

            _serializedConfig.Update();

            EditorGUILayout.Space(4);

            // ── 版本信息 ──
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"Aseprite2Unity-Ext  v{Config.Version}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("定位配置文件", GUILayout.Width(100)))
                {
                    EditorGUIUtility.PingObject(_config);
                    Selection.activeObject = _config;
                }
            }

            EditorGUILayout.Space(2);
            DrawUILine();
            EditorGUILayout.Space(4);

            // ── 配置字段 ──
            DrawConfigSection();

            EditorGUILayout.Space(8);
            DrawUILine();
            EditorGUILayout.Space(4);

            // ── 处理器列表 ──
            DrawProcessorSection();

            EditorGUILayout.Space(8);
            DrawUILine();
            EditorGUILayout.Space(4);

            // ── 操作 ──
            DrawActionsSection();

            _serializedConfig.ApplyModifiedProperties();
        }

        // ================================================================
        //  绘制各区段
        // ================================================================

        private void DrawConfigSection()
        {
            EditorGUILayout.LabelField("全局导入配置", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            // 渲染
            EditorGUILayout.LabelField("渲染", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("DefaultMaterial"),
                new GUIContent("默认材质", "SpriteRenderer 使用的默认材质"));

            EditorGUILayout.Space(4);

            // 导入默认值
            EditorGUILayout.LabelField("导入默认值", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("DefaultPixelsPerUnit"),
                new GUIContent("Pixels Per Unit", "默认每单位像素数"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("DefaultFrameRate"),
                new GUIContent("帧率", "默认动画帧率"));

            EditorGUILayout.Space(4);

            // Atlas
            EditorGUILayout.LabelField("Atlas", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("DefaultCreateAtlas"),
                new GUIContent("创建合图", "是否默认将所有帧合并为图集"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("DefaultAtlasPadding"),
                new GUIContent("合图间距", "图集帧之间的间距（像素）"));

            EditorGUILayout.Space(4);

            // 动画
            EditorGUILayout.LabelField("动画", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("DefaultCreateAnimations"),
                new GUIContent("创建动画", "是否默认生成 AnimationClip"));
        }

        private void DrawProcessorSection()
        {
            EditorGUILayout.LabelField("已注册处理器", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var processors = AsepriteProcessorRegistry.Processors;
            if (processors == null || processors.Count == 0)
            {
                EditorGUILayout.HelpBox("未发现任何 AsepriteProcessor。\n继承 AsepriteProcessor 并实现 OnImportAseprite 即可自动注册。", MessageType.Info);
                return;
            }

            _processorScrollPos = EditorGUILayout.BeginScrollView(_processorScrollPos, GUILayout.MaxHeight(200));

            var headerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorStyles.boldLabel.normal.textColor }
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("顺序", headerStyle, GUILayout.Width(40));
                GUILayout.Label("名称", headerStyle, GUILayout.Width(200));
                GUILayout.Label("类型", headerStyle);
            }

            foreach (var processor in processors)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(processor.ProcessOrder.ToString(), GUILayout.Width(40));
                    GUILayout.Label(processor.DisplayName, GUILayout.Width(200));
                    GUILayout.Label(processor.GetType().FullName, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("重新导入所有 Aseprite 文件", GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("确认重新导入",
                        "这将重新导入项目中所有 .aseprite 和 .ase 文件。\n此操作可能需要较长时间，确定继续？",
                        "确定", "取消"))
                    {
                        ReimportAllAsepriteAssets();
                    }
                }

                if (GUILayout.Button("刷新处理器列表", GUILayout.Height(24), GUILayout.Width(120)))
                {
                    AsepriteProcessorRegistry.Reload();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开引导窗口", GUILayout.Height(24)))
                {
                    Aseprite2UnityWelcomeWindow.ShowWindow();
                }
            }
        }

        // ================================================================
        //  工具方法
        // ================================================================

        private static void ReimportAllAsepriteAssets()
        {
            var guids = AssetDatabase.FindAssets("glob:\"*.aseprite\" glob:\"*.ase\"");
            if (guids.Length == 0)
            {
                // FindAssets glob 语法可能不支持，回退到遍历
                var aseFiles = new List<string>();
                foreach (var guid in AssetDatabase.FindAssets(""))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".aseprite") || path.EndsWith(".ase"))
                        aseFiles.Add(path);
                }
                guids = null; // 使用 aseFiles

                if (aseFiles.Count == 0)
                {
                    Debug.Log("[Aseprite2Unity] 未找到任何 Aseprite 文件。");
                    return;
                }

                try
                {
                    AssetDatabase.StartAssetEditing();
                    for (int i = 0; i < aseFiles.Count; i++)
                    {
                        EditorUtility.DisplayProgressBar("重新导入 Aseprite",
                            $"({i + 1}/{aseFiles.Count}) {aseFiles[i]}",
                            (float)i / aseFiles.Count);
                        AssetDatabase.ImportAsset(aseFiles[i], ImportAssetOptions.ForceUpdate);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    EditorUtility.ClearProgressBar();
                }

                Debug.Log($"[Aseprite2Unity] 已重新导入 {aseFiles.Count} 个 Aseprite 文件。");
                return;
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("重新导入 Aseprite",
                        $"({i + 1}/{guids.Length}) {path}",
                        (float)i / guids.Length);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[Aseprite2Unity] 已重新导入 {guids.Length} 个 Aseprite 文件。");
        }

        private static void DrawUILine(int thickness = 1, int padding = 4)
        {
            var rect = EditorGUILayout.GetControlRect(false, thickness + padding);
            rect.height = thickness;
            rect.y += padding * 0.5f;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        // ================================================================
        //  注册
        // ================================================================

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new Aseprite2UnitySettingsProvider("Project/Aseprite2Unity", SettingsScope.Project)
            {
                keywords = new HashSet<string>(new[]
                {
                    "Aseprite", "Import", "Sprite", "Atlas", "Animation", "Pixel", "导入", "合图"
                })
            };
        }
    }
}
