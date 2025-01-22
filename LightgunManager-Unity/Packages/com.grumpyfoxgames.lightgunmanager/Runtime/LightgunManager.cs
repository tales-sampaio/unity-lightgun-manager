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
        private static bool isDetected;
        private static bool isConnected;
        private static bool isVerbose;
        private static int pollingRate = 16;
        private static int searchingRate = 500;
        private static string detectedGun;
        private static string detectedPort;
        
        private static Thread communicationThread;
        private static SerialPort serialPort;
        private static readonly ConcurrentQueue<string> commandQueue = new ();
        
#region Public

        public struct Gun
        {
            public string name;
            public GunSettings settings;
        }
        
        public struct GunSettings
        {
            public string vid;
            public string pid;
            public string baud;
            public string startCommand;
            public string stopCommand;
            public string shootCommand;
            public string reloadCommand;
            public string damageCommand;
            public string outOfAmmoCommand;
        }

        public static GunSettings currentGunSettings;

        public delegate void OnGunDetected();
        public static event OnGunDetected onGunDetected;
        
        public delegate void OnGunConnected();
        public static event OnGunConnected onGunConnected;
        
        public delegate void OnGunDisconnected();
        public static event OnGunDisconnected onGunDisconnected;
        
        public static bool IsDetected => isDetected;
        public static bool IsConnected => isConnected;
        public static string ConnectedPort => serialPort != null ? serialPort.PortName : string.Empty;
        public static string DetectedGun => detectedGun;
        
        public static void Start(bool verboseLogging = false)
        {
#if UNITY_ANDROID
            return;
#endif
            
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
#if UNITY_ANDROID
            return;
#endif
            isRunning = false;

            if (communicationThread != null && communicationThread.IsAlive)
            {
                communicationThread.Join(); // Wait for the thread to finish
            }

            Disconnect();
        }
        
        public static void SendCommand(string command)
        {
#if UNITY_ANDROID
            return;
#endif
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
            SendCommand(currentGunSettings.shootCommand);
        }
        
        public static void SendCommand_Reload()
        {
            SendCommand(currentGunSettings.reloadCommand);
        }
        
        public static void SendCommand_Damage()
        {
            SendCommand(currentGunSettings.damageCommand);
        }
        
        public static void SendCommand_OutOfAmmo()
        {
            SendCommand(currentGunSettings.outOfAmmoCommand);
        }
        
#endregion

#region Private
    private static Gun[] guns =
    {
        new Gun
        {
            name = "RetroShooter",
            settings = new GunSettings
            {
                vid =              INIReader.GetValue("RetroShooter", "VID"),
                pid =              INIReader.GetValue("RetroShooter", "PID"),
                baud =             INIReader.GetValue("RetroShooter", "BAUD"),
                startCommand =     INIReader.GetValue("RetroShooter", "Start"),
                stopCommand =      INIReader.GetValue("RetroShooter", "Stop"),
                shootCommand =     INIReader.GetValue("RetroShooter", "Shoot"),
                reloadCommand =    INIReader.GetValue("RetroShooter", "Reload"),
                damageCommand =    INIReader.GetValue("RetroShooter", "Damage"),
                outOfAmmoCommand = INIReader.GetValue("RetroShooter", "OutOfAmmo")
            }
        },
        new Gun
        {
            name = "Gun4IR",
            settings = new GunSettings
            {
                vid =              INIReader.GetValue("Gun4IR", "VID"),
                pid =              INIReader.GetValue("Gun4IR", "PID"),
                baud =             INIReader.GetValue("Gun4IR", "BAUD"),
                startCommand =     INIReader.GetValue("Gun4IR", "Start"),
                stopCommand =      INIReader.GetValue("Gun4IR", "Stop"),
                shootCommand =     INIReader.GetValue("Gun4IR", "Shoot"),
                reloadCommand =    INIReader.GetValue("Gun4IR", "Reload"),
                damageCommand =    INIReader.GetValue("Gun4IR", "Damage"),
                outOfAmmoCommand = INIReader.GetValue("Gun4IR", "OutOfAmmo")
            }
        },
        new Gun
        {
            name = "Sinden",
            settings = new GunSettings
            {
                vid =              INIReader.GetValue("Sinden", "VID"),
                pid =              INIReader.GetValue("Sinden", "PID"),
                baud =             INIReader.GetValue("Sinden", "BAUD"),
                startCommand =     INIReader.GetValue("Sinden", "Start"),
                stopCommand =      INIReader.GetValue("Sinden", "Stop"),
                shootCommand =     INIReader.GetValue("Sinden", "Shoot"),
                reloadCommand =    INIReader.GetValue("Sinden", "Reload"),
                damageCommand =    INIReader.GetValue("Sinden", "Damage"),
                outOfAmmoCommand = INIReader.GetValue("Sinden", "OutOfAmmo")
            }
        }
    };

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
                        if (isDetected)
                        {
                            var stillDetected = false;
                        
                            foreach (var port in SerialPort.GetPortNames())
                            {
                                if (port.Equals(detectedPort))
                                {
                                    stillDetected = true;
                                    break;
                                }
                            }

                            if (!stillDetected)
                            {
                                Disconnect();
                            }
                        }
                        else
                        {
                            LogWarning("Not connected yet...");
                            SearchAndConnect();
                        }
                        
                        Thread.Sleep(searchingRate); // Prevent tight loop
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
                            serialPort.Write(command);
                        }

                        // Optionally read incoming data
                        if (serialPort.BytesToRead > 0)
                        {
                            var response = serialPort.ReadLine();
                            Log($"Response: {response}");
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
            foreach (var gun in guns)
            {
                Log($"Searching for \"{gun.name}\"");
                var gunSettings = gun.settings;
                
                // Look for defined port names for the specific device
                var targetDevicePortNames = COMPortSearcher.FindCOMPortsByVIDPID(gunSettings.vid, gunSettings.pid);

                // Look for connected COM ports available to look for the target devices
                foreach (string connectedPortName in SerialPort.GetPortNames())
                {
                    Log($"Analyzing \"{connectedPortName}\"...");

                    foreach (var devicePortName in targetDevicePortNames)
                    {
                        if (!connectedPortName.Equals(devicePortName)) continue;

                        Log($"Detected a \"{gun.name}\" on \"{connectedPortName}\"");
                        isDetected = true;
                        detectedGun = gun.name;
                        detectedPort = devicePortName;
                        currentGunSettings = gunSettings;
                        
                        onGunDetected?.Invoke();
                        
                        try
                        {
                            var baudRate = int.Parse(gunSettings.baud);
                            
                            Log($"Attempting to connect to \"{connectedPortName}\" with baud rate of {baudRate}.");
                            
                            serialPort = new SerialPort(connectedPortName, baudRate)
                            {
                                ReadTimeout = 500, // Prevent infinite blocking
                                WriteTimeout = 500
                            };

                            serialPort.Open();
                            Thread.Sleep(searchingRate); // Wait for the port to stabilize

                            if (serialPort.IsOpen)
                            {
                                Log($"Connected to \"{gun.name}\" on \"{connectedPortName}\"");
                                Log($"Sending command: {gunSettings.startCommand}");
                                isConnected = true;
                                SendCommand(currentGunSettings.startCommand);
                                SendCommand_Shoot();
                                SendCommand_Damage();
                                
                                onGunConnected?.Invoke();
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Failed to connect to \"{connectedPortName}\": {ex.Message}");
                        }

                    }
                }
            }
            
            LogWarning("No suitable device found!");
        }

        private static void Disconnect()
        {
            LogWarning("Disconnecting...");

            if (serialPort != null)
            {
                try
                {
                    serialPort.Write(currentGunSettings.stopCommand);
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    LogWarning($"Error closing serial port: {ex.Message}");
                }
            }
            
            isConnected = false;
            serialPort = null;
            isDetected = false;
            detectedGun = string.Empty;
            currentGunSettings = new GunSettings();
            
            onGunDisconnected?.Invoke();
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
