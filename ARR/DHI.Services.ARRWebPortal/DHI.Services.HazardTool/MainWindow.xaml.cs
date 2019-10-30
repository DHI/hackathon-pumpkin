using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DHI.Services.ARRWebPortal;
using Microsoft.Win32;

namespace DHI.Services.HazardTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private ObservableCollection<string> fileList = new ObservableCollection<string>();
        public ObservableCollection<ComboBoxItem> VelocityItemsList { get; set; }
        public ObservableCollection<ComboBoxItem> UItemsList { get; set; }
        public ObservableCollection<ComboBoxItem> VItemsList { get; set; }
        public ObservableCollection<ComboBoxItem> PItemsList { get; set; }
        public ObservableCollection<ComboBoxItem> QItemsList { get; set; }
        public ObservableCollection<ComboBoxItem> DepthItemsList { get; set; }
        public ObservableCollection<ComboBoxItem> ExportItemsList { get; set; }
        public ObservableCollection<ComboBoxItem> TimeStepList { get; set; }
        public string VelocityItemName { get; set; }
        public string UItemName { get; set; }
        public string VItemName { get; set; }
        public string PItemName { get; set; }
        public string QItemName { get; set; }
        public string DepthItemName { get; set; }
        public string ExportItemName { get; set; }
        public string TimeStep { get; set; }
        public bool ZeroDelete { get; set; }
        public string OutputFolder { get; set; }
                
        public MainWindow()
        {
            InitializeComponent();
            inputFiles.ItemsSource = fileList;
        }

        private void btnAddInputFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Dfs2 and Dfsu files (.dfs2,dfsu)|*.dfs2;*.dfsu";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    fileList.Add(fileName);
                }
            }

            List<string> itemNameList = MeshProcess.GetItems(fileList[0]);
            VelocityItemsList = new ObservableCollection<ComboBoxItem>();
            UItemsList = new ObservableCollection<ComboBoxItem>();
            VItemsList = new ObservableCollection<ComboBoxItem>();
            PItemsList = new ObservableCollection<ComboBoxItem>();
            QItemsList = new ObservableCollection<ComboBoxItem>();
            DepthItemsList = new ObservableCollection<ComboBoxItem>();
            ExportItemsList = new ObservableCollection<ComboBoxItem>();
            TimeStepList = new ObservableCollection<ComboBoxItem>();
            foreach (string itemName in itemNameList)
            {
                VelocityItemsList.Add(new ComboBoxItem{Content = itemName});
                UItemsList.Add(new ComboBoxItem { Content = itemName });
                VItemsList.Add(new ComboBoxItem { Content = itemName });
                PItemsList.Add(new ComboBoxItem { Content = itemName });
                QItemsList.Add(new ComboBoxItem { Content = itemName });
                DepthItemsList.Add(new ComboBoxItem { Content = itemName });
                ExportItemsList.Add(new ComboBoxItem { Content = itemName });
            }
            NotifyPropertyChanged("VelocityItemsList");
            NotifyPropertyChanged("UItemsList");
            NotifyPropertyChanged("VItemsList");
            NotifyPropertyChanged("PItemsList");
            NotifyPropertyChanged("QItemsList");
            NotifyPropertyChanged("DepthItemsList");
            NotifyPropertyChanged("ExportItemsList");

            int numberTimeSteps = MeshProcess.GetNumberTimeStpes(fileList[0]);
            for (int i = 0; i < numberTimeSteps; i++)
            {
                TimeStepList.Add(new ComboBoxItem { Content = i });
            }
            NotifyPropertyChanged("TimeStepList");
        }

        private void btnAddOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFolder = folderBrowserDialog1.SelectedPath;
                NotifyPropertyChanged("OutputFolder");
            }
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            DateTime start = DateTime.Now;

            try
            {
                if (string.IsNullOrEmpty(OutputFolder))
                {
                    throw new Exception("OutputFolder must be specified");
                }
                if (!System.IO.Directory.Exists(OutputFolder))
                {
                    throw new Exception("OutputFolder must exist: " + OutputFolder);
                }

                string velocityName = "Velocity";
                string vxdName = "vxd";
                string maxVxdName = "Max Vxd";
                string maxVName = "Max Velocity";
                string maxDName = "Max Depth";

                foreach (string filePath in fileList)
                {
                    string outputFileName = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_HazardOut" + System.IO.Path.GetExtension(filePath);
                    string outputFilePath = System.IO.Path.Combine(OutputFolder, outputFileName);
                    string outputStaticFileName = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_HazardStaticOut" + System.IO.Path.GetExtension(filePath);
                    string outputStaticFilePath = System.IO.Path.Combine(OutputFolder, outputStaticFileName);

                    float deleteValue = MeshProcess.GetDeleteValue(filePath);
                    
                    if (System.IO.Path.GetExtension(filePath).ToLower().Contains("dfs2"))
                    {
                        //MeshProcess.AddItem(filePath, outputFilePath, velocityName, vxdName, ZeroDelete);
                        GridProcess.CreateHazardFile(filePath, outputFilePath, velocityName, vxdName, ZeroDelete);
                    }
                    else
                    {
                        MeshProcess.CreateHazardFile(filePath, outputFilePath, velocityName, vxdName, ZeroDelete);
                    }
                    
                    int numberTimeSteps = MeshProcess.GetNumberTimeStpes(filePath);
                    for (int i = 0; i < numberTimeSteps; i++)
                    {
                        float[] velocityArray;
                        if (!string.IsNullOrEmpty(VelocityItemName))
                        {
                            velocityArray = MeshProcess.GetItem(filePath, VelocityItemName, i);
                        }
                        else if (!string.IsNullOrEmpty(UItemName) && !string.IsNullOrEmpty(VItemName))
                        {
                            velocityArray = MeshProcess.GetSpeed(filePath, UItemName, VItemName, i);
                        }
                        else if (!string.IsNullOrEmpty(PItemName) && !string.IsNullOrEmpty(QItemName) && !string.IsNullOrEmpty(DepthItemName) && System.IO.Path.GetExtension(filePath).ToLower().Contains("dfs2"))
                        {
                            velocityArray = GridProcess.GetSpeed(filePath, PItemName, QItemName, DepthItemName, i);
                        }
                        else
                        {
                            throw new Exception("The VelocityItemName must be specified, or both UItemName and VItemName must be specified, or all of PItemName, qItemName and DepthItemName");
                        }

                        float[] depthArray = MeshProcess.GetItem(filePath, DepthItemName, i);
                        float[] vxdArray = MeshProcess.Multiply(velocityArray, depthArray, deleteValue);

                        MeshProcess.AppendToFile(outputFilePath, i, velocityArray, vxdArray, ZeroDelete, deleteValue);

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    float[] maxVxd = MeshProcess.GetMax(outputFilePath, vxdName);
                    float[] maxV = MeshProcess.GetMax(outputFilePath, velocityName);
                    float[] maxD = MeshProcess.GetMax(filePath, DepthItemName);

                    //if dfs2 do generic, otherwise do dfsu specific
                    if (System.IO.Path.GetExtension(filePath).ToLower().Contains("dfs2"))
                    {
                        GridProcess.CreateHazardFile(filePath, outputStaticFilePath, maxVxdName, maxVName, ZeroDelete, maxDName);
                    }
                    else
                    {
                        MeshProcess.CreateHazardFile(filePath, outputStaticFilePath, maxVxdName, maxVName, ZeroDelete, maxDName);
                    }
                    MeshProcess.AppendToFile(outputStaticFilePath, 0, maxVxd, maxV, ZeroDelete, deleteValue, maxD);

                    //////if (System.IO.Path.GetExtension(filePath).ToLower().Contains("dfs2"))
                    //////{
                    //////    GridProcess.WriteAscii(outputStaticFilePath, maxVxdName, 0, deleteValue, OutputFolder);
                    //////    //GridProcess.WriteAscii(outputStaticFilePath, maxVName, 0, deleteValue, OutputFolder);
                    //////    //GridProcess.WriteAscii(outputStaticFilePath, maxDName, 0, deleteValue, OutputFolder);

                    //////    GridProcess.MakeShapeFile(outputStaticFilePath, maxVxdName, 0, OutputFolder);
                    //////    //GridProcess.MakeShapeFile(outputStaticFilePath, maxVName, 0, OutputFolder);
                    //////    //GridProcess.MakeShapeFile(outputStaticFilePath, maxDName, 0, OutputFolder);
                    //////}
                    //////else
                    //////{
                    //////    MeshProcess.MakeShapeFile(outputStaticFilePath, maxVxdName, 0, OutputFolder);
                    //////    //MeshProcess.MakeShapeFile(outputStaticFilePath, maxVName, 0, OutputFolder);
                    //////    //MeshProcess.MakeShapeFile(outputStaticFilePath, maxDName, 0, OutputFolder);
                    //////}
                }

                MessageBox.Show("Completed Hazard Calculation Duration: " + (DateTime.Now - start).TotalSeconds);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Exception: " + exception.Message);
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(OutputFolder))
                {
                    throw new Exception("OutputFolder must be specified");
                }
                if (!System.IO.Directory.Exists(OutputFolder))
                {
                    throw new Exception("OutputFolder must exist: " + OutputFolder);
                }
                string filePath = fileList[0];
                float deleteValue = MeshProcess.GetDeleteValue(filePath);
                
                int tsIndex = Convert.ToInt32(TimeStep);
                if (System.IO.Path.GetExtension(filePath).ToLower().Contains("dfs2"))
                {
                    GridProcess.MakeShapeFile(filePath, ExportItemName, tsIndex, OutputFolder);
                    GridProcess.WriteAscii(filePath, ExportItemName, tsIndex, deleteValue, OutputFolder);
                }
                else
                {
                    MeshProcess.MakeShapeFile(filePath, ExportItemName, tsIndex, OutputFolder);
                }

                MessageBox.Show("Completed GIS Export");
            }
            catch (Exception exception)
            {
                MessageBox.Show("Exception: " + exception.Message);
            }
        }

        private void btnClearInputFile_Click(object sender, RoutedEventArgs e)
        {
            fileList.Clear();

            VelocityItemsList = new ObservableCollection<ComboBoxItem>();
            UItemsList = new ObservableCollection<ComboBoxItem>();
            VItemsList = new ObservableCollection<ComboBoxItem>();
            PItemsList = new ObservableCollection<ComboBoxItem>();
            QItemsList = new ObservableCollection<ComboBoxItem>();
            DepthItemsList = new ObservableCollection<ComboBoxItem>();
            ExportItemsList = new ObservableCollection<ComboBoxItem>();
            TimeStepList = new ObservableCollection<ComboBoxItem>();
            
            NotifyPropertyChanged("VelocityItemsList");
            NotifyPropertyChanged("UItemsList");
            NotifyPropertyChanged("VItemsList");
            NotifyPropertyChanged("PItemsList");
            NotifyPropertyChanged("QItemsList");
            NotifyPropertyChanged("DepthItemsList");
            NotifyPropertyChanged("ExportItemsList");
            NotifyPropertyChanged("TimeStepList");

            VelocityItemName = string.Empty;
            NotifyPropertyChanged("VelocityItemName");
            UItemName = string.Empty;
            NotifyPropertyChanged("UItemName");
            VItemName = string.Empty;
            NotifyPropertyChanged("VItemName");
            PItemName = string.Empty;
            NotifyPropertyChanged("PItemName");
            QItemName = string.Empty;
            NotifyPropertyChanged("QItemName");
            DepthItemName = string.Empty;
            NotifyPropertyChanged("DepthItemName");
            ExportItemName = string.Empty;
            NotifyPropertyChanged("ExportItemName");
            TimeStep = string.Empty;
            NotifyPropertyChanged("TimeStepItemName");

            image1.Source = null;
        }

        private void btnShowImage_Click(object sender, RoutedEventArgs e)
        {
            image1.Source = null;

            if (fileList.Count > 0)
            {
                Stream imageStream;
                if (System.IO.Path.GetExtension(fileList[0]).ToLower().Contains("dfs2"))
                {
                    Stream stream = GridProcess.CreateIndexFile(new MemoryStream(File.ReadAllBytes(fileList[0])));
                    MemoryStream tempStream = new MemoryStream();
                    stream.CopyTo(tempStream);
                    imageStream = GridProcess.GetIndexMapImage(tempStream.ToArray());
                }
                else
                {
                    Stream stream = MeshProcess.CreateIndexFile(fileList[0]);
                    MemoryStream tempStream = new MemoryStream();
                    stream.CopyTo(tempStream);
                    imageStream = MeshProcess.GetIndexMapImage(tempStream.ToArray());
                }
                image1.Source = BitmapFrame.Create(imageStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }
    }
}
