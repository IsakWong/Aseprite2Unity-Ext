using System;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    /// <summary>
    /// Base class for Aseprite import processors.
    /// Inherit from this class to extend Aseprite import functionality.
    /// All processors will be automatically discovered and executed during import.
    /// 
    /// Processors support two complementary processing modes:
    /// 1. Override OnImportAseprite() for result-based processing (access Sprites, Clips, etc.)
    /// 2. Override Visit*() methods for Visitor-pattern chunk traversal (access raw Aseprite data)
    /// 
    /// The execution order is: OnImportAseprite() first, then Visit*() traversal (if enabled).
    /// </summary>
    [System.Serializable]
    public abstract class AsepriteProcessor
    {
        /// <summary>
        /// Display name shown in the Importer Inspector.
        /// Override to provide a human-readable name for this processor.
        /// Default is the class name.
        /// </summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        /// Execution order of this processor. Lower values execute first.
        /// Default is 0. Use negative values for pre-processing, positive for post-processing.
        /// </summary>
        public virtual int ProcessOrder => 0;

        /// <summary>
        /// Whether this processor needs Visitor-pattern traversal of AseFile chunks.
        /// Override and return true to enable Visit*() callbacks after OnImportAseprite().
        /// Default is false (only OnImportAseprite is called).
        /// </summary>
        public virtual bool NeedVisitChunks => false;

        /// <summary>
        /// Called during Aseprite asset import.
        /// Override this method to add custom processing logic.
        /// </summary>
        /// <param name="ctx">The asset import context</param>
        /// <param name="importer">The Aseprite importer instance</param>
        /// <param name="result">The import result containing sprites, clips, frames, and shared data</param>
        public abstract void OnImportAseprite(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result);

        /// <summary>
        /// Called before OnImportAseprite to check if this processor should run.
        /// Override to add custom conditions.
        /// </summary>
        /// <param name="ctx">The asset import context</param>
        /// <param name="importer">The Aseprite importer instance</param>
        /// <param name="result">The import result containing sprites, clips, frames, and shared data</param>
        /// <returns>True if the processor should run, false otherwise</returns>
        public virtual bool ShouldProcess(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
        {
            return true;
        }

        /// <summary>
        /// Called when an error occurs during processing.
        /// Override to handle errors gracefully.
        /// </summary>
        /// <param name="ctx">The asset import context</param>
        /// <param name="importer">The Aseprite importer instance</param>
        /// <param name="result">The import result containing sprites, clips, frames, and shared data</param>
        /// <param name="exception">The exception that occurred</param>
        public virtual void OnProcessError(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result, System.Exception exception)
        {
            Debug.LogError($"[{GetType().Name}] Error processing {ctx.assetPath}: {exception.Message}\n{exception.StackTrace}");
        }

        /// <summary>
        /// 获取全局导入配置（可能为 null）。
        /// Processor 可通过此属性读取共享默认值，字段自身值为空/默认时回退到全局配置。
        /// </summary>
        protected static AsepriteImportConfig GlobalConfig => AsepriteImportConfig.Find();

        // ---- Visitor-pattern callbacks ----
        // Override these methods to process raw Aseprite data (requires NeedVisitChunks = true).
        // These are called after OnImportAseprite(), following the same traversal order as AseFile.VisitContents().

        /// <summary>
        /// Called at the beginning of file traversal.
        /// </summary>
        public virtual void BeginFileVisit(AseFile file, AsepriteImportResult result) { }

        /// <summary>
        /// Called at the end of file traversal.
        /// </summary>
        public virtual void EndFileVisit(AseFile file, AsepriteImportResult result) { }

        /// <summary>
        /// Called at the beginning of each frame.
        /// </summary>
        public virtual void BeginFrameVisit(AseFrame frame, int frameIndex, AsepriteImportResult result) { }

        /// <summary>
        /// Called at the end of each frame.
        /// </summary>
        public virtual void EndFrameVisit(AseFrame frame, int frameIndex, AsepriteImportResult result) { }

        /// <summary>
        /// Called for each layer chunk.
        /// </summary>
        public virtual void VisitLayerChunk(AseLayerChunk layer, AsepriteImportResult result) { }

        /// <summary>
        /// Called for each cel chunk (pixel data within a frame/layer).
        /// </summary>
        public virtual void VisitCelChunk(AseCelChunk cel, AsepriteImportResult result) { }

        /// <summary>
        /// Called when frame tags are encountered (animation tag definitions).
        /// </summary>
        public virtual void VisitFrameTagsChunk(AseFrameTagsChunk frameTags, AsepriteImportResult result) { }

        /// <summary>
        /// Called for palette data.
        /// </summary>
        public virtual void VisitPaletteChunk(AsePaletteChunk palette, AsepriteImportResult result) { }

        /// <summary>
        /// Called for user data attached to chunks.
        /// </summary>
        public virtual void VisitUserDataChunk(AseUserDataChunk userData, AsepriteImportResult result) { }

        /// <summary>
        /// Called for slice data (e.g. pivot definitions).
        /// </summary>
        public virtual void VisitSliceChunk(AseSliceChunk slice, AsepriteImportResult result) { }

        /// <summary>
        /// Called for tileset data.
        /// </summary>
        public virtual void VisitTilesetChunk(AseTilesetChunk tileset, AsepriteImportResult result) { }

        /// <summary>
        /// Perform visitor-pattern traversal of the AseFile for this processor.
        /// Called by the registry after OnImportAseprite() when NeedVisitChunks is true.
        /// Generally you should NOT override this method; override the individual Visit*() methods instead.
        /// </summary>
        internal void PerformChunkVisit(AsepriteImportResult result)
        {
            var aseFile = result.AseFile;
            if (aseFile == null) return;

            BeginFileVisit(aseFile, result);

            for (int frameIndex = 0; frameIndex < aseFile.Frames.Count; frameIndex++)
            {
                var frame = aseFile.Frames[frameIndex];
                BeginFrameVisit(frame, frameIndex, result);

                foreach (var chunk in frame.Chunks)
                {
                    DispatchChunk(chunk, result);
                }

                EndFrameVisit(frame, frameIndex, result);
            }

            EndFileVisit(aseFile, result);
        }

        /// <summary>
        /// Dispatch a chunk to the appropriate Visit method based on its type.
        /// </summary>
        private void DispatchChunk(AseChunk chunk, AsepriteImportResult result)
        {
            switch (chunk)
            {
                case AseLayerChunk layer:
                    VisitLayerChunk(layer, result);
                    break;
                case AseCelChunk cel:
                    VisitCelChunk(cel, result);
                    break;
                case AseFrameTagsChunk frameTags:
                    VisitFrameTagsChunk(frameTags, result);
                    break;
                case AsePaletteChunk palette:
                    VisitPaletteChunk(palette, result);
                    break;
                case AseUserDataChunk userData:
                    VisitUserDataChunk(userData, result);
                    break;
                case AseSliceChunk slice:
                    VisitSliceChunk(slice, result);
                    break;
                case AseTilesetChunk tileset:
                    VisitTilesetChunk(tileset, result);
                    break;
            }
        }
    }
}
