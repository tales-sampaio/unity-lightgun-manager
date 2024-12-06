using System;
using System.Collections.Generic;

#if UNITY_STANDALONE_WIN
using Microsoft.Win32;
#endif

namespace GrumpyFoxGames
{
    public class RegistrySearch
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

    }
}