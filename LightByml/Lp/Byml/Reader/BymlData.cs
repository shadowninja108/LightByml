namespace LightByml.Lp.Byml.Reader
{
    public struct BymlData
    {
        public int RawValue;
        public BymlNodeId Type;

        public void Set(in BymlHashIter.Pair pair, bool isInvertOrder)
        {
            Type = pair.Id;
            if (!isInvertOrder)
                RawValue = pair.Value;
            else
                RawValue = pair.Value.Flip();
        }

        public readonly uint ValueAsUInt => (uint) RawValue;
        public readonly float ValueAsFloat => BitConverter.Int32BitsToSingle(RawValue);
    }
}
