using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_STANDALONE_WIN
using Microsoft.Win32;
#elif UNITY_STANDALONE_LINUX
#endif

namespace GrumpyFoxGames
{
    internal static class COMPortSearcher
    {
        public static List<string> FindCOMPortsByVIDPID(string vid, string pid)
        {
#if UNITY_STANDALONE_WIN
            return FindCOMPortsByVIDPID_Windows(vid, pid);
#elif UNITY_STANDALONE_LINUX
            return FindSerialPortsByVIDPID_Linux(vid, pid);
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
        var devPath = "/sys/class/tty/";

        try
        {
            // List all devices in /dev
            var serialPorts = Directory.GetDirectories(devPath, "ttyACM*");
            
            // Debug.LogError($"Found {serialPorts.Length} ports");

            foreach (var serialPort in serialPorts)
            {
                // Debug.LogError(serialPort);
                
                // Check if the serial port corresponds to a USB device
                // /sys/class/tty/ttyACM0/device/firmware_node/physical_node1
                var sysPath = Path.Combine(serialPort, "device/firmware_node/physical_node1");
                //$"{serialPort}/device/firmware_node/physical_node1";
                
                if (Directory.Exists(sysPath))
                {
                    // Read the vendor ID and product ID from the sysfs
                    var vendorPath = Path.Combine(sysPath, "idVendor");
                    var productPath = Path.Combine(sysPath, "idProduct");
                
                    if (File.Exists(vendorPath) && File.Exists(productPath))
                    {
                        var vendorId = File.ReadAllText(vendorPath).Trim();
                        var productId = File.ReadAllText(productPath).Trim();
                        
                        // Debug.LogError($"vendorId: {vendorId}");
                        // Debug.LogError($"productId: {productId}");
                
                        // Check if the VID and PID match
                        if (vendorId.Contains(vid, StringComparison.OrdinalIgnoreCase) &&
                            productId.Contains(pid, StringComparison.OrdinalIgnoreCase))
                        {
                            output.Add(serialPort);
                        }
                    }
                }
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