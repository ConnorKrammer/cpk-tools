using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Windows.Shapes;
using System.IO;
using Ookii.Dialogs.Wpf;


namespace CriPakGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void menu_openfile_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Load cpk");
            string fName;
            string baseName;
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
            openFileDialog.InitialDirectory = "";
            openFileDialog.Filter = "Criware CPK|*.cpk";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog().Value)
            {
                fName = openFileDialog.FileName;
                baseName = System.IO.Path.GetFileName(fName);
                
                cpkwrapper cpk = new cpkwrapper(fName);
                status_cpkname.Content = baseName;
                status_cpkmsg.Content = string.Format("{0} file(s) registered.", cpk.nums);
                datagrid_cpk.ItemsSource = cpk.table;

                
            }
        }

        private void menu_importAssets_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menu_savefile_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
