using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace CriPakTools
{
    public class Tools
    {

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        public Tools()
        {

        }

        public Dictionary<string, string> ReadBatchScript(string batch_script_name)
        {
            //---------------------
            // TXT内部
            // original_file_name(in cpk),patch_file_name(in folder)
            // /HD_font_a.ftx,patch/BOOT.cpk_unpacked/HD_font_a.ftx
            // OTHER/ICON0.PNG,patch/BOOT.cpk_unpacked/OTHER/ICON0.PNG

            Dictionary<string, string> flist = new Dictionary<string, string>();

            StreamReader sr = new StreamReader(batch_script_name, Encoding.Default);
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.IndexOf(",") > -1)
                //只读取格式正确的行
                {
                    line = line.Replace("\n", "");
                    line = line.Replace("\r", "");
                    string[] currentValue = line.Split(',');
                    flist.Add(currentValue[0], currentValue[1]);
                }


            }

            return flist;
        }

        public string ReadCString(BinaryReader br, int MaxLength = -1, long lOffset = -1, Encoding enc = null)
        {
            int Max;
            if (MaxLength == -1)
                Max = 255;
            else
                Max = MaxLength;

            long fTemp = br.BaseStream.Position;
            byte bTemp = 0;
            int i = 0;
            string result = "";

            if (lOffset > -1)
            {
                br.BaseStream.Seek(lOffset, SeekOrigin.Begin);
            }

            do
            {
                bTemp = br.ReadByte();
                if (bTemp == 0)
                    break;
                i += 1;
            } while (i < Max);

            if (MaxLength == -1)
                Max = i + 1;
            else
                Max = MaxLength;

            if (lOffset > -1)
            {
                br.BaseStream.Seek(lOffset, SeekOrigin.Begin);

                if (enc == null)
                    result = Encoding.ASCII.GetString(br.ReadBytes(i));
                else
                    result = enc.GetString(br.ReadBytes(i));

                br.BaseStream.Seek(fTemp, SeekOrigin.Begin);
            }
            else
            {
                br.BaseStream.Seek(fTemp, SeekOrigin.Begin);
                if (enc == null)
                    result = Encoding.ASCII.GetString(br.ReadBytes(i));
                else
                    result = enc.GetString(br.ReadBytes(i));

                br.BaseStream.Seek(fTemp + Max, SeekOrigin.Begin);
            }

            return result;
        }

        public void DeleteFileIfExists(string sPath)
        {
            if (File.Exists(sPath))
                File.Delete(sPath);
        }

        public string GetPath(string input)
        {
            return Path.GetDirectoryName(input) + "\\" + Path.GetFileNameWithoutExtension(input);
        }

        public byte[] GetData(BinaryReader br, long offset, int size)
        {
            byte[] result = null;
            long backup = br.BaseStream.Position;
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            result = br.ReadBytes(size);
            br.BaseStream.Seek(backup, SeekOrigin.Begin);
            return result;
        }

        unsafe public int CRICompress(byte* dest, int* destLen, byte* src, int srcLen)
        {
            int n = srcLen - 1, m = *destLen - 0x1, T = 0, d = 0; 

            int p, q = 0, i, j, k;
            byte* odest = dest;
            for (; n >= 0x100; )
            {
                j = n + 3 + 0x2000;
                if (j > srcLen) j = srcLen;
                for (i = n + 3, p = 0; i < j; i++)
                {
                    for (k = 0; k <= n - 0x100; k++)
                    {
                        if (*(src + n - k) != *(src + i - k)) break;
                    }
                    if (k > p)
                    {
                        q = i - n - 3; 
                        p = k;
                    }
                }
                if (p < 3)
                {
                    d = (d << 9) | (*(src + n--)); 
                    T += 9;
                }
                else
                {
                    d = (((d << 1) | 1) << 13) | q;
                    
                    T += 14; n -= p;
                    if (p < 6)
                    {
                        d = (d << 2) | (p - 3); T += 2;
                    }
                    else if (p < 13)
                    {
                        d = (((d << 2) | 3) << 3) | (p - 6); T += 5;
                    }
                    else if (p < 44)
                    {
                        d = (((d << 5) | 0x1f) << 5) | (p - 13); T += 10;
                    }
                    else
                    {
                        d = ((d << 10) | 0x3ff); T += 10; p -= 44;
                        for (; ; )
                        {
                            for (; T >= 8; )
                            {
                                *(dest + m--) = (byte)((d >> (T - 8)) & 0xff);
                                T -= 8; d = d & ((1 << T) - 1);
                            }
                            if (p < 255) break;
                            d = (d << 8) | 0xff; T += 8; p = p - 0xff;
                        }
                        d = (d << 8) | p; T += 8;
                    }
                }
                for (; T >= 8; )
                {
                    *(dest + m--) = (byte)((d >> (T - 8)) & 0xff);
                    T -= 8;
                    d = d & ((1 << T) - 1);
                }
            }
            if (T != 0)
            {
                *(dest + m--) = (byte)(d << (8 - T));
            }

            *(dest + m--) = 0; *(dest + m) = 0;
            for (; ; )
            {
                if (((*destLen - m) & 3) == 0) break;
                *(dest + m--) = 0;
            }

            *destLen = *destLen - m;
            dest += m;

            int[] l = { 0x4c495243, 0x414c5941, srcLen - 0x100, *destLen };

            for (j = 0; j < 4; j++)
            {
                for (i = 0; i < 4; i++)
                {
                    *(odest + i + j * 4) = (byte)(l[j] & 0xff);
                    l[j] >>= 8;
                }
            }
            for (j = 0, odest += 0x10; j < *destLen; j++)
            {
                *(odest++) = *(dest + j);
            }
            for (j = 0; j < 0x100; j++)
            {
                *(odest++) = *(src + j);
            }
            *destLen += 0x110;
            return *destLen;
        }

        public byte[] CompressCRILAYLA(byte[] input)
        {
            unsafe
            {

                byte[] bytes = new byte[input.Length];
                int destLength = input.Length;
                fixed (byte* dest = bytes)
                fixed (byte* src = input)
                fixed (int* destLen = new int[] {destLength})
                {
                    destLength = CRICompress(dest, destLen, src, input.Length);
                }
                byte[] newdata = new byte[destLength];
                Array.Copy(bytes ,0, newdata, 0, destLength);
                return newdata;
            }
            
        }
    }
}