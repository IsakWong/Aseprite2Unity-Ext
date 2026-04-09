namespace Aseprite2Unity.Editor
{
    public class AseFrameTagEntry
    {
        public ushort FromFrame { get; }
        public ushort ToFrame { get; }
        public LoopAnimationDirection LoopAnimationDirection { get; }
        public byte[] ColorRGB { get; }
        public string Name { get; }
        public bool IsOneShot { get; }
        public byte Loop { get; }

        /// <summary>
        /// Aseprite UserData 文本（FrameTags 后跟随的 UserData Chunk 按顺序分配到各 Entry）
        /// </summary>
        public string UserDataText { get; set; }

        /// <summary>
        /// Aseprite UserData 颜色 RGBA（FrameTags 后跟随的 UserData Chunk 按顺序分配到各 Entry）
        /// </summary>
        public byte[] UserDataColor { get; set; }

        public AseFrameTagEntry(AseReader reader)
        {
            FromFrame = reader.ReadWORD();
            ToFrame = reader.ReadWORD();
            LoopAnimationDirection = (LoopAnimationDirection)reader.ReadBYTE();
            Loop =  reader.ReadBYTE();
            // Ignore next 8 bytes
            reader.ReadBYTEs(7);

            ColorRGB = reader.ReadBYTEs(3);

            // Ignore a byte
            reader.ReadBYTE();

            Name = reader.ReadSTRING();

            IsOneShot = (Loop == 1);
        }
    }
}
