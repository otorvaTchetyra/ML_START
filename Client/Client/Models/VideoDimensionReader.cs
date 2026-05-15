using System.IO;
using System.Text;

namespace Client.Models
{
    public static class VideoDimensionReader
    {
        public static (int width, int height) TryRead(string path)
        {
            try
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".avi")
                    return ReadAvi(path);
                if (ext is ".mp4" or ".mov" or ".mkv")
                    return ReadMp4(path);
            }
            catch { }
            return (0, 0);
        }

        private static (int, int) ReadAvi(string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);
            fs.Seek(64, SeekOrigin.Begin);
            return (br.ReadInt32(), br.ReadInt32());
        }

        private static (int, int) ReadMp4(string path)
        {
            using var fs = File.OpenRead(path);
            return ScanBoxes(fs, fs.Length);
        }

        private static (int, int) ScanBoxes(FileStream fs, long end)
        {
            while (fs.Position < end - 8)
            {
                var start = fs.Position;
                var buf = new byte[8];
                if (fs.Read(buf, 0, 8) < 8) break;
                var size = (long)((buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3]);
                var name = Encoding.ASCII.GetString(buf, 4, 4);
                if (size == 1)
                {
                    var ext = new byte[8];
                    fs.Read(ext, 0, 8);
                    size = 0;
                    for (int i = 0; i < 8; i++) size = (size << 8) | ext[i];
                }
                if (size == 0) size = end - start;
                if (size < 8) break;
                var boxEnd = start + size;

                if (name is "moov" or "trak" or "mdia")
                {
                    var result = ScanBoxes(fs, boxEnd);
                    if (result != (0, 0)) return result;
                }
                else if (name == "tkhd")
                {
                    var ver = fs.ReadByte();
                    fs.Seek(3, SeekOrigin.Current);
                    fs.Seek(ver == 1 ? 32 : 20, SeekOrigin.Current);
                    fs.Seek(16, SeekOrigin.Current);
                    fs.Seek(36, SeekOrigin.Current);
                    var dim = new byte[8];
                    fs.Read(dim, 0, 8);
                    var w = (dim[0] << 8) | dim[1];
                    var h = (dim[4] << 8) | dim[5];
                    if (w > 0 && h > 0) return (w, h);
                }

                fs.Seek(boxEnd, SeekOrigin.Begin);
            }
            return (0, 0);
        }
    }
}
