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
using WpfApp2.ViewModels;

namespace WpfApp2.Views
{
    /// <summary>
    /// Interaction logic for MainWindow3.xaml
    /// </summary>
    public partial class MainWindow3 : Window
    {
       
        public MainWindow3()
        {
            InitializeComponent();
            this.DataContext = new OCRViewModel();
            //this.DataContext = new MouseViewModel();
           

        }
        private void About(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.xisom.com/en-us/php/home.php");
        }
    }
}
