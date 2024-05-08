using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace LightByml
{
    public ref struct Pointer
    {
        public Span<byte> Binary;
        public int Position;

        public readonly bool IsNull => Binary.IsEmpty;


        public readonly byte Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Binary[Position];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pointer(Span<byte> binary, int position)
        {
            Binary = binary; 
            Position = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Pointer Add(int relative)
        {
            return new Pointer(Binary, Position + relative);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<byte> GetSpan(int count)
        {
            return Binary.Slice(Position, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> GetSpan<T>(int count) where T : struct
        {
            return MemoryMarshal.Cast<byte, T>(GetSpan(count * Unsafe.SizeOf<T>()));
        }

        public readonly ref T Cast<T>() where T : struct
        {
            return ref MemoryMarshal.AsRef<T>(Binary.Slice(Position));
        }

        public readonly string GetUtf8String()
        {
            var span = Binary.Slice(Position);
            var length = span.IndexOf((byte)0);
            return Encoding.UTF8.GetString(span[..length]);
        }

        public readonly bool Equals(in Pointer other)
        {
            return Binary == other.Binary && Position == other.Position;
        }

        public static Pointer Null => new() { Binary = Span<byte>.Empty };
    }
}
