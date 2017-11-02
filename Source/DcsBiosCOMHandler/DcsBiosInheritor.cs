using System;
using DCS_BIOS;

namespace DcsBiosCOMHandler
{
    public class DcsBiosInheritor : DCSBIOS, ISerialPortListener
    {
        public DcsBiosInheritor(IDcsBiosDataListener iDcsBiosDataListener, String ipFrom, String ipTo, int portFrom, int portTo, DcsBiosNotificationMode dcsNoficationMode)
            : base(iDcsBiosDataListener, ipFrom, ipTo, portFrom, portTo, dcsNoficationMode)
        {
        }

        public void GetSerialPortData(String serialPortData)
        {
            var bytesSent = SendDataFunction(serialPortData);
        }
    }
}