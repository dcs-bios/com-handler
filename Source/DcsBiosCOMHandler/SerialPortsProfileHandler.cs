using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using DcsBiosCOMHandler.Properties;

namespace DcsBiosCOMHandler
{

    public class SerialPortsProfileHandler
    {
        private List<DcsSerialPortSetting> _dcsSerialPortsStringSettingsList = new List<DcsSerialPortSetting>();
        private Object _lockObject = new object();
        private List<String> _listOfSerialPortsToHide = new List<string>();
        private String _directory = "%USERPROFILE%\\Documents";
        private String _fileName = Path.GetFullPath((Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))) + "\\" + "dcs-bios_serialport.dcsbios_settings";
        private readonly String _newFileName = Path.GetFullPath((Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))) + "\\" + "dcs-bios_serialport.dcsbios_settings";
        private bool _isNewProfile;
        private bool _isDirty;
        private static SerialPortsProfileHandler _serialPortsProfileHandlerSO;

        public static SerialPortsProfileHandler GetSerialPortsProfileHandler()
        {
            if (_serialPortsProfileHandlerSO == null)
            {
                _serialPortsProfileHandlerSO = new SerialPortsProfileHandler();
            }
            return _serialPortsProfileHandlerSO;
        }

        public void AddSerialPortToHide(String portName)
        {
            lock (_lockObject)
            {

                var found = false;
                foreach (var str in _listOfSerialPortsToHide)
                {
                    if (str.Equals(portName))
                    {
                        //Already there
                        found = true;
                    }
                }
                if (!found)
                {
                    Common.DebugP("Adding port " + portName + " to list of ports to hide.");
                    _listOfSerialPortsToHide.Add(portName);
                }
            }
        }

        public void Reset()
        {
            _listOfSerialPortsToHide.Clear();
            _dcsSerialPortsStringSettingsList.Clear();
            _isDirty = true;
            _isNewProfile = true;
        }

        
        public void ClearHiddenPorts()
        {
            //User has chosen to see all ports.
            _listOfSerialPortsToHide.Clear();
            _isDirty = true;
        }

        public String DefaultFile()
        {
            return Path.GetFullPath((Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))) + "\\" + _fileName;
        }

        public String MyDocumentsPath()
        {
            return Path.GetFullPath((Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
        }

        public bool LoadProfile(String filename)
        {
            try
            {
                if (!String.IsNullOrEmpty(FileName) && !File.Exists(_fileName))
                {
                    return false;
                }

                if (!String.IsNullOrEmpty(filename))
                {
                    _fileName = filename;
                }
                
                /*
                 * 0 Open specified filename (parameter) if not null
                 * 1 If exists open last profile used (settings)
                 * 3 If none found do nothing
                 */
                _isNewProfile = false;
                _listOfSerialPortsToHide.Clear();
                _dcsSerialPortsStringSettingsList.Clear();
                if (String.IsNullOrEmpty(filename))
                {
                    if (!String.IsNullOrEmpty(Settings.Default.LastProfileFileUsed) && File.Exists(Settings.Default.LastProfileFileUsed))
                    {
                        _fileName = Settings.Default.LastProfileFileUsed;
                    }
                }

                if (String.IsNullOrEmpty(_fileName))
                {
                    return false;
                }

                Settings.Default.LastProfileFileUsed = _fileName;
                Settings.Default.Save();
                var fileLines = File.ReadAllLines(_fileName);
                foreach (var fileLine in fileLines)
                {
                    if (!fileLine.StartsWith("#") && fileLine.Length > 2)
                    {
                        if (fileLine.StartsWith("DcsSerialPort{"))
                        {
                            _dcsSerialPortsStringSettingsList.Add(DcsSerialPortSetting.ParseSetting(fileLine));
                        }
                        else if (fileLine.StartsWith("HiddenList{"))
                        {
                            //HiddenList{COM1|COM3|COM4}
                            var str = fileLine.Substring(fileLine.IndexOf("{", StringComparison.InvariantCulture) + 1);
                            //COM1|COM3|COM4}
                            str = str.Replace("}", "");
                            //COM1|COM3|COM4
                            var list = str.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var s in list)
                            {
                                _listOfSerialPortsToHide.Add(s);   
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xC1"  + ex.Message + Environment.NewLine + ex.StackTrace);
                return false;
            }
        }


        public void SaveProfile(MainWindow mainWindow)
        {
            try
            {
                SaveProfileAs(mainWindow, _fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xC2"  + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public void SaveProfileAs(MainWindow mainWindow, String filename)
        {
            try
            {
                if (String.IsNullOrEmpty(filename))
                {
                    return;
                }
                Settings.Default.LastProfileFileUsed = filename;

                var header = "#This file can be manually edited using any ASCII editor.\n#File created on " + DateTime.Today + " " + DateTime.Now;

                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(header);
                stringBuilder.AppendLine("#--------------------------------------------------------------------");

                foreach (var serialPortUserControl in Common.FindVisualChildren<SerialPortUserControl>(mainWindow))
                {
                    if (serialPortUserControl.DCSSerialPort != null)
                    {
                        stringBuilder.AppendLine(serialPortUserControl.DCSSerialPort.GetExportString());
                    }
                }
                if (_listOfSerialPortsToHide.Count > 0)
                {
                    stringBuilder.Append("HiddenList{");
                    foreach (var dcsSerialPort in _listOfSerialPortsToHide)
                    {
                        stringBuilder.Append(dcsSerialPort + "|");
                    }
                    if (stringBuilder.ToString().EndsWith("|"))
                    {
                        stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    }
                    stringBuilder.Append("}");
                }
                File.WriteAllText(filename, stringBuilder.ToString(), Encoding.ASCII);
                _isDirty = false;
                _isNewProfile = false;

                LoadProfile(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("0xC2" + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public List<string> ListOfSerialPortsToHide
        {
            get { return _listOfSerialPortsToHide; }
            set { _listOfSerialPortsToHide = value; }
        }

        public List<DcsSerialPortSetting> DCSSerialPortsStringSettingsList
        {
            get { return _dcsSerialPortsStringSettingsList; }
            set { _dcsSerialPortsStringSettingsList = value; }
        }

        private void ImportHiddenList(String str)
        {
            
        }

        public bool ShouldBeHidden(string comPort)
        {
            foreach (var str in ListOfSerialPortsToHide)
            {
                if (comPort.Equals(str))
                {
                    return true;
                }
            }
            return false;
        }

        public string Directory
        {
            get { return _directory; }
            set { _directory = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public bool IsNewProfile
        {
            get { return _isNewProfile; }
            set { _isNewProfile = value; }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            set { _isDirty = value; }
        }
    }
}
