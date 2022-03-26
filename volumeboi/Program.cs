using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AudioController
{
    public class ReadAudio
    {
        static SerialPort _serialPort;
        
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        //finds Serial Port that hardware device is connected to
        static Tuple<SerialPort, string[]> FindHardware(string[] prev_ports)
        {
            //get list of all ports
            string[] ports = SerialPort.GetPortNames();
            bool found = false;// boolean to determine if device is found
            // only try again if the list of ports has changed
            if (!ports.SequenceEqual(prev_ports))
            {
                //Try to find device 10 times
                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine("attempt " + j);
                    List<SerialPort> Portslist = new List<SerialPort>();
                    List<int> busyports = new List<int>();
                    for (int i = 0; i < ports.Length; i++)
                    {
                        Console.WriteLine(ports[i]);
                        // create object for each port and configure
                        Portslist.Add(new SerialPort());
                        Portslist[i].PortName = ports[i];
                        Portslist[i].BaudRate = 112500;
                        Portslist[i].ReadTimeout = 500;
                        // try to open port, if fail list as busy
                        try
                        {
                            Portslist[i].Open();
                        }
                        catch
                        {
                            Console.WriteLine("port " + ports[i] + " busy");
                            busyports.Add(i);
                        }
                    }
                    //remove busy ports
                    busyports.Reverse();//remove indexes back to front to ensure they stay consistent and dont shift
                    foreach (int busy in busyports)
                    {
                        Portslist.RemoveAt(busy);
                    }
                    // Ping every port that is not busy and listen for ! reply
                    for (int i = 0; i < Portslist.Count; i++)
                    {
                        string ping;
                        Portslist[i].Write("!");
                        // listen for reply
                        try
                        {
                            ping = Portslist[i].ReadLine();
                            Console.WriteLine("response recieved from device: " + ping);
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Knobs are not at " + Portslist[i].PortName);
                            continue;
                        }
                        // if  reply  is ! then set current port as device
                        if (ping == "!\r")
                        {
                            // assign port as locaiton of device
                            _serialPort = Portslist[i];
                            _serialPort.ReadTimeout = -1;
                            found = true;
                            Console.WriteLine("Knobs are at " + Portslist[i].PortName);
                            Portslist.RemoveAt(i);
                        }
                    }
                    // close remaining ports
                    for (int i = 0; i < Portslist.Count; i++)
                    {
                        Console.WriteLine(Portslist[i].PortName + " Closed");
                        Portslist[i].Close();
                    }
                    if (found) break;
                    Thread.Sleep(500);
                }
            }
            // store Serial port and portlist as Tuple to return
            var result = Tuple.Create(_serialPort, ports);
            return result;
        }
        static void Main(string[] args)
        {
            // find device 
            int[] prev_volumes = new int[4];
            string[] initial_ports = { "0" };
            var output = FindHardware(initial_ports);
            // assign serial port that device was found on
            SerialPort _serialPort = output.Item1;
            string[] prev_ports = output.Item2;

            // find all iniital processes
            int[] discord_processes = Process.GetProcessesByName("Discord").Select(p => p.Id).ToArray();
    
            int[] zoom_processes = Process.GetProcessesByName("Zoom").Select(p => p.Id).ToArray();
 
            int[] firefox_processes = Process.GetProcessesByName("Firefox").Select(p => p.Id).ToArray();
         
            int[] spotify_processes = Process.GetProcessesByName("Spotify").Select(p => p.Id).ToArray();

            List<int[]> usedProcessID = new List<int[]>(){ discord_processes, zoom_processes, firefox_processes, spotify_processes };
            bool useActive = true;

       
            while (true)
            {
                try
                {
    
                
                    // read output from device
                    string input = _serialPort.ReadLine();
                    Console.WriteLine(input);
                    // parse  recieved data  for volume value for each knob
                    List<int> volumes = new List<int>(Array.ConvertAll(input.Split(' '), int.Parse));
                    // get active window
                    IntPtr hWnd = GetForegroundWindow();
                    GetWindowThreadProcessId(hWnd, out int active_ID);
  
                    // set discord and zoom volume
                    if (prev_volumes[1] != volumes[1])
                    {
                         discord_processes = Process.GetProcessesByName("Discord").Select(p => p.Id).ToArray(); 
                        if (discord_processes.Length > 0)
                        {
                        
                            AudioManager.SetApplicationVolume(discord_processes[4], volumes[1]);
                        }
                        else
                        {
                            Console.WriteLine("Discord not open");
                        }
                        if (prev_volumes[1] != volumes[1])
                        {
                            zoom_processes = Process.GetProcessesByName("Zoom").Select(p => p.Id).ToArray();;
                            if (zoom_processes.Length > 0)
                            {
                                for (int i = 0; i < zoom_processes.Length; i++)
                                {
                                
                                    AudioManager.SetApplicationVolume(zoom_processes[i], volumes[1]);
                                }
                            }
                        }
                    }
                    // set firefox volume
                    if (prev_volumes[2] != volumes[2])
                    {
                        firefox_processes = Process.GetProcessesByName("Firefox").Select(p => p.Id).ToArray();
                        if (firefox_processes.Length > 0)
                        {
                            for (int i = 0; i < firefox_processes.Length; i++)
                            {
                              
                                AudioManager.SetApplicationVolume(firefox_processes[i], volumes[2]);
                            }
                        }
                    }
                    // set spotify volume
                    if (prev_volumes[3] != volumes[3])
                    {
                        spotify_processes = Process.GetProcessesByName("Spotify").Select(p => p.Id).ToArray(); ;
                        if (spotify_processes.Length > 0)
                        {
                            for (int i = 0; i < spotify_processes.Length; i++)
                            {
                              
                                AudioManager.SetApplicationVolume(spotify_processes[i], volumes[3]);
                            }
                        }
                    }

                    // set active aplication volume
                    if (prev_volumes[0] != volumes[0])
                    {
                        foreach (int[] program in usedProcessID)
                        {
                            if (program.Contains(active_ID)) { useActive =false ; break; }

                        }
                        if (useActive) {
                         
                        AudioManager.SetApplicationVolume(active_ID, volumes[0]);
                        useActive = true;
                    }
                    }
                    // create previous  volumes array
                    for (int i = 0; i < prev_volumes.Length; i++)
                    {
                        prev_volumes[i] = volumes[i];
                    }
                }
                // cases for when device can no longer be found
                catch (System.NullReferenceException)
                {
                    Console.WriteLine("null exception");
                    output = FindHardware(prev_ports);
                    _serialPort = output.Item1;
                    prev_ports = output.Item2;
                    Thread.Sleep(5000);
                }
                catch
                {
                    Console.WriteLine("Device Removed");
                    output = FindHardware(prev_ports);
                    _serialPort = output.Item1;
                    prev_ports = output.Item2;
                    Thread.Sleep(5000);
                }
            }
        }
    }
}
