using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DCS_BIOS;

namespace DcsBiosCOMHandler
{
    /// <summary>
    /// Interaction logic for SerialPortUserControl.xaml
    /// </summary>
    public partial class SerialPortUserControl : ISerialPortConnectionEventListener, ISerialPortListener, IDcsBiosDataListener
    {
        private DcsSerialPort _dcsSerialPort;
        private DcsBiosInheritor _dcsBiosInheritor;
        private IUIListener _interfaceUIListener = null ;
        //private DcsSerialPortSetting _dcsSerialPortSetting;
        private int _bytesFromDcs = 0;
        private int _bytesFromSerialPort = 0;

        public SerialPortUserControl(String portName, IUIListener interfaceUIListener, DcsBiosInheritor dcsBiosInheritor)
        {
            InitializeComponent();
            Common.DebugP("SerialPortUserControl created with port name " + portName);
            LabelPort.Content = "SerialPort : " + portName;
            Name = portName;
            _dcsBiosInheritor = dcsBiosInheritor;
            _dcsSerialPort = new DcsSerialPort(portName, this);
            _dcsSerialPort.AttachDataReceivedListener(dcsBiosInheritor);
            _dcsSerialPort.AttachDataReceivedListener(this);
            dcsBiosInheritor.AttachDataReceivedListener(_dcsSerialPort);
            dcsBiosInheritor.AttachDataReceivedListener(this);
            _interfaceUIListener = interfaceUIListener;
            ContextMenu = (ContextMenu)Resources["SerialPortContextMenu"];
            ContextMenu.Tag = Name;
        }

        public SerialPortUserControl(DcsSerialPortSetting dcsSerialPortSetting, IUIListener interfaceUIListener, DcsBiosInheritor dcsBiosInheritor)
        {
            InitializeComponent();
            Common.DebugP("SerialPortUserControl created with dcsSerialPortSetting.ComPort name " + dcsSerialPortSetting.ComPort);
            LabelPort.Content = "SerialPort : " + dcsSerialPortSetting.ComPort;
            Name = dcsSerialPortSetting.ComPort;
            _dcsBiosInheritor = dcsBiosInheritor;
            _dcsSerialPort = new DcsSerialPort(dcsSerialPortSetting, this);
            _dcsSerialPort.AttachDataReceivedListener(dcsBiosInheritor);
            _dcsSerialPort.AttachDataReceivedListener(this);
            dcsBiosInheritor.AttachDataReceivedListener(_dcsSerialPort);
            dcsBiosInheritor.AttachDataReceivedListener(this);
            _interfaceUIListener = interfaceUIListener;
            ContextMenu = (ContextMenu)Resources["SerialPortContextMenu"];
            ContextMenu.Tag = Name;
        }

        private void SerialPortUserControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SetWindowState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xF6 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void SetWindowState()
        {
            //IsChecked = Port öppen
            ButtonConnection.IsChecked = _dcsSerialPort != null && _dcsSerialPort.IsOpen;
        }

        public void GetDcsBiosData(byte[] bytes)
        {
            var bytesWritten = _dcsSerialPort.Write(bytes);
            Dispatcher.BeginInvoke((Action)(() => IncreaseByteCounterFromDcs(bytesWritten)));
        }

        public void GetSerialPortData(String serialPortData)
        {
            var bytesWritten = _dcsBiosInheritor.SendDataFunction(serialPortData);
            Dispatcher.BeginInvoke((Action)(() => IncreaseByteCounterFromSerialPort(bytesWritten)));
        }
        
        public void DcsBiosDataReceived(uint address, uint data)
        {
        }

        public void DcsBiosDataReceived(byte[] array)
        {
        }

        private void IncreaseByteCounterFromDcs(int count)
        {
            _bytesFromDcs = _bytesFromDcs + count;
            LabelFromDCSBytes.Content = _bytesFromDcs.ToString(CultureInfo.InvariantCulture) + " bytes";
        }

        private void IncreaseByteCounterFromSerialPort(int count)
        {
            _bytesFromSerialPort = _bytesFromDcs + count;
            LabelFromSerialPortBytes.Content = _bytesFromSerialPort.ToString(CultureInfo.InvariantCulture) + " bytes";
        }

        public void CheckValidity()
        {
            try
            {
                //Check that COM port which _dcsSerial has does exist, what more?
                var ports = SerialPort.GetPortNames();
                var found = false;
                foreach (var port in ports)
                {
                    if (port.Equals(Name))
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    RemoveVisualComponentAndClose();
                }
                SetWindowState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xC2 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ButtonConnection_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ButtonConnection.IsChecked.HasValue && ButtonConnection.IsChecked.Value)
                {
                    _dcsSerialPort.Open();
                }
                else if (ButtonConnection.IsChecked.HasValue && !ButtonConnection.IsChecked.Value)
                {
                    _dcsSerialPort.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x2 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
            //If error occurs set state 
            ButtonConnection.IsChecked = _dcsSerialPort.IsOpen;
        }

        private void ButtonRemoveSerialPort_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SerialPortsProfileHandler.GetSerialPortsProfileHandler().AddSerialPortToHide(Name);
                RemoveVisualComponentAndClose();
                _interfaceUIListener.ChangesHasBeenMade();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x3 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public void RemoveVisualComponentAndClose()
        {
            _dcsSerialPort.Close();
            if (Parent != null)
            {
                ((Panel) Parent).Children.Remove(this);
            }
        }

        private void MenuItemConfigureSerialPort_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var serialPortConfigWindow = new SerialPortConfigWindow(_dcsSerialPort.DCSSerialPortSetting);
                serialPortConfigWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (serialPortConfigWindow.ShowDialog() == true)
                {
                    _dcsSerialPort.DCSSerialPortSetting = serialPortConfigWindow.DCSSerialPortSetting;
                    _dcsSerialPort.ApplyPortConfig();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x4 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public static void LoadSerialPortsAccordingToProfile(MainWindow mainWindow, IUIListener interfaceUIListener, DcsBiosInheritor dcsBiosInheritor)
        {
            try
            {
                foreach (var dcsSerialPortSetting in SerialPortsProfileHandler.GetSerialPortsProfileHandler().DCSSerialPortsStringSettingsList)
                {
                    if (Common.SerialPortCurrentlyExists(dcsSerialPortSetting.ComPort))
                    {
                        //OK create it.
                        var serialPortUserControl = new SerialPortUserControl( dcsSerialPortSetting, interfaceUIListener, dcsBiosInheritor);
                        mainWindow.WrapPanelMain.Children.Add(serialPortUserControl);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xB4 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        
        public DcsSerialPort DCSSerialPort
        {
            get { return _dcsSerialPort; }
            set { _dcsSerialPort = value; }
        }

        public void SerialPortClosed()
        {
            try
            {
                SetWindowState();
                ButtonConnection.IsChecked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xB5 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public void SerialPortOpened()
        {
            try
            {
                SetWindowState();
                ButtonConnection.IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xB6 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }


        private void SerialPortContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu = (ContextMenu)sender;
                if (_dcsSerialPort != null && _dcsSerialPort.IsOpen)
                {
                    ((MenuItem) menu.Items[0]).IsEnabled = false;
                }
                else
                {
                    ((MenuItem)menu.Items[0]).IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xB6 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}
