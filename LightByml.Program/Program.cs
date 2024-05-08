using System.Diagnostics;
using LightByml.Lp.Byml.Reader;
using LightByml.Lp.Byml;
using System.Dynamic;
using System.Text.Json;
using System.Text;

namespace LightByml.Program
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // var dictfi = new FileInfo(@"Z:\Switch\Games\The Legend of Zelda Tears of the Kingdom\1.2.1\romfs\Pack\ZsDic\zs.zsdic");
            // var dict = new byte[dictfi.Length];
            // using (var stream = dictfi.OpenRead())
            //     stream.Read(dict);

            var fi = new FileInfo(@"R:\Games\Splatoon 3\7.2.0\Program\Data\RSDB\Tag.Product.720.rstbl.byml.zs");
            var bytes = DecompressZstd(fi);

            var iter = new BymlIter(bytes);
            var timer = new Stopwatch();
            timer.Start();
            var deserialized = VisitContainer(iter);
            Console.WriteLine($"{timer.Elapsed.TotalMilliseconds}ms");
            timer.Stop();
            var jsonbin = JsonSerializer.SerializeToUtf8Bytes(deserialized, new JsonSerializerOptions() { WriteIndented = true });
            var json = Encoding.UTF8.GetString(jsonbin);
            Console.WriteLine(deserialized.Data.Binary[0].Hash);
        }

        private static byte[] DecompressZstd(FileInfo info, byte[]? dict = null)
        {
            const int ZSTD_frameHeaderSize_max = 18;
            var frameHeader = new byte[ZSTD_frameHeaderSize_max];
            using var stream = info.OpenRead();
            stream.Read(frameHeader);
            stream.Position = 0;

            Stream decompressStream;
            if (dict != null)
            {
                decompressStream = new ZstdNet.DecompressionStream(stream, new ZstdNet.DecompressionOptions(dict));
            }
            else
            {
                decompressStream = new ZstdNet.DecompressionStream(stream);
            }

            using (decompressStream)
            {
                var decompressedSize = ZstdNet.Decompressor.GetDecompressedSize(frameHeader);
                var bytes = new byte[decompressedSize];
                decompressStream.Read(bytes);
                return bytes;
            }
        }

        private static dynamic? VisitData(in BymlIter iter, in BymlData data)
        {
            switch (data.Type)
            {
                case BymlNodeId.String:
                    if (!iter.TryConvertString(out var vs, in data))
                        throw new Exception();
                    return vs;
                case BymlNodeId.Bin:
                    if (!iter.TryConvertBinary(out var vbi, in data))
                        throw new Exception();
                    return vbi.ToArray();
                case BymlNodeId.Bool:
                    if (!iter.TryConvertBool(out var vb, in data))
                        throw new Exception();
                    return vb;
                case BymlNodeId.Int:
                    if (!iter.TryConvertInt(out var vi, in data))
                        throw new Exception();
                    return vi;
                case BymlNodeId.Float:
                    if (!iter.TryConvertFloat(out var vf, in data))
                        throw new Exception();
                    return vf;
                case BymlNodeId.UInt:
                    if (!iter.TryConvertUInt(out var vui, in data))
                        throw new Exception();
                    return vui;
                case BymlNodeId.Int64:
                    if (!iter.TryConvertInt64(out var vl, in data))
                        throw new Exception();
                    return vl;
                case BymlNodeId.UInt64:
                    if (!iter.TryConvertUInt64(out var vul, in data))
                        throw new Exception();
                    return vul;
                case BymlNodeId.Double:
                    if (!iter.TryConvertDouble(out var vd, in data))
                        throw new Exception();
                    return vd;
                case BymlNodeId.Null:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static object VisitArray(in BymlIter iter)
        {
            var size = iter.Size;
            var array = new object?[size];
            for (var i = 0; i < size; i++)
            {
                var data = new BymlData();
                if (!iter.GetBymlDataByIndex(ref data, i))
                    throw new Exception();

                if (data.Type == BymlNodeId.Array || data.Type == BymlNodeId.Hash)
                    array[i] = VisitContainer(iter.GetIterByIndex(i));
                else
                    array[i] = VisitData(in iter, in data);
            }

            return array;
        }

        private static dynamic VisitHash(in BymlIter iter)
        {
            var obj = new ExpandoObject();
            var dict = (IDictionary<string, object?>)obj;
            var size = iter.Size;
            for (var i = 0; i < size; i++)
            {
                iter.GetKeyName(out var key, i);

                var data = new BymlData();
                if (!iter.GetBymlDataByIndex(ref data, i))
                    throw new Exception();

                if (data.Type == BymlNodeId.Array || data.Type == BymlNodeId.Hash) 
                    dict[key!] = VisitContainer(iter.GetIterByIndex(i));
                else
                    dict[key!] = VisitData(in iter, in data);
            }

            return obj;
        }

        private static dynamic VisitContainer(in BymlIter iter)
        {
            if (iter.IsTypeArray)
            {
                return VisitArray(in iter);
            }
            if (iter.IsTypeHash)
            {
                return VisitHash(in iter);
            }
            throw new Exception();
        }
    }
}
