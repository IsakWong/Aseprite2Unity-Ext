using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    #if false
    [CustomEditor(typeof(AsepriteImporter), true)]
    [CanEditMultipleObjects]
    public class AsepriteImporterEditor : ScriptedImporterEditor
    {
        private readonly string[] m_AnimatorCullingModeNames = EnumExtensions.GetUpToDateEnumNames<AnimatorCullingMode>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var importer = serializedObject.targetObject as AsepriteImporter;

            // Processor 不再使用 Per-Asset Settings

            if (importer != null && importer.Errors.Any())
            {
                var asset = Path.GetFileName(importer.assetPath);
                EditorGUILayout.LabelField("There were errors importing " + asset, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(string.Join("\n\n", importer.Errors.Take(10).ToArray()), MessageType.Error);
                EditorGUILayout.Separator();
            }

            EditorGUILayout.LabelField($"Aseprite2Unity Version: {Config.Version}");
            EditorGUILayout.Space();

            // Atlas Settings Section
            EditorGUILayout.LabelField("Atlas Settings", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_CreateAtlas)),
                    new GUIContent("Create Atlas", "Combine all frames into a single texture atlas."));

                EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty(nameof(AsepriteImporter.m_CreateAtlas)).boolValue);


                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_AtlasPadding)),
                    new GUIContent("Atlas Padding", "Padding in pixels between frames in the atlas."));

                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            ExportAnimatorControllerGui();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sprite Settings", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_PixelsPerUnit)),
                    new GUIContent("Pixels Per Unit"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_InstantiatedPrefab)),
                    new GUIContent("Instantiated Prefab"));

                DisplayStringChoiceProperty(serializedObject.FindProperty(nameof(AsepriteImporter.m_SortingLayerName)),
                    SortingLayer.layers.Select(l => l.name).ToArray(),
                    new GUIContent("Sorting Layer"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_SortingOrder)),
                    new GUIContent("Order in Layer"));

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Animator Settings", EditorStyles.boldLabel);
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_FrameRate)),
                    new GUIContent("Frame Rate"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AsepriteImporter.m_AnimatorController)),
                    new GUIContent("Animator Controller"));

                DisplayEnumProperty(serializedObject.FindProperty(nameof(AsepriteImporter.m_AnimatorCullingMode)),
                    m_AnimatorCullingModeNames,
                    new GUIContent("Culling Mode"));

                EditorGUI.indentLevel--;
            }


            // Draw processor settings
            // DrawProcessorSettings(); // Per-Asset Settings 已移除

            // 全局配置快捷入口
            DrawGlobalConfigSection();

            EditorGUILayout.HelpBox("Tip: You can change sprite pivot by adding a pivot slice named unity:pivot to your first frame in Aseprite.", MessageType.Info);
            
            if (importer != null && importer.m_CreateAtlas)
            {
                EditorGUILayout.HelpBox("Atlas Mode: All frames will be combined into a single texture.", MessageType.Info);
            }

            ApplyRevertGUI();
        }

        private void ExportAnimatorControllerGui()
        {
            if (serializedObject.targetObject is AsepriteImporter importer)
            {
                if (GUILayout.Button("Export Default Animator Controller"))
                {
                    var animationControllerAssetPath = EditorUtility.SaveFilePanelInProject("Save Animator Controller",
                        $"{Path.GetFileNameWithoutExtension(importer.assetPath)}.AnimatorController",
                        "controller",
                        "Chose location for Animation Controller",
                        Path.GetDirectoryName(importer.assetPath));

                    if (!string.IsNullOrEmpty(animationControllerAssetPath))
                    {
                        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animationControllerAssetPath);

                        if (controller == null)
                        {
                            controller = AnimatorController.CreateAnimatorControllerAtPath(animationControllerAssetPath);
                        }
                        else
                        {
                            var machine = controller.layers[0].stateMachine;
                            foreach (var state in machine.states.ToArray())
                            {
                                machine.RemoveState(state.state);
                            }
                        }

                        var fsm = controller.layers[0].stateMachine;
                        var position = fsm.entryPosition;
                        position.x += 200;

                        var prefix = $"{Path.GetFileNameWithoutExtension(importer.assetPath)}.Animations.";
                        var clips = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).OfType<AnimationClip>().OrderBy(a => a.name);
                        foreach (var clip in clips)
                        {
                            var clipName = clip.name;
                            if (clipName.StartsWith(prefix))
                            {
                                clipName = clipName.Substring(prefix.Length);
                            }

                            clipName = clipName.Replace('.', '_');
                            var state = fsm.AddState(clipName, position);
                            state.motion = clip;
                            position.y += 80;
                        }
                    }
                }
            }
        }

        // Per-Asset Processor Settings 已移除，Inspector 不再绘制

        private void DrawGlobalConfigSection()
        {
            EditorGUILayout.Space();

            var config = AsepriteImportConfig.Find();
            if (config != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("全局配置", EditorStyles.boldLabel);
                if (GUILayout.Button("选择", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(config);
                    Selection.activeObject = config;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("全局配置", EditorStyles.boldLabel);
                if (GUILayout.Button("创建", GUILayout.Width(50)))
                {
                    var newConfig = ScriptableObject.CreateInstance<AsepriteImportConfig>();
                    var path = "Assets/Config/AsepriteImportConfig.asset";

                    // 确保目录存在
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                        AssetDatabase.Refresh();
                    }

                    AssetDatabase.CreateAsset(newConfig, path);
                    AssetDatabase.SaveAssets();
                    AsepriteImportConfig.ClearCache();
                    EditorGUIUtility.PingObject(newConfig);
                    Selection.activeObject = newConfig;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox("未找到 AsepriteImportConfig 资源。点击「创建」生成全局配置，或通过 Assets → Create → Aseprite2Unity → Import Config 手动创建。", MessageType.Info);
            }
        }

        private static void DisplayEnumProperty(SerializedProperty prop, string[] displayNames, GUIContent guicontent)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, guicontent, prop);
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            GUIContent[] options = new GUIContent[displayNames.Length];
            for (int i = 0; i < options.Length; ++i)
            {
                options[i] = new GUIContent(ObjectNames.NicifyVariableName(displayNames[i]), "");
            }

            var selection = EditorGUI.Popup(rect, guicontent, prop.intValue, options);
            if (EditorGUI.EndChangeCheck())
            {
                prop.intValue = selection;
            }

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }

        private static void DisplayStringChoiceProperty(SerializedProperty prop, string[] choices, GUIContent content)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, content, prop);
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            GUIContent[] options = new GUIContent[choices.Length];
            for (int i = 0; i < options.Length; ++i)
            {
                options[i] = new GUIContent(choices[i], "");
            }

            int selection = Array.IndexOf(choices, prop.stringValue);
            if (selection == -1)
            {
                selection = 0;
            }

            selection = EditorGUI.Popup(rect, content, selection, options);
            if (EditorGUI.EndChangeCheck())
            {
                prop.stringValue = choices[selection];
            }

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }
    }
#endif
}
