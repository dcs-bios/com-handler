using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Windows;
using System.Windows.Media;

namespace DcsBiosCOMHandler
{
    public static class Common
    {

        public static bool Debug = true;

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                var count = 0;
                Application.Current.Dispatcher.Invoke(new Action(() => count= VisualTreeHelper.GetChildrenCount(depObj)));
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = null;
                    Application.Current.Dispatcher.Invoke(new Action(() => child = VisualTreeHelper.GetChild(depObj, i))); 
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static void DebugP(String str)
        {
            if (Debug)
            {
                Console.WriteLine(str);
            }
        }

        public static bool SerialPortCurrentlyExists(string portName)
        {
            if (String.IsNullOrEmpty(portName))
            {
                return false;
            }
            var existingPorts = SerialPort.GetPortNames();
            foreach (var existingPort in existingPorts)
            {
                if (portName.Equals(existingPort))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
