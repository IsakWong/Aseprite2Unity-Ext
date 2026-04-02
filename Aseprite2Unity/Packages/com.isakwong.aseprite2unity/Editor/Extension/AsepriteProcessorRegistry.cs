using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Aseprite Processor 注册表。
    /// 自动发现所有 AsepriteProcessor 子类，管理 Settings 同步与导入执行。
    /// </summary>
    public static class AsepriteProcessorRegistry
    {
        private static List<AsepriteProcessor> s_Processors;
        private static bool s_Initialized = false;

        /// <summary>所有已注册的 Processor（按 ProcessOrder 排序）</summary>
        public static IReadOnlyList<AsepriteProcessor> Processors
        {
            get
            {
                if (!s_Initialized) Initialize();
                return s_Processors;
            }
        }

        // ================================================================
        //  初始化
        // ================================================================

        public static void Initialize()
        {
            if (s_Initialized) return;

            s_Processors = new List<AsepriteProcessor>();

            try
            {
                var processorTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(GetTypesFromAssembly)
                    .Where(t => t != null && !t.IsAbstract && typeof(AsepriteProcessor).IsAssignableFrom(t))
                    .ToList();

                foreach (var type in processorTypes)
                {
                    try
                    {
                        if (Activator.CreateInstance(type) is AsepriteProcessor processor)
                        {
                            s_Processors.Add(processor);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[AsepriteProcessorRegistry] 创建 Processor 失败: {type.Name}\n{e.Message}");
                    }
                }

                s_Processors.Sort((a, b) => a.ProcessOrder.CompareTo(b.ProcessOrder));
                s_Initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AsepriteProcessorRegistry] 初始化失败: {e.Message}\n{e.StackTrace}");
                s_Processors = new List<AsepriteProcessor>();
                s_Initialized = true;
            }
        }

        // ================================================================
        //  Settings 同步
        // ================================================================

        /// <summary>
        /// 确保 Importer 的 m_ProcessorSettings 列表与注册表中的 Processor 保持同步。
        /// - 为每个声明了 SettingsType 的 Processor 确保列表中有一个对应实例
        /// - 移除不再存在的 Settings 类型
        /// - 保留已有实例的用户修改
        /// 
        /// 调用时机：Editor 绘制 Inspector 前、导入执行前。
        /// </summary>
        public static void EnsureSettings(AsepriteImporter importer)
        {
            if (!s_Initialized) Initialize();
            if (importer == null) return;

            importer.m_ProcessorSettings ??= new List<AsepriteProcessorSettings>();

            // 收集注册表中所有需要 Settings 的类型
            var expectedTypes = new HashSet<Type>();
            foreach (var p in s_Processors)
            {
                if (p.SettingsType != null)
                    expectedTypes.Add(p.SettingsType);
            }

            // 移除已不存在的类型 或 null
            importer.m_ProcessorSettings.RemoveAll(s => s == null || !expectedTypes.Contains(s.GetType()));

            // 已有类型集合
            var existingTypes = new HashSet<Type>(
                importer.m_ProcessorSettings.Where(s => s != null).Select(s => s.GetType()));

            // 补齐缺失的 Settings
            foreach (var type in expectedTypes)
            {
                if (existingTypes.Contains(type)) continue;

                try
                {
                    if (Activator.CreateInstance(type) is AsepriteProcessorSettings newSettings)
                    {
                        importer.m_ProcessorSettings.Add(newSettings);
                        existingTypes.Add(type);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AsepriteProcessorRegistry] 创建 Settings 失败: {type.Name}\n{e.Message}");
                }
            }
        }

        // ================================================================
        //  导入执行
        // ================================================================

        /// <summary>
        /// 执行所有已注册的 Processor。
        /// 自动同步 Settings、注入到 Processor、构建 ImportResult、按序执行。
        /// </summary>
        public static void ProcessImport(AssetImportContext ctx, AsepriteImporter importer)
        {
            if (!s_Initialized) Initialize();
            if (s_Processors == null || s_Processors.Count == 0) return;

            // 同步 Settings 列表
            EnsureSettings(importer);

            // 按类型建立 Settings 查找表
            var settingsMap = new Dictionary<Type, AsepriteProcessorSettings>();
            if (importer.m_ProcessorSettings != null)
            {
                foreach (var s in importer.m_ProcessorSettings)
                {
                    if (s != null)
                        settingsMap[s.GetType()] = s;
                }
            }

            // 构建导入结果上下文
            var result = new AsepriteImportResult(
                ctx, importer,
                importer.AseFileData,
                importer.Sprites,
                importer.AnimClips,
                importer.Frames,
                importer.LayerChunks,
                importer.FrameTagsChunk,
                importer.ImportedGameObject);

            foreach (var processor in s_Processors)
            {
                // 注入 Settings
                if (processor.SettingsType != null &&
                    settingsMap.TryGetValue(processor.SettingsType, out var settings))
                {
                    processor.InjectSettings(settings);
                }

                try
                {
                    if (!processor.ShouldProcess(ctx, importer, result))
                        continue;

                    processor.OnImportAseprite(ctx, importer, result);

                    if (processor.NeedVisitChunks && result.AseFile != null)
                        processor.PerformChunkVisit(result);
                }
                catch (Exception e)
                {
                    try { processor.OnProcessError(ctx, importer, result, e); }
                    catch (Exception ee)
                    {
                        Debug.LogError($"[AsepriteProcessorRegistry] {processor.GetType().Name} 错误处理器异常: {ee.Message}");
                    }
                }
            }
        }

        // ================================================================
        //  工具方法
        // ================================================================

        public static void Clear()
        {
            s_Processors?.Clear();
            s_Initialized = false;
        }

        public static void Reload()
        {
            Clear();
            Initialize();
        }

        public static T GetProcessor<T>() where T : AsepriteProcessor
        {
            if (!s_Initialized) Initialize();
            return s_Processors.OfType<T>().FirstOrDefault();
        }

        private static Type[] GetTypesFromAssembly(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).ToArray(); }
            catch { return Array.Empty<Type>(); }
        }
    }
}
