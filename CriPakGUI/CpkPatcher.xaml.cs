using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Navigation;
using System.Diagnostics;
using Ookii.Dialogs.Wpf;
using System.Threading;
using System.Windows.Threading;
using LibCPK;

namespace CriPakGUI
{
    /// <summary>
    /// CpkPatcher.xaml 的交互逻辑
    /// </summary>
    public partial class CpkPatcher : Window
    {
        public CpkPatcher(double x, double y)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = x;
            this.Left = y;
        }

        private void button_selPatchPath_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog saveFilesDialog = new VistaFolderBrowserDialog();
            saveFilesDialog.SelectedPath = myPackage.basePath + "/";
            if (saveFilesDialog.ShowDialog().Value)
            {
                Debug.Print(saveFilesDialog.SelectedPath);
                textbox_patchDir.Text = saveFilesDialog.SelectedPath;
            }

        }

        

        private void button_selDstCPK_Click(object sender, RoutedEventArgs e)
        {
            VistaSaveFileDialog saveDialog = new VistaSaveFileDialog();
            saveDialog.InitialDirectory = myPackage.basePath;
            saveDialog.RestoreDirectory = true;
            saveDialog.Filter = "CPK File（*.cpk）|*.cpk";
            if (saveDialog.ShowDialog() == true)
            {
                string saveFileName = saveDialog.FileName;
                textbox_cpkDir.Text = saveFileName;
            }

        }
        private delegate void textblockDelegate(string text);
        private void updateTextblock(string text)
        {
            textblock0.Text += string.Format("Updating ... {0}\n", text);
            scrollview0.ScrollToEnd();
        }

        private delegate void progressbarDelegate(float no);
        private void updateprogressbar(float no)
        {
            progressbar1.Value = no;
        }
        public class actionCPK
        {
            public string cpkDir { get; set; }
            public string patchDir { get; set; }
            public bool bForceCompress { get; set; }
            public Dictionary<string, string> batch_file_list { get; set; }
        }

        private void button_PatchCPK_Click(object sender, RoutedEventArgs e)
        {
            string cpkDir = textbox_cpkDir.Text;
            string patchDir = textbox_patchDir.Text;
            Dictionary<string, string> batch_file_list = new Dictionary<string, string>();
            List<string> ls = new List<string>();
            if ((myPackage.cpk != null) && (Directory.Exists(patchDir)))
            {

                GetFilesFromPath(patchDir, ref ls);
                Debug.Print(string.Format("GOT {0} Files.", ls.Count));
                foreach (string s in ls)
                {
                    string name = s.Remove(0, patchDir.Length + 1);
                    name = name.Replace("\\" , @"/");
                    if (!name.Contains(@"/"))
                    {
                        name = @"/" + name;
                    }
                    batch_file_list.Add(name, s);
                }
                actionCPK t = new actionCPK();
                t.cpkDir = cpkDir;
                t.patchDir = patchDir;
                if (checkbox_donotcompress.IsChecked == true)
                {
                    t.bForceCompress = false;
                }
                else
                {
                    t.bForceCompress = true;
                }
                t.batch_file_list = batch_file_list;
                ThreadPool.QueueUserWorkItem(new WaitCallback(PatchCPK), t);
            }
            else
            {
                MessageBox.Show("Error, cpkdata or patchdata not found.");

            }
        }

        private void PatchCPK(object t)
        {
            string msg; 
            string cpkDir = ((actionCPK)t).cpkDir;
            string patchDir = ((actionCPK)t).patchDir;
            bool bForceCompress = ((actionCPK)t).bForceCompress;
            Dictionary<string, string> batch_file_list = ((actionCPK)t).batch_file_list;
            CPK cpk = myPackage.cpk;
            BinaryReader oldFile = new BinaryReader(File.OpenRead(myPackage.cpk_name));
            string outputName = cpkDir;

            BinaryWriter newCPK = new BinaryWriter(File.OpenWrite(outputName));

            List<FileEntry> entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();

            Tools tool = new Tools();

            int id;
            bool bFileRepeated = Tools.CheckListRedundant(entries);
            for (int i = 0; i < entries.Count; i++)
            {
                this.UI_SetProgess((float)i / (float)entries.Count * 100f);
                if (entries[i].FileType != "CONTENT")
                {

                    if (entries[i].FileType == "FILE")
                    {
                        // I'm too lazy to figure out how to update the ContextOffset position so this works :)
                        if ((ulong)newCPK.BaseStream.Position < cpk.ContentOffset)
                        {
                            ulong padLength = cpk.ContentOffset - (ulong)newCPK.BaseStream.Position;
                            for (ulong z = 0; z < padLength; z++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                    }

                    id = Convert.ToInt32(entries[i].ID);
                    string currentName;

                    if (id > 0 && bFileRepeated)
                    {
                        currentName = (((entries[i].DirName != null) ?
                                        entries[i].DirName + "/" : "") + string.Format("[{0}]", id.ToString()) + entries[i].FileName);
                    }
                    else
                    {
                        currentName = ((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName;
                    }

                     

                    if (!currentName.Contains("/"))
                    {
                        currentName = "/" + currentName;
                    }
                    Debug.Print("Got File:" + currentName.ToString());

                    if (!batch_file_list.Keys.Contains(currentName.ToString()))
                    //如果不在表中，复制原始数据
                    {
                        oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);

                        entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;

                        if (entries[i].FileName.ToString() == "ETOC_HDR")
                        {

                            cpk.EtocOffset = entries[i].FileOffset;

                            Debug.Print("Fix ETOC_OFFSET to {0:x8}", cpk.EtocOffset);

                        }

                        cpk.UpdateFileEntry(entries[i]);

                        byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                        newCPK.Write(chunk);

                        if ((newCPK.BaseStream.Position % 0x800) > 0 && i < entries.Count - 1)
                        {
                            long cur_pos = newCPK.BaseStream.Position;
                            for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }

                    }
                    else
                    {
                        
                        string replace_with = batch_file_list[currentName.ToString()];
                        //Got patch file name
                        msg = string.Format("Patching: {0}", currentName.ToString());

                        this.UI_SetTextBlock(msg);
                        Debug.Print(msg);

                        byte[] newbie = File.ReadAllBytes(replace_with);
                        entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                        int o_ext_size = Int32.Parse((entries[i].ExtractSize).ToString());
                        int o_com_size = Int32.Parse((entries[i].FileSize).ToString());
                        if ((o_com_size < o_ext_size) && entries[i].FileType == "FILE" && bForceCompress == true)
                        {
                            // is compressed
                            msg = string.Format("Compressing data:{0:x8}", newbie.Length);
                            this.UI_SetTextBlock(msg);
                            Console.Write(msg);

                            byte[] dest_comp = cpk.CompressCRILAYLA(newbie);

                            entries[i].FileSize = Convert.ChangeType(dest_comp.Length, entries[i].FileSizeType);
                            entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            cpk.UpdateFileEntry(entries[i]);
                            newCPK.Write(dest_comp);
                            msg = string.Format(">> {0:x8}\r\n", dest_comp.Length);
                            this.UI_SetTextBlock(msg);
                            Console.Write(msg);
                        }

                        else
                        {
                            msg = string.Format("Storing data:{0:x8}\r\n", newbie.Length);
                            this.UI_SetTextBlock(msg);
                            Console.Write(msg);

                            entries[i].FileSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            cpk.UpdateFileEntry(entries[i]);
                            newCPK.Write(newbie);
                        }


                        if ((newCPK.BaseStream.Position % 0x800) > 0 && i < entries.Count - 1)
                        {
                            long cur_pos = newCPK.BaseStream.Position;
                            for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                    }


                }
                else
                {
                    // Content is special.... just update the position
                    cpk.UpdateFileEntry(entries[i]);
                }
            }

            cpk.WriteCPK(newCPK);
            msg = string.Format("Writing TOC....");
            this.UI_SetTextBlock(msg);
            Console.WriteLine(msg);

            cpk.WriteITOC(newCPK);
            cpk.WriteTOC(newCPK);
            cpk.WriteETOC(newCPK, cpk.EtocOffset);
            cpk.WriteGTOC(newCPK);

            newCPK.Close();
            oldFile.Close();
            msg = string.Format("Saving CPK to {0}....", outputName);
            this.UI_SetTextBlock(msg);
            Console.WriteLine(msg);

            MessageBox.Show("CPK Patched.");
            this.UI_SetProgess(0f);



        }

        public void UI_SetProgess(float value)
        {
            this.Dispatcher.Invoke(new progressbarDelegate(updateprogressbar), new object[] { (float)value });
        }

        public void UI_SetTextBlock(string msg)
        {
            this.Dispatcher.Invoke(new textblockDelegate(updateTextblock), new object[] { msg });
        }

        private void  GetFilesFromPath(string directoryname , ref List<string> ls)
        {
            FileInfo[] fi = new DirectoryInfo(directoryname).GetFiles();
            DirectoryInfo[] di = new DirectoryInfo(directoryname).GetDirectories();
            if (fi.Length != 0)
            {
                foreach (FileInfo v in fi)
                {
                    ls.Add(v.FullName);
                }
            }
            if (di.Length != 0)
            {
                foreach (DirectoryInfo v in di)
                {
                    GetFilesFromPath(v.FullName , ref ls);

                }
            }
        }
    }
}
