using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;

namespace SubSync
{
    public static class Utilities
    {
        public static byte[] ComputeMovieHash(string filename)
        {
            byte[] result;
            using (Stream input = File.OpenRead(filename))
            {
                result = ComputeMovieHash(input);
            }
            return result;
        }

        public static byte[] ComputeMovieHash(Stream input)
        {
            long lhash, streamsize;
            streamsize = input.Length;
            lhash = streamsize;

            long i = 0;
            byte[] buffer = new byte[sizeof(long)];
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }

            input.Position = Math.Max(0, streamsize - 65536);
            i = 0;
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }
            input.Close();
            byte[] result = BitConverter.GetBytes(lhash);
            Array.Reverse(result);
            return result;
        }

        public static string ToHexadecimal(byte[] bytes)
        {
            StringBuilder hexBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                hexBuilder.Append(bytes[i].ToString("x2"));
            }
            return hexBuilder.ToString();
        }

        public static string DecompressGzipBase64(string base64)
        {
            var compressedBytes = Convert.FromBase64String(base64);
            var data = GzipDecompress(compressedBytes);
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] GzipDecompress(byte[] gzip)
        {
            var buffer = new byte[4096];
            using (var stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            using (var memory = new MemoryStream())
            {
                var size = 0;
                while ((size = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memory.Write(buffer, 0, size);
                }
                return memory.ToArray();
            }
        }

    }
}
