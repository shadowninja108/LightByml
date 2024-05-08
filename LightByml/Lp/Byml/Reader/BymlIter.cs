namespace LightByml.Lp.Byml.Reader
{
    public ref struct BymlIter
    {
        public readonly Pointer HeaderPtr;
        public Pointer CurrentPtr;

        public readonly ref BymlHeader Header;

        public BymlIter()
        {
            HeaderPtr = Pointer.Null;
            CurrentPtr = Pointer.Null;
        }

        public BymlIter(Span<byte> data)
        {
            var ptr = new Pointer() { Binary = data };

            HeaderPtr = ptr;
            CurrentPtr = Pointer.Null;

            if (BymlLocalUtil.VerifiByml(ptr))
            {
                Header = ref HeaderPtr.Cast<BymlHeader>();
                if(Header.GetDataOffset() != 0)
                    CurrentPtr = ptr.Add(Header.GetDataOffset());
            }
            else
            {
                HeaderPtr = Pointer.Null;;
                CurrentPtr = Pointer.Null;
            }
        }

        public BymlIter(Pointer header, Pointer current)
        {
            HeaderPtr = header;
            Header = ref header.Cast<BymlHeader>();
            CurrentPtr = current;
        }

        public BymlIter(in BymlIter other)
        {
            this = other;
        }

        public readonly bool IsValid => !CurrentPtr.IsNull;
        public readonly bool IsTypeHash => (BymlNodeId)CurrentPtr.Value == BymlNodeId.Hash;
        public readonly bool IsTypeArray => (BymlNodeId)CurrentPtr.Value == BymlNodeId.Array;
        public readonly bool IsTypeContainer => IsTypeArray || IsTypeHash;
        public readonly bool IsInvertOrder => Header.IsInvertOrder;

        public readonly int Size
        {
            get
            {
                if (CurrentPtr.IsNull)
                    return 0;
                if (!IsTypeContainer)
                    return 0;

                return CurrentPtr.Cast<BymlContainerHeader>().GetCount(IsInvertOrder);
            }
        }

        public readonly bool IsExistKey(string key)
        {
            if (!IsValid) return false;
            if (!IsTypeHash) return false;

            var hashKeyTable = BymlLocalUtil.GetHashKeyTable(HeaderPtr);
            if (!hashKeyTable.IsValidate)
                return false;

            var index = hashKeyTable.FindStringIndex(key);
            if (index < 0)
                return false;

            var hash = new BymlHashIter(CurrentPtr, IsInvertOrder);
            return !hash.FindPair(index).IsNull;
        }

        public readonly int GetKeyIndex(string key)
        {
            var hashKeyTable = BymlLocalUtil.GetHashKeyTable(HeaderPtr);
            if (!hashKeyTable.IsValidate)
                return -1;
            return hashKeyTable.FindStringIndex(key);
        }

        public readonly BymlIter GetIterByIndex(int index)
        {
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
            {
                return new BymlIter();
            }
            
            TryConvertIter(out var iter, in data);
            return iter;
        }

        public readonly bool GetBymlDataByIndex(ref BymlData data, int index)
        {
            if (CurrentPtr.IsNull)
                return false;

            if (IsTypeHash)
            {
                var hash = new BymlHashIter(CurrentPtr, IsInvertOrder);
                return hash.GetDataByIndex(ref data, index);
            }

            if (IsTypeArray)
            {
                var array = new BymlArrayIter(CurrentPtr, IsInvertOrder);
                return array.GetDataByIndex(ref data, index);
            }

            return false;
        }

        public readonly bool GetBymlDataByKey(ref BymlData data, string key)
        {
            if (!IsValid)
                return false;
            if (!IsTypeHash)
                return false;

            var hashKeyTable = BymlLocalUtil.GetHashKeyTable(HeaderPtr);
            if (!hashKeyTable.IsValidate)
                return false;

            var isInvertOrder = IsInvertOrder;
            var hash = new BymlHashIter(CurrentPtr, IsInvertOrder);
            var size = hash.Size;
            if (size < 1)
                return false;

            var start = 0;
            var end = size;
            while (start < end)
            {
                var mid = (start + end) / 2;
                var pairPtr = hash.GetPairByIndex(mid);
                ref var pair = ref pairPtr.Cast<BymlHashIter.Pair>();
                var pairKey = pair.GetKey(isInvertOrder);
                var pairKeyString = hashKeyTable.GetString(pairKey);
                var cmp = string.Compare(key, pairKeyString, StringComparison.Ordinal);
                if (cmp == 0)
                {
                    data.Set(in pair, isInvertOrder);
                    return true;
                }

                if (cmp < 0)
                    end = mid;
                if (cmp > 0)
                    start = mid + 1;
            }
            return false;
        }

        public readonly bool GetBymlDataByKeyIndex(ref BymlData data, int index)
        {
            if(!IsValid)
                return false;
            if(!IsTypeHash)
                return false;

            var hash = new BymlHashIter(CurrentPtr, IsInvertOrder);
            return hash.GetDataByKey(ref data, index);
        }

        public readonly bool GetBymlDataAndKeyName(ref BymlData data, out string? keyName, int index)
        {
            keyName = null;
            if (!IsValid)
                return false;
            if (!IsTypeHash)
                return false;

            var hash = new BymlHashIter(CurrentPtr, IsInvertOrder);
            var pairPtr = hash.GetPairByIndex(index);
            if (pairPtr.IsNull)
                return false;

            /* Null check data. */
            ref var pair = ref pairPtr.Cast<BymlHashIter.Pair>();
            data.Set(in pair, IsInvertOrder);

            var hashKeyTable = BymlLocalUtil.GetHashKeyTable(HeaderPtr);
            if (!hashKeyTable.IsValidate)
                return false;

            var key = pair.GetKey(IsInvertOrder);
            keyName = hashKeyTable.GetString(key);
            return true;
        }

        public readonly bool GetKeyName(out string? keyName, int index)
        {
            /* Normally null ptr, but this is easier. */
            var data = new BymlData();
            return GetBymlDataAndKeyName(ref data, out keyName, index);
        }

        public readonly bool TryGetIterByIndex(out BymlIter iter, int index)
        {
            iter = GetIterByIndex(index);
            return !iter.HeaderPtr.IsNull;
        }

        public readonly bool TryGetIterAndKeyNameByIndex(out BymlIter iter, out string? keyName, int index)
        {
            var data = new BymlData();
            if (!GetBymlDataAndKeyName(ref data, out keyName, index))
            {
                return TryGetIterByIndex(out iter, index);
            }
            return TryConvertIter(out iter, in data);
        }

        public readonly bool TryGetIterByKey(out BymlIter iter, string key)
        {
            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
            {
                iter = new BymlIter();
                return false;
            }

            return TryConvertIter(out iter, in data);
        }

        public readonly bool TryGetStringByKey(out string? value, string key)
        {
            value = null;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if (data.Type == BymlNodeId.Null)
                return false;
            return TryConvertString(out value, in data);
        }

        public readonly bool TryConvertString(out string? value, in BymlData data)
        {
            value = null;
            if (data.Type != BymlNodeId.String)
                return false;

            var stringTablePtr = HeaderPtr.Add(Header.GetStringTableOffset());
            var stringTable = new BymlStringTableIter(stringTablePtr, Header.IsInvertOrder);
            value = stringTable.GetString(data.RawValue);
            return true;
        }

        public readonly bool TryGetBinaryByKey(out Span<byte> value, string key)
        {
            value = null;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            return TryConvertBinary(out value, in data);
        }

        public readonly bool TryConvertBinary(out Span<byte> value, scoped in BymlData data)
        {
            value = Span<byte>.Empty;
            if (data.Type != BymlNodeId.Bin)
                return false;

            value = BymlLocalUtil.GetBinaryIter(HeaderPtr, data.ValueAsUInt, IsInvertOrder);
            return true;
        }

        public readonly bool TryGetBoolByKey(out bool value, string key)
        {
            value = false;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if(data.Type == BymlNodeId.Null)
                return false;

            return TryConvertBool(out value, in data);
        }

        public readonly bool TryConvertBool(out bool value, scoped in BymlData data)
        {
            value = false;
            if(data.Type != BymlNodeId.Bool && data.Type != BymlNodeId.Int)
                return false;

            value = data.ValueAsUInt != 0;
            return true;
        }

        public readonly bool TryGetIntByKey(out int value, string key)
        {
            value = 0;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if (data.Type == BymlNodeId.Null)
                return false;

            return TryConvertInt(out value, in data);
        }

        public readonly bool TryGetIntByKey(Span<int> values, string key)
        {
            if (!TryGetIterByKey(out var iter, key))
                return false;

            var count = Size;
            var toRead = Math.Min(values.Length, count);
            if(toRead < 1)
                return true;

            for (var i = 0; i < toRead; i++)
            {
                if(!iter.TryGetIntByIndex(out values[i], i))
                    return false;
            }

            return true;
        }

        public readonly bool TryGetIntByIndex(out int value, int index)
        {
            value = 0;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertInt(out value, in data);
        }

        public readonly bool TryConvertInt(out int value, scoped in BymlData data)
        {
            value = 0;
            if(data.Type != BymlNodeId.Int)
                return false;
            value = data.RawValue;
            return true;
        }

        public bool TryGetUIntByKey(out uint value, string key)
        {
            value = 0;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if (data.Type == BymlNodeId.Null)
                return false;

            return TryConvertUInt(out value, in data);
        }

        public readonly bool TryConvertUInt(out uint value, scoped in BymlData data)
        {
            value = 0;
            if (data.Type == BymlNodeId.UInt)
            {
                value = data.ValueAsUInt;
                return true;
            }

            if (data.Type == BymlNodeId.Int)
            {
                value = (uint)Math.Max(0, data.RawValue);
                return data.RawValue >= 0;
            }
            return false;
        }

        public readonly bool TryGetUIntByKey(Span<uint> values, string key)
        {
            if (!TryGetIterByKey(out var iter, key))
                return false;

            var count = Size;
            var toRead = Math.Min(values.Length, count);
            if (toRead < 1)
                return true;

            for (var i = 0; i < toRead; i++)
            {
                if (!iter.TryGetUIntByIndex(out values[i], i))
                    return false;
            }

            return true;
        }

        public readonly bool TryGetUIntByIndex(out uint value, int index)
        {
            value = 0;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertUInt(out value, in data);
        }

        public readonly bool TryGetFloatByKey(out float value, string key)
        {
            value = 0;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if (data.Type == BymlNodeId.Null)
                return false;

            return TryConvertFloat(out value, in data);
        }

        public readonly bool TryConvertFloat(out float value, scoped in BymlData data)
        {
            value = 0;
            if (data.Type != BymlNodeId.Float)
                return false;
            value = data.ValueAsFloat;
            return true;
        }

        public readonly bool TryGetFloatByKey(Span<float> values, string key)
        {
            if (!TryGetIterByKey(out var iter, key))
                return false;

            var count = Size;
            var toRead = Math.Min(values.Length, count);
            if (toRead < 1)
                return true;

            for (var i = 0; i < toRead; i++)
            {
                if (!iter.TryGetFloatByIndex(out values[i], i))
                    return false;
            }

            return true;
        }

        public readonly bool TryGetFloatByIndex(out float value, int index)
        {
            value = 0;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertFloat(out value, in data);
        }

        public readonly bool TryGetInt64ByKey(out long value, string key)
        {
            value = 0;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if (data.Type == BymlNodeId.Null)
                return false;

            return TryConvertInt64(out value, in data);
        }

        public readonly bool TryConvertInt64(out long value, scoped in BymlData data)
        {
            value = 0;

            var innerValue = data.ValueAsUInt;
            if (data.Type == BymlNodeId.Int)
            {
                value = (int)innerValue;
                return true;
            }

            if (data.Type == BymlNodeId.UInt)
            {
                value = innerValue;
                return true;
            }

            if (data.Type == BymlNodeId.Int64)
            {
                value = (long)BymlLocalUtil.GetData64Bit(HeaderPtr, innerValue, IsInvertOrder);
                return true;
            }

            return false;
        }

        public readonly bool TryGetUInt64ByKey(out ulong value, string key)
        {
            value = 0;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if (data.Type == BymlNodeId.Null)
                return false;

            return TryConvertUInt64(out value, in data);
        }

        public readonly bool TryConvertUInt64(out ulong value, scoped in BymlData data)
        {
            value = 0;

            var innerValue = data.ValueAsUInt;
            if (data.Type == BymlNodeId.Int)
            {
                value = innerValue;
                return data.RawValue >= 0;
            }

            if (data.Type == BymlNodeId.UInt)
            {
                value = innerValue;
                return true;
            }

            /* Yes, they just...blindly assume there's a big data? */
            var value64 = BymlLocalUtil.GetData64Bit(HeaderPtr, innerValue, IsInvertOrder);

            if (data.Type == BymlNodeId.UInt64)
            {
                value = value64;
                return true;
            }

            if (data.Type == BymlNodeId.Int64)
            {
                value = Math.Max(0, value64);
                return data.RawValue >= 0;
            }

            return false;
        }

        public bool TryGetDoubleByKey(out double value, string key)
        {
            value = 0;

            var data = new BymlData();
            if (!GetBymlDataByKey(ref data, key))
                return false;
            if (data.Type == BymlNodeId.Null)
                return false;

            return TryConvertDouble(out value, in data);
        }

        public readonly bool TryConvertDouble(out double value, scoped in BymlData data)
        {
            value = 0;

            if (data.Type == BymlNodeId.Float)
            {
                value = data.ValueAsFloat;
                return true;
            }

            if (data.Type == BymlNodeId.Double)
            {
                value = BitConverter.UInt64BitsToDouble(BymlLocalUtil.GetData64Bit(HeaderPtr, (uint)data.RawValue, IsInvertOrder));
                return true;
            }

            return false;
        }

        public bool TryGetStringByIndex(out string? value, int index)
        {
            value = null;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertString(out value, in data);
        }

        public bool TryGetBinaryByIndex(out Span<byte> value, int index)
        {
            value = null;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertBinary(out value, in data);
        }

        public bool TryGetBoolByIndex(out bool value, int index)
        {
            value = false;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertBool(out value, in data);
        }

        public bool TryGetInt64ByIndex(out long value, int index)
        {
            value = 0;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertInt64(out value, in data);
        }

        public bool TryGetUInt64ByIndex(out ulong value, int index)
        {
            value = 0;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertUInt64(out value, in data);
        }

        public bool TryGetDoubleByIndex(out double value, int index)
        {
            value = 0;
            var data = new BymlData();
            if (!GetBymlDataByIndex(ref data, index))
                return false;
            return TryConvertDouble(out value, in data);
        }

        public readonly bool TryConvertIter(out BymlIter iter, scoped in BymlData data)
        {
            if (data.Type == BymlNodeId.Array || data.Type == BymlNodeId.Hash)
            {
                iter = new BymlIter(HeaderPtr, HeaderPtr.Add(data.RawValue));
                return true;
            }

            if (data.Type == BymlNodeId.Null)
            {
                iter = new BymlIter(HeaderPtr, Pointer.Null);
                return true;
            }

            iter = new BymlIter();
            return false;
        }

        public readonly bool IsEqualData(in BymlIter other)
        {
            if(HeaderPtr.IsNull)
                return false;
            if (other.HeaderPtr.IsNull)
                return false;

            return HeaderPtr.Equals(other.HeaderPtr) && CurrentPtr.Equals(other.CurrentPtr);
        }
    }
}
