using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GarminAGWPEInterface.UI
{
	public partial class Form1 : Form
	{
		private String _CallSing { get; set; }
		private Int32 _DefaultSendSeconds { get; set; }
		private GarminProcess _GarminProcess { get; set; }
		private SerialPort _SerialPort { get; set; }
		private APRSPacketBuilder _APRSPacketBuilder { get; set; }

		private Dictionary<String, DateTime> _TrackedAssetSentTime { get; set; }

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
		}

		private void GarminProcess_ReportReceived(object sender, ReportReceivedEventArgs e)
		{
			String identifier = e.TrackedAsset.Identifier;
			DateTime lastSent = DateTime.MinValue;
			if (this._TrackedAssetSentTime.ContainsKey(e.TrackedAsset.Identifier))
				lastSent = (DateTime)this._TrackedAssetSentTime[e.TrackedAsset.Identifier];
			else
				this._TrackedAssetSentTime.Add(e.TrackedAsset.Identifier, DateTime.UtcNow);
			if (lastSent.AddSeconds(this._DefaultSendSeconds) < DateTime.UtcNow)
			{
				this._TrackedAssetSentTime[e.TrackedAsset.Identifier] = DateTime.UtcNow;
				textBox1.Invoke
				(
					(MethodInvoker)delegate()
					{
						textBox1.AppendText(e.TrackedAsset.ToString() + "\r\n");
						textBox1.AppendText(e.TrackedAsset.ToAPRSString(this._CallSing) + "\r\n\r\n");
					}
				);

				//Byte[] buffer = this._APRSPacketBuilder.Build("K9BDM-11", e.TrackedAsset.Position.Latitude, e.TrackedAsset.Position.Longitude, e.TrackedAsset.Comment);
				String aprs = e.TrackedAsset.ToAPRSString(this._CallSing);
				if (this._SerialPort.IsOpen)
					this._SerialPort.WriteLine(aprs);
					//this._SerialPort.Write(buffer, 0, buffer.Length);
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this._DefaultSendSeconds = 30;
			this._TrackedAssetSentTime = new Dictionary<String, DateTime>();
			this._SerialPort = new SerialPort("COM5", 9600, Parity.None, 8, StopBits.One);
			this._SerialPort.NewLine = "\r\n";
			//this._SerialPort.Handshake = Handshake.XOnXOff;
			this._SerialPort.Open();
			this._APRSPacketBuilder = new APRSPacketBuilder();
			this._GarminProcess = new GarminProcess();
			this._GarminProcess.ReportReceived += new GarminProcess.ReportReceivedEventHandler(this.GarminProcess_ReportReceived);
			this._GarminProcess.Start();

		}
	}
}
