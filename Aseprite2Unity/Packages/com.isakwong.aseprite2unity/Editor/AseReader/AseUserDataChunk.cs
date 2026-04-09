using System;
using UnityEngine;

namespace Aseprite2Unity.Editor
{
    public class AseUserDataChunk : AseChunk
    {
        [Flags]
        public enum UserDataFlags : uint
        {
            HasText = 1,
            HasColor = 2,
            HasProperties = 4,
        }


        public override ChunkType ChunkType => ChunkType.UserData;

        public UserDataFlags Flags { get; }
        public string Text { get; }
        public byte[] ColorRGBA { get; }

        public AseUserDataChunk(AseFrame frame, AseReader reader)
            : base(frame)
        {
            Flags = (UserDataFlags)reader.ReadDWORD();
            
            if (Flags.HasFlag(UserDataFlags.HasText))
            {
                Text = reader.ReadSTRING();
            }

            if (Flags.HasFlag(UserDataFlags.HasColor))
            {
                ColorRGBA = reader.ReadBYTEs(4);
            }

            if (Flags.HasFlag(UserDataFlags.HasProperties))
            {
                var sizeInBytes = reader.ReadDWORD();
                var numberOfProperties = reader.ReadDWORD();
                Debug.LogWarning($"[Aseprite2Unity] UserData properties 暂不支持。Size = {sizeInBytes}, Properties = {numberOfProperties}");

                // Read the amount of data needed for this section so at least we can keep on reading other chunks
                int extra = (int)sizeInBytes - 8;
                if (extra >= 0)
                {
                    reader.ReadBYTEs(extra);
                }
            }

            // FrameTags 特殊处理：Aseprite 格式中 FrameTags chunk 后紧跟 N 个 UserData chunk，
            // 按顺序分配到各 FrameTagEntry
            if (reader.PendingFrameTagsChunk != null)
            {
                var tagsChunk = reader.PendingFrameTagsChunk;
                int idx = reader.PendingFrameTagIndex;

                if (idx < tagsChunk.Entries.Count)
                {
                    tagsChunk.Entries[idx].UserDataText = Text;
                    tagsChunk.Entries[idx].UserDataColor = ColorRGBA;
                    reader.PendingFrameTagIndex = idx + 1;
                }

                // 所有 Entry 分发完毕，清除待分发状态
                if (reader.PendingFrameTagIndex >= tagsChunk.Entries.Count)
                {
                    reader.PendingFrameTagsChunk = null;
                    reader.PendingFrameTagIndex = 0;
                }
            }
            else
            {
                // 普通情况：将 UserData 附加到前一个 Chunk
                if (reader.LastChunk != null)
                {
                    reader.LastChunk.UserDataText = Text;
                    reader.LastChunk.UserDataColor = ColorRGBA;
                }
            }
        }

        public override void Visit(IAseVisitor visitor)
        {
            visitor.VisitUserDataChunk(this);
        }
    }
}
