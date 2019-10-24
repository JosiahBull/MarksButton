using System;
using System.IO.Ports;
using System.Media;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Windows.UI.Notifications;

namespace MarksButton
{

    public class notificationHandler
    {
        public static void play() 
        { 
            new SoundPlayer(Testing.Properties.Resources.notification).Play(); 
        }
        private void ShowToastNotification(string title, string stringContent)
        {
            ToastNotifier ToastNotifier = ToastNotificationManager.CreateToastNotifier();
            Windows.Data.Xml.Dom.XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            Windows.Data.Xml.Dom.XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
            toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
            toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(stringContent));
            Windows.Data.Xml.Dom.IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            Windows.Data.Xml.Dom.XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:Notification.SMS");

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTime.Now.AddSeconds(4);
            ToastNotifier.Show(toast);
        }
    }
    public class musicHandler
    {
        static bool _musicOn;
        static InputSimulator _inputSimulator = new InputSimulator();
        public static void toggle()
        {
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PLAY_PAUSE);
        }
    }
    public class PortChat
    {
        static SerialPort _serialPort;
        const int BaudRateDefault = 9600;
        public static void Main()
        {
            Thread.Sleep(2000);


            //_serialport = new serialport(getportname(), baudratedefault);
            //_serialport.readtimeout = 500;
            //_serialport.writetimeout = 500;
            //_serialport.datareceived += new serialdatareceivedeventhandler(datareceivedhandler);
            //_serialport.open();
            Console.WriteLine("Button succesfully instantiated. Press any key to continue... ");
            Console.ReadKey();
            //_serialport.close();
        }
        public static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            Console.WriteLine("Data Received: ");
            Console.Write(indata);
            notificationHandler.play();
        }
        // Get the correct port.
        public static string GetPortName()
        {
            string portName = "";
            string[] availablePorts = SerialPort.GetPortNames();
            if (availablePorts.Length == 1)
            {
                portName = availablePorts[0];
            }
            while (portName == "")
            {
                Console.WriteLine("Checking for new connections!");
                foreach (string port in availablePorts)
                {
                    SerialPort checkPort = new SerialPort(port, BaudRateDefault);
                    checkPort.ReadTimeout = 2000;
                    checkPort.WriteTimeout = 2000;
                    try
                    {
                        checkPort.Open();
                        try
                        {
                            checkPort.Write("ping!");
                            string response = checkPort.ReadLine();
                            if (response.Contains("pong!"))
                            {
                                portName = port;
                            }
                            else
                            {
                                Console.WriteLine("I received: " + response);
                            }
                        }
                        catch (TimeoutException)
                        {
                            Console.WriteLine("Port: " + port + " timed out.");
                        }
                        checkPort.Close();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("Port: " + port + " is unavailable.");
                    }

                }
                availablePorts = SerialPort.GetPortNames();
                Thread.Sleep(200);
            }
            Console.WriteLine("Suitable connection found!");
            return portName;
        }

    }
}
