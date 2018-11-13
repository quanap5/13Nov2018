using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Preprocessor
{
    /// <summary>
    /// This class is used in Crop function
    /// </summary>
    static class CropImage
    {
        /// <summary>
        /// This method is used to crop input image with parametter as Rectangle
        /// </summary>
        /// <param name="ori_image">This is input image</param>
        /// <param name="cropArea">Rectangle scope will be cropped on input image</param>
        /// <returns>Return new image with same size as Rectangle form</returns>
        public static Image Crop(Image ori_image, Rectangle cropArea)
        {

            Bitmap bmp = new Bitmap(cropArea.Width, cropArea.Height);
            bmp.SetResolution(ori_image.HorizontalResolution, ori_image.VerticalResolution);
            Graphics grph = Graphics.FromImage(bmp);
            grph.DrawImage(ori_image, 0, 0, cropArea, GraphicsUnit.Pixel);
            grph.Dispose();
            return bmp;

        }
        /// <summary>
        /// This method is used to Save and read after cropping
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="cropArea"></param>
        /// <returns></returns>
        public static string SaveAndRead(string filename, Rectangle cropArea)
        {
            //string bmstr = @"C:\Users\admin\source\repos\RGB2Gray\japan3.png";
            var bmstr = filename;
            var temFile = bmstr.Substring(bmstr.LastIndexOf('\\') + 1);
            var temFile2 = temFile.Substring(0, temFile.LastIndexOf('.'));
            string graystr = bmstr.Substring(0, bmstr.LastIndexOf('\\') + 1) + temFile2 + "Croped.png";
            Console.WriteLine(graystr);

            Image sourcebm;
            Image graybm;

            if (!File.Exists(graystr))
            {
                try
                {
                    Stopwatch stpWatch = Stopwatch.StartNew();
                    sourcebm = Image.FromFile(bmstr);
                    graybm = Crop(sourcebm, cropArea);
                    graybm.Save(graystr);
                    stpWatch.Stop();
                    var t = stpWatch.Elapsed.TotalMilliseconds.ToString();
                    Console.WriteLine("Convert RGB to Gray sucessfully within {0} ms", t);
                    return graystr;


                }
                catch (Exception e)
                {

                    Console.WriteLine("Error message" + e.Message);
                    return null;
                }


            }
            return graystr;

        }

    }
}
