using System.Runtime.InteropServices;

namespace LightByml
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3)]
    public struct Int24
    {
        public byte Byte0;
        public byte Byte1;
        public byte Byte2;

        public uint Value
        {
            readonly get =>
                (uint)Byte2 << 16 |
                (uint)Byte1 << 8 |
                (uint)Byte0 << 0;
            set
            {
                Byte0 = (byte)(value >> 0 & byte.MaxValue);
                Byte1 = (byte)(value >> 8 & byte.MaxValue);
                Byte2 = (byte)(value >> 16 & byte.MaxValue);
            }
        }

        public uint ValueFlipped
        {
            readonly get =>
                (uint)Byte0 << 16 |
                (uint)Byte1 << 8 |
                (uint)Byte2 << 0;
            set
            {
                Byte1 = (byte)(value >> 0 & byte.MaxValue);
                Byte1 = (byte)(value >> 8 & byte.MaxValue);
                Byte0 = (byte)(value >> 16 & byte.MaxValue);
            }
        }
    }
}
