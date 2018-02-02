using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Epsylon.ImageSharp.Procedural.Android
{
    // http://addreference.net/?p=158&ckattempt=1

    // ImageSharp can't read custom chunks yet.
    // https://github.com/SixLabors/ImageSharp/issues/251

    //https://github.com/iBotPeaches/Apktool/issues/1604

    //https://stackoverflow.com/questions/5079868/create-a-ninepatch-ninepatchdrawable-in-runtime

    public class NinePatchChunk
    {
        public int XDivsCount { get; set; }
        public int YDivsCount { get; set; }
        public int[] XDivs { get; set; }
        public int[] YDivs { get; set; }

        public int PaddingLeft { get; set; }
        public int PaddingRight { get; set; }
        public int PaddingTop { get; set; }
        public int PaddingBottom { get; set; }

        public static NinePatchChunk ExtractNinePatchChunk(Stream pngStream)
        {
            using (var reader = new BinaryReader(pngStream))
            {
                if (reader.ReadBytes(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }) == false) return null;

                do
                {
                    int length = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                    string type = Encoding.ASCII.GetString(reader.ReadBytes(4));

                    if (type == "npTc")
                    {
                        var result = new NinePatchChunk();

                        var data = reader.ReadBytes(length);

                        int readBigEndianInt(int i) => BitConverter.ToInt32(data.Skip(i).Take(4).Reverse().ToArray(), 0);

                        result.XDivsCount = data[1];
                        result.YDivsCount = data[2];

                        result.PaddingLeft = readBigEndianInt(12);
                        result.PaddingRight = readBigEndianInt(16);
                        result.PaddingTop = readBigEndianInt(20);
                        result.PaddingBottom = readBigEndianInt(24);

                        result.XDivs = Enumerable.Range(0, result.XDivsCount).Select(i => readBigEndianInt(32 + i * sizeof(int))).ToArray();
                        result.YDivs = Enumerable.Range(0, result.YDivsCount).Select(i => readBigEndianInt(32 + (result.XDivsCount + i) * sizeof(int))).ToArray();


                        return result;
                    }
                    else
                    {
                        reader.BaseStream.Seek(length + 4, SeekOrigin.Current);
                    }

                } while (pngStream.Position < pngStream.Length);
            }

            return null;
        }
    }

    
}
