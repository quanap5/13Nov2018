using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfLicensePlateRecognition.ViewModels;

namespace WpfLicensePlateRecognition.Views
{
    /// <summary>
    /// Interaction logic for ShowInfo.xaml
    /// </summary>
    public partial class ShowInfo : Window
    {
        #region This part is used to discard default closs button on Window WPF
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

        public ShowInfo(ImageClass im)
        {
            InitializeComponent();
            this.DataContext = new ImageInfoViewModel(im);
        }
        /// <summary>
        /// This method is handle click mouse on OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InfoOKbtn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
