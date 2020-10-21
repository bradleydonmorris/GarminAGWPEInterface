using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace GarminAGWPEInterface.UI
{
    public class APRSPacketBuilder
    {
        private const byte KISS_FEND = 0xc0;
        private const byte KISS_FESC = 0xdb;
        private const byte KISS_TFEND = 0xdc;
        private const byte KISS_TFESC = 0xdd;

        private const byte KISS_CMD_DATAFRAME0 = 0x00;
        private const byte KISS_CMD_TXDELAY = 0x01;
        private const byte KISS_CMD_TXTAIL = 0x04;
        private const byte KISS_CMD_SETHARDWARE = 0x06;


        public Byte[] Build(String callSign, Double latitude, Double longitude, String comment)
        {
            Byte[] returnValue;
            String latString = BuildLatString(latitude);
            String lonString = BuildLonString(longitude);
            String symbolTable = "/"; //Primary
            String symbolCode = "p"; //Rover (Canine)
            String infoField = $"!{latString}{symbolTable}{lonString}{symbolCode}{comment}";
            String[] digiCalls = new String[] { "WIDE2-1" };

            List<Byte> packetBytes = new List<Byte>();
            packetBytes.AddRange(GetCallsignBytes("CQ", false));
            packetBytes.AddRange(GetCallsignBytes(callSign, digiCalls.Length == 0));
            for (int i = 0; i < digiCalls.Length; i++)
            {
                packetBytes.AddRange(GetCallsignBytes(digiCalls[i], i == digiCalls.Length - 1));
            }
            packetBytes.Add(0x03);
            packetBytes.Add(0xf0);
            packetBytes.AddRange(Encoding.ASCII.GetBytes(infoField));

            List<Byte> kissFrame = new List<Byte>();
            kissFrame.Add(KISS_FEND);
            kissFrame.Add(KISS_CMD_DATAFRAME0);
            kissFrame.AddRange(packetBytes);
            kissFrame.Add(KISS_FEND);
            kissFrame.Add(0x0D);
            kissFrame.Add(0x0A);
            returnValue = kissFrame.ToArray();
            return returnValue;
        }

        /// <summary>
        /// Fixed 9 character field.
        /// In degrees and decimal minutes to two DP, followed by E or W.
        /// e.g. 07201.75W = 72 degrees, 1.75 minutes (1m45s) west
        /// </summary>
        /// <param name="lon"></param>
        /// <returns></returns>
        private String BuildLonString(double lon)
        {
            double absLon = Math.Abs(lon);
            string degStr = string.Format("{0:000}", absLon);
            double fraction = (absLon - (int)absLon) * 60;
            string fracStr = string.Format("{0:00.00}", fraction);
            string ew = lon < 0 ? "W" : "E";

            string result = $"{degStr}{fracStr}{ew}";

            return result;
        }

        /// <summary>
        /// Fixed 8 character field.
        /// In degrees and decimal minutes to two DP, followed by N or S.
        /// </summary>
        /// <param name="lat"></param>
        /// <returns></returns>
        private String BuildLatString(double lat)
        {
            double absLat = Math.Abs(lat);
            string degStr = string.Format("{0:00}", absLat);
            double fraction = (absLat - (int)absLat) * 60;
            string fracStr = string.Format("{0:00.00}", fraction);
            string ew = lat > 0 ? "N" : "S";

            string result = $"{degStr}{fracStr}{ew}";

            return result;
        }

        private Byte[] GetCallsignBytes(string callAndSsid, bool isLastCallsign)
        {
            string call;
            int ssid = 0;

            if (callAndSsid.Contains("-"))
            {
                string[] comps = callAndSsid.Split('-');
                call = comps[0];
                ssid = int.Parse(comps[1]);
            }
            else
            {
                call = callAndSsid;
            }

            while (call.Length < 6)
            {
                call += " ";
            }

            var result = new List<byte>();

            foreach (byte letter in call)
            {
                result.Add((byte)(letter << 1));
            }

            BitArray ssidBits = new BitArray(new[] { (Byte)ssid });
            BitArray ssidByte = new BitArray(8);
            ssidByte[0] = false;
            ssidByte[1] = true;
            ssidByte[2] = true;
            ssidByte[3] = ssidBits[3];
            ssidByte[4] = ssidBits[2];
            ssidByte[5] = ssidBits[1];
            ssidByte[6] = ssidBits[0];
            ssidByte[7] = isLastCallsign;

            Byte[] methodResult = new Byte[7];
            result.CopyTo(methodResult);
            Reverse(ssidByte);
            methodResult[6] = GetIntFromBitArray(ssidByte);

            return methodResult;
        }

        private void Reverse(BitArray array)
        {
            Int32 length = array.Length;
            Int32 mid = (length / 2);

            for (Int32 i = 0; i < mid; i++)
            {
                Boolean bit = array[i];
                array[i] = array[length - i - 1];
                array[length - i - 1] = bit;
            }
        }

        private Byte GetIntFromBitArray(BitArray bitArray)
        {
            if (bitArray.Length > 8)
                throw new ArgumentException("Argument length shall be at most 8 bits.");
            Byte[] array = new Byte[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }
    }
}