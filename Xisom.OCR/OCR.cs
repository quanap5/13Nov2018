using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Xisom.OCR.Preprocessor;
using Tesseract;
using System.Windows;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using Xisom.OCR.Geometry;
using Xisom.OCR;

namespace Xisom.OCR
{
    /// <summary>
    /// This struct data is used to store output of Optical Characters Recognition process
    /// </summary>
    public struct OCRData
    {
        public string outOCR;
        public Rectangle[] charBoxs;
        public Rectangle[] wordBoxs;
        public Rectangle[] lineBoxs;
        public Rectangle[] paraBoxs;
    }
    /// <summary>
    /// This is the languages collection that could be supported by Optical Character Recognition Engine
    /// </summary>
    public enum OCRLanguage
    {
        /// <summary>
        /// Language is anything from available language collection
        /// </summary>
        Default,
        /// <summary>
        /// Language is English
        /// </summary>
        English,
        /// <summary>
        /// Language is Korean
        /// </summary>
        Korean,
        /// <summary>
        /// Language is Japanese
        /// </summary>
        Japanese,

    }

    //OptionKmean
    public struct KmeanOption
    {
        public bool posVertical;
        public bool posHorizontal;
        public bool pos2Dimesions;

        public bool strokeWidth;

        public bool dimVertical;
        public bool dimHorizontal;
        public bool dim2Dimensions;

        public bool direction;

    }

    [Flags]
    public enum KMeanOptionEnum
    {
        None = 0,
        Position = 1 << 0, //1
        Dimension = 1 << 1, //2
        Direction = 1 << 2,  //4
        StrokeWidth = 1 << 3, //8
    }

    
    /// <summary>
    /// This class is used for Optical Character Recognition
    /// </summary>
    public class OCRLib
    {
        private Dictionary<string, TesseractEngine> availableEngines = new Dictionary<string, TesseractEngine>();
        public bool Grayscale { get; set; }
        public int GrayscaleMethod { get; set; }
        public bool Deskew { get; set; }
        public OCRLanguage Language { get; set; }

        //text detection auto option
        public bool posVer { get; set; }
        public bool posHor { get; set; }
        public bool pos2D { get; set; }

        public KMeanOptionEnum OptionTest1;

        public KMeanOptionEnum OptionTest2;

        public KmeanOption kmeanOption;

        public KmeanOption kmeanOption2;

        /// <summary>
        /// This is constructsor
        /// </summary>
        public OCRLib(bool deskew = false, bool grayscale = false)
        {
            this.Deskew = deskew;
            this.Grayscale = grayscale;
        }

        /// <summary>
        /// This method is used to load Tesseract OCR Engine using specified input parametters
        /// </summary>
        /// <param name="lang">This is input parameter</param>
        /// <returns>Return the OCR model will be used for performing Optical Character Recognition</returns>
        /// <remarks>Auto is the best option but it consume much time</remarks>
        private TesseractEngine LoadOCREngine(OCRLanguage lang)
        {
            //TesseractEngine ocrEngine;
            if (lang == OCRLanguage.English)
            {
                var ocrEngine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Korean)
            {
                var ocrEngine = new TesseractEngine("./tessdata", "kor", EngineMode.Default);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Japanese)
            {
                var ocrEngine = new TesseractEngine("./tessdata", "jpn", EngineMode.Default);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Default)
            {
                var ocrEngine = new TesseractEngine("./tessdata", "eng+kor+jpn", EngineMode.Default);
                return ocrEngine;
            }
            return null;
        }
        /// <summary>
        /// This method is used to load Tesseract OCR Engine using specified input parameters
        /// </summary>
        /// <param name="datapath">The path to the parent directory that contain the 'tessdata' </param>
        /// <param name="lang">The language to load</param>
        /// <returns>Return the OCR model will be used for performing Optical Character Recognition</returns>
        /// <remarks>The datapath parameter should point to the directory that contains the 'tessdata' folder</remarks>
        public TesseractEngine LoadOCREngine(string datapath, OCRLanguage lang)
        {
            //TesseractEngine ocrEngine;
            if (lang == OCRLanguage.English)
            {
                var ocrEngine = new TesseractEngine(datapath, "eng", EngineMode.Default);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Korean)
            {
                var ocrEngine = new TesseractEngine(datapath, "kor", EngineMode.Default);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Japanese)
            {
                var ocrEngine = new TesseractEngine(datapath, "jpn", EngineMode.Default);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Default)
            {
                var ocrEngine = new TesseractEngine(datapath, "eng+kor+jpn", EngineMode.Default);
                return ocrEngine;
            }
            return null;

        }
        /// <summary>
        /// This method is used to load Tesseract OCR Engine using specified input parameters
        /// </summary>
        /// <param name="datapath">The path to the parent directory that contains the 'tessdata'</param>
        /// <param name="lang">The language to load</param>
        /// <param name="engineMode">The Tesseract.EngineMode value to use when initialising the tesseract engine</param>
        /// <returns>Return the OCR model will be used for performing Optical Character Recognition</returns>
        public TesseractEngine LoadOCREngine(string datapath, OCRLanguage lang, EngineMode engineMode)
        {
            //TesseractEngine ocrEngine;
            if (lang == OCRLanguage.English)
            {
                var ocrEngine = new TesseractEngine(datapath, "eng", engineMode);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Korean)
            {
                var ocrEngine = new TesseractEngine(datapath, "kor", engineMode);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Japanese)
            {
                var ocrEngine = new TesseractEngine(datapath, "jpn", engineMode);
                return ocrEngine;
            }
            if (lang == OCRLanguage.Default)
            {
                var ocrEngine = new TesseractEngine(datapath, "eng+kor+jpn", engineMode);
                return ocrEngine;
            }
            return null;
        }
        /// <summary>
        /// This method is used to load image from filename
        /// </summary>
        /// <param name="path">This is directory of image</param>
        /// <returns>Return Image image</returns>
        /// <remarks>In order to perform Optical Character Recognition. We need one image at least</remarks>
        public Image LoadImage(string path)
        {
            Image img = null;
            return img;
        }
        /// <summary>
        /// This method is used to perform OCR on input image
        /// </summary>
        /// <param name="ocrEngine">This is the input Tesseract engine</param>
        /// <param name="image">This is the input image</param>
        /// <returns>Return output text on input image</returns>
        /// <remarks>This is the almost final step of Optical Character Recognition System. Generally, the performance of OCR system will be affected by
        /// preprocessing stage or postprocessing stage</remarks>
        public string ProcessOCR(Image image)
        {
            Image img = image;

            if (this.Grayscale == true)
            {
                // grayscale.
                img = ProcessGrayscale(image);
            }

            if (this.Deskew == true)
            {
                // deskew.
                img = ProcessDeskew(image);
            }

            using (var imgPix = PixConverter.ToPix((Bitmap)img))
            {
                using (var page = GetEngine().Process(imgPix))
                {
                    var resultText = page.GetText();
                    if (!String.IsNullOrEmpty(resultText))
                    {
                        return resultText;
                    }
                    return string.Empty;
                }
            }

        }
        /// <summary>
        /// This method is used to get Tesseract engine
        /// </summary>
        /// <returns></returns>
        private TesseractEngine GetEngine()
        {
            var str = "eng + kor + jpn";
            if (this.Language == OCRLanguage.Korean)
            {
                str = "kor";
            }
            else if (this.Language == OCRLanguage.English)
            {
                str = "eng";

            }
            else if (this.Language == OCRLanguage.Japanese)
            {
                str = "jpn";

            }

            TesseractEngine engine = null;

            // check the exist of TesseractEngine
            if (availableEngines.TryGetValue(str, out engine))
            {
                // already created
                return engine;
            }

            // create TesseractEngine with language
            engine = LoadOCREngine(this.Language);
            availableEngines.Add(str, engine);
            return engine;
        }
        /// <summary>
        /// This method is used to perform OCR on image along with segmented regions
        /// </summary>
        /// <param name="ocrEngine">This is tesseract engine</param>
        /// <param name="image">This is the input image</param>
        /// <returns>Return the struct data of text and segmented region of chars, words, line and paragraght</returns>
        /// <remarks>In some cases, we need the segmented region for further processing</remarks>
        public OCRData ProcessOCRwithBoxs(Image image)
        {
            OCRData outBoxs = new OCRData();
            Image img = image;

            if (this.Grayscale == true)
            {
                // grayscale.
                img = ProcessGrayscale(image);
            }

            if (this.Deskew == true)
            {
                // deskew.
                img = ProcessDeskew(image);
            }

            using (var imgPix = PixConverter.ToPix((Bitmap)img))
            {
                //PageSegMode.SingleChar
                //using (var page = GetEngine().Process(imgPix, PageSegMode.SingleChar))
                using (var page = GetEngine().Process(imgPix))
                {
                    var resultText = page.GetText();
                    if (!String.IsNullOrEmpty(resultText))
                    {
                        outBoxs.outOCR = resultText;
                        outBoxs.charBoxs = page.GetSegmentedRegions(PageIteratorLevel.Symbol).ToArray();
                        outBoxs.wordBoxs = page.GetSegmentedRegions(PageIteratorLevel.Word).ToArray();
                        outBoxs.lineBoxs = page.GetSegmentedRegions(PageIteratorLevel.TextLine).ToArray();
                        outBoxs.paraBoxs = page.GetSegmentedRegions(PageIteratorLevel.Para).ToArray();

                    }
                }
            }
            return outBoxs;
        }

        public OCRData ProcessPLatewithBoxs(Image image)
        {
            OCRData outBoxs = new OCRData();
            Image img = image;

            if (this.Grayscale == true)
            {
                // grayscale.
                img = ProcessGrayscale(image);
            }

            if (this.Deskew == true)
            {
                // deskew.
                img = ProcessDeskew(image);
            }

            using (var imgPix = PixConverter.ToPix((Bitmap)img))
            {
                //PageSegMode.SingleChar
                //using (var page = GetEngine().Process(imgPix, PageSegMode.SingleChar))
                using (var page = GetEngine().Process(imgPix, PageSegMode.SingleChar))
                {
                    var resultText = page.GetText();
                    if (!String.IsNullOrEmpty(resultText))
                    {
                        outBoxs.outOCR = resultText;
                        outBoxs.charBoxs = page.GetSegmentedRegions(PageIteratorLevel.Symbol).ToArray();
                        outBoxs.wordBoxs = page.GetSegmentedRegions(PageIteratorLevel.Word).ToArray();
                        outBoxs.lineBoxs = page.GetSegmentedRegions(PageIteratorLevel.TextLine).ToArray();
                        outBoxs.paraBoxs = page.GetSegmentedRegions(PageIteratorLevel.Para).ToArray();

                    }
                }
            }
            return outBoxs;
        }
        /// <summary>
        /// This method is used to perform OCR on many image (list of image)
        /// </summary>
        /// <param name="ocrEngine">This is the input Tesseract engine</param>
        /// <param name="imageList">This is the list of input images that will be performed OCR</param>
        /// <returns>Return the list of the respective text for each input image</returns>
        public string[] ProcessOCR(IEnumerable<Image> imageList)
        {
            List<String> textList = new List<string>();
            foreach (var item in imageList)
            {
                var temp = ProcessOCR(item);
                textList.Add(temp);
            }
            return textList.ToArray();
        }
        /// <summary>
        /// This method is used to perform OCR on a part region of the input image
        /// </summary>
        /// <param name="image">This is the input image</param>
        /// <param name="rect">This is the subregion which will be performed OCR</param>
        /// <returns>Return the output text on the subregion</returns>
        /// <remarks></remarks>
        public string ProcessOCR(Image image, Rectangle rect)
        {
            Image img = ProcessCrop(image, rect);
            return ProcessOCR(img);
        }
        /// <summary>
        /// This method is used to perform OCR on input image  at many region
        /// </summary>
        /// <param name="ocrEngine">This is Tesseract engine</param>
        /// <param name="image">The input image</param>
        /// <param name="rectList">The list of rectange in which will be OCR</param>
        /// <returns>Return the list of output text corresponding to ecah rectangle regions</returns>
        public string[] ProcessOCR(Image image, IEnumerable<Rectangle> rectList)
        {
            List<string> textList = new List<string>();
            foreach (var item in rectList)
            {
                var temp = ProcessOCR(image, item);
                textList.Add(temp);
            }
            return textList.ToArray();

        }
        /// <summary>
        /// This method is used to convert input image to grayscale image
        /// </summary>
        /// <param name="image">This is input image</param>
        /// <returns>Return the gray output image</returns>
        /// <remarks>In many case, we should preprocesing input with grayconvert to improve the output of Optical Characteristic Recognition</remarks>
        public Image ProcessGrayscale(Image image)
        {

            if (this.GrayscaleMethod == 1)
            {
                // gray with method 1
                return RGB2Gray.Convert2Grayscale(new Bitmap(image));
            }
            if (this.GrayscaleMethod == 2)
            {
                // gray with method 2
                return ConvertGray.ConvertGrays(image);
            }
            if (this.GrayscaleMethod == 3)
            {
                //gray with method 3
                //var bitmap = (image as Bitmap);
                var bmpImage = new Bitmap(image);
                if (bmpImage == null)
                {
                    MessageBox.Show("Can not convert Image to Bitmap for Deskew");
                }
                var pixImg = PixConverter.ToPix(bmpImage);
                pixImg = pixImg.ConvertRGBToGray();
                return PixConverter.ToBitmap(pixImg);
            }
            return null;

        }

        /// <summary>
        /// This method is used to deskew input image
        /// </summary>
        /// <param name="image">This is input image</param>
        /// <returns>Return deskewed image</returns>
        /// <remarks>Deskew preprocessing is one of the most important step to significantly improve Optical Character Recogniation</remarks>
        public Image ProcessDeskew(Image image)
        {
            ////var bitmap = (image as Bitmap);
            //var bmpImage = new Bitmap(image);
            //if (bmpImage == null)
            //{
            //    MessageBox.Show("Can not convert Image to Bitmap for Deskew");
            //}
            //Scew skew;
            //var pix = PixConverter.ToPix(bmpImage);
            //pix = pix.Deskew(ScewSweep.Default, 2, 130,out skew);

            return gmseDeskew.DeskewImage(image);
        }
        /// <summary>
        /// This method is used to deskew a part region of input image
        /// </summary>
        /// <param name="image">This is inpur image</param>
        /// <param name="rect">The subregion on the input image will be performed deskew</param>
        /// <returns>Return the deskewed image with size of input rectangle</returns>
        public Image ProcessDeskew(Image image, Rectangle rect)
        {
            Image img = ProcessCrop(image, rect);
            return ProcessDeskew(img);
        }
        /// <summary>
        /// This method is used to crop input image at many region
        /// </summary>
        /// <param name="image">This is the input image</param>
        /// <param name="rectList">The input rectangle list will be refer to crop</param>
        /// <returns>List of the croped image from input image</returns>
        public Image[] ProcessCrop(Image image, IEnumerable<Rectangle> rectList)
        {
            List<Image> imgList = new List<Image>();
            foreach (var item in rectList)
            {
                var img = ProcessCrop(image, item);
                imgList.Add(img);

            }
            return imgList.ToArray();
        }
        /// <summary>
        /// This method is used to crop the input image following form of input rectangle
        /// </summary>
        /// <param name="image">This is input image</param>
        /// <param name="rect">This is input rectangle for cropping</param>
        /// <returns>Return the new image with size of the input rectangle</returns>
        /// <remarks>Crop functions could be used in setting specificed region for performing OCR. OCR just run on the croped
        /// region can reduce the consumming time significantly</remarks>
        public Image ProcessCrop(Image image, Rectangle rect)
        {
            Image img = CropImage.Crop(image, rect);
            return img;
        }
        /// <summary>
        /// This method is used to perform both preprocessing; Deskew and then crop using input rectangle
        /// </summary>
        /// <param name="image">This is the input image</param>
        /// <param name="rect">This is the input rectangle used to crop</param>
        /// <param name="HighQuality">Option for quality on output image</param>
        /// <returns>Return the croped image</returns>
        /// <remarks>This is good preprocessing for Optical Character Recognition in skewed text</remarks>
        public Image ProcessDeskewCrop(Image image, Rectangle rect)
        {
            var img = new Bitmap(image);
            //return gmseDeskew.CropRotationRect2(img, rect);//skew nhieu lan
            return gmseDeskew.CropRotationRect2(img, rect);//skew 1 lan
            
        }

        public Image ProcessMinimalCrop(Image image, Polygon2d box)
        {
            var img = new Bitmap(image);
            return gmseDeskew.CropRotationPoly(img, box);//crop mininal bouding box
        }
        /// <summary>
        /// This method is used to detect the text region based on Stroke Width Transform feature
        /// Step1: Calculate SWT image (Edage detection --> SWT)
        /// Step2: Using association to get character candidate
        /// Step3: Text detection using K-mean algorithm on text candidate (#number of optimized region is selected by elbow algorithm) 
        /// </summary>
        /// <param name="image">This is the input image</param>
        /// <returns>Return the rectang region that is bounding the text candidate</returns>
        public OCRRegion[] DetectTextRegion(Image image)
        {
            //var quanRect = SWT.textDetection(image, true);
            // implement by OpenCV 
            // or implement by other Libray
            IplImage testImg = BitmapConverter.ToIplImage((Bitmap)image);
            //var autoTextRegion = SWT.numberDetection(testImg, true);
            //var autoTextRegion = SWT.textDetection(testImg, true);
            var autoTextRegion = SWT.textDetection(kmeanOption, testImg, true);
            //var autoTextRect = new List<Rectangle>();
            //foreach (var item in autoTextRegion)
            //{
            //    var SWTRect = new Rectangle(item.Item1.X, item.Item1.Y, item.Item2.X - item.Item1.X, item.Item2.Y - item.Item1.Y);
            //    autoTextRect.Add(SWTRect);
            //}
            return autoTextRegion.ToArray();
        }

        public OCRRegion[] DetectTextPolyRegion(Image image)
        {
            IplImage testImg = BitmapConverter.ToIplImage((Bitmap)image);
            var autoTextPolyRegion = SWT.textPolyDetection(kmeanOption, testImg, true);
            return autoTextPolyRegion.ToArray();
        }
        /// <summary>
        /// This method is use to detect number plate using License Plate Recognition
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public Rectangle[] DetectPlateRegion(Image image)
        {
            //var quanRect = SWT.textDetection(image, true);
            // implement by OpenCV 
            // or implement by other Libray
            IplImage testImg = BitmapConverter.ToIplImage((Bitmap)image);
            var autoTextRegion = SWT.numberDetection(testImg, true);
            //var autoTextRegion = SWT.textDetection(testImg, true);
            var autoTextRect = new List<Rectangle>();
            foreach (var item in autoTextRegion)
            {
                var SWTRect = new Rectangle(item.Item1.X, item.Item1.Y, item.Item2.X - item.Item1.X, item.Item2.Y - item.Item1.Y);
                autoTextRect.Add(SWTRect);
            }
            return autoTextRect.ToArray();
        }

        public Rectangle[] DetectTextRegion(Image image, int numberRegion)
        {
            IplImage testImg = BitmapConverter.ToIplImage((Bitmap)image);
            //var autoTextRegion = SWT.textDetection(testImg, true);
            var autoTextRegion = SWT.textDetection(kmeanOption, testImg, true);
            var autoTextRect = new List<Rectangle>();
            //foreach (var item in autoTextRegion)
            //{
            //    var SWTRect = new Rectangle(item.Item1.X, item.Item1.Y, item.Item2.X - item.Item1.X, item.Item2.Y - item.Item1.Y);
            //    autoTextRect.Add(SWTRect);
            //}
            return autoTextRect.ToArray();
        }
    }
}
