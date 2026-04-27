using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    // [ScriptedImporter] 特性已移至业务层子类 JasaAsepriteImporter
    // 保留为可继承基类，不直接注册扩展名
    public class AsepriteImporter : ScriptedImporter, IAseVisitor
    {
        // Editor fields — 逐资源覆盖值
        [Header("Override Settings")] public bool m_OverridePixelsPerUnit;
        public float m_PixelsPerUnit = 16.0f;

        public bool m_OverrideFrameRate;
        public float m_FrameRate = 60.0f;

        public string m_SortingLayerName;
        public int m_SortingOrder;

        public AnimatorCullingMode m_AnimatorCullingMode = AnimatorCullingMode.AlwaysAnimate;
        public AnimatorController m_AnimatorController;

        // Atlas settings
        [Header("Atlas Settings")] public bool m_OverrideCreateAtlas;
        public bool m_CreateAtlas = true;

        public bool m_OverrideAtlasPadding;
        public int m_AtlasPadding = 0;

        [Header("Material Settings")] public bool m_OverrideMaterial;
        public Material m_DefaultMaterial;

        [Header("Animation Settings")] public bool m_OverrideCreateAnimations;
        public bool m_CreateAnimations = true;

        // ================================================================
        //  解析有效值：Override → 全局配置 → 字段默认值
        // ================================================================

        /// <summary>有效的 PixelsPerUnit（考虑全局配置）</summary>
        public float EffectivePixelsPerUnit
        {
            get
            {
                if (m_OverridePixelsPerUnit) return m_PixelsPerUnit;
                var cfg = AsepriteImportConfig.Find();
                return cfg != null ? cfg.DefaultPixelsPerUnit : m_PixelsPerUnit;
            }
        }

        /// <summary>有效的帧率（考虑全局配置）</summary>
        public float EffectiveFrameRate
        {
            get
            {
                if (m_OverrideFrameRate) return m_FrameRate;
                var cfg = AsepriteImportConfig.Find();
                return cfg != null ? cfg.DefaultFrameRate : m_FrameRate;
            }
        }

        /// <summary>有效的 CreateAtlas（考虑全局配置）</summary>
        public bool EffectiveCreateAtlas
        {
            get
            {
                if (m_OverrideCreateAtlas) return m_CreateAtlas;
                var cfg = AsepriteImportConfig.Find();
                return cfg != null ? cfg.DefaultCreateAtlas : m_CreateAtlas;
            }
        }

        /// <summary>有效的 AtlasPadding（考虑全局配置）</summary>
        public int EffectiveAtlasPadding
        {
            get
            {
                if (m_OverrideAtlasPadding) return m_AtlasPadding;
                var cfg = AsepriteImportConfig.Find();
                return cfg != null ? cfg.DefaultAtlasPadding : m_AtlasPadding;
            }
        }

        /// <summary>有效的材质（考虑全局配置）</summary>
        public Material EffectiveMaterial
        {
            get
            {
                if (m_OverrideMaterial) return m_DefaultMaterial;
                var cfg = AsepriteImportConfig.Find();
                return cfg != null ? cfg.DefaultMaterial : m_DefaultMaterial;
            }
        }

        /// <summary>有效的 CreateAnimations（考虑全局配置）</summary>
        public bool EffectiveCreateAnimations
        {
            get
            {
                if (m_OverrideCreateAnimations) return m_CreateAnimations;
                var cfg = AsepriteImportConfig.Find();
                return cfg != null ? cfg.DefaultCreateAnimations : m_CreateAnimations;
            }
        }

        // Per-Asset Processor 配置已移除，Processor 使用默认值

        // Properties based on file header
        public int CanvasWidth => m_AseFile.Header.Width;
        public int CanvasHeight => m_AseFile.Header.Height;
        public ColorDepth ColorDepth => m_AseFile.Header.ColorDepth;
        public int TransparentIndex => m_AseFile.Header.TransparentIndex;

        private readonly List<AseLayerChunk> m_LayerChunks = new List<AseLayerChunk>();
        private readonly List<AseTilesetChunk> m_TilesetChunks = new List<AseTilesetChunk>();
        private readonly List<AseFrame> m_Frames = new List<AseFrame>();
        private readonly List<Sprite> m_Sprites = new List<Sprite>();
        private readonly List<AnimationClip> m_AnimationClips = new List<AnimationClip>();

        /// <summary>
        /// Tag 名称 → 对应帧范围的 Sprite 列表。
        /// 在 BuildAnimations 中与动画剪辑同步填充，供业务层 Processor 直接按 Tag 名获取 Sprite。
        /// </summary>
        private readonly Dictionary<string, List<Sprite>> m_TagSprites = new Dictionary<string, List<Sprite>>();

        /// <summary>
        /// 获取按 Tag 分组的 Sprite 字典（只读）。
        /// Key 为 Aseprite 中 FrameTag 的名称，Value 为该 Tag 帧范围内的 Sprite 列表。
        /// </summary>
        public IReadOnlyDictionary<string, List<Sprite>> TagSprites => m_TagSprites;

        /// <summary>导入生成的所有 Sprite（按帧顺序）</summary>
        public IReadOnlyList<Sprite> Sprites => m_Sprites;

        /// <summary>导入生成的所有 AnimationClip</summary>
        public IReadOnlyList<AnimationClip> AnimClips => m_AnimationClips;

        /// <summary>导入过程中解析的所有帧数据</summary>
        public IReadOnlyList<AseFrame> Frames => m_Frames;

        /// <summary>导入过程中解析的所有图层数据</summary>
        public IReadOnlyList<AseLayerChunk> LayerChunks => m_LayerChunks;

        /// <summary>帧标签数据</summary>
        public AseFrameTagsChunk FrameTagsChunk => m_AseFrameTagsChunk;

        /// <summary>解析后的 Aseprite 文件对象</summary>
        public AseFile AseFileData => m_AseFile;

        /// <summary>导入生成的主 GameObject</summary>
        public GameObject ImportedGameObject => m_GameObject;

        // Atlas data - store frame canvases temporarily
        private readonly List<AseCanvas> m_FrameCanvases = new List<AseCanvas>();

        private GameObject m_GameObject;

        private AssetImportContext m_Context;
        private AseFile m_AseFile;

        private AseFrameTagsChunk m_AseFrameTagsChunk;
        private Vector2? m_Pivot;

        private AseCanvas m_FrameCanvas;
        private readonly AseGraphics.GetPixelArgs m_GetPixelArgs = new AseGraphics.GetPixelArgs();

        private readonly UniqueNameifier m_UniqueNameifierAnimations = new UniqueNameifier();

        [SerializeField] private List<string> m_Errors = new List<string>();

        public IEnumerable<string> Errors
        {
            get { return m_Errors; }
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            m_Errors.Clear();

#if UNITY_2020_3_OR_NEWER
            m_Context = ctx;

            using (var reader = new AseReader(m_Context.assetPath))
            {
                m_AseFile = new AseFile(reader);
                m_AseFile.VisitContents(this);
            }

            // Processor 在 EndFileVisit 之后执行，此时所有数据（Sprites/AnimClips/TagSprites）均已就绪
            try
            {
                AsepriteProcessorRegistry.ProcessImport(ctx, this);
            }
            catch (System.Exception e)
            {
                var errorMsg = $"Error in Aseprite processors: {e.Message}";
                m_Errors.Add(errorMsg);
                Debug.LogError($"{errorMsg}\n{e.StackTrace}");
            }

            // Processor 全部执行完毕后再清理临时数据
            CleanupImportData();
#else
            string msg =
 string.Format("Aesprite2Unity requires Unity 2020.3 or later. You are using {0}", Application.unityVersion);
            m_Errors.Add(msg);
            Debug.LogError(msg);
#endif
        }


        public void BeginFileVisit(AseFile file)
        {
            m_GetPixelArgs.ColorDepth = ColorDepth;
            m_Pivot = null;
            m_FrameCanvases.Clear();

            // 始终在导入开始注册 _main 根节点，保证主资源 LocalIdentifier 稳定；
            // Processor（如环境）只在此基础上 AddComponent / 挂子物体，不再 late AddObjectToAsset 顶替主物体。
            // （AsepriteImportConfig.GenerateDefaultPrefab 保留字段兼容旧资源，此处不再读取。）
            var icon = AssetDatabaseEx.LoadFirstAssetByFilter<Texture2D>("aseprite2unity-icon-0x1badd00d");

            m_GameObject = CreateImportedGameObject();

            m_Context.AddObjectToAsset("_main", m_GameObject, icon);
            m_Context.SetMainObject(m_GameObject);
        }

        /// <summary>
        /// 创建导入主 GameObject。
        /// 优先交由扩展层 Processor 处理，未处理时回退到 Importer 自身默认实现。
        /// </summary>
        protected virtual GameObject CreateImportedGameObject()
        {
            return AsepriteProcessorRegistry.TryCreateImportedGameObject(m_Context, this) ?? CreateFallbackImportedGameObject();
        }

        /// <summary>
        /// 默认兜底实现。业务层也可以通过覆写此方法调整最终回退行为。
        /// </summary>
        protected virtual GameObject CreateFallbackImportedGameObject()
        {
            return new GameObject();
        }

        public void EndFileVisit(AseFile file)
        {
            // Create atlas texture from all frames
            if (EffectiveCreateAtlas && m_FrameCanvases.Count > 0)
            {
                CreateAtlasAndSprites();
            }

            BuildAnimations();


            if (m_GameObject)
            {
                // Add a sprite renderer if needed and assign our sprite to it
                var renderer = m_GameObject.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = m_GameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingLayerName = m_SortingLayerName;
                    renderer.sortingOrder = m_SortingOrder;
                }

                if (m_Sprites.Count > 0)
                {
                    renderer.sprite = m_Sprites[0];
                }

                // Add an animator if needed
                var animator = m_GameObject.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = m_GameObject.AddComponent<Animator>();
                    animator.cullingMode = m_AnimatorCullingMode;

                    // Make a default animator controller if needed
                    if (m_AnimatorController == null)
                    {
                        var controller = new AnimatorController();
                        controller.name = Path.GetFileNameWithoutExtension(assetPath);
                        controller.AddLayer("Base Layer");

                        foreach (var clip in m_AnimationClips)
                        {
                            controller.AddMotion(clip);
                        }

                        m_Context.AddObjectToAsset(controller.name + "_Controller", controller);

                        foreach (var layer in controller.layers)
                        {
                            var stateMachine = layer.stateMachine;
                            m_Context.AddObjectToAsset(stateMachine.name + "_StateMachine", stateMachine);

                            foreach (var state in stateMachine.states)
                            {
                                m_Context.AddObjectToAsset(state.state.name + "_State", state.state);
                            }
                        }

                        AnimatorController.SetAnimatorController(animator, controller);
                    }
                    else
                    {
                        AnimatorController.SetAnimatorController(animator, m_AnimatorController);
                    }
                }
            }


            // Cleanup canvas data (no longer needed after atlas creation)
            foreach (var canvas in m_FrameCanvases)
            {
                canvas?.Dispose();
            }

            m_FrameCanvases.Clear();
            m_UniqueNameifierAnimations.Clear();
        }

        /// <summary>
        /// 清理导入过程中的临时数据。
        /// 在 OnImportAsset 中所有 Processor 执行完毕后调用，
        /// 确保 Processor 能访问完整的 Sprites/AnimClips/TagSprites 等数据。
        /// </summary>
        private void CleanupImportData()
        {
            m_LayerChunks.Clear();
            m_Frames.Clear();
            m_Sprites.Clear();
            m_AnimationClips.Clear();
            m_TagSprites.Clear();
            m_AseFrameTagsChunk = null;
            m_GameObject = null;
        }

        public void BeginFrameVisit(AseFrame frame)
        {
            m_FrameCanvas = new AseCanvas(CanvasWidth, CanvasHeight);
            m_Frames.Add(frame);
        }

        public void EndFrameVisit(AseFrame frame)
        {
            // Store the canvas for atlas creation later
            if (EffectiveCreateAtlas)
            {
                m_FrameCanvases.Add(m_FrameCanvas);
                // Don't dispose the canvas yet - we need it for atlas creation
                m_FrameCanvas = null;
            }
            else
            {
                // Original behavior: create individual textures
                var texture2d = m_FrameCanvas.ToTexture2D();
                texture2d.filterMode = FilterMode.Point;
                texture2d.wrapMode = TextureWrapMode.Clamp;
                m_FrameCanvas.Dispose();
                m_FrameCanvas = null;

                var assetName = Path.GetFileNameWithoutExtension(assetPath);
                var textureId = $"Textures._{m_Frames.Count - 1}";
                var textureName = $"{assetName}.{textureId}";

                texture2d.name = textureName;
                m_Context.AddObjectToAsset(textureId, texture2d);

                var pivot = m_Pivot ?? new Vector2(0.5f, 0.5f);
                var sprite = Sprite.Create(texture2d, new Rect(0, 0, CanvasWidth, CanvasHeight), pivot,
                    EffectivePixelsPerUnit);
                m_Sprites.Add(sprite);

                var spriteId = $"Sprites._{m_Sprites.Count - 1}";
                var spriteName = $"{assetName}.{spriteId}";

                sprite.name = spriteName;
                m_Context.AddObjectToAsset(spriteId, sprite);
            }
        }

        /// <summary>
        /// 计算网格排列的最优列数，使图集尽量接近正方形
        /// </summary>
        private static (int columns, int rows) CalculateGridSize(int frameCount, int frameW, int frameH, int padding)
        {
            // 目标：图集宽高比尽量接近 1:1
            // 列数 = ceil(sqrt(frameCount * frameH / frameW))
            int bestCols = 1;
            float bestRatio = float.MaxValue;
            int maxCols = frameCount;

            for (int cols = 1; cols <= maxCols; cols++)
            {
                int rows = (frameCount + cols - 1) / cols;
                float w = cols * frameW + (cols - 1) * padding;
                float h = rows * frameH + (rows - 1) * padding;
                float ratio = w > h ? w / h : h / w; // 越接近1越好
                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    bestCols = cols;
                }
            }

            int bestRows = (frameCount + bestCols - 1) / bestCols;
            return (bestCols, bestRows);
        }

        private void CreateAtlasAndSprites()
        {
            int frameCount = m_FrameCanvases.Count;
            if (frameCount == 0) return;

            // Cache effective values to avoid repeated property lookups in loops
            int padding = EffectiveAtlasPadding;
            float ppu = EffectivePixelsPerUnit;

            int atlasWidth, atlasHeight;
            int gridColumns, gridRows;

            // 网格排列：自动计算最优行列数，使图集尽量接近正方形
            (gridColumns, gridRows) = CalculateGridSize(frameCount, CanvasWidth, CanvasHeight, padding);

            atlasWidth = gridColumns * CanvasWidth + (gridColumns - 1) * padding;
            atlasHeight = gridRows * CanvasHeight + (gridRows - 1) * padding;

            // Create atlas texture
            Texture2D atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);
            atlasTexture.filterMode = FilterMode.Point;
            atlasTexture.wrapMode = TextureWrapMode.Clamp;

            // Fill with transparent pixels
            Color32[] clearPixels = new Color32[atlasWidth * atlasHeight];
            for (int i = 0; i < clearPixels.Length; i++)
            {
                clearPixels[i] = Color.clear;
            }

            atlasTexture.SetPixels32(clearPixels);

            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var pivot = m_Pivot ?? new Vector2(0.5f, 0.5f);

            // Copy each frame into the atlas (网格排列：行优先，从左到右、从上到下)
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var canvas = m_FrameCanvases[frameIndex];

                int col = frameIndex % gridColumns;
                int row = frameIndex / gridColumns;
                int xOffset = col * (CanvasWidth + padding);
                int yOffset = row * (CanvasHeight + padding);

                // Copy pixels from canvas to atlas
                unsafe
                {
                    var canvasPixels = (Color32*)canvas.Pixels.GetUnsafePtr();
                    int canvasPixelCount = canvas.Pixels.Length;

                    for (int y = 0; y < CanvasHeight; y++)
                    {
                        for (int x = 0; x < CanvasWidth; x++)
                        {
                            int canvasIndex = y * CanvasWidth + x;

                            // 边界检查：确保不越界访问canvas数组
                            if (canvasIndex >= 0 && canvasIndex < canvasPixelCount)
                            {
                                int atlasX = xOffset + x;
                                int atlasY = yOffset + y;

                                if (atlasX >= 0 && atlasX < atlasWidth && atlasY >= 0 && atlasY < atlasHeight)
                                {
                                    atlasTexture.SetPixel(atlasX, atlasY, canvasPixels[canvasIndex]);
                                }
                            }
                        }
                    }
                }
            }

            // Apply pixel data
            atlasTexture.Apply();

            // Flip the atlas texture to match Unity's coordinate system (bottom-left origin)
            // This is the same as what AseCanvas.ToTexture2D() does
            var renderTexture = new RenderTexture(atlasWidth, atlasHeight, 0, RenderTextureFormat.ARGB32, 0);
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.filterMode = FilterMode.Point;
            RenderTexture oldRenderTexture = RenderTexture.active;
            RenderTexture.active = renderTexture;
            {
                Graphics.Blit(atlasTexture, renderTexture, new Vector2(1, -1), new Vector2(0, 1));
                atlasTexture.ReadPixels(new Rect(0, 0, atlasWidth, atlasHeight), 0, 0);
                atlasTexture.Apply();
            }
            RenderTexture.active = oldRenderTexture;

            var atlasName = $"{assetName}_Atlas";
            atlasTexture.name = atlasName;
            m_Context.AddObjectToAsset("Atlas", atlasTexture);

            // Now create sprites with correct coordinates (after the texture has been flipped)
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                int col = frameIndex % gridColumns;
                int row = frameIndex / gridColumns;
                int xOffset = col * (CanvasWidth + padding);
                // 翻转后 Y 坐标：row 0 在最上面 → 翻转后在最下面
                int yOffset = atlasHeight - (row + 1) * CanvasHeight - row * padding;

                // Create sprite for this frame with flipped coordinates
                Rect spriteRect = new Rect(xOffset, yOffset, CanvasWidth, CanvasHeight);
                var sprite = Sprite.Create(atlasTexture, spriteRect, pivot, ppu);

                var spriteId = $"Sprites._{frameIndex}";
                var spriteName = $"{assetName}.{spriteId}";
                sprite.name = spriteName;

                m_Sprites.Add(sprite);
                m_Context.AddObjectToAsset(spriteId, sprite);
            }
        }

        public void VisitCelChunk(AseCelChunk cel)
        {
            var layer = m_LayerChunks[cel.LayerIndex];
            if (!layer.IsVisible)
            {
                // Ignore cels from invisible layers
                return;
            }

            if (cel.LinkedCel != null)
            {
                cel = cel.LinkedCel;
            }

            if (cel.CelType == CelType.CompressedImage)
            {
                // Get the pixels from this cel and blend them into the canvas for this frame
                unsafe
                {
                    var canvas = m_FrameCanvas;
                    var canvasPixels = (Color32*)canvas.Pixels.GetUnsafePtr();
                    int canvasPixelCount = canvas.Pixels.Length;

                    m_GetPixelArgs.PixelBytes = cel.PixelBytes;
                    m_GetPixelArgs.Stride = cel.Width;

                    for (int x = 0; x < cel.Width; x++)
                    {
                        for (int y = 0; y < cel.Height; y++)
                        {
                            Color32 celPixel = AseGraphics.GetPixel(x, y, m_GetPixelArgs);
                            celPixel.a = AseGraphics.CalculateOpacity(celPixel.a, layer.Opacity, cel.Opacity);
                            if (celPixel.a > 0)
                            {
                                int cx = cel.PositionX + x;
                                int cy = cel.PositionY + y;

                                // 边界检查：确保cx和cy在canvas范围内
                                if (cx >= 0 && cx < canvas.Width && cy >= 0 && cy < canvas.Height)
                                {
                                    int index = cx + (cy * canvas.Width);

                                    // 双重保险：检查计算出的索引是否有效
                                    if (index >= 0 && index < canvasPixelCount)
                                    {
                                        Color32 basePixel = canvasPixels[index];
                                        Color32 blendedPixel =
                                            AseGraphics.BlendColors(layer.BlendMode, basePixel, celPixel);
                                        canvasPixels[index] = blendedPixel;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (cel.CelType == CelType.CompressedTilemap)
            {
                // Find layer that is a Tilemap type and has a matching Tileset Index
                var tileset = m_TilesetChunks.FirstOrDefault(ts => ts.TilesetId == layer.TilesetIndex);
                if (tileset != null)
                {
                    unsafe
                    {
                        var canvas = m_FrameCanvas;
                        var canvasPixels = (Color32*)canvas.Pixels.GetUnsafePtr();
                        int canvasPixelCount = canvas.Pixels.Length;

                        m_GetPixelArgs.PixelBytes = tileset.PixelBytes;
                        m_GetPixelArgs.Stride = tileset.TileWidth;

                        for (int t = 0; t < cel.TileData32.Length; t++)
                        {
                            // A tileId of zero means an empty tile
                            int tileId = (int)cel.TileData32[t];
                            if (tileId != 0)
                            {
                                int tile_i = t % cel.NumberOfTilesWide;
                                int tile_j = t / cel.NumberOfTilesWide;

                                // What are the start and end coordinates for the tile?
                                int txmin = 0;
                                int txmax = txmin + tileset.TileWidth;
                                int tymin = tileId * tileset.TileHeight;
                                int tymax = tymin + tileset.TileHeight;

                                // What are the start and end coordinates for the canvas we are copying tile pixels to?
                                int cxmin = cel.PositionX + (tile_i * tileset.TileWidth);
                                int cxmax = Math.Min(canvas.Width, cxmin + tileset.TileWidth);
                                int cymin = cel.PositionY + (tile_j * tileset.TileHeight);
                                int cymax = Math.Min(canvas.Height, cymin + tileset.TileHeight);

                                for (int tx = txmin, cx = cxmin; tx < txmax && cx < cxmax; tx++, cx++)
                                {
                                    for (int ty = tymin, cy = cymin; ty < tymax && cy < cymax; ty++, cy++)
                                    {
                                        // 边界检查：确保cx和cy在有效范围内
                                        if (cx >= 0 && cx < canvas.Width && cy >= 0 && cy < canvas.Height)
                                        {
                                            Color32 tilePixel = AseGraphics.GetPixel(tx, ty, m_GetPixelArgs);
                                            tilePixel.a = AseGraphics.CalculateOpacity(tilePixel.a, layer.Opacity,
                                                cel.Opacity);
                                            if (tilePixel.a > 0)
                                            {
                                                int canvasPixelIndex = cx + (cy * canvas.Width);

                                                // 双重保险：检查计算出的索引是否有效
                                                if (canvasPixelIndex >= 0 && canvasPixelIndex < canvasPixelCount)
                                                {
                                                    Color32 basePixel = canvasPixels[canvasPixelIndex];
                                                    Color32 blendedPixel = AseGraphics.BlendColors(layer.BlendMode,
                                                        basePixel, tilePixel);
                                                    canvasPixels[canvasPixelIndex] = blendedPixel;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Cannot find tileset {layer.TilesetIndex} for layer {layer.Name}");
                }
            }
        }

        public void VisitDummyChunk(AseDummyChunk dummy)
        {
        }

        public void VisitFrameTagsChunk(AseFrameTagsChunk frameTags)
        {
            m_AseFrameTagsChunk = frameTags;
        }

        public void VisitLayerChunk(AseLayerChunk layer)
        {
            m_LayerChunks.Add(layer);
        }

        public void VisitOldPaletteChunk(AseOldPaletteChunk palette)
        {
            m_GetPixelArgs.Palette.Clear();
            m_GetPixelArgs.Palette.AddRange(palette.Colors.Select(c => new Color32(c.red, c.green, c.blue, 255)));
            m_GetPixelArgs.Palette[TransparentIndex] = Color.clear;
        }

        public void VisitPaletteChunk(AsePaletteChunk palette)
        {
            m_GetPixelArgs.Palette.Clear();
            m_GetPixelArgs.Palette.AddRange(palette.Entries.Select(e => new Color32(e.Red, e.Green, e.Blue, e.Alpha)));
            m_GetPixelArgs.Palette[TransparentIndex] = Color.clear;
        }

        public void VisitSliceChunk(AseSliceChunk slice)
        {
            if (string.Equals("unity:pivot", slice.Name, StringComparison.OrdinalIgnoreCase))
            {
                // Assumes the first slice entry under pivot is the pivot for our sprite
                // The center of the slice is our pivot point. This allows for half-pixel pivots.
                var entry = slice.Entries[0];
                float pw = entry.Width;
                float ph = entry.Height;

                float px = entry.OriginX + pw * 0.5f;
                float py = entry.OriginY + ph * 0.5f;

                m_Pivot = new Vector2(px / CanvasWidth, 1.0f - py / CanvasHeight);
            }
        }

        public void VisitUserDataChunk(AseUserDataChunk userData)
        {
        }

        public void VisitTilesetChunk(AseTilesetChunk tileset)
        {
            m_TilesetChunks.Add(tileset);
        }

        private void BuildAnimations()
        {
            // Frame indices to be used in animations
            var frameIndices = Enumerable.Range(0, m_Frames.Count).ToList();

            // If we have any frame tags then make animations out of them
            if (m_AseFrameTagsChunk != null)
            {
                foreach (var entry in m_AseFrameTagsChunk.Entries)
                {
                    var animIndices = Enumerable.Range(entry.FromFrame, entry.ToFrame - entry.FromFrame + 1).ToList();
                    MakeAnimationClip(entry.Name, !entry.IsOneShot, animIndices);

                    // 同步填充 TagSprites 字典
                    var tagSpriteList = new List<Sprite>(animIndices.Count);
                    foreach (var idx in animIndices)
                    {
                        if (idx >= 0 && idx < m_Sprites.Count)
                            tagSpriteList.Add(m_Sprites[idx]);
                    }

                    m_TagSprites[entry.Name] = tagSpriteList;

                    // Remove the indices from the pool of animation frames
                    frameIndices.RemoveAll(i => i >= animIndices.First() && i <= animIndices.Last());
                }
            }

            // if (frameIndices.Count > 0)
            // {
            //     // Make an animation out of any left over (untagged) frames
            //     MakeAnimationClip("Untagged", true, frameIndices);
            // }
        }

        private void MakeAnimationClip(string animationName, bool isLooping, List<int> frameIndices)
        {
            animationName = m_UniqueNameifierAnimations.MakeUniqueName(animationName);
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var clipName = $"{assetName}.Clip.{animationName}";
            var clipId = $"Clip.{animationName}";

            var clip = new AnimationClip();
            clip.name = clipName;
            clip.frameRate = EffectiveFrameRate;


            // Black magic for creating a sprite animation curve
            // from: https://answers.unity.com/questions/1080430/create-animation-clip-from-sprites-programmaticall.html
            var binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "";
            binding.propertyName = "m_Sprite";

            var time = 0.0f;
            var keys = new ObjectReferenceKeyframe[frameIndices.Count];

            // Keep track of animation events
            List<AnimationEvent> animationEvents = new List<AnimationEvent>();

            for (int i = 0; i < keys.Length; i++)
            {
                var frameIndex = frameIndices[i];

                var key = new ObjectReferenceKeyframe();
                key.time = time;
                key.value = m_Sprites[frameIndex];
                keys[i] = key;

                // Are there any animation events to add for this frame?
                var frame = m_Frames[frameIndex];
                foreach (var celData in frame.Chunks.OfType<AseCelChunk>())
                {
                    // Cel data on invisible layers is ignored
                    if (m_LayerChunks[celData.LayerIndex].IsVisible && !string.IsNullOrEmpty(celData.UserDataText))
                    {
                        // Is the user data of "event:SomeName" format?
                        const string eventTag = "event:";
                        if (celData.UserDataText.StartsWith(eventTag, StringComparison.OrdinalIgnoreCase))
                        {
                            string eventName = celData.UserDataText.Substring(eventTag.Length);
                            if (!string.IsNullOrEmpty(eventName))
                            {
                                var animationEvent = new AnimationEvent();
                                animationEvent.functionName = eventName;
                                animationEvent.time = time;
                                animationEvents.Add(animationEvent);
                            }
                        }
                    }
                }

                // Advance time for next frame
                time += m_Frames[frameIndex].FrameDurationMs / 1000.0f;
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            // Settings for looping
            AnimationClipSettings settings = new AnimationClipSettings();
            settings.startTime = 0;
            settings.stopTime = time;
            settings.loopTime = isLooping;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Animation events
            if (animationEvents.Any())
            {
                AnimationUtility.SetAnimationEvents(clip, animationEvents.ToArray());
            }

            m_Context.AddObjectToAsset(clipId, clip);
            m_AnimationClips.Add(clip);
        }
    }
}
