using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using DcsBiosCOMHandler.Properties;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Forms;

namespace DcsBiosCOMHandler
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        private String _ipAddressFrom;
        private String _portFrom;
        private String _ipAddressTo;
        private String _portTo;
        private String _dcsBiosLocation;

        public OptionsWindow()
        {
            InitializeComponent();
            if (!String.IsNullOrEmpty(Settings.Default.DCSAddressFrom))
            {
                TextBoxIPFrom.Text = Settings.Default.DCSAddressFrom;
            }
            if (Settings.Default.DCSPortFrom != 0)
            {
                TextBoxPortFrom.Text = Settings.Default.DCSPortFrom.ToString(CultureInfo.InvariantCulture);
            }
            if (Settings.Default.DCSPortTo != 0)
            {
                TextBoxPortTo.Text = Settings.Default.DCSPortTo.ToString(CultureInfo.InvariantCulture);
            }
            if (!String.IsNullOrEmpty(Settings.Default.DCSAddressTo))
            {
                TextBoxIPTo.Text = Settings.Default.DCSAddressTo;
            }
            if (!String.IsNullOrEmpty(Settings.Default.DCSBiosLocation))
            {
                TextBoxDcsBiosLocation.Text = Settings.Default.DCSBiosLocation;
            }
        }

        public OptionsWindow(String addressFrom, String portFrom, String addressTo, String portTo, String dcsBiosLocation)
        {
            InitializeComponent();
            TextBoxIPFrom.Text = addressFrom;
            TextBoxPortFrom.Text = portFrom;
            TextBoxIPTo.Text = addressTo;
            TextBoxPortTo.Text = portTo;
            if (String.IsNullOrEmpty(Settings.Default.DCSBiosLocation))
            {
                TextBoxDcsBiosLocation.Text = @"%USERPROFILE%\Saved Games\DCS\Scripts\DCS-BIOS";
            }
            else
            {
                TextBoxDcsBiosLocation.Text = Settings.Default.DCSBiosLocation;
            }
        }
        
        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckValues();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x111D" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x211D" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }


        private void CheckValues()
        {
            try
            {
                IPAddress ipAddress;
                if (String.IsNullOrEmpty(TextBoxIPFrom.Text))
                {
                    throw new Exception("IP address from cannot be empty");
                }
                if (String.IsNullOrEmpty(TextBoxIPTo.Text))
                {
                    throw new Exception("IP address to cannot be empty");
                }
                if (String.IsNullOrEmpty(TextBoxPortFrom.Text))
                {
                    throw new Exception("Port from cannot be empty");
                }
                if (String.IsNullOrEmpty(TextBoxPortTo.Text))
                {
                    throw new Exception("Port to cannot be empty");
                }
                if (String.IsNullOrEmpty(TextBoxDcsBiosLocation.Text))
                {
                    throw new Exception("DCS-BIOS directory cannot be empty");
                }
                try
                {
                    if (!IPAddress.TryParse(TextBoxIPFrom.Text, out ipAddress))
                    {
                        throw new Exception();
                    }
                    _ipAddressFrom = TextBoxIPFrom.Text;
                    Settings.Default.DCSAddressFrom = _ipAddressFrom;
                }
                catch (Exception e)
                {
                    throw new Exception("Error while checking IP from : " + e.Message);
                }
                try
                {
                    if (!IPAddress.TryParse(TextBoxIPTo.Text, out ipAddress))
                    {
                        throw new Exception();
                    }
                    _ipAddressTo = TextBoxIPTo.Text;
                    Settings.Default.DCSAddressTo= _ipAddressTo;
                }
                catch (Exception e)
                {
                    throw new Exception("Error while checking IP to : " + e.Message);
                }
                try
                {
                    var test = Convert.ToInt32(TextBoxPortFrom.Text);
                    _portFrom = TextBoxPortFrom.Text;
                    Settings.Default.DCSPortFrom = int.Parse(_portFrom);
                }
                catch (Exception e)
                {
                    throw new Exception("Error while Port from : " + e.Message);
                }
                try
                {
                    var test = Convert.ToInt32(TextBoxPortTo.Text);
                    _portTo = TextBoxPortTo.Text;
                    Settings.Default.DCSPortTo = int.Parse(_portTo);
                }
                catch (Exception e)
                {
                    throw new Exception("Error while Port to : " + e.Message);
                }
                try
                {
                    var directoryInfo = new DirectoryInfo(TextBoxDcsBiosLocation.Text);
                    _dcsBiosLocation = TextBoxDcsBiosLocation.Text;
                    Settings.Default.DCSBiosLocation = _dcsBiosLocation;
                }
                catch (Exception e)
                {
                    throw new Exception("Error while checking DCS-BIOS location : " + e.Message);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error checking values : " + Environment.NewLine + e.Message);
            }
            Settings.Default.Save();
        }

        public string IPAddressFrom
        {
            get { return _ipAddressFrom; }
        }

        public string PortFrom
        {
            get { return _portFrom; }
        }

        public string IPAddressTo
        {
            get { return _ipAddressTo; }
        }

        public string PortTo
        {
            get { return _portTo; }
        }

        public string DCSBiosLocation
        {
            get { return _dcsBiosLocation; }
        }

        private void ButtonBrowse_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.ShowNewFolderButton = false;
                if (!String.IsNullOrEmpty(Settings.Default.DCSBiosLocation))
                {
                    folderBrowserDialog.SelectedPath = Settings.Default.DCSBiosLocation;
                }
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TextBoxDcsBiosLocation.Text = folderBrowserDialog.SelectedPath;
                    Settings.Default.DCSBiosLocation = folderBrowserDialog.SelectedPath;
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("0x311D" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}
