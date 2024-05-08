using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LightByml
{
    public static class Endian
    {
        public static Endianness Native =>
            BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;

        public static Endianness OppositeOfNative => Opposite(Native);

        public static Endianness Opposite(Endianness v) => v == Endianness.Little ? Endianness.Big : Endianness.Little;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FlipInPlace(this ref int v)
        {
            v = BinaryPrimitives.ReverseEndianness(v);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FlipInPlace(this ref ulong v)
        {
            v = BinaryPrimitives.ReverseEndianness(v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Flip(this int v)
        {
            return BinaryPrimitives.ReverseEndianness(v);
        }

        public static uint Flip(this uint v)
        {
            return BinaryPrimitives.ReverseEndianness(v);
        }

        // public static void FlipInPlace<T>(this ref T v) where T : struct, IBinaryInteger<T>
        // {
        //     var span = MemoryMarshal.CreateSpan(ref v, 1);
        //     var bytes = MemoryMarshal.AsBytes(span);
        //     bytes.Reverse();
        // }

        // public static T Flip<T>(this T v) where T : struct, IBinaryInteger<T>
        // {
        //     var n = v;
        //     n.FlipInPlace();
        //     return n;
        // }
    }
}
