using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Garmin.Device.Core;
using Newtonsoft.Json;

namespace GarminAGWPEInterface.UI
{
    public class ReportReceivedEventArgs : EventArgs
    {
        public TrackedAsset TrackedAsset { get; set; }


        public ReportReceivedEventArgs(TrackedAsset trackedAsset)
        {
            this.TrackedAsset = trackedAsset;
        }
    }



    class GarminProcess
	{
        public delegate void ReportReceivedEventHandler(object sender, ReportReceivedEventArgs e);
        public event ReportReceivedEventHandler ReportReceived;
        public virtual void OnReportReceived(ReportReceivedEventArgs e)
        {
            if (ReportReceived != null)
                ReportReceived(this, e);
        }


        StreamWriter _writer = null;
        readonly HttpClient _web = new HttpClient();
        readonly string[] _errors = new string[5];
        int errorIndex = 0;
        Dictionary<string, TrackedAsset> latest = new Dictionary<string, TrackedAsset>();
        object syncLock = new object();

        public GarminProcess()
        {
            //_server = server;
        }

        public void Start()
        {
            string input = string.Empty;
            //Print();
            while (input.Length == 0)
            {
                GarminDevice baseStation = null;

                var list = GarminDevice.DiscoverDevices();
                list.ForEach(f =>
                {
                    var reader = new GarminReader(f);
                    var info = reader.ReadInfo();
                    Console.WriteLine(info.Id + ": " + info.Description);

                    if (baseStation == null && info.SupportedProtocols != null && info.SupportedProtocols.Any(p => p.tag == (byte)'A' && p.data == 1100))
                    {
                        baseStation = f;
                    }
                    else
                    {
                        f.Dispose();
                    }
                });


                if (baseStation != null)
                {
                    var reader = new GarminReader(baseStation);

                    while (true)
                    {
                        try
                        {
                            var packet = reader.WaitForPacket(3078);
                            Update(new TrackedAsset(packet.data));
                            ReportReceived?.Invoke(this, new ReportReceivedEventArgs(new TrackedAsset(packet.data)));
                        }
                        catch (Exception e)
                        {
                            LogException(e.Message);
                        }
                    }
                }

                Console.WriteLine("Found " + list.Count + " devices. Enter to go again.");
                input = Console.ReadLine();

            }
        }

        private void Update(TrackedAsset entry)
        {
			//Change to write TNC Packet to COMM port.

			if (_writer == null)
			{
				var filename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"track-{DateTime.Now.ToString("yyyy-MM-dd-HHmm")}.log");
				_writer = new StreamWriter(File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
				_writer.AutoFlush = true;
			}

			_writer.WriteLine(JsonConvert.SerializeObject(entry));
            lock (syncLock)
            {
                if (latest.ContainsKey(entry.Identifier))
                {
                    latest[entry.Identifier] = entry;
                }
                else
                {
                    latest.Add(entry.Identifier, entry);
                }
            }
#pragma warning disable 4014
            //Task.Run(async () =>
            //{
            //    try
            //    {
            //        string identifier = entry.Identifier;
            //        if (!string.IsNullOrWhiteSpace(_callsign)) identifier = _callsign + "-" + identifier;

            //        await _web.GetAsync($"{_server}/rest/location/update/position?lat={entry.Position.Latitude}&lng={entry.Position.Longitude}&id=FLEET:{identifier}");
            //    }
            //    catch (HttpRequestException ex)

            //    {
            //        LogException(ex.InnerException.Message);
            //    }
            //    catch (Exception ex)
            //    {
            //        LogException(ex.Message);
            //    }
            //}).ConfigureAwait(false);
#pragma warning restore 4014

            //Print();
        }

        private void LogException(string message)
        {
            lock (syncLock)
            {
                _errors[errorIndex] = $"{DateTime.Now:T} - {message}";
                errorIndex = (errorIndex + 1) % _errors.Length;
            }
            Print();
        }

        private void WriteLine(string text)
        {
            string padding = new string(' ', Console.WindowWidth - Console.CursorLeft - text.Length - 1);
            Console.WriteLine(text + padding);
        }

        private void Print()
        {
            //int width = Console.WindowWidth - 1;
            //lock (syncLock)
            //{
            //    Console.SetCursorPosition(0, 0);
            //    Console.ForegroundColor = ConsoleColor.Gray;
            //    WriteLine("Garmin Alpha Track Download v" + typeof(Program).Assembly.GetName().Version);
            //    //WriteLine($"Logging to {_server} with call sign {_callsign}");
            //    WriteLine(string.Empty);
            //    foreach (var p in latest.Keys.OrderBy(f => f).Select(f => latest[f]))
            //    {
            //        Console.ForegroundColor = ConsoleColor.White;
            //        string id = p.Identifier.PadRight(4);
            //        Console.Write(id);
            //        Console.ForegroundColor = ConsoleColor.Gray;
            //        WriteLine($"  {p.Time:T}  Batt:{p.Battery}/4  Comm:{p.Comm}/5  GPS:{p.Gps}/3  ID:{p.CollarId / 256}-{p.CollarId % 256}".PadRight(width - id.Length));
            //        Console.ForegroundColor = ConsoleColor.Green;
            //        WriteLine($"           {p.Position.Latitude:0.000000}, {p.Position.Longitude:0.000000}     {p.DogStatus}".PadRight(width));
            //        WriteLine(string.Empty);
            //    }

            //    Console.ForegroundColor = ConsoleColor.Red;
            //    WriteLine(string.Empty);
            //    for (var i = 0; i < _errors.Length; i++)
            //    {
            //        foreach (var line in _errors[(i + errorIndex) % _errors.Length].Wrap(width))
            //        {
            //            WriteLine(line);
            //        }
            //    }
            //}
        }
    }
}
