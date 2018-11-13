using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfApp2.Commands;
using WpfApp2.Models;
using System.Collections.ObjectModel;
//using WpfApp2.Preprocessor;

using WpfApp2.Views;
using System.Drawing;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Diagnostics;
using Xisom.OCR;


namespace WpfApp2.ViewModels
{
    /// <summary>
    /// ViewModel of Model-View-ViewModel (MVVM) design pattern
    /// 1. View: as UI
    /// 2. Models folder: contain Data Class
    /// 3. ViewModel: Glue code (Event handling, Binding, logical processing)
    /// </summary>
    public class OCRViewModel : ViewModelBase
    {
        /// <summary>
        /// LangClass ussed in bindding data on Menu
        /// </summary>
        public class Lang
        {
            public Boolean English { get; set; }
            public Boolean Korean { get; set; }
            public Boolean Japanese { get; set; }
            public Boolean Auto { get; set; }
            public Lang()
            {
                this.English = true;
                this.Korean = false;
                this.Japanese = false;
                this.Auto = false;

            }

        }

        System.Drawing.Point anchorPoint = new System.Drawing.Point(); //used in croping
        Rectangle cropedRect; //used in croping
        private bool isDragging = false; //used in croping
        private bool isSetting = false; //used to check setting window open or nor
        JsonOCRClass jsonObject; //Contain information of OCR

        public Lang _selectedLang;
        public Lang SelectedLang
        {
            get { return _selectedLang; }
            set
            {
                _selectedLang = value;
                OnPropertyChanged("SelectedLang");
                Debug.WriteLine("update item lang selection");
                _processOCR = new ProcessOCR(this);
            }
        }
        #region Commands
        public ICommand OpenImageCommand { get; set; }
        public ICommand StartOCRCommand { get; set; }
        public ICommand StartOCRCropedRegionCommand { get; set; }
        public ICommand StartAllOCRCommand { get; set; }
        public ICommand OpenImgInfoCommand { get; set; }
        public ICommand OpenCropSettingsCommand { get; set; }
        public ICommand ContrastAdjustCommand { get; set; }
        public ICommand ChangeLanguageCommand { get; set; }
        public ICommand CloseCommand { get; set; }
        public ICommand Save2JsonCommand { get; set; }
        public ICommand CloseWindowCommand { get; set; }
        public ICommand MouseLeftButtonDownCommand { get; set; }
        public ICommand MouseLeftButtonUpCommand { get; set; }
        public ICommand MouseMoveCommand { get; set; }
        public ICommand RunToGray1Command { get; set; }
        public ICommand RunToGray2Command { get; set; }
        public ICommand RunToGray3Command { get; set; }
        public ICommand DeskewCommand { get; set; }
        public ICommand StartOCRCAutoCommand { get; set; }


        private Dictionary<SolidColorBrush, List<Rectangle>> _ocrDetectedRegionVM;
        //For segmented region (char, word, line, para)
        private ObservableCollection<RectItemClass> _rectItems;
        public ObservableCollection<RectItemClass> RectItems
        {
            get { return _rectItems; }
            set
            {
                _rectItems = value;
                OnPropertyChanged("RectItems");
                Debug.WriteLine("update drawing");
            }
        }
        //For cropped by hand using mouse
        private ObservableCollection<RectItemClass> _cropRect;
        public ObservableCollection<RectItemClass> CropRect
        {
            get { return _cropRect; }
            set
            {
                _cropRect = value;
                OnPropertyChanged("CropRect");
                Debug.WriteLine("update draw croped rect");
            }
        }
        //For cropped by setting opption
        public ObservableCollection<RectItemClass> _cropRectSettings;

        private ObservableCollection<CropSettingsViewModel.Item> _curCropSetting;
        public ObservableCollection<CropSettingsViewModel.Item> CurCropSetting
        {
            get { return _curCropSetting; }
            set
            {
                _curCropSetting = value;
                OnPropertyChanged("CurCropSetting");
            }
        }

        public ObservableCollection<RectItemClass> CropRectSettings
        {
            get
            {
                return _cropRectSettings;
            }
            set
            {
                _cropRectSettings = value;
                //_cropRect2curRect();
                OnPropertyChanged("CropRectSettings");
                Debug.WriteLine("-----------------------");
                //_cropRect2curRect();


            }


        }

        private ObservableCollection<RectItemClass> _curTextRect; //quan moi them October 17

        public string laucherMouseOnTextRect(int xCoor, int yCoor)
        {
            if (_curTextRect.Count > 0)
            {
                int count = 0;
                foreach (var item in _curTextRect)
                {
                    if (xCoor >= item.Rect.X && xCoor <= item.Rect.X + item.Rect.Width && yCoor >= item.Rect.Y && yCoor <= item.Rect.Y + item.Rect.Height)
                    {
                        Debug.WriteLine("Toado x" + item.Rect.X);
                        Debug.WriteLine("Toado y" + item.Rect.Y);
                        Debug.WriteLine("Chieu rong" + item.Rect.Width);
                        Debug.WriteLine("Chieu dai" + item.Rect.Height);
                        Debug.WriteLine("Vung so" + count.ToString());
                        return "Region " + count.ToString();
                    }
                    count++;
                }

            }

            return null;
        }
        /// <summary>
        /// This method is used to update _cropRectSettings to items list in cropsetting window
        /// </summary>
        private void _cropRect2curRect()
        {
            var item = _cropRectSettings[_cropRectSettings.Count() - 1];

            _curCropSetting.Add(new CropSettingsViewModel.Item("Manual " + (_cropRectSettings.Count()).ToString(), item.Rect.X, item.Rect.Y, item.Rect.Width, item.Rect.Height));
            Debug.WriteLine("_curCropSetting: " + _curCropSetting.Count().ToString());


        }
        /// <summary>
        /// This method is used to update _autoRect to items list in cropsetting window
        /// </summary>
        private void _autoRect2curRect()
        {
            CurCropSetting.Clear();

            foreach (var item in _cropRectSettings)
            {
                this.CurCropSetting.Add(new CropSettingsViewModel.Item("Auto " + (_curCropSetting.Count()).ToString(), item.Rect.X, item.Rect.Y, item.Rect.Width, item.Rect.Height));
                Debug.WriteLine("_curCropSetting from Auto: " + _curCropSetting.Count().ToString());
            }
        }

        public OCRLib xisomOCR;

        #endregion

        #region pathToTessData
        private ProcessOCR _processOCR;
        private readonly string _pathTessData = Environment.CurrentDirectory + @"\tessdata";
        #endregion

        #region Properties
        private List<ImageClass> _imagesList;
        public List<ImageClass> ImagesList
        {
            get { return _imagesList; }
            set
            {
                _imagesList = value;
                OnPropertyChanged("ImagesList");
            }
        }

        //Single Image
        private ImageClass _imageOne;

        public ImageClass ImageOne
        {
            get { return _imageOne; }
            set
            {
                _imageOne = value;
                OnPropertyChanged("ImageOne");

            }
        }

        //List of Image
        private List<ImageClass> _imageList;
        public List<ImageClass> ImageList
        {
            get { return _imageList; }
            set
            {
                _imageList = value;
                OnPropertyChanged("ImageList");
            }
        }

        // This is for current Image, this is being displayed on GUI
        private ImageClass _currentImage;
        public ImageClass CurrentImage
        {
            get { return _currentImage; }
            set
            {
                _currentImage = value;
                OnPropertyChanged("CurrentImage");
            }
        }
        #endregion

        private string _outPutText;
        public String OutPutText
        {
            get { return _outPutText; }
            set
            {
                _outPutText = value;
                OnPropertyChanged("OutPutText");
            }
        }
        private string _outTime; //Time for running
        public CropSetting2 _cropSettings; //Window setting
        public ShowInfo _showInfo; //Window information
        public String OutTime
        {
            get { return _outTime; }
            set
            {
                _outTime = value;
                OnPropertyChanged("OutTime");
            }
        }

        /// <summary>
        /// This is used to display region box including (charBox, wordBox, lineBox and ParagraphBox)
        /// </summary>
        #region Option for vivualization of the detected region
        private Boolean _charChecked = false;
        private Boolean _wordChecked = false;
        private Boolean _lineChecked = false;
        private Boolean _paraChecked = false;

        //Char
        public Boolean CharChecked
        {
            get { return _charChecked; }
            set
            {
                _charChecked = value;
                OnPropertyChanged("CharChecked");
                DrawocrDetectedRegion();
            }
        }
        //Word
        public Boolean WordChecked
        {
            get { return _wordChecked; }
            set
            {
                _wordChecked = value;
                OnPropertyChanged("WordChecked");
                DrawocrDetectedRegion();
            }
        }
        //Line
        public Boolean LineChecked
        {
            get { return _lineChecked; }
            set
            {
                _lineChecked = value;
                OnPropertyChanged("LineChecked");
                DrawocrDetectedRegion();
            }
        }
        //Para
        public Boolean ParaChecked
        {
            get { return _paraChecked; }
            set
            {
                _paraChecked = value;
                OnPropertyChanged("ParaChecked");
                DrawocrDetectedRegion();
            }
        }
        #endregion

        private int _xCoordinate;
        public int XCoordinate
        {
            get { return _xCoordinate; }
            set
            {
                _xCoordinate = value;
                OnPropertyChanged("XCoordinate");
            }
        }

        private int _yCoordinate;
        public int YCoordinate
        {
            get { return _yCoordinate; }
            set
            {
                _yCoordinate = value;
                OnPropertyChanged("YCoordinate");
            }
        }

        private string _nameRegion;
        public string NameRegion
        {
            get { return _nameRegion; }
            set
            {
                _nameRegion = value;
                OnPropertyChanged("Nameregion");
            }
        }

        private double _xPosition;
        private double _yPosition;

        public double XPosition
        {
            get { return _xPosition; }
            set
            {
                _xPosition = value;
                //XOffset = -_xPosition;
                OnPropertyChanged("XPosition");
                Debug.WriteLine("OffsetXposition: " + _xPosition);

            }
        }
        public double YPosition
        {
            get { return _yPosition; }
            set
            {
                _yPosition = value;
                //YOffset = -_yPosition;
                OnPropertyChanged("YPosition");
                Debug.WriteLine("OffsetYposition: " + _yPosition);

            }
        }
        #region kmean option
        public ICommand ChangeAutoPosVeticalCommand { get; set; }
        public ICommand ChangeAutoPosHorizontalCommand { get; set; }
        public ICommand ChangeAutoPos2DCommand { get; set; }
        public ICommand ChangeAutoStrokeWidthCommand { get; set; }
        public ICommand ChangeAutoDimVerticalCommand { get; set; }
        public ICommand ChangeAutoDimHorizontalCommand { get; set; }
        public ICommand ChangeAutoDim2DCommand { get; set; } 
        public ICommand ChangeAutoDirectionCommand { get; set; }


        private Boolean _autoPosVertical;
        private Boolean _autoPosHorizontal;
        private Boolean _autoPos2D;
        private Boolean _autoStrokeW;
        private Boolean _autoDimVertical;
        private Boolean _autoDimHorizontal;
        private Boolean _autoDim2D;
        private Boolean _autoDirection;
        public Boolean AutoPosVertical
        {
            get { return _autoPosVertical; }
            set
            {
                _autoPosVertical = value;
                OnPropertyChanged("AutoPosVertical");
            }
        }
        public Boolean AutoPosHorizontal
        {
            get { return _autoPosHorizontal; }
            set
            {
                _autoPosHorizontal = value;
                OnPropertyChanged("AutoPosHorizontal");
            }
        }
        public Boolean AutoPos2D
        {
            get { return _autoPos2D; }
            set
            {
                _autoPos2D = value;
                OnPropertyChanged("AutoPos2D");
            }
        }
        public Boolean AutoStrokeW
        {
            get
            {
                return _autoStrokeW;
            }
            set
            {
                _autoStrokeW = value;
                OnPropertyChanged("AutoStrokeW");
            }
        }
        public Boolean AutoDimVertical
        {
            get
            {
                return _autoDimVertical;
            }
            set
            {
                _autoDimVertical = value;
                OnPropertyChanged("AutoDimVertical");
            }
        }
        public Boolean AutoDimHorizontal
        {
            get { return _autoDimHorizontal; }
            set
            {
                _autoDimHorizontal = value;
                OnPropertyChanged("AutoDimHorizontal");
            }
        }
        public Boolean AutoDim2D
        {
            get { return _autoDim2D; }
            set
            {
                _autoDim2D = value;
                OnPropertyChanged("AutoDim2D");
            }
        }
        public Boolean AutoDirection
        {
            get { return _autoDirection; }
            set
            {
                _autoDirection = value;
                OnPropertyChanged("AutoDirection");
            }
        }
        #endregion
        //public ICommand ContrastAdjustCommand { get; set; }



        //private double _xOffset;
        //private double _yOffset;
        //public double XOffset
        //{
        //    get { return _xOffset; }
        //    set
        //    {
        //        _xOffset = value;
        //        OnPropertyChanged("XOffset");
        //    }
        //}
        //public double YOffset
        //{
        //    get { return _yOffset; }
        //    set
        //    {
        //        _yOffset = value;
        //        OnPropertyChanged("YOffset");
        //    }
        //}

        /// <summary>
        /// Constructor of OCRViewmodel
        /// </summary>
        public OCRViewModel()
        {
            //_processOCR = new TesseractOCR(this);
            OpenImageCommand = new RelayCommand(OpenImage);
            StartOCRCommand = new RelayCommand(StartOCR);
            StartOCRCropedRegionCommand = new RelayCommand(StartOCRwithmultiRegions);
            StartOCRCAutoCommand = new RelayCommand(StartOCRAuto);

            StartAllOCRCommand = new RelayCommand(StartAllOCR);
            CloseWindowCommand = new RelayCommand(CloseWindow);

            _selectedLang = new Lang();
            
            //Mouse Behavior Commands
            MouseLeftButtonDownCommand = new RelayCommand2(para => MouseLeftButtonDown((MouseEventArgs)para));
            MouseLeftButtonUpCommand = new RelayCommand2(para => MouseLeftButtonUp((MouseEventArgs)para));
            MouseMoveCommand = new RelayCommand2(para => MouseMove((MouseEventArgs)para));

            //Preprocessing Command
            RunToGray1Command = new RelayCommand(RunToGray1);
            RunToGray2Command = new RelayCommand(RunToGray2);
            RunToGray3Command = new RelayCommand(RunToGray3);
            DeskewCommand = new RelayCommand(Deskew);

            //Open other window Commands
            OpenImgInfoCommand = new RelayCommand(OpenImgInfo);
            OpenCropSettingsCommand = new RelayCommand(OpenCropSettings);
            ContrastAdjustCommand = new RelayCommand(ContrastAdjust); //not used yet

            Save2JsonCommand = new RelayCommand(Save2Json);
            ChangeLanguageCommand = new RelayCommand(ChangeLanguage);
            OutPutText = "Please open an image first";

            //Process List of Image
            ImageList = new List<ImageClass>();

            //Croped region by setting
            _curCropSetting = new ObservableCollection<CropSettingsViewModel.Item>();

            CropRectSettings = new ObservableCollection<RectItemClass>();
            CropRect = new ObservableCollection<RectItemClass>();
            //CropRectSettings.Add(new RectItemClass(new SolidColorBrush(Colors.Pink), new Rectangle(20,20,300,300), Visibility.Visible));
            _cropSettings = new CropSetting2(this);
            _cropSettings.Visibility = Visibility.Hidden;

            //xisom lib instance
            xisomOCR = new OCRLib();
            _processOCR = new ProcessOCR(this);
            _curTextRect = new ObservableCollection<RectItemClass>();

            //scrollViewer
            _xPosition = 100;
            _yPosition = 100;
            //_xOffset = 100;
            //_xOffset = 100;

            //K-mean option
            ChangeAutoPosVeticalCommand = new RelayCommand(ChangeAutoPosVertical);
            ChangeAutoPosHorizontalCommand = new RelayCommand(ChangeAutoHorizontal);
            ChangeAutoPos2DCommand = new RelayCommand(ChangeAutoPos2D);
            ChangeAutoStrokeWidthCommand = new RelayCommand(ChangeAutoStrokeWidth);
            ChangeAutoDimVerticalCommand = new RelayCommand(ChangeAutoDimVertical);
            ChangeAutoDimHorizontalCommand = new RelayCommand(ChangeAutoDimHorizontal);
            ChangeAutoDim2DCommand = new RelayCommand(ChangeAutoDim2D);
            ChangeAutoDirectionCommand = new RelayCommand(ChangeAutoDirection);
            _autoPosVertical = false;
            _autoPos2D = true;
            _autoPosHorizontal = false;
            _autoDimVertical = false;
            _autoDim2D = false;
            _autoDimHorizontal = false;
            _autoDirection = false;
            //AutoPosVertical = false;
        }

        private void ChangeAutoDirection()
        {
            if (AutoDirection==true)
            {
                AutoDirection = false;

            }
            else
            {
                //AutoPos2D = false;
                //AutoPosVertical = false;
                //AutoPosHorizontal = false;

                //AutoDimHorizontal = false;
                //AutoDimVertical = false;
                //AutoDim2D = false;

                //AutoStrokeW = false;

                AutoDirection = true;
            }
        }

        private void ChangeAutoStrokeWidth()
        {
            if (AutoStrokeW==true)
            {
                AutoStrokeW = false;
            }
            else
            {
                AutoPos2D = false;
                AutoPosVertical = false;
                AutoPosHorizontal = false;

                AutoDimHorizontal = false;
                AutoDimVertical = false;
                AutoDim2D = false;

                AutoStrokeW = true;

                AutoDirection = false;
            }
        }

        private void ChangeAutoDimHorizontal()
        {
            if (AutoDimHorizontal==true)
            {
                AutoDimHorizontal = false;
            }
            else
            {
                //AutoPos2D = false;
                //AutoPosVertical = false;
                //AutoPosHorizontal = false;

                AutoStrokeW = false;
                
                AutoDimVertical = false;
                AutoDim2D = false;
                AutoDimHorizontal = true;

                AutoDirection = false;
            }
        }

        private void ChangeAutoDimVertical()
        {
            if (AutoDimVertical == true)
            {
                AutoDimVertical = false;
            }
            else
            {
                //AutoPos2D = false;
                //AutoPosVertical = false;
                //AutoPosHorizontal = false;

                AutoStrokeW = false;

                AutoDimHorizontal = false;
                AutoDim2D = false;
                AutoDimVertical = true;

                AutoDirection = false;
            }
        }

        private void ChangeAutoDim2D()
        {
            if (AutoDim2D == true)
            {
                AutoDim2D = false;
            }
            else
            {
                //AutoPos2D = false;
                //AutoPosVertical = false;
                //AutoPosHorizontal = false;

                AutoStrokeW = false;

                AutoDimHorizontal = false;
                AutoDimVertical = false;
                AutoDim2D = true;

                AutoDirection = false;
            }
        }

        private void ChangeAutoPos2D()
        {
            if (AutoPos2D == true)
            {
                AutoPos2D = false;
                Debug.WriteLine("Auto mode on AutoPos2D: " + AutoPos2D.ToString());
            }
            else
            {
                AutoPos2D = true;
                AutoPosVertical = false;
                AutoPosHorizontal = false;

                AutoStrokeW = false;

                //AutoDimHorizontal = false;
                //AutoDimVertical = false;
                //AutoDim2D = false;

                AutoDirection = false;
                Debug.WriteLine("Auto mode on PosVertica:l " + AutoPos2D.ToString());
            }
        }

        private void ChangeAutoHorizontal()
        {
            if (AutoPosHorizontal == true)
            {
                AutoPosHorizontal = false;
                Debug.WriteLine("Auto mode on AutoPosHorizontal: " + AutoPosHorizontal.ToString());
            }
            else
            {
                AutoPos2D = false;
                AutoPosVertical = false;
                AutoPosHorizontal = true;

                AutoStrokeW = false;

                //AutoDimHorizontal = false;
                //AutoDimVertical = false;
                //AutoDim2D = false;

                AutoDirection = false;
                Debug.WriteLine("Auto mode on AutoPosHorizontal: " + AutoPosHorizontal.ToString());
            }
        }

        private void ChangeAutoPosVertical()
        {
            if (AutoPosVertical==true)
            {
                AutoPosVertical = false;
                Debug.WriteLine("Auto mode on PosVertical: "+ AutoPosVertical.ToString());
            }
            else
            {
                AutoPos2D = false;
                AutoPosVertical = true;
                AutoPosHorizontal = false;

                AutoStrokeW = false;

                //AutoDimHorizontal = false;
                //AutoDimVertical = false;
                //AutoDim2D = false;

                AutoDirection = false;
                Debug.WriteLine("Auto mode on PosVertica:l "+ AutoPosVertical.ToString());
            }
        }

        /// <summary>
        /// This method is used for open Crop Setting Window
        /// </summary>
        private void OpenCropSettings()
        {
            _cropSettings.Show();
            isSetting = true;
        }
        /// <summary>
        /// This method is used to convert input image to gray image using method 1
        /// </summary>
        private void RunToGray1()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to converting grayscale 1");
                return;
            }

            Debug.WriteLine("USING XISOM OCR LIB: convert gray1");
            xisomOCR.GrayscaleMethod = 1;
            var img = xisomOCR.ProcessGrayscale(ImageConverter.BitmapImage2Bitmap(_currentImage.Image));

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage = ImageConverter.Image2BitmapImage(img);
            CurrentImage = new ImageClass
            {
                FilePath = "Gray1",
                Image = bitmapImage
            };

            ImageList.Add(CurrentImage);
            Debug.WriteLine("The lenghth of list of Image: " + ImageList.Count().ToString());

        }
        /// <summary>
        /// This method is used to convert input image to gray image using method2
        /// </summary>
        private void RunToGray2()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to converting grayscale 2");
                return;
            }
            Debug.WriteLine("USING XISOM OCR LIB: convert gray2");
            xisomOCR.GrayscaleMethod = 1;
            var img = xisomOCR.ProcessGrayscale(ImageConverter.BitmapImage2Bitmap(_currentImage.Image));

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage = ImageConverter.Image2BitmapImage(img);
            CurrentImage = new ImageClass
            {
                FilePath = "Gray2",
                Image = bitmapImage
            };

            ImageList.Add(CurrentImage);
            Debug.WriteLine("The lenghth of list of Image: " + ImageList.Count().ToString());

            //CurrentImage = ImageOne;
            //UpdateProcessedImage(filename);
        }
        /// <summary>
        /// This is used for convert RGB to GrayScale using Tesseract
        /// </summary>
        private void RunToGray3()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to converting grayscale 3");
                return;
            }

            Debug.WriteLine("USING XISOM OCR LIB: convert gray3");
            xisomOCR.GrayscaleMethod = 3;
            var img = xisomOCR.ProcessGrayscale(ImageConverter.BitmapImage2Bitmap(_currentImage.Image));

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage = ImageConverter.Image2BitmapImage(img);
            CurrentImage = new ImageClass
            {
                FilePath = "Gray3",
                Image = bitmapImage
            };

            ImageList.Add(CurrentImage);
            Debug.WriteLine("The lenghth of list of Image: " + ImageList.Count().ToString());
        }

        /// <summary>
        /// This method is used to Deskew input image
        /// </summary>
        private void Deskew()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to deskew");
                return;
            }
            Stopwatch stopW = Stopwatch.StartNew();

            //string filename = gmseDeskew.SaveAndRead(ImageOne.FilePath);
            //UpdateProcessedImage(filename);
            //Bitmap img2 = gmseDeskew.DeskewImage(ImageConverter.BitmapImage2Bitmap(_currentImage.Image)); //Manual deskew

            Debug.WriteLine("USING XISOM OCR LIB: desxkew");
            var ocr = new OCRLib();
            var img2 = ocr.ProcessDeskew(ImageConverter.BitmapImage2Bitmap(_currentImage.Image));

            //IplImage testImg = Cv.LoadImage(imagePaths[i]);
            //Using Stroke Width Transform before Auto deskew crop
            //IplImage testImg = Cv.LoadImage("C:\\Users\\admin\\Desktop\\Quanap5\\OCRQuan\\WpfApp2\\TestData9\\raw_image\\A2.png");
            //List<Tuple<CvPoint, CvPoint>> quanRect = SWT.textDetection(testImg, true);

            //Rectangle SWTRect = new Rectangle(quanRect[0].Item1.X, quanRect[0].Item1.Y,
            //    quanRect[0].Item2.X - quanRect[0].Item1.X, quanRect[0].Item2.Y - quanRect[0].Item1.Y);

            //stopW.Stop();
            //Debug.WriteLine("Time chay tinh toan SWT: " + stopW.ElapsedMilliseconds.ToString());
            //stopW.Start();

            //Bitmap img2 = gmseDeskew.CropRotationRect2(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), SWTRect, true);


            //Bitmap img2 = gmseDeskew.CropRotationRect2(ImageConverter.BitmapImage2Bitmap(_currentImage.Image),
            //   new Rectangle(84,79, 126, 114), true);

            //Bitmap img2 = gmseDeskew.CropRotationRect(ImageConverter.BitmapImage2Bitmap(_currentImage.Image),
            //   new Rectangle(169, 144, 309, 221), true);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage = ImageConverter.Bitmap2BitmapImage((Bitmap)img2);
            CurrentImage = new ImageClass
            {
                FilePath = "Deskew",
                Image = bitmapImage
            };

            ImageList.Add(CurrentImage);
            Debug.WriteLine("The lenghth of list of Image: " + ImageList.Count().ToString());

            stopW.Stop();
            Debug.WriteLine("Time chay Skew using SWT: " + stopW.ElapsedMilliseconds.ToString());
            OutTime = stopW.ElapsedMilliseconds.ToString();
            
        }
        /// <summary>
        /// This method to handle the mouse event of MouseLeftButtonDown
        /// </summary>
        /// <param name="e"></param>
        private void MouseLeftButtonDown(MouseEventArgs e)
        {
            if (_cropSettings.Visibility == Visibility.Hidden)
            {
                isSetting = false;
                ////this part of code for making CropSetting appear when you crop
                //isSetting = true;
                //_cropSettings.Visibility = Visibility.Visible;
            }
            else
            {
                isSetting = true;
            }

            if (isDragging == false)
            {
                Debug.WriteLine("This is mouse down: " + e.GetPosition((IInputElement)e.Source));
                isDragging = true;
                anchorPoint.X = (int)e.GetPosition((IInputElement)e.Source).X;
                anchorPoint.Y = (int)e.GetPosition((IInputElement)e.Source).Y;
                cropedRect = new Rectangle();
            }
        }
        /// <summary>
        /// This method to handle the mouse event of MouseMove
        /// </summary>
        /// <param name="e"></param>
        private void MouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                Debug.WriteLine("This is mouse Moving: " + e.GetPosition((IInputElement)e.Source));
                double x = e.GetPosition((IInputElement)e.Source).X;
                double y = e.GetPosition((IInputElement)e.Source).Y;
                cropedRect.X = (int)Math.Min(x, anchorPoint.X);
                cropedRect.Y = (int)Math.Min(y, anchorPoint.Y);
                cropedRect.Width = (int)Math.Abs(x - anchorPoint.X);
                cropedRect.Height = (int)Math.Abs(y - anchorPoint.Y);
                Debug.WriteLine("The WIDTH: " + Math.Abs(x - anchorPoint.X));
                Debug.WriteLine("The HEIGHT: " + Math.Abs(y - anchorPoint.Y));
                CropRect.Clear();
                CropRect.Add(new RectItemClass(new SolidColorBrush(Colors.Red), cropedRect, Visibility.Visible));

            }
            else
            {
                //Debug.WriteLine("This is mouse Moving without Crop: " + e.GetPosition((IInputElement)e.Source));
                var x = e.GetPosition((IInputElement)e.Source).X;
                var y = e.GetPosition((IInputElement)e.Source).Y;
                XCoordinate = (int)x;
                YCoordinate = (int)y;
                NameRegion = laucherMouseOnTextRect(XCoordinate, YCoordinate);

            }
        }
        /// <summary>
        /// This method to handle the mouse event of MouseLeftButtonUp
        /// </summary>
        /// <param name="e"></param>
        private void MouseLeftButtonUp(MouseEventArgs e)
        {
            if (isDragging)
            {
                Debug.WriteLine("This is mouse up: " + e.GetPosition((IInputElement)e.Source));
                isDragging = false;
                if (cropedRect.Width > 0 && cropedRect.Height > 0)
                {
                    CropRect.Clear();
                    Debug.WriteLine("Finish crop");

                    //Image img = CropImage.Crop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), cropedRect);

                    Debug.WriteLine("USING XISOM OCR LIB: crop");
                    var ocr = new OCRLib();
                    var img = ocr.ProcessCrop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), cropedRect);

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage = ImageConverter.Image2BitmapImage(img);

                    //Add crop rect to setting when setting window turn on
                    if (isSetting)
                    {
                        CropRectSettings.Add(new RectItemClass(new SolidColorBrush(Colors.Black), cropedRect, Visibility.Visible));
                        _cropRect2curRect();
                        return;
                        
                    }
                    CurrentImage = new ImageClass
                    {
                        FilePath = "Deskew",
                        Image = bitmapImage
                    };

                    ImageList.Add(CurrentImage);
                    Debug.WriteLine("The lenghth of list of Image: " + ImageList.Count().ToString());
                }
            }
        }

        /// <summary>
        /// Used for Change Language use Icommand parametter
        /// </summary>
        /// <param name="lang">To determine which language was selected</param>
        private void ChangeLanguage(object lang)
        {
            //there is a bit confuse here
            if (lang.ToString().Equals("eng"))
            {
                if (_selectedLang.English == false)
                {
                    Debug.WriteLine("Unchecked English");
                    _selectedLang.English = false;
                    SelectedLang = _selectedLang;
                }
                else
                {
                    Debug.WriteLine("Checked English");
                    _selectedLang.English = true;
                    _selectedLang.Korean = _selectedLang.Japanese = _selectedLang.Auto = false;
                    SelectedLang = _selectedLang;
                }
                return;
            }
            if (lang.ToString().Equals("kor"))
            {
                if (_selectedLang.Korean == false)
                {
                    Debug.WriteLine("Unchecked Korea");
                    _selectedLang.Korean = false;
                    SelectedLang = _selectedLang;

                }
                else
                {
                    Debug.WriteLine("Checked Korean");
                    _selectedLang.Korean = true;
                    _selectedLang.English = _selectedLang.Japanese = _selectedLang.Auto = false;
                    SelectedLang = _selectedLang;
                }
                return;
            }
            if (lang.ToString().Equals("jpn"))
            {
                if (_selectedLang.Japanese == false)
                {
                    Debug.WriteLine("Unchecked Japanese");
                    _selectedLang.Japanese = false;
                    SelectedLang = _selectedLang;

                }
                else
                {
                    Debug.WriteLine("Checked Japnese");
                    SelectedLang.Japanese = true;
                    _selectedLang.English = _selectedLang.Korean = _selectedLang.Auto = false;
                    SelectedLang = _selectedLang;
                }
                return;
            }
            if (lang.ToString().Equals("auto"))
            {
                if (SelectedLang.Auto == false)
                {
                    Debug.WriteLine("Unchecked Auto");
                    _selectedLang.Auto = false;
                    SelectedLang = _selectedLang;
                }
                else
                {
                    Debug.WriteLine("Checked Auto");
                    _selectedLang.Auto = true;
                    _selectedLang.English = _selectedLang.Korean = _selectedLang.Japanese = false;
                    SelectedLang = _selectedLang;
                }
                return;
            }

        }
        /// <summary>
        /// This method is used to close whole application
        /// </summary>
        private void CloseWindow()
        {
            //_cropSettings.Close();
            Environment.Exit(0);
        }
        /// <summary>
        /// This method is used to save output text and OCR into file
        /// </summary>
        private void Save2Json()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "text Document",
                DefaultExt = ".txt",
                Filter = "Text document (.txt)|*.txt"
            };

            string path = dlg.ShowDialog() != true ? null : dlg.FileName;

            if (string.IsNullOrEmpty(path))
            {
                Debug.WriteLine("DO NOT Save");
                return;
            }
            else
            {
                Debug.WriteLine("SAVE OK OK");
                using (StreamWriter file = File.CreateText(path))
                {
                    jsonObject = new JsonOCRClass(ImageOne, null, _ocrDetectedRegionVM);
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, jsonObject);
                }

            }

        }

        /// <summary>
        /// this method is used for adjusting contrast
        /// </summary>
        private void ContrastAdjust()
        {
            ContrasAdjust _contrastAdjust = new ContrasAdjust();
            _contrastAdjust.Show();
            Debug.WriteLine("Open image Infro");
        }

        /// <summary>
        /// This method is used for show ImageProperty 
        /// </summary>
        private void OpenImgInfo()
        {
            if (ImageOne == null)
            {
                MessageBox.Show("Please open at least one image");
                return;
            }
            //ShowInfo _showInfo = new ShowInfo(ImageOne);
            //_showInfo.Show();
            _showInfo.Show();
            Debug.WriteLine("Open image Infro");
        }
        /// <summary>
        /// StartOCR run when we click menu item run 
        /// It perform on current image
        /// </summary>
        private void StartOCR()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to start OCR");
                return;
            }

            if (!_processOCR.IsSelectedLanguage)
            {
                MessageBox.Show("Please select one language for loading respective model in current mode");
                return;
            }

            Debug.WriteLine("Executing StartOCR");
            Debug.WriteLine(_pathTessData);
            if (!Directory.Exists(_pathTessData))
            {
                MessageBox.Show("You dont have Tess data. OCR can not Run");
                return;
            }
               var tem_Text = _processOCR.OneImageOCR(_currentImage);

                if (tem_Text == null)
                {
                    OutPutText = "No answer";
                    OutTime = _processOCR.getTime() + " ms for running";
                }
                else
                {
                    OutPutText = tem_Text;
                    OutTime = _processOCR.getTime() + " ms for running";
                    _ocrDetectedRegionVM = _processOCR.GetocrDetectedRegion();
                    DrawocrDetectedRegion();
                }
        }
        /// <summary>
        /// This method is used for eunning OCR on multiple croped regions
        /// </summary>
        private void StartOCRwithmultiRegions()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to start OCR at multiple regions");
                return;
            }

            if (!_processOCR.IsSelectedLanguage)
            {
                MessageBox.Show("Please select one language for loading respective model in crop mode");
                return;
            }


            _curTextRect = _cropRectSettings;
            Stopwatch stopW = Stopwatch.StartNew();
            Debug.WriteLine("Executing StartOCRwithmultiRegions");
            if (_cropRectSettings.Count>0)
            {
                ImageList = new List<ImageClass>();
                foreach (var item in _cropRectSettings)
                {
                    Debug.WriteLine("USING XISOM OCR LIB: processdeskewcrop multiregion");
                    var ocr = new OCRLib();
                    var img2 = ocr.ProcessDeskewCrop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), item.Rect);

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage = ImageConverter.Image2BitmapImage(img2);
                    var SubImage = new ImageClass
                    {
                        FilePath = "SubImage",
                        Image = bitmapImage
                    };
                    //Add to ImageList
                    ImageList.Add(SubImage);
                }

                StartAllOCR();

                stopW.Stop();
                Debug.WriteLine("Time chay Skew using multi crop region: " + stopW.ElapsedMilliseconds.ToString());
                OutTime = stopW.ElapsedMilliseconds.ToString();
                return;
            }
            MessageBox.Show("No region was selected");
            
        }
        /// <summary>
        /// This method is used to peform Optical Character recognition automatically
        /// Step1: Calculate Stroke Width Transform feature
        /// Step2: Text detextion based on K-mean cluster
        /// Step3: OCR on individual text region
        /// </summary>
        private void StartOCRAuto()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to start OCR in Auto mode");
                return;
            }

            if (!_processOCR.IsSelectedLanguage)
            {
                MessageBox.Show("Please select one language for loading respective model in Auto mode");
                return;
            }

            Stopwatch stopW = Stopwatch.StartNew();
            
            xisomOCR.kmeanOption.pos2Dimesions = AutoPos2D;
            xisomOCR.kmeanOption.posHorizontal = AutoPosHorizontal;
            xisomOCR.kmeanOption.posVertical = AutoPosVertical;
            xisomOCR.kmeanOption.strokeWidth = AutoStrokeW;
            xisomOCR.kmeanOption.dim2Dimensions = AutoDim2D;
            xisomOCR.kmeanOption.dimHorizontal = AutoDimHorizontal;
            xisomOCR.kmeanOption.dimVertical = AutoDimVertical;
            xisomOCR.kmeanOption.direction = AutoDirection;

            if (AutoPos2D == true)
            {
                xisomOCR.OptionTest1 |= KMeanOptionEnum.Position;
                Debug.WriteLine((xisomOCR.OptionTest1 & KMeanOptionEnum.Position)!=0);
            }
            if (AutoDim2D == true)
            {
                xisomOCR.OptionTest1 |= KMeanOptionEnum.Dimension;
                Debug.WriteLine((xisomOCR.OptionTest1 & KMeanOptionEnum.Dimension) != 0);
            }
            if (AutoDirection == true)
            {
                xisomOCR.OptionTest1 |= KMeanOptionEnum.Direction;
                Debug.WriteLine((xisomOCR.OptionTest1 & KMeanOptionEnum.Direction) != 0);
            }
            if (AutoStrokeW == true)
            {
                xisomOCR.OptionTest1 |= KMeanOptionEnum.StrokeWidth;
                Debug.WriteLine((xisomOCR.OptionTest1 & KMeanOptionEnum.StrokeWidth) != 0);
            }

            Debug.WriteLine(~(~0 << 4));
           

            xisomOCR.OptionTest2 = KMeanOptionEnum.None;
            if(AutoPos2D == true)
            {
                xisomOCR.OptionTest2 |= KMeanOptionEnum.Position;
            }


            #region autoTextRect

            var autoTextRect = xisomOCR.DetectTextRegion(ImageConverter.BitmapImage2Bitmap(_currentImage.Image));
            if (autoTextRect.Count() > 0)
            {
                _curTextRect.Clear();
                ImageList.Clear();
                //ImageList = new List<ImageClass>();

                CropRectSettings = new ObservableCollection<RectItemClass>();
                //CropRectSettings.Clear();
                int index = 0;
                foreach (var item in autoTextRect)
                {
                    _curTextRect.Add(new RectItemClass(null, item.rect2));
                    Debug.WriteLine("USING XISOM OCR LIB: processdeskewcrop");
                    var ocr = new OCRLib();
                    //var img2 = ocr.ProcessDeskewCrop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), item);
                    //var img2 = ocr.ProcessCrop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), item);
                    //var img2 = ocr.ProcessMinimalCrop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), item);
                    var img2 = ocr.ProcessMinimalCrop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), item.mininalbox);

                    img2.Save("Region" + index.ToString() + ".png");
                    index++;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage = ImageConverter.Image2BitmapImage(img2);
                    var SubImage = new ImageClass
                    {
                        FilePath = "SubImage",
                        Image = bitmapImage
                    };

                    //Add to ImageList
                    ImageList.Add(SubImage);
                    //Update to GUI
                    //CurrentImage = SubImage;

                    CropRectSettings.Add(new RectItemClass(new SolidColorBrush(Colors.Black), item.rect2, Visibility.Hidden)); // update to crop setting window

                }
                _autoRect2curRect();// update to crop setting window

                StartAllOCR();

                // return;
            }
            #endregion

            #region autoTextPoly
            //var autoMinimalTextRect = xisomOCR.DetectTextPolyRegion(ImageConverter.BitmapImage2Bitmap(_currentImage.Image));
            //if (autoMinimalTextRect.Count() > 0)
            //{
            //    _curTextRect.Clear();
            //    ImageList.Clear();
            //    //ImageList = new List<ImageClass>();

            //    CropRectSettings = new ObservableCollection<RectItemClass>();
            //    //CropRectSettings.Clear();
            //    int index = 0;
            //    foreach (var bitem in autoMinimalTextRect)
            //    {
            //        //_curTextRect.Add(new RectItemClass(null, item));
            //        Debug.WriteLine("USING XISOM OCR LIB: processdeskewcrop");
            //        var ocr = new OCRLib();
            //        var img2 = ocr.ProcessMinimalCrop(ImageConverter.BitmapImage2Bitmap(_currentImage.Image), bitem.mininalbox);
            //        img2.Save("Region" + index.ToString() + ".png");
            //        index++;

            //        BitmapImage bitmapImage = new BitmapImage();
            //        bitmapImage = ImageConverter.Image2BitmapImage(img2);
            //        var SubImage = new ImageClass
            //        {
            //            FilePath = "SubImage",
            //            Image = bitmapImage
            //        };

            //        //Add to ImageList
            //        ImageList.Add(SubImage);
            //        //Update to GUI
            //        //CurrentImage = SubImage;

            //        CropRectSettings.Add(new RectItemClass(new SolidColorBrush(Colors.Black), bitem.rect2, Visibility.Hidden)); // update to crop setting window

            //    }

            //    _autoRect2curRect();// update to crop setting window

            //    StartAllOCR();
            //}
            #endregion

            stopW.Stop();
            Debug.WriteLine("Consuming time for OCR Auto running: " + stopW.ElapsedMilliseconds.ToString());
            OutTime = stopW.ElapsedMilliseconds.ToString();
        }
        /// <summary>
        /// StartAllOCR when we click menu item run All
        /// </summary>
        private void StartAllOCR()
        {
            if (_currentImage == null)
            {
                MessageBox.Show("Please open one image at least to start all oCR");
                return;
            }

            if (!_processOCR.IsSelectedLanguage)
            {
                MessageBox.Show("Please select one language for loading respective model in all mode");
                return;
            }


            Debug.WriteLine("Executing StartAllOCR");
            Debug.WriteLine(_pathTessData);
            if (!Directory.Exists(_pathTessData))
            {
                MessageBox.Show("You dont have Tess data. ALLOCR can not Run");
            }

            if (!_processOCR.IsSelectedLanguage)
            {
                MessageBox.Show("Please select one language for loadding respective model");
            }
            else
            {
                try
                {
                    OutTime = ""; //reset
                    OutPutText = ""; //reset textbox
                    var index = 0;

                    var outputTextList = new List<string>();
                    foreach (var item in _imageList)
                    {
                        var tem_Text = _processOCR.OneImageOCR(item);
                        outputTextList.Add(tem_Text);
                        OutTime = string.Concat(OutTime, _processOCR.getTime() + "(ms); ");

                        OutPutText = string.Concat(_outPutText, "===============" + "Region " + index.ToString() + "===============\n");
                        this.OutPutText = string.Concat(_outPutText, tem_Text);
                        index += 1;
                    }
                }
                catch (Exception e)
                {
                    //throw;
                    Debug.WriteLine(e);
                }
            }

        }
        /// <summary>
        /// This is used to update rectangle box to ObservableCollect and binding to UI
        /// </summary>
        private void DrawocrDetectedRegion()
        {
            RectItems = new ObservableCollection<RectItemClass>();
            if (_ocrDetectedRegionVM != null)
            {
                foreach (SolidColorBrush colr in _ocrDetectedRegionVM.Keys)
                {
                    if (colr.Color == Colors.Violet && _charChecked == true)
                    {
                        foreach (Rectangle rect in _ocrDetectedRegionVM[colr])
                        {
                            RectItems.Add(new RectItemClass(colr, rect, Visibility.Visible));
                        }
                    }

                    //word
                    if (colr.Color == Colors.Yellow && _wordChecked == true)
                    {
                        foreach (Rectangle rect in _ocrDetectedRegionVM[colr])
                        {
                            RectItems.Add(new RectItemClass(colr, rect, Visibility.Visible));
                        }

                    }
                    //line
                    if (colr.Color == Colors.Green && _lineChecked == true)
                    {
                        foreach (Rectangle rect in _ocrDetectedRegionVM[colr])
                        {
                            RectItems.Add(new RectItemClass(colr, rect, Visibility.Visible));
                        }

                    }
                    //para
                    if (colr.Color == Colors.Red && _paraChecked == true)
                    {
                        foreach (Rectangle rect in _ocrDetectedRegionVM[colr])
                        {
                            RectItems.Add(new RectItemClass(colr, rect, Visibility.Visible));
                        }

                    }

                }
            }
        }
        /// <summary>
        /// This method is used for open an image when user click on menu
        /// </summary>
        private void OpenImage()
        {
           
            Debug.WriteLine("Executing OpenImage");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a image for OCR";
            openFileDialog.Filter = "All supported graphics |*.jpg; *.jpeg;*.png|" +
                "JPEG(*.jpg;*.jpeg)|*.jpg; *.jpeg|" +
                "Portable Network Graphic (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                //Invisible detected reegion of previous image when new image was openned
                CharChecked = false; WordChecked = false;
                LineChecked = false; ParaChecked = false;

                string filename = openFileDialog.FileName;
                UpdateProcessedImage(filename); //update processed image to GUI
                OutPutText = "Click RUN button to start OCR demo";

                //Information window
                _showInfo = new ShowInfo(ImageOne);
                //_showInfo.Show();
                _showInfo.Hide();

            }
        }
        /// <summary>
        /// This method to used to upadate processed image to Image box
        /// </summary>
        /// <param name="filename"></param>
        private void UpdateProcessedImage(string filename)
        {
            var bitmap = new BitmapImage(new Uri(filename));

            ImageOne = new ImageClass
            {
                FilePath = filename,
                Image = bitmap
            };

            CurrentImage = ImageOne;
            ImageList.Add(CurrentImage);
            Debug.WriteLine("The lenghth of list of Image: " + ImageList.Count().ToString());
           
        }

    }
}
