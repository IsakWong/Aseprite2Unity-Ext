using UnityEditor;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite2Unity-Ext 引导窗口。
    /// 提供包信息、快速配置入口、处理器状态概览和入门指南。
    /// </summary>
    public class Aseprite2UnityWelcomeWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        private static class Styles
        {
            public static readonly GUIStyle TitleStyle;
            public static readonly GUIStyle SubtitleStyle;
            public static readonly GUIStyle SectionHeaderStyle;
            public static readonly GUIStyle StatusOkStyle;
            public static readonly GUIStyle StatusWarnStyle;
            public static readonly GUIStyle DescriptionStyle;
            public static readonly GUIStyle StepStyle;

            static Styles()
            {
                TitleStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };

                SubtitleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = Color.gray }
                };

                SectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13
                };

                StatusOkStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
                };

                StatusWarnStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1f, 0.7f, 0.2f) }
                };

                DescriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    richText = true,
                    margin = new RectOffset(8, 8, 4, 4)
                };

                StepStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true,
                    margin = new RectOffset(16, 8, 2, 2)
                };
            }
        }

        [MenuItem("Window/Aseprite2Unity-Ext/Welcome")]
        public static void ShowWindow()
        {
            var window = GetWindow<Aseprite2UnityWelcomeWindow>();
            window.titleContent = new GUIContent("Aseprite2Unity-Ext");
            window.minSize = new Vector2(420, 500);
            window.Show();
        }

        [MenuItem("Window/Aseprite2Unity-Ext/Project Settings")]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/Aseprite2Unity");
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(12);
            DrawHeader();
            EditorGUILayout.Space(8);
            DrawUILine();

            EditorGUILayout.Space(8);
            DrawStatusSection();
            EditorGUILayout.Space(8);
            DrawUILine();

            EditorGUILayout.Space(8);
            DrawQuickActionsSection();
            EditorGUILayout.Space(8);
            DrawUILine();

            EditorGUILayout.Space(8);
            DrawGettingStartedSection();
            EditorGUILayout.Space(8);
            DrawUILine();

            EditorGUILayout.Space(8);
            DrawProcessorSection();
            EditorGUILayout.Space(8);
            DrawUILine();

            EditorGUILayout.Space(8);
            DrawLinksSection();
            EditorGUILayout.Space(12);

            EditorGUILayout.EndScrollView();
        }

        // ================================================================
        //  各区段
        // ================================================================

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("🎨  Aseprite2Unity-Ext", Styles.TitleStyle);
                    GUILayout.Label($"Version {Config.Version}  —  Extended Aseprite Importer for Unity", Styles.SubtitleStyle);
                }
            }
        }

        private void DrawStatusSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("状态", Styles.SectionHeaderStyle);
                    EditorGUILayout.Space(4);

                    // 全局配置状态
                    var config = AsepriteImportConfig.Find();
                    if (config != null)
                    {
                        var path = AssetDatabase.GetAssetPath(config);
                        DrawStatusItem("✓", "全局配置", $"已找到  ({path})", true);
                    }
                    else
                    {
                        DrawStatusItem("✗", "全局配置", "未创建 — 点击下方按钮创建", false);
                    }

                    // 处理器状态
                    var processors = AsepriteProcessorRegistry.Processors;
                    var count = processors?.Count ?? 0;
                    DrawStatusItem(count > 0 ? "✓" : "─", "处理器",
                        $"已注册 {count} 个", count > 0);
                }
            }
        }

        private void DrawStatusItem(string icon, string label, string detail, bool ok)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var style = ok ? Styles.StatusOkStyle : Styles.StatusWarnStyle;
                GUILayout.Label(icon, style, GUILayout.Width(16));
                GUILayout.Label(label, EditorStyles.label, GUILayout.Width(80));
                GUILayout.Label(detail, EditorStyles.miniLabel);
            }
        }

        private void DrawQuickActionsSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("快速操作", Styles.SectionHeaderStyle);
                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("打开 Project Settings", GUILayout.Height(28)))
                        {
                            SettingsService.OpenProjectSettings("Project/Aseprite2Unity");
                        }

                        var config = AsepriteImportConfig.Find();
                        if (config == null)
                        {
                            if (GUILayout.Button("创建全局配置", GUILayout.Height(28)))
                            {
                                AsepriteImportConfig.GetOrCreate();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("定位配置文件", GUILayout.Height(28)))
                            {
                                EditorGUIUtility.PingObject(config);
                                Selection.activeObject = config;
                            }
                        }
                    }
                }
            }
        }

        private void DrawGettingStartedSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("入门指南", Styles.SectionHeaderStyle);
                    EditorGUILayout.Space(4);

                    DrawStep("1", "导入资源",
                        "将 <b>.aseprite</b> 或 <b>.ase</b> 文件拖入项目，插件会自动生成 Sprite 和 AnimationClip。");

                    DrawStep("2", "配置全局设置",
                        "在 <b>Project Settings → Aseprite2Unity</b> 中配置默认材质、像素密度、Atlas 等全局参数。");

                    DrawStep("3", "扩展处理管线",
                        "继承 <b>AsepriteProcessor</b> 并实现 <b>OnImportAseprite</b> 方法，插件会自动发现并执行。");

                    DrawStep("4", "逐资源配置",
                        "在 Inspector 中为单个 Aseprite 文件调整导入设置，覆盖全局默认值。");
                }
            }
        }

        private void DrawStep(string number, string title, string description)
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"<b>{number}. {title}</b>", Styles.StepStyle);
                GUILayout.Label(description, Styles.DescriptionStyle);
            }
        }

        private void DrawProcessorSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("已注册处理器", Styles.SectionHeaderStyle);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("刷新", GUILayout.Width(50)))
                        {
                            AsepriteProcessorRegistry.Reload();
                        }
                    }
                    EditorGUILayout.Space(4);

                    var processors = AsepriteProcessorRegistry.Processors;
                    if (processors == null || processors.Count == 0)
                    {
                        EditorGUILayout.HelpBox(
                            "暂无处理器。继承 AsepriteProcessor 即可自动注册：\n\n" +
                            "public class MyProcessor : AsepriteProcessor\n" +
                            "{\n" +
                            "    public override void OnImportAseprite(...) { }\n" +
                            "}",
                            MessageType.Info);
                    }
                    else
                    {
                        foreach (var processor in processors)
                        {
                            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                            {
                                GUILayout.Label($"[{processor.ProcessOrder}]",
                                    EditorStyles.miniLabel, GUILayout.Width(36));
                                GUILayout.Label(processor.DisplayName, EditorStyles.boldLabel);
                                GUILayout.FlexibleSpace();
                                GUILayout.Label(processor.GetType().Name, EditorStyles.miniLabel);
                            }
                        }
                    }
                }
            }
        }

        private void DrawLinksSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label("链接", Styles.SectionHeaderStyle);
                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("GitHub 仓库", GUILayout.Height(24)))
                        {
                            Application.OpenURL("https://github.com/IsakWong/Aseprite2Unity-Ext");
                        }

                        if (GUILayout.Button("原始项目 (Aseprite2Unity)", GUILayout.Height(24)))
                        {
                            Application.OpenURL("https://github.com/Seanba/Aseprite2Unity");
                        }
                    }
                }
            }
        }

        // ================================================================
        //  工具
        // ================================================================

        private static void DrawUILine(int thickness = 1, int padding = 4)
        {
            var rect = EditorGUILayout.GetControlRect(false, thickness + padding);
            rect.height = thickness;
            rect.y += padding * 0.5f;
            rect.x += 12;
            rect.width -= 24;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
}
