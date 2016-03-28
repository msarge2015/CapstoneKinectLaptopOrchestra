using LaptopOrchestra.Kinect.ViewModel;
using System.ComponentModel;
using System.Diagnostics;

namespace LaptopOrchestra.Kinect.Model
{
    public class MainWindowModel : ViewModelBase
    {
        #region Properties
        private string _iconPhoto;
        public string IconPhoto
        {
            get { return _iconPhoto; }
            set
            {
                if (value != _iconPhoto)
                {
                    _iconPhoto = value;
                    OnPropertyChanged("IconPhoto");
                }
            }
        }

        private int _state;
        public int State
        {
            get { return _state; }
            set
            {
                if (value != _state)
                {
                    _state = value;
                    OnPropertyChanged("State");
                    SetIconPhoto();
                }
            }
        }

        private int _imageOrientationFlag;
        public int ImageOrientationFlag
        {
            get { return _imageOrientationFlag; }
            set
            {
                if (value != _imageOrientationFlag)
                {
                    _imageOrientationFlag = value;
                    OnPropertyChanged("ImageOrientationFlag");
                    SetIconPhoto();
                }
            }
        }

        private static string[] photo = {
            "/Assets/sensor-off.jpg",
            "/Assets/sensor-off-flip.jpg",
            "/Assets/sensor-standby.jpg",
            "/Assets/sensor-standby-flip.jpg",
            "/Assets/sensor-tracking.jpg",
            "/Assets/sensor-tracking-flip.jpg"
        };

        #endregion

        #region functions
        protected void SetIconPhoto()
        {
            IconPhoto = photo[(State * 2) + ((ImageOrientationFlag - 1) / (-2))];
            Debug.WriteLine("\n State is: " + State + "... Photo is: " + IconPhoto);
        }
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}