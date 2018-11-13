using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfLicensePlateRecognition.Commands;
using WpfLicensePlateRecognition.Models;

namespace WpfLicensePlateRecognition.ViewModels
{
    public class CropSettingsViewModel: ViewModelBase
    {

        #region Command of CropSettings
        public ICommand ApplyCropSettingsCommand { get; set; }
        public ICommand AddCommand { get; set; }
        public ICommand RemoveCommand { get; set; }
        public ICommand SaveEditCommand { get; set; }
        #endregion

        #region Textbox on CropSettings
        public OCRViewModel oCr;
        private string _cropID;
        private int _cropXPosition;
        private int _cropYPosition;
        private int _cropWidth;
        private int _cropHeight;
        #endregion

        private ObservableCollection<Item> _items;
        public ObservableCollection<Item> Items
        {
            get
            {
                //return _items;
                return oCr.CurCropSetting=_items;
            }
            set
            {
                _items = value;
                oCr.CurCropSetting = value;
                OnPropertyChanged("Items");
            }
        }
        /// <summary>
        /// Binding Item selected by mouse
        /// </summary>
        private Item _selectedItem;
        public Item SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (value !=null)
                {
                    _selectedItem = value;
                    OnPropertyChanged("SelectedItem");
                    Debug.WriteLine("click" + _selectedItem.Name);
                    //Update info to Textboxs
                    CropXPosition = _selectedItem.x1;
                    CropYPosition = _selectedItem.x2;
                    CropWidth = _selectedItem.x3;
                    CropHeight = _selectedItem.x4;
                    CropID = _selectedItem.Name;
                }
                
            }
        }
        //Name
        public string CropID
        {
            get { return _cropID; }
            set
            {
                _cropID = value;
                OnPropertyChanged("CropID");
            }
        }
        //X Position
        public int CropXPosition
        {
            get { return _cropXPosition; }
            set
            {
                _cropXPosition = value;
                OnPropertyChanged("CropXPosition");
                RealTimeUpdate();
            }
        }
        //Y Position
        public int CropYPosition
        {
            get { return _cropYPosition; }
            set
            {
                _cropYPosition = value;
                OnPropertyChanged("CropYPosition");
                RealTimeUpdate();
            }
        }
        //Width of CropRect
        public int CropWidth
        {
            get { return _cropWidth; }
            set
            {
                _cropWidth = value;
                OnPropertyChanged("CropWidth");
                RealTimeUpdate();
            }
        }
        //Height of CropRect
        public int CropHeight
        {
            get { return _cropHeight; }
            set
            {
                _cropHeight = value;
                OnPropertyChanged("CropHeight");
                RealTimeUpdate();
            }
        }
        /// <summary>
        /// Method create cropsettingViewmodel
        /// </summary>
        /// <param name="ocr"></param>
        public CropSettingsViewModel(OCRViewModel ocr)
        {
            
            oCr = ocr;
            ApplyCropSettingsCommand = new RelayCommand(ApplyCropSettings);
            RemoveCommand = new RelayCommand(Remove);
            AddCommand = new RelayCommand(Add);
            SaveEditCommand = new RelayCommand(SaveEdit);
            

            //Load croped setting before
            Items = new ObservableCollection<Item>();
            foreach (var item in oCr.CropRectSettings)
            {
                _items.Add(new Item("Region " + (_items.Count()+1).ToString(), item.Rect.X, item.Rect.Y, item.Rect.Width, item.Rect.Height));

            }

        }
        /// <summary>
        /// This method is used for edit the size of Crop Rect in setting mode
        /// </summary>
        private void SaveEdit()
        {
            if ((_items.FirstOrDefault(c => c.Name == _cropID) == null && _selectedItem !=null ) || (_selectedItem != null && _cropID == SelectedItem.Name ))
            {
                var editedItem = new Item(_cropID, _cropXPosition, _cropYPosition, _cropWidth,
                _cropHeight);
                var found = _items.FirstOrDefault(i => i.Name == _selectedItem.Name);
                if (found != null)
                {
                    int it = _items.IndexOf(found);
                    _items[it] = editedItem;
                }
                else
                {
                    Debug.WriteLine("No item was selected");
                }

            }
            else
            {
                MessageBox.Show("Name is not available or you have to add first");
            }
            

        }
        /// <summary>
        /// This method is used for create new Crop Rect in setting mode
        /// </summary>
        public void Add()
        {
            if (_cropID!=null)
            {
                if (_items.FirstOrDefault(c => c.Name == _cropID) == null)
                {
                    //_items.Add(new Item("Region " + (_items.Count()+1).ToString(), 20, 20, 20, 20));
                    _items.Add(new Item(_cropID, _cropXPosition, _cropYPosition, _cropWidth,_cropHeight));
                    return;
                }
                MessageBox.Show("Add different name");
                
            }
            else
            {
                MessageBox.Show("Add name for your initial region");
            }
            

        }
        /// <summary>
        /// This method is used to remove available Crop rect in Setting mode
        /// </summary>
        private void Remove()
        {
            Debug.WriteLine("chuan bi xoa "+ _cropID);
            Debug.WriteLine("Chieu dai list hien tai: " + _items.Count().ToString() );
            try
            {
                var itemwillRemove = _items.SingleOrDefault(r => r.Name == _cropID);
                if (itemwillRemove != null)
                {

                    _items.Remove(itemwillRemove);
                    Debug.WriteLine("Xoa " + _cropID + "thanh cong");
                    Debug.WriteLine("Chieu dai list sau khi xoa:" + _items.Count().ToString());

                }
                else
                {
                    Debug.WriteLine("Khong ton tai " + _cropID + "trong list");
                }

            }
            catch (Exception)
            {

                MessageBox.Show("There are two same name. Edit before remove");
            }
          
        }
        /// <summary>
        /// This method is used to apply the Crop Rect
        /// </summary>
        private void ApplyCropSettings()
        {
            int index = 0;
            oCr.CropRectSettings = new ObservableCollection<RectItemClass>();
            foreach (var item in _items)
            {
                //create color for each Crop Rect
                SolidColorBrush color;
                if (index % 3 == 0) color = new SolidColorBrush(Colors.Red);
                else if (index % 3 == 1) color = new SolidColorBrush(Colors.Green);
                else color = new SolidColorBrush(Colors.Yellow);
                index=index +1;
                oCr.CropRectSettings.Add(new RectItemClass(color, new Rectangle(item.x1, item.x2, item.x3, item.x4), Visibility.Visible));
                //oCr.CropRectSettings.Add(new RectItemClass(color, new Rectangle(item.x1*2, item.x2*2, item.x3-10, item.x4-10), Visibility.Visible));
                Console.WriteLine("da cai dat may hinh crop: " +oCr.CropRectSettings.Count().ToString());
                
            }
        }
        /// <summary>
        /// This method is used to hidden the Crop rect (as close click)
        /// </summary>
        public void HiddenCropSettings()
        {
            int index = 0;
            oCr.CropRectSettings = new ObservableCollection<RectItemClass>();
            foreach (var item in _items)
            {
                //create color for each Crop Rect
                SolidColorBrush color;
                if (index % 3 == 0) color = new SolidColorBrush(Colors.Red);
                else if (index % 3 == 1) color = new SolidColorBrush(Colors.Green);
                else color = new SolidColorBrush(Colors.Yellow);
                index = index + 1;
                oCr.CropRectSettings.Add(new RectItemClass(color, new Rectangle(item.x1, item.x2, item.x3, item.x4), Visibility.Hidden));
            }
        }

        /// <summary>
        /// This method is used to real-time update Crop Rect when edit size in Textbox
        /// </summary>
        private void RealTimeUpdate()
        {
            oCr.CropRectSettings = new ObservableCollection<RectItemClass>();
            oCr.CropRectSettings.Add(new RectItemClass(new SolidColorBrush(Colors.Green), new Rectangle(CropXPosition, CropYPosition, CropWidth, CropHeight), Visibility.Visible));
        }
        /// <summary>
        /// Item class it is considered as embody of RectItem Class
        /// This class is used in Table List in GUI setting
        /// </summary>
        public class Item
        {
            public string Name { get; set; }
            public int x1 { get; set; }
            public int x2 { get; set; }
            public int x3 { get; set; }
            public int x4 { get; set; }
            public Item(string name, int x1, int x2, int x3, int x4)
            {
                Name = name;
                this.x1 = x1;
                this.x2 = x2;
                this.x3 = x3;
                this.x4 = x4; ;
            }
        }
    }
}
