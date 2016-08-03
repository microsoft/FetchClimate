using Microsoft.Research.Science.FetchClimate2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
using System.Windows.Shapes;

namespace LandOceanMaskSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataMaskAnalyzer mask;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            GZipStream stream = new GZipStream(File.OpenRead("1.bf"), CompressionMode.Decompress, false);
            mask = new DataMaskAnalyzer(stream);
            sw.Stop();
            double res = sw.Elapsed.TotalSeconds;
            this.TimeElapsedBlock.Text = res.ToString("0.00000");
            this.IsLandButton.IsEnabled = true;
            this.PercButton.IsEnabled = true;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool res = mask.HasData(Double.Parse(Latmin.Text), Double.Parse(Lonmin.Text));
            sw.Stop();
            this.Result.Text = res.ToString();
            this.WorkTime.Text = sw.Elapsed.TotalSeconds.ToString("0.00000");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            double res = mask.GetDataPercentage(Double.Parse(Latmin.Text), Double.Parse(Latmax.Text), Double.Parse(Lonmin.Text), Double.Parse(Lonmax.Text));
            sw.Stop();
            this.Result.Text = res.ToString("0.00000");
            this.WorkTime.Text = sw.Elapsed.TotalSeconds.ToString("0.00000");
        }
    }
}
