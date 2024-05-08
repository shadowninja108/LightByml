using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace LightByml.Lp.Byml.Reader
{
    public ref struct BymlStringTableIter
    {
        public Pointer Pointer;
        public bool IsInvertOrder;

        public BymlStringTableIter(Pointer pointer, bool isInvertOrder)
        {
            Pointer = pointer;
            IsInvertOrder = isInvertOrder;
        }

        public readonly ref BymlContainerHeader Container => ref Pointer.Cast<BymlContainerHeader>();
        public readonly bool IsValidate => !Pointer.IsNull;
        public readonly int Size => Container.GetCount(IsInvertOrder);
        public readonly Pointer AddressTable => Pointer.Add(Unsafe.SizeOf<BymlContainerHeader>());

        public readonly int GetStringAddress(int index)
        {
            var address = AddressTable.Add(index * Unsafe.SizeOf<int>()).Cast<int>();
            if (!IsInvertOrder)
                return address;
            else
                return address.Flip();
        }

        public readonly int GetEndAddress()
        {
            return GetStringAddress(Size);
        }

        public readonly unsafe string GetString(int index)
        {
            /* Deviate for efficiency. */
            var (start, end) = GetStringRange(index);
            var slice = Pointer.Add(start).GetSpan(end - start - 1);

            if (slice.IsEmpty)
                return string.Empty;

            fixed (byte* ptr = slice)
            {
                return Marshal.PtrToStringUTF8((IntPtr) ptr, slice.Length);
            }

            return Encoding.UTF8.GetString(slice);

            //return Pointer.Add(GetStringAddress(index)).GetUtf8String();
        }

        public readonly int GetStringSize(int index)
        {
            var indicies = AddressTable.Add(index * Unsafe.SizeOf<int>()).GetSpan<int>(2);

            var start = indicies[0];
            var end = indicies[1];

            if (IsInvertOrder)
            {
                start.FlipInPlace();
                end.FlipInPlace();
            }

            return end - start - 1;
        }

        /* Unofficial. */
        private readonly (int, int) GetStringRange(int index)
        {
            var indicies = AddressTable.Add(index * Unsafe.SizeOf<int>()).GetSpan<int>(2);

            var start = indicies[0];
            var end = indicies[1];

            if (IsInvertOrder)
            {
                start.FlipInPlace();
                end.FlipInPlace();
            }

            return (start, end);
        }

        public int FindStringIndex(string key)
        {
            int count = Size;
            if (count < 1)
                return -1;

            var indicies = Pointer.Add(Unsafe.SizeOf<BymlContainerHeader>()).GetSpan<uint>(count + 1);

            var start = 0;
            var end = count;
            int mid = -1;
            while (start < end)
            {
                mid = (start + end) / 2;
                var idx = (int)indicies[mid];
                if(IsInvertOrder)
                    idx.FlipInPlace();
                var ptr = Pointer.Add(idx);
                var str = ptr.GetUtf8String();
                var cmp = string.Compare(key, str, StringComparison.Ordinal);
                if (cmp == 0)
                    break;
                if (cmp < 0)
                    end = mid;
                if (cmp > 0)
                    start = mid + 1;
                mid = -1;
            }

            return mid;
        }
    }
}
