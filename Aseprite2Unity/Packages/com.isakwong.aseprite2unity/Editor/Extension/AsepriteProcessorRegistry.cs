using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Registry for Aseprite processors.
    /// Automatically discovers and manages all AsepriteProcessor subclasses.
    /// </summary>
    public static class AsepriteProcessorRegistry
    {
        private static List<AsepriteProcessor> s_Processors;
        private static bool s_Initialized = false;

        /// <summary>
        /// Get all registered processors, sorted by ProcessOrder.
        /// </summary>
        public static IReadOnlyList<AsepriteProcessor> Processors
        {
            get
            {
                if (!s_Initialized)
                {
                    Initialize();
                }
                return s_Processors;
            }
        }

        /// <summary>
        /// Initialize the registry by discovering all processor types.
        /// </summary>
        public static void Initialize()
        {
            if (s_Initialized)
                return;

            s_Processors = new List<AsepriteProcessor>();

            try
            {
                // Find all types that inherit from AsepriteProcessor
                var processorTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => GetTypesFromAssembly(assembly))
                    .Where(type => type != null &&
                                   !type.IsAbstract &&
                                   typeof(AsepriteProcessor).IsAssignableFrom(type))
                    .ToList();

                // Create instances of all processor types
                foreach (var processorType in processorTypes)
                {
                    try
                    {
                        var processor = Activator.CreateInstance(processorType) as AsepriteProcessor;
                        if (processor != null)
                        {
                            s_Processors.Add(processor);
                            Debug.Log($"[AsepriteProcessorRegistry] Registered processor: {processorType.Name}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[AsepriteProcessorRegistry] Failed to create processor instance: {processorType.Name}\n{e.Message}");
                    }
                }

                // Sort processors by execution order
                s_Processors.Sort((a, b) => a.ProcessOrder.CompareTo(b.ProcessOrder));

                s_Initialized = true;

                Debug.Log($"[AsepriteProcessorRegistry] Initialized with {s_Processors.Count} processor(s)");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AsepriteProcessorRegistry] Failed to initialize: {e.Message}\n{e.StackTrace}");
                s_Processors = new List<AsepriteProcessor>();
                s_Initialized = true;
            }
        }

        /// <summary>
        /// Execute all registered processors for the given import context.
        /// Uses the importer's persisted processor settings (m_ProcessorSettings) for execution.
        /// </summary>
        /// <param name="ctx">The asset import context</param>
        /// <param name="importer">The Aseprite importer instance</param>
        /// <param name="result">The import result containing sprites, clips, frames, and shared data</param>
        public static void ProcessImport(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
        {
            if (!s_Initialized)
            {
                Initialize();
            }

            // Use the importer's persisted settings, sorted by ProcessOrder
            var processors = importer.m_ProcessorSettings;
            if (processors == null || processors.Count == 0)
            {
                return;
            }

            // Sort by ProcessOrder (stable sort preserves registration order for equal values)
            var sortedProcessors = processors
                .Where(p => p != null)
                .OrderBy(p => p.ProcessOrder)
                .ToList();

            foreach (var processor in sortedProcessors)
            {
                try
                {
                    // Check if processor should run
                    if (!processor.ShouldProcess(ctx, importer, result))
                    {
                        continue;
                    }

                    // Execute processor main logic
                    processor.OnImportAseprite(ctx, importer, result);

                    // Execute chunk visitor traversal if processor needs it
                    if (processor.NeedVisitChunks && result.AseFile != null)
                    {
                        processor.PerformChunkVisit(result);
                    }
                }
                catch (Exception e)
                {
                    // Let processor handle the error
                    try
                    {
                        processor.OnProcessError(ctx, importer, result, e);
                    }
                    catch (Exception errorHandlingException)
                    {
                        Debug.LogError($"[AsepriteProcessorRegistry] Error in error handler for {processor.GetType().Name}: {errorHandlingException.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Ensure the importer's processor settings list contains one instance per discovered processor type.
        /// Adds new processor types and removes types that no longer exist, preserving existing settings.
        /// Call this from the importer editor or during import to keep the list in sync.
        /// </summary>
        /// <param name="importer">The importer to synchronize</param>
        public static void EnsureProcessorSettings(AsepriteImporter importer)
        {
            if (!s_Initialized)
            {
                Initialize();
            }

            if (importer.m_ProcessorSettings == null)
            {
                importer.m_ProcessorSettings = new List<AsepriteProcessor>();
            }

            // Build a set of expected processor types from the registry
            var expectedTypes = s_Processors
                .Where(p => p != null)
                .Select(p => p.GetType())
                .ToHashSet();

            // Remove entries whose type no longer exists (or are null)
            importer.m_ProcessorSettings.RemoveAll(p => p == null || !expectedTypes.Contains(p.GetType()));

            // Build a set of types already present in the importer's settings
            var existingTypes = new HashSet<Type>(
                importer.m_ProcessorSettings
                    .Where(p => p != null)
                    .Select(p => p.GetType())
            );

            // Add missing processor types with default instances
            foreach (var registryProcessor in s_Processors)
            {
                if (registryProcessor == null) continue;
                var type = registryProcessor.GetType();
                if (!existingTypes.Contains(type))
                {
                    var newProcessor = Activator.CreateInstance(type) as AsepriteProcessor;
                    if (newProcessor != null)
                    {
                        importer.m_ProcessorSettings.Add(newProcessor);
                        existingTypes.Add(type);
                    }
                }
            }
        }

        /// <summary>
        /// Clear the registry. Useful for testing or reinitialization.
        /// </summary>
        public static void Clear()
        {
            if (s_Processors != null)
            {
                s_Processors.Clear();
            }
            s_Initialized = false;
        }

        /// <summary>
        /// Reload the registry by clearing and reinitializing.
        /// </summary>
        public static void Reload()
        {
            Clear();
            Initialize();
        }

        /// <summary>
        /// Get a processor by type.
        /// </summary>
        /// <typeparam name="T">The processor type</typeparam>
        /// <returns>The processor instance, or null if not found</returns>
        public static T GetProcessor<T>() where T : AsepriteProcessor
        {
            if (!s_Initialized)
            {
                Initialize();
            }

            return s_Processors.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Safely get types from an assembly, handling potential load errors.
        /// </summary>
        private static Type[] GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Return the types that were successfully loaded
                return e.Types.Where(t => t != null).ToArray();
            }
            catch (Exception)
            {
                // If assembly can't be loaded, return empty array
                return new Type[0];
            }
        }

        /// <summary>
        /// Get detailed information about registered processors.
        /// Useful for debugging.
        /// </summary>
        public static string GetRegistryInfo()
        {
            if (!s_Initialized)
            {
                Initialize();
            }

            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Aseprite Processor Registry ===");
            info.AppendLine($"Total Processors: {s_Processors.Count}");
            info.AppendLine();

            for (int i = 0; i < s_Processors.Count; i++)
            {
                var processor = s_Processors[i];
                if (processor != null)
                {
                    info.AppendLine($"{i + 1}. {processor.GetType().Name}");
                    info.AppendLine($"   Order: {processor.ProcessOrder}");
                    info.AppendLine($"   Type: {processor.GetType().FullName}");
                }
                else
                {
                    info.AppendLine($"{i + 1}. [NULL]");
                }
                info.AppendLine();
            }

            return info.ToString();
        }
    }
}
