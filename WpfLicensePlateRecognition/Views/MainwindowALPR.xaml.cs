﻿using System;
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
using WpfLicensePlateRecognition.ViewModels;

namespace WpfLicensePlateRecognition.Views
{
    /// <summary>
    /// Interaction logic for MainwindowALPR.xaml
    /// </summary>
    public partial class MainwindowALPR : Window
    {
        public MainwindowALPR()
        {
            InitializeComponent();
            this.DataContext = new OCRViewModel();
        }

        private void About(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.xisom.com/en-us/php/home.php");
        }
    }
}
