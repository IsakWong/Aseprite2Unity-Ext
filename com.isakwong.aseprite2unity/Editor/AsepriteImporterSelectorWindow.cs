using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// 手动选择 Aseprite Importer 的工具窗口。
    ///
    /// 入口：Tools → Aseprite2Unity → Select Importer...
    ///
    /// 设计意图：
    /// - 基类默认通过 [ScriptedImporter] 处理 .ase / .aseprite。
    /// - 业务层子类用 overrideExts 登记后，并不会自动接管资源；
    ///   开发者在此窗口选中子类并点击 "Apply To All" 即可把项目里所有
    ///   .ase / .aseprite 资源批量切换到子类（内部通过
    ///   <see cref="AssetDatabase.SetImporterOverride{T}"/> 实现）。
    /// - 单个资源也可以在 Inspector 顶部的 "Importer" 下拉里就地切换，
    ///   两条路径互不冲突。
    /// </summary>
    internal class AsepriteImporterSelectorWindow : EditorWindow
    {
        [MenuItem("Tools/Aseprite2Unity/Select Importer...")]
        public static void Open()
        {
            var win = GetWindow<AsepriteImporterSelectorWindow>(true, "Aseprite Importer", true);
            win.minSize = new Vector2(420, 260);
            win.Refresh();
        }

        private List<Type> m_Candidates = new List<Type>();
        private Dictionary<Type, int> m_Counts = new Dictionary<Type, int>();
        private int m_TotalAseAssets;
        private int m_Selected = -1;

        private static readonly MethodInfo s_SetImporterOverrideGeneric =
            typeof(AssetDatabase).GetMethod(nameof(AssetDatabase.SetImporterOverride),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo s_GetImporterOverride =
            typeof(AssetDatabase).GetMethod(nameof(AssetDatabase.GetImporterOverride),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo s_ClearImporterOverride =
            typeof(AssetDatabase).GetMethod(nameof(AssetDatabase.ClearImporterOverride),
                BindingFlags.Public | BindingFlags.Static);

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            m_Candidates.Clear();
            m_Counts.Clear();
            m_Candidates.Add(typeof(AsepriteImporter));

            var baseType = typeof(AsepriteImporter);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t == null || t == baseType) continue;
                    if (t.IsAbstract || t.IsGenericTypeDefinition) continue;
                    if (!baseType.IsAssignableFrom(t)) continue;
                    m_Candidates.Add(t);
                }
            }

            var paths = GetAseAssetPaths();
            m_TotalAseAssets = paths.Count;
            foreach (var t in m_Candidates) m_Counts[t] = 0;

            foreach (var p in paths)
            {
                var current = (s_GetImporterOverride?.Invoke(null, new object[] { p }) as Type) ?? typeof(AsepriteImporter);
                if (m_Counts.ContainsKey(current)) m_Counts[current]++;
            }

            if (m_Selected < 0 || m_Selected >= m_Candidates.Count) m_Selected = 0;
        }

        private static List<string> GetAseAssetPaths()
        {
            return AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                            (p.EndsWith(".aseprite", StringComparison.OrdinalIgnoreCase) ||
                             p.EndsWith(".ase", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Aseprite Importer Selector", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"项目中共 {m_TotalAseAssets} 个 .ase / .aseprite 资源。\n" +
                "选择一个 Importer 类型，点击 \"Apply To All\" 批量切换；\n" +
                "选 AsepriteImporter 则相当于清除所有 override，恢复默认。",
                MessageType.Info);

            EditorGUILayout.Space();
            for (int i = 0; i < m_Candidates.Count; i++)
            {
                var t = m_Candidates[i];
                bool isBase = (t == typeof(AsepriteImporter));
                int cnt = m_Counts.TryGetValue(t, out var c) ? c : 0;
                string label = $"{t.FullName}{(isBase ? "  (default)" : string.Empty)}    [{cnt}]";
                bool toggled = GUILayout.Toggle(m_Selected == i, label, EditorStyles.radioButton);
                if (toggled) m_Selected = i;
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Height(24))) Refresh();

                using (new EditorGUI.DisabledScope(m_Candidates.Count == 0 || m_TotalAseAssets == 0))
                {
                    if (GUILayout.Button("Apply To All", GUILayout.Height(24)))
                    {
                        var t = m_Candidates[m_Selected];
                        if (EditorUtility.DisplayDialog(
                                "Aseprite2Unity",
                                $"将项目中所有 .ase / .aseprite 资源切换到：\n{t.FullName}\n\n是否继续？",
                                "确定", "取消"))
                        {
                            ApplyTo(t);
                            Refresh();
                        }
                    }
                }
            }
        }

        private static void ApplyTo(Type importerType)
        {
            bool isBase = (importerType == typeof(AsepriteImporter));
            var paths = GetAseAssetPaths();
            int changed = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var path in paths)
                {
                    var current = (s_GetImporterOverride?.Invoke(null, new object[] { path }) as Type) ?? typeof(AsepriteImporter);
                    if (current == importerType) continue;

                    if (isBase)
                    {
                        s_ClearImporterOverride?.Invoke(null, new object[] { path });
                    }
                    else
                    {
                        var setMethod = s_SetImporterOverrideGeneric?.MakeGenericMethod(importerType);
                        setMethod?.Invoke(null, new object[] { path });
                    }
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Aseprite2Unity] 已将 {changed} 个资源切换到 {importerType.FullName}。");
        }
    }
}
