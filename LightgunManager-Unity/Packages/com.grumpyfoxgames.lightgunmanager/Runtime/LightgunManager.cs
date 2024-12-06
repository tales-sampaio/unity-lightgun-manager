using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace GrumpyFoxGames
{
    public class LightgunManager : MonoBehaviour
    {
        private Thread communicationThread;
        private SerialPort serialPort;
        private ConcurrentQueue<string> commandQueue = new ConcurrentQueue<string>();
        private bool isRunning = false;
        private bool isConnected = false;
        // private string deviceName = "gun4IR"; // Expected device name identifier

        // public string serialPortName = "COM1";
        [SerializeField] private string VID = "2341";
        [SerializeField] private string PID = "8046";
        [SerializeField] private string startCommand = "S6M1.2M3.1";
        [SerializeField] private string stopCommand = "M1.1M3.0E";
        [SerializeField] private string command;
        [SerializeField] private bool sendCommand;

        public bool IsConnected => isConnected;
        public string ConnectedPort => serialPort != null ? serialPort.PortName : string.Empty;

        private void OnEnable()
        {
            // Debug.LogError("Open Debug");
            StartCommunicationThread();
        }

        private void OnDisable()
        {
            isRunning = false;

            if (communicationThread != null && communicationThread.IsAlive)
            {
                communicationThread.Join();
            }

            Disconnect();
        }

        private void LateUpdate()
        {
            if (sendCommand)
            {
                SendCommand(command);
                sendCommand = false;
            }
        }

        private void OnApplicationQuit()
        {
            isRunning = false;

            if (communicationThread != null && communicationThread.IsAlive)
            {
                communicationThread.Join(); // Wait for the thread to finish
            }

            Disconnect();
        }

        private void StartCommunicationThread()
        {
            isRunning = true;

            communicationThread = new Thread(CommunicationLoop)
            {
                IsBackground = true
            };

            communicationThread.Start();
        }

        private void CommunicationLoop()
        {
            while (isRunning)
            {
                try
                {
                    if (!isConnected)
                    {
                        // Debug.LogError("not connected");
                        SearchAndConnect();
                    }
                    else if (serialPort != null)
                    {
                        // Debug.Log(serialPort.IsOpen);
                        if (!serialPort.IsOpen)
                        {
                            continue;
                        }

                        // Debug.LogError("handling commands");
                        // Handle queued commands
                        if (commandQueue.TryDequeue(out string command))
                        {
                            // Debug.Log($"dequeuing: \"{command}\"");
                            serialPort.WriteLine(command);
                        }

                        // Optionally read incoming data
                        if (serialPort.BytesToRead > 0)
                        {
                            var response = serialPort.ReadLine();
                            // Debug.Log($"Arduino Response: {response}");
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // Timeout is expected if no data is received
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Communication Error: {ex.Message}");
                    Disconnect();
                }

                Thread.Sleep(10); // Prevent tight loop
            }

            // Debug.LogError("Finished communication loop");
        }

        private void SearchAndConnect()
        {
            // Look for defined port names for the specific device
            var targetDevicePortNames = RegistrySearch.FindCOMPortsByVIDPID(VID, PID);

            // foreach (var devicePortName in targetDevicePortNames)
            // {
            //     Debug.Log(devicePortName);
            // }

            // Look for connected COM ports available to look for the target devices
            foreach (string connectedPortName in SerialPort.GetPortNames())
            {
                Debug.Log($"Searching for {connectedPortName}...");

                foreach (var devicePortName in targetDevicePortNames)
                {
                    // Debug.Log($"Try match {devicePortName} to {connectedPortName}");

                    if (!connectedPortName.Equals(devicePortName)) continue;

                    try
                    {
                        Debug.Log($"Attempting to connect to {connectedPortName}");
                        serialPort = new SerialPort(connectedPortName, 9600)
                        {
                            ReadTimeout = 500, // Prevent infinite blocking
                            WriteTimeout = 500
                        };

                        serialPort.Open();

                        Thread.Sleep(1000); // Wait for the port to stabilize

                        if (serialPort.IsOpen)
                        {
                            Debug.Log($"Connected to {connectedPortName}");
                            serialPort.WriteLine(startCommand);
                            serialPort.WriteLine("F3.2.1");
                            isConnected = true;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to connect to {connectedPortName}: {ex.Message}");
                    }

                }



            }

            Debug.LogWarning("No suitable device found");
        }


        public void SendCommand(string command)
        {
            if (isConnected)
            {
                Debug.Log($"Sending command: {command}");
                commandQueue.Enqueue(command);
            }
            else
            {
                Debug.LogWarning($"Cannot send command \"{command}\", device is not connected");
            }
        }

        private void Disconnect()
        {
            // Debug.LogError("Disconnecting");
            isConnected = false;

            if (serialPort != null)
            {
                try
                {
                    serialPort.WriteLine(stopCommand);
                    serialPort.WriteLine("F2.2.2");
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error closing serial port: {ex.Message}");
                }
            }

            serialPort = null;
        }


    }
}
