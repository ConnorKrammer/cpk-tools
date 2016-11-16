using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibCPK;
using System.IO;

namespace CriPakGUI
{
    public enum packageEncodings
    {
        UTF_8 = 65001,
        SHIFT_JIS = 932,
        
    }
    public static class myPackage
    {
        public static CPK cpk { get; set; }
        public static string basePath { get; set; }
        public static string cpk_name { get; set; }
        public static string baseName { get; set; }
        public static string fileName { get; set; }
        public static Encoding encoding = Encoding.GetEncoding(65001);
    }
    public class CPKTable
    {
        public int id { get; set; }
        public string FileName { get; set; }
        public string _localName { get; set; }
        //public string DirName;
        public UInt64 FileOffset { get; set; }
        public int FileSize { get; set; }
        public int ExtractSize { get; set; }
        public string FileType { get; set; }
        public float Pt { get; set; }
    }

    public class cpkwrapper
    {

        public int nums = 0;
        public List<CPKTable> table;
        public cpkwrapper(string inFile)
        {
            string cpk_name = inFile;
            table = new List<CPKTable>();
            myPackage.cpk = new CPK(new Tools());
            myPackage.cpk.ReadCPK(cpk_name, myPackage.encoding);
            myPackage.cpk_name = cpk_name;

            BinaryReader oldFile = new BinaryReader(File.OpenRead(cpk_name));
            List<FileEntry> entries = myPackage.cpk.FileTable.OrderBy(x => x.FileOffset).ToList();
            int i = 0;
            bool bFileRepeated = Tools.CheckListRedundant(entries);
            while (i < entries.Count)
            {
                /*
                Console.WriteLine("FILE ID:{0},File Name:{1},File Type:{5},FileOffset:{2:x8},Extract Size:{3:x8},Chunk Size:{4:x8}", entries[i].ID,
                                                            (((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName),
                                                            entries[i].FileOffset,
                                                            entries[i].ExtractSize,
                                                            entries[i].FileSize,
                                                            entries[i].FileType);
                */
                
                
                if (entries[i].FileType != null)
                {
                    nums += 1;

                    CPKTable t = new CPKTable();
                    if (entries[i].ID == null)
                    {
                        t.id = -1;
                    }
                    else
                    {
                        t.id = Convert.ToInt32(entries[i].ID);
                    }
                    if (t.id >= 0 && bFileRepeated)
                    {
                        t.FileName = (((entries[i].DirName != null) ? 
                                        entries[i].DirName + "/" : "") + string.Format("[{0}]",t.id.ToString()) + entries[i].FileName);
                    }
                    else
                    {
                        t.FileName = (((entries[i].DirName != null) ?
                                        entries[i].DirName + "/" : "") +  entries[i].FileName);
                    }
                    t._localName = entries[i].FileName.ToString();

                    t.FileOffset = Convert.ToUInt64(entries[i].FileOffset);
                    t.FileSize = Convert.ToInt32(entries[i].FileSize);
                    t.ExtractSize = Convert.ToInt32(entries[i].ExtractSize);
                    t.FileType = entries[i].FileType;
                    if (entries[i].FileType == "FILE")
                    {
                        t.Pt = (float)Math.Round((float)t.FileSize / (float)t.ExtractSize, 2) * 100f;
                    }
                    else
                    {
                        t.Pt = (float)1f * 100f;
                    }
                    table.Add(t);
                }
                i += 1;

            }
            oldFile.Close();

        }
    }
}
