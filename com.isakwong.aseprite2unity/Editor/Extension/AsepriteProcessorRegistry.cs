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
        //  导入执行
        // ================================================================

        /// <summary>
        /// 按 Processor 顺序尝试创建导入主 GameObject。
        /// 返回 null 表示没有 Processor 接管，由 Importer 自身兜底。
        /// </summary>
        public static GameObject TryCreateImportedGameObject(AssetImportContext ctx, AsepriteImporter importer)
        {
            if (!s_Initialized) Initialize();
            if (s_Processors == null || s_Processors.Count == 0) return null;

            foreach (var processor in s_Processors)
            {
                try
                {
                    var gameObject = processor.TryCreateImportedGameObject(ctx, importer);
                    if (gameObject != null)
                        return gameObject;
                }
                catch (Exception e)
                {
                    try { processor.OnCreateGameObjectError(ctx, importer, e); }
                    catch (Exception ee)
                    {
                        Debug.LogError($"[AsepriteProcessorRegistry] {processor.GetType().Name} 前置创建错误处理器异常: {ee.Message}");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 执行所有已注册的 Processor。
        /// Processor 使用各自的默认 Settings（无 Per-Asset 配置）。
        /// </summary>
        public static void ProcessImport(AssetImportContext ctx, AsepriteImporter importer)
        {
            if (!s_Initialized) Initialize();
            if (s_Processors == null || s_Processors.Count == 0) return;

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
