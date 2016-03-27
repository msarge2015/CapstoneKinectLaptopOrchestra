using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using LaptopOrchestra.Kinect.ViewModel;
using System.Timers;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Windows;

namespace LaptopOrchestra.Kinect.Model
{
    public class TabWindowViewModel: ViewModelBase
    {
        #region Properties
        private SessionManager _sessionManager;
        private Timer _timer;

        private int _numSessions;
        public int NumSessions
        {
            get { return _numSessions; }
            set
            {
                if (value != _numSessions)
                {
                    _numSessions = value;
                    OnPropertyChanged("NumSessions");
                }
            }
        }

        private string _selectedTab;
        public string SelectedTab
        {
            get { return _selectedTab; }
            set
            {
                if (value != _selectedTab)
                {
                    _selectedTab = value;
                    UpdateSecondList(_selectedTab);
                    OnPropertyChanged("SelectedTab");
                }
            }
        }

        private ObservableCollection<string> _firstList;
        public ObservableCollection<string> FirstList
        {
            get { return _firstList; }
            set
            {
                if (value != _firstList)
                {
                    _firstList = value;
                    OnPropertyChanged("FirstList");
                }
            }
        }

        private string[] _firstArray;
        public string[] FirstArray
        {
            get { return _firstArray; }
            set
            {
                if (value != _firstArray)
                {
                    _firstArray = value;
                    //UpdateFirstList();
                    OnPropertyChanged("FirstArray");
                }
            }
        }

        private ObservableCollection<string> _secondList;
        public ObservableCollection<string> SecondList
        {
            get { return _secondList; }
            set
            {
                if (value != _secondList)
                {
                    _secondList = value;
                    OnPropertyChanged("SecondList");
                }
            }
        }

        private string[,] _allJoints;
        public string[,] AllJoints
        {
            get { return _allJoints; }
            set
            {
                if (value != _allJoints)
                {
                    _allJoints = value;
                    //UpdateSecondList(SelectedTab);
                    OnPropertyChanged("AllJoints");
                }
            }
        }
        #endregion

        #region Constructor
        public TabWindowViewModel(SessionManager sessionManager)
        {
            //Initialize
            _sessionManager = sessionManager;
            NumSessions = 0;
            FirstList = new ObservableCollection<string>();
            SecondList = new ObservableCollection<string>();
            AllJoints = new string[20,25]; //20 possible connections, 25 possible joints per connection

            //Start thread
            _timer = new System.Timers.Timer(500);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }
        #endregion

        #region Functions
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Debug.WriteLine("\nBackground thread hit");
            UpdateFirstArray();
            UpdateSecondList(SelectedTab);
        }

        private void UpdateFirstArray()
        {
            //Debug.WriteLine("\nAddTabs");
            NumSessions = 0;

            AllJoints = new string[20, 25]; 
            SessionWorker[] workers = new SessionWorker[_sessionManager.OpenConnections.Count];
            _sessionManager.OpenConnections.CopyTo(workers);
            var jointTypes = Enum.GetValues(typeof(JointType));
            FirstArray = new string[20];

            foreach (SessionWorker sw in workers)
            {
                // Get the port and IP of this session
                string id = sw.Ip + ":" + sw.Port.ToString();

                //Get the flags of the current session
                ConfigFlags flags = sw.ConfigFlags;

                //Add session name to session array
                FirstArray[NumSessions] = id;

                //get joint list for current session
                List<string> MyJointList = new List<string>();
                foreach (JointType jt in jointTypes) {
                    if (flags.JointFlags[jt])
                    {
                        MyJointList.Add(jt.ToString());
                    }
                }

                //get joint array
                string[] MyJointArray = MyJointList.ToArray();

                //set current joint array in the master 2d joint array
                int index;
                for (index = 0; index < MyJointArray.Length; index++)
                {
                    AllJoints[NumSessions, index] = MyJointArray[index];
                }
                    
                //increment number of tracked sessions
                NumSessions++;
            }

            //update First GUI Box
            UpdateFirstList();
        }

        private void UpdateFirstList()
        {
            //update FirstList (GUI)
            bool x = true;
            int y = 0;
            int z = 0;
            var TempList = new ObservableCollection<string>(FirstArray);

            //Find Tab to show
            for (y = 0; y < 20; y++)
            {
                if (SelectedTab == null)
                {
                    z = 0;
                }
                else if (SelectedTab == FirstArray[y] && x == true)
                {
                    z = y;
                    x = false;
                }
                //break;
            }

            DispatchService.Invoke(() =>
            {
                //this.FirstList = new ObservableCollection<string>(TempArray1);
                this.FirstList = TempList;
                this.SelectedTab = this.FirstArray[z];
            });
        }

        private void UpdateSecondList(string id)
        {
            //Debug.WriteLine("\n Updating Second List!");
            SecondList = new ObservableCollection<string>();

            if (id != null)
            {   
                int index = 0;
                for (index = 0; index < NumSessions; index++)
                {
                    //Debug.WriteLine("\n index: " + index + ", NumSess = " + NumSessions + "\nid: "+ id + "\n : FirstList[index]" + FirstList[index]);
                    if (FirstList[index] == id) break;
                }
                
                string[] TempArray = new string[25];
                for (int i = 0; i<25; i++)
                {
                    if (AllJoints[index, i] == "/0") break;
                    TempArray[i] = AllJoints[index,i];
                }

                //update SecondList (GUI)
                var TempList2 = new ObservableCollection<string>(TempArray);
                DispatchService.Invoke(() =>
                {
                    this.SecondList = TempList2;
                });
            }
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

public static class DispatchService
{
    public static void Invoke(Action action)
    {
        Dispatcher dispatchObject = Application.Current.Dispatcher;
        if (dispatchObject == null || dispatchObject.CheckAccess())
        {
            action();
        }
        else
        {
            dispatchObject.Invoke(action);
        }
    }

    public static void Stop()
    {
        Dispatcher dispatchObject = Application.Current.Dispatcher;
        dispatchObject.InvokeShutdown();
    }
}