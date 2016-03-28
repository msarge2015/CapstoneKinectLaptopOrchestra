using LaptopOrchestra.Kinect.Model;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System;

//UI THREAD
namespace LaptopOrchestra.Kinect.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Public Properties
        public EventHandler RequestClose { get; internal set; }
        private CoordinateMapper _coordinateMapper;
        private SessionManager _sessionManager;
        private Dictionary<JointType, bool> _configurationFlags;
        private KinectProcessor _kinectProcessor;
        private UDPReceiver _udpRec;
        private IList<Body> _bodies;

        private MainWindowModel _currentWindow;
        public MainWindowModel CurrentWindow
        {
            get { return _currentWindow; }
            set
            {
                if (_currentWindow != value)
                {
                    _currentWindow = value;
                    NotifyPropertyChanged("CurrentWindow");
                }
            }
        }

        private TabWindowViewModel _myTabWindowViewModel;
        public TabWindowViewModel MyTabWindowViewModel
        {
            get { return _myTabWindowViewModel; }
            set
            {
                if (_myTabWindowViewModel != value)
                {
                    _myTabWindowViewModel = value;
                    NotifyPropertyChanged("MyTabWindowViewModel");
                }
            }
        }

        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            //Initialize ViewModel
            _configurationFlags = new Dictionary<JointType, bool>();
            _kinectProcessor = new KinectProcessor();
            _sessionManager = new SessionManager();
            _udpRec = new UDPReceiver(8080, _sessionManager, _kinectProcessor);
            _coordinateMapper = _kinectProcessor.CoordinateMapper;

            //Initialize  MainWindow model
            CurrentWindow = new MainWindowModel();
            CurrentWindow.ImageOrientationFlag = 1;
            CurrentWindow.State = 0;

            //Start the background thread for updating tabs.
            MyTabWindowViewModel = new TabWindowViewModel(_sessionManager);

            //Start the UI thread for updating the UI. //Debug.WriteLine("\n starting UI thread \n");
            _kinectProcessor.Reader.MultiSourceFrameArrived += UI_Thread;
            _kinectProcessor.Sensor.IsAvailableChanged += UpdateSensorAvailablility;
        }
        #endregion

        #region Helper Functions
        private void UI_Thread(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //Debug.WriteLine("\nUI thread hit");
            var reference = e.FrameReference.AcquireFrame();
            var mainWin = System.Windows.Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;

            #region draw image
            // Draw the Image from the Camera
            var frame = reference.ColorFrameReference.AcquireFrame();
            if (frame != null)
            {
                using (frame)
                {
                    if (frame != null)
                    {
                        //frame is valid, so set state to STANDBY
                        CurrentWindow.State = 1;

                        //draw frame to screen
                        mainWin.XAMLImage.Source = frame.ToBitmap();
                    }
                }
            }
            else
                return;
            #endregion draw bodies

            #region draw skeleton
            // Acquire skeleton data
            var bodyFrame = reference.BodyFrameReference.AcquireFrame();

            if (bodyFrame == null)
                return;
            else
            {
                using (bodyFrame)
                {
                    mainWin.XAMLCanvas.Children.Clear();
                    _bodies = new Body[bodyFrame.BodyFrameSource.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(_bodies);
                    bool isFirst = true;

                    foreach (var body in _bodies)
                    {
                        if (body == null || !body.IsTracked) continue;

                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, System.Windows.Point> alignedJointPoints = new Dictionary<JointType, System.Windows.Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                                position.Z = 0.01f;

                            ColorSpacePoint colorPoint = _coordinateMapper.MapCameraPointToColorSpace(position);
                            alignedJointPoints[jointType] = new System.Windows.Point(colorPoint.X, colorPoint.Y);
                        }

                        mainWin.XAMLCanvas.DrawSkeleton(body, alignedJointPoints, isFirst);
                        isFirst = false;
                        CurrentWindow.State = 2;
                    }
                }
            }
            #endregion draw skeleton

        }

        private void UpdateSensorAvailablility(object sender, IsAvailableChangedEventArgs e)
        {
            CurrentWindow.State = 0;
            var mainWin = System.Windows.Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
            mainWin.XAMLImage.Source = null;
        }

        #endregion

        #region Commands
        //Commands for the menu and buttons

        private RelayCommand _exitCommand;
        public RelayCommand ExitCommand
        {
            get
            {
                if (_exitCommand == null)
                {
                    _exitCommand = new RelayCommand(param => this.ExitCommandLogic());
                }
                return _exitCommand;
            }
        }
        private void ExitCommandLogic()
        {
            System.Windows.Application.Current.Shutdown();
        }

        private RelayCommand _flipCameraCommand;
        public RelayCommand FlipCameraCommand
        {
            get
            {
                if (_flipCameraCommand == null)
                {
                    _flipCameraCommand = new RelayCommand(param => this.FlipCameraCommandLogic());
                }
                return _flipCameraCommand;
            }
        }
        private void FlipCameraCommandLogic()
        {
            if (CurrentWindow.ImageOrientationFlag == 1)
            {
                CurrentWindow.ImageOrientationFlag = -1;
            }   
            else
            {
                CurrentWindow.ImageOrientationFlag = 1;
            }
        }

        private RelayCommand _openWebsiteCommand;
        public RelayCommand OpenWebsiteCommand
        {
            get
            {
                if (_openWebsiteCommand == null)
                {
                    _openWebsiteCommand = new RelayCommand(param => this.OpenWebsiteCommandLogic());
                }
                return _openWebsiteCommand;
            }
        }
        private void OpenWebsiteCommandLogic()
        {
            Process.Start("https://ubcimpart.wordpress.com");
        }

        private RelayCommand _aboutCommand;
        public RelayCommand AboutCommand
        {
            get
            {
                if (_aboutCommand == null)
                {
                    _aboutCommand = new RelayCommand(param => this.AboutCommandLogic());
                }
                return _aboutCommand;
            }
        }
        private void AboutCommandLogic()
        {
            //inserting about command logic here
            const string message = "KiCASS was created and developed by students from the University of British Columbia, Canada:"
                + "\n\nIsaac Cheng, Russil Glover, Kelsey Hawley, Kevin Hui, and Michael Sargent."
                + "\n\nRead more about the project at the UBC IMPART blog: http://www.ubcimpart.wordpress.com."
                + "\n\nKiCASS ver.1.00. Released Mar 22, 2016.";
            const string caption = "About KiCASS";
            var result = System.Windows.Forms.MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion // Commands

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