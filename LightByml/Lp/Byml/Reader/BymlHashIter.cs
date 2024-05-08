using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LightByml.Lp.Byml.Reader
{
    public ref struct BymlHashIter
    {
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
        public struct Pair
        {
            [FieldOffset(0)] public Int24 Key;
            [FieldOffset(3)] public BymlNodeId Id;
            [FieldOffset(4)] public int Value;

            public readonly int GetKey(bool isInvertOrder)
            {
                if (!isInvertOrder)
                    return (int)Key.Value;
                else
                    return (int)Key.ValueFlipped;
            }
        }

        public BymlHashIter(Pointer pointer, bool isInvertOrder)
        {
            Pointer = pointer;
            Container = ref Pointer.Cast<BymlContainerHeader>();
            IsInvertOrder = isInvertOrder;
        }

        public Pointer Pointer;
        public bool IsInvertOrder;

        public readonly ref BymlContainerHeader Container;
        public readonly Pointer PairTable => !Pointer.IsNull ? Pointer.Add(Unsafe.SizeOf<BymlContainerHeader>()) : Pointer.Null;
        public readonly int Size => Container.GetCount(IsInvertOrder);


        public readonly bool GetDataByIndex(ref BymlData data, int index)
        {
            if(Pointer.IsNull)
                return false;

            var count = Size;
            if (count < 1)
                return false;

            var pairs = PairTable.GetSpan<Pair>(count);
            ref var pair = ref pairs[index];

            data.Type = pair.Id;
            data.RawValue = pair.Value;
            if(IsInvertOrder)
                data.RawValue.FlipInPlace();

            return true;
        }

        public readonly bool GetDataByKey(ref BymlData data, int key)
        {
            var ptr = FindPair(key);
            if(ptr.IsNull)
                return false;

            var pair = ptr.Cast<Pair>();
            data.Type = pair.Id;
            data.RawValue = pair.Value;
            if(IsInvertOrder)
                data.RawValue.FlipInPlace();

            return true;
        }

        public readonly Pointer FindPair(int key)
        {
            var pairTable = PairTable;
            if (pairTable.IsNull)
                return Pointer.Null;

            var count = Size;
            if (count < 1)
                return Pointer.Null;

            var start = 0;
            var end = count;
            var pairPtr = Pointer.Null;
            while (start < end)
            {
                var mid = (start + end) / 2;
                pairPtr = pairTable.Add(mid * Unsafe.SizeOf<Pair>());
                var pairKey = pairPtr.Cast<Pair>().Key;
                uint keyValue;
                if (!IsInvertOrder)
                    keyValue = pairKey.Value;
                else
                    keyValue = pairKey.ValueFlipped;
                var cmp = key.CompareTo(keyValue);
                if (cmp == 0)
                    break;
                if (cmp < 0)
                    end = mid;
                if (cmp > 0)
                    start = mid + 1;
            }
            return pairPtr;
        }

        public readonly Pointer GetPairByIndex(int index)
        {
            if(index  < 0)
                return Pointer.Null;
            if(Size <= index)
                return Pointer.Null;

            return PairTable.Add(index * Unsafe.SizeOf<Pair>());
        }
    }
}
