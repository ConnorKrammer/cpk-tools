using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibCPK;
using System.IO;

namespace CriPakGUI
{
    public class CPKTable
    {
        public int id { get; set; }
        public string FileName { get; set; }
        //public string DirName;
        public int FileOffset { get; set; }
        public int FileSize { get; set; }
        public int ExtractSize { get; set; }
        public string FileType { get; set; }
        public float Pt { get; set; }
    }
    public class cpkwrapper
    {
        public CPK cpk;
        public int nums = 0;
        public List<CPKTable> table;
        public cpkwrapper(string inFile)
        {
            string cpk_name = inFile;
            table = new List<CPKTable>();
            cpk = new CPK(new Tools());
            cpk.ReadCPK(cpk_name);

            BinaryReader oldFile = new BinaryReader(File.OpenRead(cpk_name));
            List<FileEntry> entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();
            for (int i = 0; i < entries.Count; i++)
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
                    t.id = i;
                    t.FileName = (((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName);

                    t.FileOffset = Convert.ToInt32(entries[i].FileOffset);
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

            }
        }
    }
}
