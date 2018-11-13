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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xisom.OCR;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ocr = new OCRLib();

            ocr.Grayscale = true;
            ocr.Deskew = true;

            ocr.Language = OCRLanguage.Default;

            Image image = null;
            //var str = ocr.Process(image);
            //var str = ocr.Process(image, new Rectangle(0, 0, 32, 32));

            //var grayscaledImage = ocr.ProcessGrayscale(image)
        }
    }
}
