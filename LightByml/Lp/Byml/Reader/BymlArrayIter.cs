using System.Runtime.CompilerServices;

namespace LightByml.Lp.Byml.Reader
{
    public ref struct BymlArrayIter
    {
        public Pointer Pointer;
        public bool IsInvertOrder;

        public BymlArrayIter(Pointer pointer, bool isInvertOrder)
        {
            Pointer = pointer;
            Container = ref Pointer.Cast<BymlContainerHeader>();
            IsInvertOrder = isInvertOrder;
        }

        public readonly ref BymlContainerHeader Container;
        public readonly Pointer TypeTable => Pointer.Add(Unsafe.SizeOf<BymlContainerHeader>());
        public readonly Pointer DataTable => TypeTable.Add((Size + 4-1) & ~(4-1));

        public readonly int Size => Container.GetCount(IsInvertOrder);


        public bool GetDataByIndex(ref BymlData data, int index)
        {
            if(index < 0) 
                return false;

            var size = Size;
            if (size < 1)
                return false;

            var types = TypeTable.GetSpan<BymlNodeId>(size);
            data.Type = types[index];

            var datas = DataTable.GetSpan<int>(size);
            data.RawValue = datas[index];

            if(IsInvertOrder)
                data.RawValue.FlipInPlace();
            return true;
        }
    }
}
