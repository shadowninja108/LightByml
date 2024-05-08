using System.Runtime.InteropServices;

namespace LightByml.Lp.Byml.Reader
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
    public struct BymlContainerHeader
    {
        [FieldOffset(0)] public BymlNodeId Type;
        [FieldOffset(1)] public Int24 PackedSize;

        public readonly int GetCount(bool isInvertOrder)
        {
            uint count;
            if (!isInvertOrder)
                count = PackedSize.Value;
            else
                count = PackedSize.ValueFlipped;
            return (int)count;
        }
    }
}
