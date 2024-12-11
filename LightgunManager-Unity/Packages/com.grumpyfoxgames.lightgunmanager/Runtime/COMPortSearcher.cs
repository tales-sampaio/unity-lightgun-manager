using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_STANDALONE_WIN
using Microsoft.Win32;
#endif

namespace GrumpyFoxGames
{
    internal static class COMPortSearcher
    {
        public static List<string> FindCOMPortsByVIDPID(string vid, string pid)
        {
#if UNITY_STANDALONE_WIN
            return FindCOMPortsByVIDPID_Windows(vid, pid);
#else
        return null;
#endif
        }

#if UNITY_STANDALONE_WIN
        private static List<string> FindCOMPortsByVIDPID_Windows(string vid, string pid)
        {
            var registryPath = @"SYSTEM\ControlSet001\Control\COM Name Arbiter\Devices";
            var output = new List<string>();

            try
            {
                // Open the registry key.
                using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            // Get the data for the current value.
                            object valueData = key.GetValue(valueName);

                            if (valueData != null && valueData is string data)
                            {
                                // Check if the data contains the specific PID and VID.
                                if (data.Contains(pid, StringComparison.OrdinalIgnoreCase) &&
                                    data.Contains(vid, StringComparison.OrdinalIgnoreCase))
                                {
                                    output.Add(valueName);
                                    // return $"Found COM Port: {valueName}, Data: {data}";
                                }
                            }
                        }

                        // return $"No COM port found with PID: {pid} and VID: {vid}.";
                    }
                    else
                    {
                        // throw new KeyNotFoundException($"Registry key not found: {registryPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return output;
        }
#endif

        
#if UNITY_STANDALONE_LINUX
        public static List<string> FindSerialPortsByVIDPID_Linux(string vid, string pid)
    {
        var output = new List<string>();

        // Check the /dev/ directory for serial devices
        string devPath = "/sys/class/tty/";

        try
        {
            // List all devices in /dev
            var serialPorts = Directory.GetFiles(devPath, "ttyACM*");

            foreach (var serialPort in serialPorts)
            {
                UnityEngine.Debug.LogError(serialPort);
                // // Check if the serial port corresponds to a USB device
                // string sysPath = $"/sys/class/tty/{Path.GetFileName(serialPort)}/device/";

                // if (Directory.Exists(sysPath))
                // {
                //     // Read the vendor ID and product ID from the sysfs
                //     string vendorPath = Path.Combine(sysPath, "idVendor");
                //     string productPath = Path.Combine(sysPath, "idProduct");
                //
                //     if (File.Exists(vendorPath) && File.Exists(productPath))
                //     {
                //         string vendorId = File.ReadAllText(vendorPath).Trim();
                //         string productId = File.ReadAllText(productPath).Trim();
                //
                //         // Check if the VID and PID match
                //         if (vendorId.Equals(vid, StringComparison.OrdinalIgnoreCase) &&
                //             productId.Equals(pid, StringComparison.OrdinalIgnoreCase))
                //         {
                //             output.Add(serialPort);
                //         }
                //     }
                // }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error: {ex.Message}");
        }

        return output;
    }
#endif
    }
}