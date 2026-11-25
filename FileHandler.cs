using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace lvglConverter
{
    /// <summary>
    /// Worker that converts a slice of PNG map tiles into an LVGL binary image (.bin).
    /// Runs on background threads (one per CPU-ish partition) launched from MainWindow.
    /// </summary>
    public class FileHandler
    {
        public List<string> Path;
        public List<string> UnhandledFiles = new List<string>();
        public int StartIndex;
        public int Length;
        public int HandledCount;
        public bool IgnoreExistedFile;
        public bool IsCompleted;
        public LVGLImageType Type = LVGLImageType.RGB565;

        public FileHandler(List<string> path, int startIndex, int length, LVGLImageType type, bool ignoreExistedFile)
        {
            Path = path;
            StartIndex = startIndex;
            Length = length;
            Type = type;
            IgnoreExistedFile = ignoreExistedFile;
        }

        private List<Color> GetPixels(string path, out int width, out int height)
        {
            List<Color> list = new List<Color>();
            Bitmap bitmap = new Bitmap(path);
            width = bitmap.Width;
            height = bitmap.Height;
            for (int i = 0; i < bitmap.Height; i++)
            {
                for (int j = 0; j < bitmap.Width; j++)
                {
                    list.Add(bitmap.GetPixel(j, i));
                }
            }
            return list;
        }

        // LVGL RGB565 layout: high16 = R(5) | G(6) | B(5), little-endian. Header packs WxH + flags.
        private byte[] RGB565GetBinary(List<Color> pixels, int width, int height)
        {
            List<byte> list = new List<byte>();
            uint num = 0u;
            num = (uint)height;
            num <<= 11;
            num |= (uint)width;
            num <<= 2;
            num <<= 3;
            num <<= 5;
            num |= 4;
            list.Add((byte)(num & 0xFF));
            list.Add((byte)((num >> 8) & 0xFF));
            list.Add((byte)((num >> 16) & 0xFF));
            list.Add((byte)((num >> 24) & 0xFF));
            for (int i = 0; i < pixels.Count; i++)
            {
                int num2 = 0;
                num2 |= pixels[i].R >> 3;
                num2 <<= 6;
                num2 |= pixels[i].G >> 2;
                num2 <<= 5;
                num2 |= pixels[i].B >> 3;
                list.Add((byte)(num2 & 0xFF));
                list.Add((byte)((num2 >> 8) & 0xFF));
            }
            return list.ToArray();
        }

        // RGB565SWAP: same packing but big-endian pixel words (high byte first).
        private byte[] RGB565SwapGetBinary(List<Color> pixels, int width, int height)
        {
            List<byte> list = new List<byte>();
            uint num = 0u;
            num = (uint)height;
            num <<= 11;
            num |= (uint)width;
            num <<= 2;
            num <<= 3;
            num <<= 5;
            num |= 4;
            list.Add((byte)(num & 0xFF));
            list.Add((byte)((num >> 8) & 0xFF));
            list.Add((byte)((num >> 16) & 0xFF));
            list.Add((byte)((num >> 24) & 0xFF));
            for (int i = 0; i < pixels.Count; i++)
            {
                int num2 = 0;
                num2 |= pixels[i].R >> 3;
                num2 <<= 6;
                num2 |= pixels[i].G >> 2;
                num2 <<= 5;
                num2 |= pixels[i].B >> 3;
                list.Add((byte)((num2 >> 8) & 0xFF));
                list.Add((byte)(num2 & 0xFF));
            }
            return list.ToArray();
        }

        private byte[] GetBinary(List<Color> pixels, int width, int height)
        {
            return Type switch
            {
                LVGLImageType.RGB332 => null,
                LVGLImageType.RGB565 => RGB565GetBinary(pixels, width, height),
                LVGLImageType.RGB565SWAP => RGB565SwapGetBinary(pixels, width, height),
                LVGLImageType.RGB888 => null,
                _ => null,
            };
        }

        public void Start()
        {
            for (int i = 0; i < Length; i++)
            {
                string text = Path[i + StartIndex];
                int width = 0;
                int height = 0;
                try
                {
                    FileInfo fileInfo = new FileInfo(text);
                    string text2 = fileInfo.DirectoryName + "\\" + fileInfo.Name.Replace(fileInfo.Extension, "") + ".bin";
                    FileInfo fileInfo2 = new FileInfo(text2);
                    if (!IgnoreExistedFile || !File.Exists(text2) || fileInfo2.Length != 131076)
                    {
                        List<Color> pixels = GetPixels(text, out width, out height);
                        byte[] binary = GetBinary(pixels, width, height);
                        FileStream fileStream = new FileStream(text2, FileMode.Create);
                        fileStream.Write(binary, 0, binary.Length);
                        fileStream.Close();
                    }
                    HandledCount++;
                }
                catch (Exception)
                {
                    UnhandledFiles.Add(text);
                }
            }
            IsCompleted = true;
        }
    }
}
