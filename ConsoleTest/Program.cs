//InMind-VR


using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Globalization;

namespace DSIStreamer.Connector
{

    public struct Vector4
    {
        public float t,x, y, z;

        public override string ToString()
        {
            return "[" + t.ToString(CultureInfo.InvariantCulture) +","+ x.ToString(CultureInfo.InvariantCulture)+","+ y.ToString(CultureInfo.InvariantCulture) + ","+ z.ToString(CultureInfo.InvariantCulture) + "]";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            string address = "127.0.0.1";
            int port = 8844;
            bool debug = true;
            if (args.Length >= 1) address = args[0];
            if (args.Length >= 2) port = int.Parse(args[1]);
            if (args.Length >= 3) debug = bool.Parse(args[2]);

            Thread thread = new Thread(() => Process(address, port, debug));
            thread.IsBackground = true;
            thread.Start();

            Console.ReadLine();
        }

        public static void Process(string adress, int port, bool debug)
        {
            try
            {
                TcpClient tcpclnt = new TcpClient();

                tcpclnt.Connect(adress, port);
                Stream stm = tcpclnt.GetStream();

                while (true)
                {
                    var data = new byte[12];
                    int ret = stm.Read(data, 0, 12);
                    string contentHeader = "";
                    for (int i = 0; i < 12; i += 1)
                    {
                        contentHeader += " " + data[i].ToString();

                    }
                    if (debug) Console.WriteLine("Header packet " + contentHeader);

                    byte[] array = new byte[5];
                    for (int i = 0; i < 5; i++) array[i] = data[i];

                    string result = System.Text.Encoding.UTF8.GetString(array);

                    int packetType = data[5];
                    int length = data[7];
                    int packetnumber = data[8] * 255 * 255 * 255 + data[9] * 255 * 255 + data[10] * 255 + data[11];
                    if (debug) Console.WriteLine("Packet Header : " + result + " Packer Type : " + packetType + " Packet length :" + length + " Packet Number : " + packetnumber);


                    switch (packetType)
                    {
                        case 1: ReadEegPacket(stm, packetType, length, debug); break;
                        case 4: ReadImpedancePacket(stm, packetType, length, debug); break;
                        case 5: ReadEventPackect(stm, packetType, length, debug); break;
                        case 130: ReadAccelerometerPacket(stm, packetType, length, debug); break;
                        default:
                            break;
                    }
                    Thread.Sleep(1);
                }

                tcpclnt.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine("exception occures " + e.Message);
            }
        }

        public static void ReadImpedancePacket(Stream stm, int packetType, int packetLength, bool debug)
        {
            if (packetType == 4)
            {
                Console.WriteLine("Reading impedance");

                byte[] data = new byte[packetLength];
                int ret = stm.Read(data, 0, packetLength);
                
                string content = "";

                for (int i = 0; i < packetLength; i += 1)
                {
                    content += " " + data[i].ToString();
                }
                Console.WriteLine("Packet data " + content + "\nHexa : " + BitConverter.ToString(data));                

                try
                {
                    float timestamp = ReadSingle(data, 0, false); //BitConverter.ToSingle(data, 0);
                    int dataCounter = data[4] + data[5] * 255 + data[6] * 255 * 255; // sur 3 bytes ?!?
                    UInt32 ADCStatus = BitConverter.ToUInt32(data, 6);
                    //maintenant, les canaux !
                    float[] channels = new float[25];
                    string dataContent = "";
                    dataContent = "[" + timestamp.ToString(CultureInfo.InvariantCulture) + "," + dataCounter.ToString(CultureInfo.InvariantCulture) + "," + ADCStatus.ToString(CultureInfo.InvariantCulture) + "]";

                    dataContent += "\n";
                    for (int i = 0; i < 25; i++)
                    {
                        channels[i] = ReadSingle(data, 11 + i * sizeof(float), false);
                        dataContent += "CH : " + i + " : " + channels[i].ToString(CultureInfo.InvariantCulture) + "\n";
                    }
                    Console.WriteLine("Packet data : " + dataContent);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

       

        public static void ReadAccelerometerPacket(Stream stm, int packetType, int packetLength, bool debug)
        {
            if (packetType == 130)
            {
                if (debug)  Console.WriteLine("Reading Accelerometer");

                byte[] data = new byte[packetLength];
                int ret = stm.Read(data, 0, packetLength);
              
                try
                {
                    int adcSequence = data[0];
                    Vector4[] accs = new Vector4[3];
                    string dataContent = "";

                    for(int i = 0; i < 3; i++)
                    {
                        accs[i].t = ReadSingle(data, 1 + (i*16) , false);
                        accs[i].x = ReadSingle(data, 1 + (i*16) + sizeof(float), false);
                        accs[i].y = ReadSingle(data, 1 + (i*16) + 2 * sizeof(float), false);
                        accs[i].z = ReadSingle(data, 1 + (i*16) + 3 * sizeof(float), false);

                        dataContent += "\n" + accs[i].ToString();
                        
                    }
                   
                    if(debug)Console.WriteLine("Packet data : " + dataContent);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }


        public static void ReadEegPacket(Stream stm, int packetType, int packetLength, bool debug=true)
        {
            if (packetType == 1)
            {
                if (debug)  Console.WriteLine("Reading Eeg");
                byte[] data = new byte[packetLength];
                int ret = stm.Read(data, 0, packetLength);
                try
                {
                    float timestamp = ReadSingle(data, 0, false); //BitConverter.ToSingle(data, 0);
                    int dataCounter = data[4] + data[5] * 255 + data[6] * 255 * 255; // sur 3 bytes ?!?

                    UInt32 ADCStatus = BitConverter.ToUInt32(data, 6);
                    
                    //Channels reading !

                    float[] channels = new float[25]; //Default, we have 25 channels
                    string dataContent = "";
                    dataContent = "[" + timestamp.ToString(CultureInfo.InvariantCulture) + "," + dataCounter.ToString(CultureInfo.InvariantCulture) + "," + ADCStatus.ToString(CultureInfo.InvariantCulture) + "]";

                    dataContent += "\n";
                    for (int i = 0; i < 25; i++)
                    {
                        int index = 11 + i * sizeof(float);
                        if (index + sizeof(float) > data.Length)
                        {
                            Console.WriteLine("Something strange append : bufer length is less than 25 channels ?!? ");
                            channels[i] = 0;                            
                        }
                        else
                        {
                            channels[i] = ReadSingle(data, index, false);                           
                        }

                        dataContent += "CH : " + i + " : " + channels[i].ToString(CultureInfo.InvariantCulture) + "\n";
                    }

                    if(debug) Console.WriteLine("Packet data : " + dataContent);
                }
                catch (System.Exception ex)
                {
                    if (debug) Console.WriteLine(ex.Message);
                }
            }
        }

        public static void ReadEventPackect(Stream stm, int packetType, int packetLength, bool debug)
        {
            if (packetType == 5)
            {
                if (debug) Console.WriteLine("Reading Event");

                byte[]  data = new byte[packetLength];
                int ret = stm.Read(data, 0, packetLength);
                uint eventcode = data[3];
                uint sendingnode = data[7];

                string content = BitConverter.ToString(data);
                string evt = System.Text.Encoding.ASCII.GetString(data);

                if (eventcode == 9)
                {
                    if(debug)
                        Console.WriteLine("Here is the map : " + evt.Substring(12));
                }

                if(eventcode == 10)
                {
                    if (debug) 
                        Console.WriteLine("Here are the frequencies : " + evt.Substring(12));
                }

                if(eventcode == 1)
                {
                    if (debug) 
                        Console.WriteLine("Here is the greetings mesage : " + evt.Substring(12));
                }
               
            }
        }

        //Big endian / little endian handlers !

        public static float ReadSingleBigEndian(byte[] data, int offset)
        {
            return ReadSingle(data, offset, false);
        }
        public static float ReadSingleLittleEndian(byte[] data, int offset)
        {
            return ReadSingle(data, offset, true);
        }
        private static float ReadSingle(byte[] data, int offset, bool littleEndian)
        {
            if (BitConverter.IsLittleEndian != littleEndian)
            {   // other-endian; reverse this portion of the data (4 bytes)
                byte tmp = data[offset];
                data[offset] = data[offset + 3];
                data[offset + 3] = tmp;
                tmp = data[offset + 1];
                data[offset + 1] = data[offset + 2];
                data[offset + 2] = tmp;
            }
            return BitConverter.ToSingle(data, offset);
        }

    }
}