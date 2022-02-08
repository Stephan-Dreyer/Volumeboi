using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Threading;




namespace AudioController
{
        public class ReadAudio { 
        static SerialPort _serialPort;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);


        static Tuple<SerialPort, string[]> find_arduino(string[] prev_ports)
        {
            string[] ports = SerialPort.GetPortNames();
            bool found = false;

  

            if (!ports.SequenceEqual(prev_ports) )
            {
                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine("itteration " + j);
                    List<SerialPort> Portslist = new List<SerialPort>();
                    List<int> busyports = new List<int>();

                    for (int i = 0; i < ports.Length; i++)

                    {
                        Console.WriteLine(ports[i]);
                        Portslist.Add(new SerialPort());
                        Portslist[i].PortName = ports[i];
                        Portslist[i].BaudRate = 112500;
                        Portslist[i].ReadTimeout = 500;
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
                    busyports.Reverse();
                    foreach (int busy in busyports)
                    {
                        Portslist.RemoveAt(busy);
                    }


                    for (int i = 0; i < Portslist.Count; i++)

                    {
                        string ping;
                        Portslist[i].Write("!");
                        try
                        {
                            ping = Portslist[i].ReadLine();
                            Console.WriteLine("response recieved from device: " + ping);
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Arduino is not at " + Portslist[i].PortName);
                            continue;
                        }


                        if (ping == "!\r")
                        {
                            _serialPort = Portslist[i];
                            _serialPort.ReadTimeout = -1;
                            found = true;
                            

                            Console.WriteLine("Arduino is at " + Portslist[i].PortName);
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
             var result=Tuple.Create(_serialPort, ports);
            return result;
        }

        //static void update_volume(int index, int[] prev, List<int> Volume) {

        //    if (prev_volumes[0] != volumes[0])
        //    {
        //        AudioManager.SetApplicationVolume(active_ID, volumes[0]);
        //    }

        //}
        static void Main(string[] args)

        {
           
            int[] prev_volumes = new int[4];
            string[] initial_ports = { "0" };
            var output = find_arduino(initial_ports);
            SerialPort _serialPort = output.Item1;
            string[] prev_ports =output.Item2;
           

            while (true)
            {
                try
                {
                    string input = _serialPort.ReadLine();
                    Console.WriteLine(input);
                    List<int> volumes = new List<int>(Array.ConvertAll(input.Split(' '), int.Parse));


                    // get active window
                    IntPtr hWnd = GetForegroundWindow();
                    int active_ID;
                    GetWindowThreadProcessId(hWnd, out active_ID);


                    // set active aplication

                    if (prev_volumes[0] != volumes[0])
                    {
                        AudioManager.SetApplicationVolume(active_ID, volumes[0]);
                    }

                    if (prev_volumes[1] != volumes[1])
                    {
                        var discord_processes = Process.GetProcessesByName("Discord");
                        if (discord_processes.Length > 0)
                        {

                            //if (active_ID == discord_processes[i].Id) use_active = false;// check if discord is active program
                            AudioManager.SetApplicationVolume(discord_processes[4].Id, volumes[1]);
                        }
                        else {
                            Console.WriteLine("discord not open");
                       }

                        if (prev_volumes[1] != volumes[1])
                        {
                            var zoom_processes = Process.GetProcessesByName("Zoom");
                            if (zoom_processes.Length > 0)
                            {
                                for (int i = 0; i < zoom_processes.Length; i++)
                                {
                                    //if (active_ID == firefox_processes[i].Id) use_active = false; // check if firefox is active program
                                    AudioManager.SetApplicationVolume(zoom_processes[i].Id, volumes[1]);
                                }
                            }
                        }

                    }
                    if (prev_volumes[2] != volumes[2])
                    {
                        var firefox_processes = Process.GetProcessesByName("Firefox");
                        if (firefox_processes.Length > 0) { 
                        for (int i = 0; i < firefox_processes.Length; i++)
                        {
                            //if (active_ID == firefox_processes[i].Id) use_active = false; // check if firefox is active program
                            AudioManager.SetApplicationVolume(firefox_processes[i].Id, volumes[2]);
                        }
                    }
                    }
                 
                   
                        // create previous  volumes array
                    for (int i = 0; i < prev_volumes.Length; i++)
                    {
                        prev_volumes[i] = volumes[i];
                    }
                }
                catch (System.NullReferenceException)
                {
                    Console.WriteLine("null exception");
                    output = find_arduino(prev_ports);
                     _serialPort = output.Item1;
                    prev_ports = output.Item2;
                    Thread.Sleep(5000);

                }
                catch
                {
                    Console.WriteLine("Device Removed");
                    output = find_arduino(prev_ports);
                    _serialPort = output.Item1;
                    prev_ports = output.Item2;
                    Thread.Sleep(5000);

                }
            }
        }
    }
    
}
