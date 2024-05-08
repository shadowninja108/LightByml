using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LightByml.Lp.Byml
{
    public enum BymlNodeId : byte
    {
        String = 0xA0,
        Bin = 0xA1,
        Array = 0xC0,
        Hash = 0xC1,
        StringTable = 0xC2,
        PathArray = 0xC3,   /* Obscure, only observed in MK8DX. */
        Bool = 0xD0,
        Int = 0xD1,
        Float = 0xD2,
        UInt = 0xD3,
        Int64 = 0xD4,
        UInt64 = 0xD5,
        Double = 0xD6,
        Null = 0xFF,
    };

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct BymlHeader
    {
        public const ushort ExpectedBeMagic = 0x4259;
        public const ushort ExpectedLeMagic = 0x5942;

        public ushort Magic;
        public ushort Version;
        public uint HashKeyOffset;
        public uint StringTableOffset;
        public uint RootOrPathArrayOffset;

        public readonly bool IsInvertOrder =>
            Endian.Native switch
            {
                Endianness.Little => Magic == ExpectedLeMagic,
                Endianness.Big => Magic == ExpectedBeMagic,
                _ => false
            };

        public readonly int GetStringTableOffset()
        {
            if (IsInvertOrder)
                return (int)StringTableOffset.Flip();

            return (int)StringTableOffset;
        }

        public readonly int GetDataOffset()
        {
            if (IsInvertOrder)
                return (int)RootOrPathArrayOffset.Flip();

            /* I couldn't care less right now. God damn you Turbo. */
            return (int)RootOrPathArrayOffset;
        }
    }
}
