using System.Collections.Generic;
using System.Linq;

namespace Aseprite2Unity.Editor
{
    public class AseFrameTagsChunk : AseChunk
    {
        public override ChunkType ChunkType => ChunkType.FrameTags;

        public ushort NumTags { get; }
        public List<AseFrameTagEntry> Entries { get; }

        public AseFrameTagsChunk(AseFrame frame, AseReader reader)
            : base(frame)
        {
            NumTags = reader.ReadWORD();

            // Ignore next 8 bytes
            reader.ReadBYTEs(8);

            Entries = Enumerable.Repeat<AseFrameTagEntry>(null, NumTags).ToList();
            for (int i = 0; i < (int)NumTags; i++)
            {
                Entries[i] = new AseFrameTagEntry(reader);
            }

            // Aseprite 格式：FrameTags chunk 后紧跟 N 个 UserData chunk，按顺序对应各 Entry
            // 设置待分发状态，供后续 AseUserDataChunk 读取时使用
            if (NumTags > 0)
            {
                reader.PendingFrameTagsChunk = this;
                reader.PendingFrameTagIndex = 0;
            }
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitFrameTagsChunk(this);
        }
    }
}
