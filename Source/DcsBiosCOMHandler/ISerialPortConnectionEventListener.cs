using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DcsBiosCOMHandler
{
    public interface ISerialPortConnectionEventListener
    {
        void SerialPortClosed();
        void SerialPortOpened();
    }
}
