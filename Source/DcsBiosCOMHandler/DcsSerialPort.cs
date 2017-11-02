using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows.Media.Animation;
using DCS_BIOS;

namespace DcsBiosCOMHandler
{

    public class DcsSerialPort : IDcsBiosDataListener
    {
        public delegate void DataReceivedOverSerialPortEventHandler(String stringData);
        public event DataReceivedOverSerialPortEventHandler DataReceivedOverSerialPort;

        //mode COM%COMPORT% BAUD=500000 PARITY=N DATA=8 STOP=1 TO=off DTR=on
        private SerialPort _serialPort;
        //private Thread _readThread;
        private bool _enabled;

        private bool _valid;
        private bool _changesHasBeenMade;
        private object _lockExceptionObject = new object();
        private Exception _lastException;
        private long _communicating;
        private long _waitForData;
        private ISerialPortConnectionEventListener _listener;
        private DcsSerialPortSetting _dcsSerialPortSetting = new DcsSerialPortSetting();

        public DcsSerialPort(String comPort, ISerialPortConnectionEventListener interfaceSerialPortConnectionEventListener)
        {
            _dcsSerialPortSetting.ComPort = comPort;
            _serialPort = new SerialPort();
            ApplyPortConfig();
            _serialPort.DataReceived += ReceiveSerialData;
            _listener = interfaceSerialPortConnectionEventListener;
        }

        public DcsSerialPort(DcsSerialPortSetting dcsSerialPortSetting, ISerialPortConnectionEventListener interfaceSerialPortConnectionEventListener)
        {
            _dcsSerialPortSetting = dcsSerialPortSetting;
            _serialPort = new SerialPort();
            _serialPort.DataReceived += ReceiveSerialData;
            ApplyPortConfig();
            _listener = interfaceSerialPortConnectionEventListener;
            if (dcsSerialPortSetting.Connected)
            {
                _serialPort.Open();
                _listener.SerialPortOpened();
            }
        }
        
        ~DcsSerialPort()
        {
            try
            {
                Close();
            }
            catch (Exception)
            {
            }
        }

        public void Open()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                Common.DebugP("DcsSerialPort " + _dcsSerialPortSetting.ComPort + " is already open");
                return;
            }
            Common.DebugP("DcsSerialPort is opening " + _dcsSerialPortSetting.ComPort);
            if (_changesHasBeenMade)
            {
                ApplyPortConfig();
                _changesHasBeenMade = false;
            }
            _serialPort.Open();
            _listener.SerialPortOpened();
            //_readThread = new Thread(new ThreadStart(Read));
        }

        public bool IsOpen
        {
            get { return _serialPort != null && _serialPort.IsOpen; }
        }

        public String GetExportString()
        {
            var connectedStatus = "";
            if (_serialPort.IsOpen)
            {
                connectedStatus = "Open";
            }
            else
            {
                connectedStatus = "Closed";
            }
            var result = new StringBuilder();
            result.Append("DcsSerialPort{" + _dcsSerialPortSetting.ComPort + "|" + _dcsSerialPortSetting.BaudRate + "|" + _dcsSerialPortSetting.Handshake + "|" + _dcsSerialPortSetting.Databits + "|" + _dcsSerialPortSetting.Stopbits + "|" + _dcsSerialPortSetting.Parity + "|" + _dcsSerialPortSetting.WriteTimeout + "|" + _dcsSerialPortSetting.ReadTimeout + "|" + _dcsSerialPortSetting.LineSignalDtr + "|" + _dcsSerialPortSetting.LineSignalRts + "|" + connectedStatus + "}");
            Common.DebugP("Exporting : " + result);
            return result.ToString();
        }

        public void Close()
        {
            try
            {
                if (_serialPort != null && !_serialPort.IsOpen)
                {
                    Common.DebugP("DcsSerialPort " + _dcsSerialPortSetting.ComPort + " is already closed");
                    return;
                }
                Common.DebugP("DcsSerialPort is closing " + _dcsSerialPortSetting.ComPort);
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _listener.SerialPortClosed();
                }
            }
            catch (Exception e)
            {
                SetLastException("Close() ", e);
            }
        }

        private void ReceiveSerialData(object sender, SerialDataReceivedEventArgs e)
        {
            var incomingData = new StringBuilder();
            Interlocked.Exchange(ref _communicating, 1);
            Interlocked.Exchange(ref _waitForData, 1);
            switch (e.EventType)
            {
                case SerialData.Chars:
                    {
                        while (Interlocked.Read(ref _waitForData) == 1)
                        {
                            incomingData.Append(_serialPort.ReadChar());
                        }
                        break;
                    }
                case SerialData.Eof:
                    {
                        break;
                    }
            }
            if (DataReceivedOverSerialPort != null)
            {
                DataReceivedOverSerialPort(incomingData.ToString());
            }
            Interlocked.Exchange(ref _waitForData, 0);
            Interlocked.Exchange(ref _communicating, 0);
        }

        public void AttachDataReceivedListener(ISerialPortListener iSerialPortListener)
        {
            DataReceivedOverSerialPort += iSerialPortListener.GetSerialPortData;
        }

        public void DetachDataReceivedListener(ISerialPortListener iSerialPortListener)
        {
            DataReceivedOverSerialPort -= iSerialPortListener.GetSerialPortData;
        }

        public void GetDcsBiosData(byte[] bytes)
        {
            //todo
            //user control handles this. OK design?
            //Common.DebugP("DATA BEING RECEIVED 1");
        }
        
        public void DcsBiosDataReceived(uint address, uint data)
        {
            //todo
        }

        public void DcsBiosDataReceived(byte[] array)
        {
            //todo
        }
        
        public void ApplyPortConfig()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                return;
            }
            if (_serialPort != null)
            {
                _serialPort.PortName = _dcsSerialPortSetting.ComPort;
                _serialPort.BaudRate = _dcsSerialPortSetting.BaudRate;
                _serialPort.Parity = _dcsSerialPortSetting.Parity;
                _serialPort.StopBits = _dcsSerialPortSetting.Stopbits;
                _serialPort.DataBits = _dcsSerialPortSetting.Databits;
                if (!_dcsSerialPortSetting.LineSignalDtr && !_dcsSerialPortSetting.LineSignalRts)
                {
                    _serialPort.Handshake = Handshake.XOnXOff;
                }
                _serialPort.DtrEnable = _dcsSerialPortSetting.LineSignalDtr;
                _serialPort.RtsEnable = _dcsSerialPortSetting.LineSignalRts;
                if (_dcsSerialPortSetting.WriteTimeout == 0)
                {
                    _serialPort.WriteTimeout = SerialPort.InfiniteTimeout;
                }
                else
                {
                    _serialPort.WriteTimeout = _dcsSerialPortSetting.WriteTimeout;
                }
                if (_dcsSerialPortSetting.ReadTimeout == 0)
                {
                    _serialPort.ReadTimeout = SerialPort.InfiniteTimeout;
                }
                else
                {
                    _serialPort.ReadTimeout = _dcsSerialPortSetting.ReadTimeout;
                }
            }
        }
        
        public int Write(byte[] array)
        {
            try
            {
                if (array.Length == 0 || _serialPort == null || !_serialPort.IsOpen)
                {
                    return 0;
                }
                _serialPort.Write(array, 0, array.Length);
            }
            catch (TimeoutException e)
            {
                SetLastException("SerialPort.Write", e);
            }
            return array.Length;
        }

        public void SetLastException(String id, Exception ex)
        {
            try
            {
                if (ex == null)
                {
                    return;
                }
                lock (_lockExceptionObject)
                {
                    _lastException = new Exception(id + "  " + Environment.NewLine + ex.GetType() + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                }
            }
            catch (Exception)
            {
            }
        }

        public Exception GetLastException(bool resetException = false)
        {
            Exception result;
            lock (_lockExceptionObject)
            {
                result = _lastException;
                if (resetException)
                {
                    _lastException = null;
                }
            }
            return result;
        }

        public DcsSerialPortSetting DCSSerialPortSetting
        {
            get { return _dcsSerialPortSetting; }
            set { _dcsSerialPortSetting = value; }
        }

        public Handshake Handshake
        {
            get { return _dcsSerialPortSetting.Handshake; }
            set
            {
                _dcsSerialPortSetting.Handshake = value;
                _changesHasBeenMade = true;
            }
        }

        public string ComPort
        {
            get { return _dcsSerialPortSetting.ComPort; }
            set
            {
                _dcsSerialPortSetting.ComPort = value;
                _changesHasBeenMade = true;
            }
        }

        public int BaudRate
        {
            get { return _dcsSerialPortSetting.BaudRate; }
            set
            {
                _dcsSerialPortSetting.BaudRate = value;
                _changesHasBeenMade = true;
            }
        }

        public int Databits
        {
            get { return _dcsSerialPortSetting.Databits; }
            set
            {
                _dcsSerialPortSetting.Databits = value;
                _changesHasBeenMade = true;
            }
        }

        public StopBits Stopbits
        {
            get { return _dcsSerialPortSetting.Stopbits; }
            set
            {
                _dcsSerialPortSetting.Stopbits = value;
                _changesHasBeenMade = true;
            }
        }

        public Parity Parity
        {
            get { return _dcsSerialPortSetting.Parity; }
            set
            {
                _dcsSerialPortSetting.Parity = value;
                _changesHasBeenMade = true;
            }
        }

        public int WriteTimeout
        {
            get { return _dcsSerialPortSetting.WriteTimeout; }
            set
            {
                _dcsSerialPortSetting.WriteTimeout = value;
                _changesHasBeenMade = true;
            }
        }

        public int ReadTimeout
        {
            get { return _dcsSerialPortSetting.ReadTimeout; }
            set
            {
                _dcsSerialPortSetting.ReadTimeout = value;
                _changesHasBeenMade = true;
            }
        }

        public bool LineSignalDtr
        {
            get { return _dcsSerialPortSetting.LineSignalDtr; }
            set
            {
                _dcsSerialPortSetting.LineSignalDtr = value;
                _changesHasBeenMade = true;
            }
        }

        public bool LineSignalRts
        {
            get { return _dcsSerialPortSetting.LineSignalRts; }
            set
            {
                _dcsSerialPortSetting.LineSignalRts = value;
                _changesHasBeenMade = true;
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public bool IsValid
        {
            get { return _valid && _serialPort != null && _serialPort.IsOpen; }
            set { _valid = value; }
        }
    }


    public class DcsSerialPortSetting
    {
        private String _comPort;
        private int _baudRate = 460800;
        private Handshake _handshake; //if not DTR && RTS then XOn XOff
        private int _databits = 8;
        private StopBits _stopbits = StopBits.One;
        private Parity _parity = Parity.None;
        private int _writeTimeout = 0;
        private int _readTimeout = 0;
        private bool _lineSignalDtr = true;
        private bool _lineSignalRts = false;
        private bool _connected = false;

        public static DcsSerialPortSetting ParseSetting(String portSetting)
        {
            //DcsSerialPort{_comPort|_baudRate|_handshake|_databits|_stopbits|_parity|_writeTimeout|_readTimeout|_lineSignalDtr|LineSignalRts|connectedStatus}
            //DcsSerialPort{COM1|500000|None|8|One|None|40000|40000|True|False|Closed}
            if (String.IsNullOrEmpty(portSetting))
            {
                return null;
            }
            //COM1|500000|None|8|One|None|40000|40000|True|False|Closed}
            var str = portSetting.Substring(portSetting.IndexOf("{", StringComparison.InvariantCulture) + 1);
            str = str.Replace("}", "");
            //COM1|500000|None|8|One|None|40000|40000|True|False|Closed}
            var list = str.Split(new[] {"|"}, StringSplitOptions.RemoveEmptyEntries);
            var result = new DcsSerialPortSetting();
            result.ComPort = list[0];
            result.BaudRate = int.Parse(list[1]);
            result.Handshake = (Handshake) Enum.Parse(typeof(Handshake), list[2]);
            result.Databits = int.Parse(list[3]);
            result.Stopbits = (StopBits) Enum.Parse(typeof(StopBits), list[4]);
            result.Parity = (Parity) Enum.Parse(typeof(Parity), list[5]);
            result.WriteTimeout = int.Parse(list[6]);
            result.ReadTimeout = int.Parse(list[7]);
            result.LineSignalDtr = bool.Parse(list[8]);
            result.LineSignalRts = bool.Parse(list[9]);
            result.LineSignalRts = bool.Parse(list[9]);
            result.Connected = list[10].Equals("Open");
            return result;
        }

        public string ComPort
        {
            get { return _comPort; }
            set { _comPort = value; }
        }

        public Handshake Handshake
        {
            get { return _handshake; }
            set { _handshake = value; }
        }

        public int BaudRate
        {
            get { return _baudRate; }
            set { _baudRate = value; }
        }

        public int Databits
        {
            get { return _databits; }
            set { _databits = value; }
        }

        public Parity Parity
        {
            get { return _parity; }
            set { _parity = value; }
        }

        public int ReadTimeout
        {
            get { return _readTimeout; }
            set { _readTimeout = value; }
        }

        public int WriteTimeout
        {
            get { return _writeTimeout; }
            set { _writeTimeout = value; }
        }

        public StopBits Stopbits
        {
            get { return _stopbits; }
            set { _stopbits = value; }
        }

        public bool LineSignalDtr
        {
            get { return _lineSignalDtr; }
            set { _lineSignalDtr = value; }
        }

        public bool LineSignalRts
        {
            get { return _lineSignalRts; }
            set { _lineSignalRts = value; }
        }

        public bool Connected
        {
            get { return _connected; }
            set { _connected = value; }
        }


    }
}
