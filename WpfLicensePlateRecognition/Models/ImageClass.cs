using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WpfLicensePlateRecognition
{
    /// <summary>
    /// Image class is used OCR project
    /// </summary>
    public class ImageClass
    {
        public string FilePath { get; set; }
        public BitmapImage Image { get; set; }
        public ImageClass()
        {
            Image = new BitmapImage();
        }
    }
}
