using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xisom.OCR;

namespace WpfApp2.Models
{
    /// <summary>
    /// This class use Tesseract Lib to perform OCR
    /// </summary>
    public class ProcessOCR
    {
        #region property
        private string executedTime;
        private ViewModels.OCRViewModel taskViewModel;
        public bool IsSelectedLanguage;
        #endregion

        private Dictionary<SolidColorBrush, List<Rectangle>> ocrDetectedRegion;
        /// <summary>
        /// Loading tesseractMode based on language parametter
        /// </summary>
        /// <param name="vm">context of OCRViewmodel</param>
        public ProcessOCR(ViewModels.OCRViewModel vm)
        {
            taskViewModel = vm;
            ///Check language selected to perform respective TesseractEnginee
            if (taskViewModel._selectedLang.English == true)
            {

                taskViewModel.xisomOCR.Language = OCRLanguage.English;
                IsSelectedLanguage = true;
                return;
            }
            if (taskViewModel._selectedLang.Korean == true)
            {

                taskViewModel.xisomOCR.Language = OCRLanguage.Korean;
                IsSelectedLanguage = true;
                return;
            }
            if (taskViewModel._selectedLang.Japanese == true)
            {

                taskViewModel.xisomOCR.Language = OCRLanguage.Japanese;
                IsSelectedLanguage = true;
                return;
            }
            //This is Auto mode
            if (taskViewModel._selectedLang.Auto == true)
            {
                taskViewModel.xisomOCR.Language = OCRLanguage.Default;
                IsSelectedLanguage = true;
                return;
            }
            else
            {
                IsSelectedLanguage = false;
            }


        }

        /// <summary>
        /// This method is used to get consuming time for running
        /// </summary>
        /// <returns></returns>
        public String getTime()
        {
            return this.executedTime;
        }
        public List<string> ListImageOCR(List<ImageClass> list)
        {
            throw new NotImplementedException();
        }

        public string OneImageOCR(ImageClass one)
        {

            return runningOCR(one);
        }
        /// <summary>
        /// Click run button to start the OCR function
        /// </summary>
        /// <param name="currentImg">Is image we are processing it</param>
        /// <returns></returns>
        private string runningOCR(ImageClass currentImg)
        {

            try
            {
                if (currentImg == null)
                {
                    //return "please open at least one image";
                    MessageBox.Show("Please open at least one image");
                    return null;
                }

                // Check the language if it be selected or not
                if (!IsSelectedLanguage)
                {
                    MessageBox.Show("Please select language for recognizing");
                    return "Waiting for Select language";
                }
                else
                {
                    Stopwatch stopW = Stopwatch.StartNew();
                    Debug.WriteLine("USING XISOM OCR LIB: processOCR");

                    //Convert Grayscale using method 3
                    taskViewModel.xisomOCR.Grayscale = true;
                    taskViewModel.xisomOCR.GrayscaleMethod = 3;

                    var ocrData = taskViewModel.xisomOCR.ProcessOCRwithBoxs(ImageConverter.BitmapImage2Bitmap(currentImg.Image));
                    ocrDetectedRegion = new Dictionary<SolidColorBrush, List<Rectangle>>();
                    ocrDetectedRegion.Add(new SolidColorBrush(Colors.Violet), ocrData.charBoxs.ToList());
                    ocrDetectedRegion.Add(new SolidColorBrush(Colors.Yellow), ocrData.wordBoxs.ToList());
                    ocrDetectedRegion.Add(new SolidColorBrush(Colors.Green), ocrData.lineBoxs.ToList());
                    ocrDetectedRegion.Add(new SolidColorBrush(Colors.Red), ocrData.paraBoxs.ToList());

                    stopW.Stop();
                    var time_dur = stopW.Elapsed.TotalMilliseconds.ToString();
                    this.executedTime = time_dur;
                    return ocrData.outOCR;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return null;
        }

        public Dictionary<SolidColorBrush, List<Rectangle>> GetocrDetectedRegion()
        {
            return this.ocrDetectedRegion;
        }

    }


}
