using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DcsBiosCOMHandler
{
    /// <summary>
    /// Interaction logic for SerialPortConfigWindow.xaml
    /// </summary>
    public partial class SerialPortConfigWindow : Window
    {
        private DcsSerialPortSetting _dcsSerialPortSetting = new DcsSerialPortSetting();

        public SerialPortConfigWindow(String comPort)
        {
            InitializeComponent();
            _dcsSerialPortSetting.ComPort = comPort;
        }


        public SerialPortConfigWindow(DcsSerialPortSetting dcsSerialPortSetting)
        {
            InitializeComponent();
            _dcsSerialPortSetting = dcsSerialPortSetting;
        }


        private void SerialPortConfigWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            //PopulateCombos();
            ShowValues();
        }
        
        private void ShowValues()
        {
            LabelSerialPortName.Content = _dcsSerialPortSetting.ComPort;
            ComboBoxBaud.SelectedValue = _dcsSerialPortSetting.BaudRate;
            ComboBoxParity.SelectedValue = _dcsSerialPortSetting.Parity;
            ComboBoxStopBits.SelectedValue = _dcsSerialPortSetting.Stopbits;
            ComboBoxDataBits.SelectedValue = _dcsSerialPortSetting.Databits;
            CheckBoxLineSignalRts.IsChecked = _dcsSerialPortSetting.LineSignalRts;
            CheckBoxLineSignalDtr.IsChecked = _dcsSerialPortSetting.LineSignalDtr;
            ComboBoxWriteTimeout.SelectedValue = _dcsSerialPortSetting.WriteTimeout;
            ComboBoxReadTimeout.SelectedValue = _dcsSerialPortSetting.ReadTimeout;

        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _dcsSerialPortSetting.BaudRate = int.Parse(ComboBoxBaud.SelectedValue.ToString());
                _dcsSerialPortSetting.Parity = (Parity)Enum.Parse(typeof(Parity), ComboBoxParity.SelectedValue.ToString());
                _dcsSerialPortSetting.Stopbits = (StopBits)Enum.Parse(typeof(StopBits), ComboBoxStopBits.SelectedValue.ToString());
                _dcsSerialPortSetting.Databits = int.Parse(ComboBoxDataBits.SelectedValue.ToString());
                _dcsSerialPortSetting.LineSignalRts = CheckBoxLineSignalRts.IsChecked.GetValueOrDefault();
                _dcsSerialPortSetting.LineSignalDtr = CheckBoxLineSignalDtr.IsChecked.GetValueOrDefault();
                _dcsSerialPortSetting.WriteTimeout = int.Parse(ComboBoxWriteTimeout.SelectedValue.ToString());
                _dcsSerialPortSetting.ReadTimeout = int.Parse(ComboBoxReadTimeout.SelectedValue.ToString());
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xA3 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xA4 " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public DcsSerialPortSetting DCSSerialPortSetting
        {
            get { return _dcsSerialPortSetting; }
            set { _dcsSerialPortSetting = value; }
        }

    }
}
