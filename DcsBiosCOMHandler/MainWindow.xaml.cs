using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Timers;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DcsBiosCOMHandler.Properties;
using DCS_BIOS;
using MessageBox = System.Windows.MessageBox;

namespace DcsBiosCOMHandler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IUIListener, IDcsBiosDataListener
    {
        private String[] _lastSerialPortsWhenChecking = new String[] { };
        private readonly object _lockObject = new object();
        private const String WindowName = "DCS-BIOS COM Port Handler ";
        private System.Timers.Timer _dcsStopGearTimer = new System.Timers.Timer(5000);
        private DcsBiosInheritor _dcsBiosInheritor;

        public MainWindow()
        {
            InitializeComponent();
            _dcsStopGearTimer.Elapsed += TimerStopRotation;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _lastSerialPortsWhenChecking = SerialPort.GetPortNames();
                _dcsBiosInheritor = new DcsBiosInheritor(this, Settings.Default.DCSAddressFrom, Settings.Default.DCSAddressTo, Settings.Default.DCSPortFrom, Settings.Default.DCSPortTo, DcsBiosNotificationMode.ByteArray);
                _dcsBiosInheritor.Startup();
                if (!String.IsNullOrEmpty(Settings.Default.LastProfileFileUsed))
                {
                    SerialPortsProfileHandler.GetSerialPortsProfileHandler().LoadProfile(Settings.Default.LastProfileFileUsed);
                    if (SerialPortsProfileHandler.GetSerialPortsProfileHandler().DCSSerialPortsStringSettingsList.Count == 0)
                    {
                        ListAllAvailableSerialPorts();
                    }
                    else
                    {
                        SerialPortUserControl.LoadSerialPortsAccordingToProfile(this, this, _dcsBiosInheritor);
                    }
                }
                else
                {
                    ListAllAvailableSerialPorts();
                }
                SerialPortService.PortsChanged += (sender1, changedArgs) => SerialPortsHasChanged(changedArgs.SerialPorts);
                SetWindowState();
                RotateGear(2000);
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x0 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ListAllAvailableSerialPorts()
        {
            _lastSerialPortsWhenChecking = SerialPort.GetPortNames();
            SerialPortsProfileHandler.GetSerialPortsProfileHandler().ClearHiddenPorts();
            foreach (var port in _lastSerialPortsWhenChecking)
            {
                var found = false;
                foreach (var serialPortUserControl in Common.FindVisualChildren<SerialPortUserControl>(this))
                {
                    if (serialPortUserControl.Name == port)
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    AddSerialPort(port);
                }
            }
            SetWindowState();
        }

        private void OpenProfile()
        {
            if (SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty && MessageBox.Show("Discard unsaved changes to current profile?", "Discard changes?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }
            var tempDirectory = String.IsNullOrEmpty(SerialPortsProfileHandler.GetSerialPortsProfileHandler().FileName) ? SerialPortsProfileHandler.GetSerialPortsProfileHandler().MyDocumentsPath() : Path.GetFullPath(SerialPortsProfileHandler.GetSerialPortsProfileHandler().FileName);
            //_bindingProfilePZ55 = new BindingProfilePZ55(this);
            //ClearAll(false);
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory = tempDirectory;
            openFileDialog.FileName = "*.dcsbios_settings";
            openFileDialog.DefaultExt = ".dcsbios_settings";
            openFileDialog.Filter = "dcs-bios_serialport.dcsbios_settings (.dcsbios_settings)|*.dcsbios_settings";
            if (openFileDialog.ShowDialog() == true)
            {
                ClearOldSerialPorts();
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().LoadProfile(openFileDialog.FileName);
                SerialPortUserControl.LoadSerialPortsAccordingToProfile(this,this, _dcsBiosInheritor);
            }
            SetWindowState();
        }

        private void RotateGear(int howLong = 5000)
        {
            try
            {
                if (ImageDcsBiosConnected.IsEnabled)
                {
                    return;
                }
                ImageDcsBiosConnected.IsEnabled = true;
                if (_dcsStopGearTimer.Enabled)
                {
                    _dcsStopGearTimer.Stop();
                }
                _dcsStopGearTimer.Interval = howLong;
                _dcsStopGearTimer.Start();
            }
            catch (Exception)
            {
            }
        }

        private void SaveAsNewProfile()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.InitialDirectory = SerialPortsProfileHandler.GetSerialPortsProfileHandler().MyDocumentsPath();
            saveFileDialog.FileName = "*.dcsbios_settings";
            saveFileDialog.DefaultExt = ".dcsbios_settings";
            saveFileDialog.Filter = "dcs-bios_serialport.dcsbios_settings (.dcsbios_settings)|*.dcsbios_settings";
            saveFileDialog.OverwritePrompt = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().FileName = saveFileDialog.FileName;
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().SaveProfile(this);
            }
            SetWindowState();
        }

        private void SaveNewOrExistingProfile()
        {
            if (SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsNewProfile)
            {
                SaveAsNewProfile();
            }
            else
            {
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().SaveProfile(this);
            }
            SetWindowState();
        }

        private void SetWindowState()
        {
            ButtonImageSave.IsEnabled = SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty;
            MenuItemSave.IsEnabled = SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty && !SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsNewProfile;
            MenuItemSaveAs.IsEnabled = true;//SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty && !SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsNewProfile;
            MenuItemOpen.IsEnabled = true;
            ButtonImageNotepad.IsEnabled = !SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsNewProfile && !SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty;
            SetWindowTitle();
        }

        private void SetWindowTitle()
        {
            if (SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsNewProfile)
            {
                Title = WindowName;
            }
            else
            {
                Title = WindowName + SerialPortsProfileHandler.GetSerialPortsProfileHandler().FileName;
            }
            if (SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty)
            {
                Title = Title + " *";
            }
        }

        private void MenuItemOpenClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x0 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void SerialPortsHasChanged(String[] serialPorts)
        {
            var thread = new Thread(CheckComPortExistenceStatus);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public void ChangesHasBeenMade()
        {
            try
            {
                //how to make this proper?
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty = true;
                Dispatcher.Invoke(new Action(SetWindowState));
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xCA " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void CheckComPortExistenceStatus()
        {
            try
            {
                lock (_lockObject)
                {
                    //make all SerialPortUserControl check whether their SerialPort is OK
                    IEnumerable<SerialPortUserControl> list = null;
                    Application.Current.Dispatcher.Invoke(new Action(() => list = Common.FindVisualChildren<SerialPortUserControl>(this)));

                    foreach (var serialPortUserControl in list)
                    {
                        Dispatcher.Invoke(serialPortUserControl.CheckValidity);
                    }

                    var currentPorts = SerialPort.GetPortNames();
                    var portAdded = currentPorts.Count() > _lastSerialPortsWhenChecking.Count();

                    if (Common.Debug)
                    {
                        if (portAdded)
                        {
                            Common.DebugP("******** Port has been added ********");
                        }
                        else
                        {
                            Common.DebugP("******** Port has been removed ********");
                        }
                        Common.DebugP("Current ports are : ");
                        foreach (var currentPort in currentPorts)
                        {
                            Common.DebugP(currentPort);
                        }
                        Common.DebugP("Previous ports were : ");
                        foreach (var previousPort in _lastSerialPortsWhenChecking)
                        {
                            Common.DebugP(previousPort);
                        }
                    }
                    foreach (var nowExistingPort in currentPorts)
                    {
                        var found = false;
                        foreach (var thenExistingPort in _lastSerialPortsWhenChecking)
                        {
                            if (nowExistingPort.Equals(thenExistingPort))
                            {
                                found = true;
                                //do nothing, not deleted
                            }
                        }
                        if (!found && portAdded)
                        {
                            Common.DebugP("Creating new SerialPort Handler for the new port " + nowExistingPort);
                            //it is a new port
                            var port = nowExistingPort;
                            WrapPanelMain.Dispatcher.Invoke((() => AddSerialPort(port)));
                        }
                    }

                    _lastSerialPortsWhenChecking = SerialPort.GetPortNames();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xD1 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void MenuItemSave_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().SaveProfile(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x1 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void MenuItemSaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAsNewProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x2 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void MenuItemExit_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x3 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void MenuItemOptions_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var options = new OptionsWindow();
                options.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                options.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x4 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var about = new AboutWindow();
                about.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x5 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void AddSerialPort(String serialPortName)
        {
            try
            {
                var serialPortUserControl = new SerialPortUserControl(serialPortName, this, _dcsBiosInheritor);
                WrapPanelMain.Children.Add(serialPortUserControl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xE5 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ButtonNew_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SerialPortsProfileHandler.GetSerialPortsProfileHandler().IsDirty && MessageBox.Show("Discard unsaved changes to current profile?", "Discard changes?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().Reset();
                ListAllAvailableSerialPorts();
                SetWindowState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x6 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveNewOrExistingProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x7 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ButtonButtonSearchForSerialPorts_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ListAllAvailableSerialPorts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xB " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ButtonOpen_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenProfile();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x8 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        
        private void ButtonOpenInEditor_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(SerialPortsProfileHandler.GetSerialPortsProfileHandler().FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xA " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            try
            {
                SerialPortService.CleanUp();
                _dcsBiosInheritor.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xA " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ClearOldSerialPorts()
        {
            try
            {
                do
                {
                    ((SerialPortUserControl)WrapPanelMain.Children[0]).RemoveVisualComponentAndClose();
                } while (WrapPanelMain.Children.Count > 0);
                SetWindowState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xA1 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void TimerStopRotation(object sender, ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.BeginInvoke((Action)(() => ImageDcsBiosConnected.IsEnabled = false));
                _dcsStopGearTimer.Stop();
            }
            catch (Exception)
            {
            }
        }

        public void DcsBiosDataReceived(uint address, uint data)
        {
            try
            {
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x91" + ex.Message);
            }
        }

        public void DcsBiosDataReceived(byte[] array)
        {
            try
            {
                //todo här skall rotorn snurra, vad mera? byte räknarna skall uppdateras.

                Dispatcher.BeginInvoke((Action)(() => RotateGear()));
                //if (address == 0x14be )
                //{
                //todo Dispatcher.BeginInvoke((Action)(() => _bindingProfilePZ55.CheckForColorChanges(_switchPanelPZ55, address, data)));
                /*var value = (data & 0x4000) >> 14;
                if (value == 1)
                {
                    Dispatcher.BeginInvoke((Action)(() => _switchPanelPZ55.SetLandingGearLED(SwitchPanelPZ55LEDPosition.UP, SwitchPanelPZ55LEDColor.GREEN)));
                }
                else
                {
                    Dispatcher.BeginInvoke((Action)(() => _switchPanelPZ55.SetLandingGearLED(SwitchPanelPZ55LEDPosition.UP, SwitchPanelPZ55LEDColor.DARK)));
                }
                Dispatcher.BeginInvoke((Action)(() => textBoxLog.Text = textBoxLog.Text.Insert(0, "DcsBios FIRETEST BUTTON!!! ---> " + value + Environment.NewLine)));
            }/*

            /*
            for(int i = 0; i < data.Length; i++)
            { 
                var b = data[i];
                //Output Type: integer Address: 0x14be Mask: 0x4000 Shift By: 14 Max. Value: 1 Description: selector position
                if (b == 0x14be)
                {
                    var fireTestBtnValue = (data[0] & 0x4000) >> 14;
                    Dispatcher.BeginInvoke((Action)(() => textBoxLog.Text = textBoxLog.Text.Insert(0, "DcsBios : " + data.Length + Environment.NewLine)));
                }   
            }*/
                //Dispatcher.BeginInvoke((Action)(() => textBoxLog.Text = textBoxLog.Text.Insert(0, "DcsBios : " + data.Length + Environment.NewLine))); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x91" + ex.Message);
            }
        }

        public void GetDcsBiosData(byte[] bytes)
        {
            //todo
            return;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var serialPortUserControl in Common.FindVisualChildren<SerialPortUserControl>(this))
            {
                if (serialPortUserControl.DCSSerialPort.IsOpen)
                {
                    Common.DebugP("SerialPort " + serialPortUserControl.DCSSerialPort.ComPort + " is open");
                }
                if (!serialPortUserControl.DCSSerialPort.IsOpen)
                {
                    Common.DebugP("SerialPort " + serialPortUserControl.DCSSerialPort.ComPort + " is closed");
                }
            }
        }
    
    }
}
