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
using WpfApp2.ViewModels;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace WpfApp2.Views
{
    /// <summary>
    /// Interaction logic for CropSetting2.xaml
    /// </summary>
    public partial class CropSetting2 : Window
    {
        #region This part is used to discard default close button on Window WPF
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion

        public CropSetting2(ViewModels.OCRViewModel oCRViewModel)
        {
            InitializeComponent();
            this.DataContext = new CropSettingsViewModel(oCRViewModel);
        }

        
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            //this.Close();
            var vm = DataContext as CropSettingsViewModel;
            vm.HiddenCropSettings();
            this.Hide();

        }
    }
}
