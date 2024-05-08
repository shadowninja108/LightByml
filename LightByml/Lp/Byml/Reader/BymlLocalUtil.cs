using System.Runtime.CompilerServices;

namespace LightByml.Lp.Byml.Reader
{
    public static class BymlLocalUtil
    {
        public static bool VerifiByml(Pointer pointer)
        {
            /* TODO: */
            return true;
        }

        public static BymlStringTableIter GetHashKeyTable(Pointer headerPtr)
        {
            ref var header = ref headerPtr.Cast<BymlHeader>();
            bool nativeEndianness;
            switch (header.Magic)
            {
                case BymlHeader.ExpectedBeMagic:
                    nativeEndianness = true;
                    break;
                case BymlHeader.ExpectedLeMagic:
                    nativeEndianness = false;
                    break;
                default:
                    throw new Exception("Bad magic!");
            }

            var offset = (int)header.HashKeyOffset;
            if (!nativeEndianness)
                offset.FlipInPlace();

            Pointer ptr;
            if (offset != 0)
                ptr = headerPtr.Add(offset);
            else
                ptr = Pointer.Null;


            return new BymlStringTableIter()
            {
                Pointer = ptr,
                IsInvertOrder = !nativeEndianness
            };
        }

        public static Span<byte> GetBinaryIter(Pointer headerPtr, uint position, bool isInvertOrder)
        {
            var block = headerPtr.Add((int)position);
            var length = block.Cast<int>();
            if(isInvertOrder)
                length.FlipInPlace();

            return block.Add(Unsafe.SizeOf<int>()).GetSpan(length);
        }

        public static ulong GetData64Bit(Pointer headerPtr, uint position, bool isInvertOrder)
        {
            var pointer = headerPtr.Add((int)position);
            var val = pointer.Cast<ulong>();
            if(isInvertOrder)
                val.FlipInPlace();
            return val;
        }
    }
}
