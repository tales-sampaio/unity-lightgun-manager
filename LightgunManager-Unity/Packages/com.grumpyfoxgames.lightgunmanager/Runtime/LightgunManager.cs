using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

namespace GrumpyFoxGames
{
    public static class LightgunManager
    {
        private static bool isRunning;
        private static bool isConnected;
        private static bool isVerbose;
        private static int pollingRate = 16;
        
        private static Thread communicationThread;
        private static SerialPort serialPort;
        private static ConcurrentQueue<string> commandQueue = new ();
        
        private static string VID = "2341";
        private static string PID = "8046";
        private static string startCommand = "S6M0.0M1.2F3.2.1";
        private static string stopCommand = "M1.1M3.0EF2.2.2";
        private static string shootCommand = "F0.2.1";
        private static string reloadCommand = "F4.2.1";
        private static string damageCommand = "F1.2.1";

#region Public

        public static bool IsConnected => isConnected;
        public static string ConnectedPort => serialPort != null ? serialPort.PortName : string.Empty;

        public static void Start(bool verboseLogging = false)
        {
            // Read INI values
            
            if (pollingRate < 16)
            {
                LogError("Polling rate must be at least 10ms.");
                pollingRate = 10;
            }
            
            Log($"Starting Lightgun Manager (polling rate: {pollingRate}ms)");
            isVerbose = verboseLogging;
            StartCommunicationThread();
        }

        public static void Stop()
        {
            isRunning = false;

            if (communicationThread != null && communicationThread.IsAlive)
            {
                communicationThread.Join(); // Wait for the thread to finish
            }

            Disconnect();
        }
        
        public static void SendCommand(string command)
        {
            if (isConnected)
            {
                Log($"Sending command: {command}");
                commandQueue.Enqueue(command);
            }
            else
            {
                LogWarning($"Cannot send command \"{command}\", device is not connected");
            }
        }

        public static void SendCommand_Shoot()
        {
            SendCommand(shootCommand);
        }
        
        public static void SendCommand_Reload()
        {
            SendCommand(reloadCommand);
        }
        
        public static void SendCommand_Damage()
        {
            SendCommand(damageCommand);
        }
        
#endregion

#region private
        private static void StartCommunicationThread()
        {
            isRunning = true;

            communicationThread = new Thread(CommunicationThreadLoop)
            {
                IsBackground = true
            };

            communicationThread.Start();
        }

        private static void CommunicationThreadLoop()
        {
            while (isRunning)
            {
                try
                {
                    if (!isConnected)
                    {
                        LogWarning("Not connected yet...");
                        SearchAndConnect();
                    }
                    else if (serialPort != null)
                    {
                        if (!serialPort.IsOpen)
                        {
                            continue;
                        }

                        // Handle queued commands
                        if (commandQueue.TryDequeue(out string command))
                        {
                            // Log($"dequeuing: \"{command}\"");
                            serialPort.WriteLine(command);
                        }

                        // Optionally read incoming data
                        if (serialPort.BytesToRead > 0)
                        {
                            var response = serialPort.ReadLine();
                            // Log($"Arduino Response: {response}");
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Timeout is expected if no data is received
                }
                catch (Exception ex)
                {
                    LogError($"Communication Error: {ex.Message}");
                    Disconnect();
                }

                Thread.Sleep(pollingRate); // Prevent tight loop
            }

            LogWarning("Finished communication loop");
        }

        private static void SearchAndConnect()
        {
            // Look for defined port names for the specific device
            var targetDevicePortNames = COMPortSearcher.FindCOMPortsByVIDPID(VID, PID);

            // Look for connected COM ports available to look for the target devices
            foreach (string connectedPortName in SerialPort.GetPortNames())
            {
                Log($"Analyzing \"{connectedPortName}\"...");

                foreach (var devicePortName in targetDevicePortNames)
                {

                    if (!connectedPortName.Equals(devicePortName)) continue;

                    try
                    {
                        Log($"Attempting to connect to \"{connectedPortName}\"");
                        
                        serialPort = new SerialPort(connectedPortName, 9600)
                        {
                            ReadTimeout = 500, // Prevent infinite blocking
                            WriteTimeout = 500
                        };

                        serialPort.Open();
                        Thread.Sleep(1000); // Wait for the port to stabilize

                        if (serialPort.IsOpen)
                        {
                            Log($"Connected to \"{connectedPortName}\"");
                            serialPort.WriteLine(startCommand);
                            isConnected = true;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Failed to connect to \"{connectedPortName}\": {ex.Message}");
                    }

                }
            }

            LogWarning("No suitable device found!");
        }

        private static void Disconnect()
        {
            LogWarning("Disconnecting...");
            isConnected = false;

            if (serialPort != null)
            {
                try
                {
                    serialPort.WriteLine(stopCommand);
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    LogWarning($"Error closing serial port: {ex.Message}");
                }
            }

            serialPort = null;
        }
        
#endregion
        
#region Logger
        
        private static void Log(string message)
        {
            if (!isVerbose) return;
            Debug.Log(message);
        }
        
        private static void LogWarning(string message)
        {
            if (!isVerbose) return;
            Debug.LogWarning(message);
        }
        
        private static void LogError(string message)
        {
            if (!isVerbose) return;
            Debug.LogError(message);
        }
        
#endregion
    }
}
