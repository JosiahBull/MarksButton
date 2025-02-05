﻿using System.Windows.Forms;
using System;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications; // Notifications library
using WindowsInput;
using WindowsInput.Native;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DesktopNotifications;
using GuidAttribute = System.Runtime.InteropServices.GuidAttribute;
using Windows.Data.Xml.Dom;
using MarksButton.Properties;
using System.Media;

namespace MarksButton
{
    public static class serialHandler
    {
        static SerialPort _serialPort;
        const int BaudRateDefault = 9600;
        public static void startSerial()
        {
            _serialPort = new SerialPort(GetPortName(), BaudRateDefault);
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            _serialPort.Open();
        }
        public static void stopSerial()
        {
            _serialPort.Close();
        }
        private static string GetPortName() //Gets all open ports, pings each one until it gets the expected response and then returns the name of that port.
        {
            string portName = "";
            string[] availablePorts = SerialPort.GetPortNames();
            if (availablePorts.Length == 1)
            {
                portName = availablePorts[0];
            }
            while (portName.Length == 0)
            {
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
                            checkPort.Write("ping!"); //The arduino is looking for this, and should respond with "pong!"
                            string response = checkPort.ReadLine();
                            if (response.Contains("pong!"))
                            {
                                portName = port;
                            }
                        }
                        catch (TimeoutException) { } //Cannot connect to port (timeout), ignore it.
                        checkPort.Close();
                    }
                    catch (UnauthorizedAccessException) { } //Something else is connected to the Serial Port, ignore it.
                }
                availablePorts = SerialPort.GetPortNames();
                Thread.Sleep(200);
            }
            return portName;
        }
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) //The respond handler from the Arduino
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            if (indata.Contains("press")) { //This is sent when the button is pressed down and the LED is turned on.
                notificationHandler.combinationNotify();
            }   
            if (indata.Contains("release")) //This is sent when the button is pressed a second time and the LED is turned off.
            {
                notificationHandler.toggleMusic();
            }
        }
    }
    public static class notificationHandler
    {
        public static void combinationNotify()
        {
            toggleMusic();
            playSound();
            toastNotification();
        }
        private static void playSound()
        {
            new SoundPlayer(Resources.notification).Play();
        }
        private static void toastNotification()
        {
            ToastContent toastContent = new ToastContent()
            {
                Launch = "buttonPressed",
                Audio = new ToastAudio { Silent = true }, //Silent Toast Notification. :)
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = "Button Pressed"
                            },
                            new AdaptiveText()
                            {
                                Text = "Someone wants you. :)"
                            }
                        }
                    }
                }
            };
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());
            var toast = new ToastNotification(doc);
            DesktopNotificationManagerCompat.CreateToastNotifier().Show(toast); //Spawn me a toast notification.
        }
        public static void toggleMusic() //Simulates a keyboard press on the media play/pause keys. This isn't particuarly intelligent, so if there isn't music playing when the button is pressed it will play music...
        {
            InputSimulator _inputSimulator = new InputSimulator();
            _inputSimulator.Keyboard.KeyPress(VirtualKeyCode.MEDIA_PLAY_PAUSE);
        }
    }
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("2936e1ba-8488-429d-bbc4-9baf6ae02bbf"), ComVisible(true)]
    public class MyNotificationActivator : NotificationActivator
    {
        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId) //What to do when the toast notification is clicked.
        {
            DesktopNotificationManagerCompat.History.Clear(); //Clear this and all previous notifications from the notification bar.
            ;
        }
    }
    public class hiddenStartApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public hiddenStartApplicationContext() //Starting in the tray.
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.iconSmall,
                Text = "MarksButton",
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };
        }
        void Exit(object sender, EventArgs e) //How to close the application.
        {
            trayIcon.Visible = false;
            serialHandler.stopSerial();
            Application.Exit();
        }

    }
    static class Program
    {
        [STAThread]
        static void Main() //The entry point for the program.
        {
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<MyNotificationActivator>("JB.MarksButton");
            DesktopNotificationManagerCompat.RegisterActivator<MyNotificationActivator>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            serialHandler.startSerial();
            Application.Run(new hiddenStartApplicationContext());
        }
    }
}
namespace DesktopNotifications //This boi was copy-pasted from Microsofts site.
{
    public class DesktopNotificationManagerCompat
    {
        public const string TOAST_ACTIVATED_LAUNCH_ARG = "-ToastActivated";

        private static bool _registeredAumidAndComServer;
        private static string _aumid;
        private static bool _registeredActivator;

        /// <summary>
        /// If not running under the Desktop Bridge, you must call this method to register your AUMID with the Compat library and to
        /// register your COM CLSID and EXE in LocalServer32 registry. Feel free to call this regardless, and we will no-op if running
        /// under Desktop Bridge. Call this upon application startup, before calling any other APIs.
        /// </summary>
        /// <param name="aumid">An AUMID that uniquely identifies your application.</param>
        public static void RegisterAumidAndComServer<T>(string aumid)
            where T : NotificationActivator
        {
            if (string.IsNullOrWhiteSpace(aumid))
            {
                throw new ArgumentException("You must provide an AUMID.", nameof(aumid));
            }

            // If running as Desktop Bridge
            if (DesktopBridgeHelpers.IsRunningAsUwp())
            {
                // Clear the AUMID since Desktop Bridge doesn't use it, and then we're done.
                // Desktop Bridge apps are registered with platform through their manifest.
                // Their LocalServer32 key is also registered through their manifest.
                _aumid = null;
                _registeredAumidAndComServer = true;
                return;
            }

            _aumid = aumid;

            String exePath = Process.GetCurrentProcess().MainModule.FileName;
            RegisterComServer<T>(exePath);

            _registeredAumidAndComServer = true;
        }

        private static void RegisterComServer<T>(String exePath)
            where T : NotificationActivator
        {
            // We register the EXE to start up when the notification is activated
            string regString = String.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}\\LocalServer32", typeof(T).GUID);
            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regString);

            // Include a flag so we know this was a toast activation and should wait for COM to process
            // We also wrap EXE path in quotes for extra security
            key.SetValue(null, '"' + exePath + '"' + " " + TOAST_ACTIVATED_LAUNCH_ARG);
        }

        /// <summary>
        /// Registers the activator type as a COM server client so that Windows can launch your activator.
        /// </summary>
        /// <typeparam name="T">Your implementation of NotificationActivator. Must have GUID and ComVisible attributes on class.</typeparam>
        public static void RegisterActivator<T>()
            where T : NotificationActivator
        {
            // Register type
            var regService = new RegistrationServices();

            regService.RegisterTypeForComClients(
                typeof(T),
                RegistrationClassContext.LocalServer,
                RegistrationConnectionType.MultipleUse);

            _registeredActivator = true;
        }

        /// <summary>
        /// Creates a toast notifier. You must have called <see cref="RegisterActivator{T}"/> first (and also <see cref="RegisterAumidAndComServer(string)"/> if you're a classic Win32 app), or this will throw an exception.
        /// </summary>
        /// <returns></returns>
        public static ToastNotifier CreateToastNotifier()
        {
            EnsureRegistered();

            if (_aumid != null)
            {
                // Non-Desktop Bridge
                return ToastNotificationManager.CreateToastNotifier(_aumid);
            }
            else
            {
                // Desktop Bridge
                return ToastNotificationManager.CreateToastNotifier();
            }
        }

        /// <summary>
        /// Gets the <see cref="DesktopNotificationHistoryCompat"/> object. You must have called <see cref="RegisterActivator{T}"/> first (and also <see cref="RegisterAumidAndComServer(string)"/> if you're a classic Win32 app), or this will throw an exception.
        /// </summary>
        public static DesktopNotificationHistoryCompat History
        {
            get
            {
                EnsureRegistered();

                return new DesktopNotificationHistoryCompat(_aumid);
            }
        }

        private static void EnsureRegistered()
        {
            // If not registered AUMID yet
            if (!_registeredAumidAndComServer)
            {
                // Check if Desktop Bridge
                if (DesktopBridgeHelpers.IsRunningAsUwp())
                {
                    // Implicitly registered, all good!
                    _registeredAumidAndComServer = true;
                }

                else
                {
                    // Otherwise, incorrect usage
                    throw new Exception("You must call RegisterAumidAndComServer first.");
                }
            }

            // If not registered activator yet
            if (!_registeredActivator)
            {
                // Incorrect usage
                throw new Exception("You must call RegisterActivator first.");
            }
        }

        /// <summary>
        /// Gets a boolean representing whether http images can be used within toasts. This is true if running under Desktop Bridge.
        /// </summary>
        public static bool CanUseHttpImages { get { return DesktopBridgeHelpers.IsRunningAsUwp(); } }

        /// <summary>
        /// Code from https://github.com/qmatteoq/DesktopBridgeHelpers/edit/master/DesktopBridge.Helpers/Helpers.cs
        /// </summary>
        private class DesktopBridgeHelpers
        {
            const long APPMODEL_ERROR_NO_PACKAGE = 15700L;

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

            private static bool? _isRunningAsUwp;
            public static bool IsRunningAsUwp()
            {
                if (_isRunningAsUwp == null)
                {
                    if (IsWindows7OrLower)
                    {
                        _isRunningAsUwp = false;
                    }
                    else
                    {
                        int length = 0;
                        StringBuilder sb = new StringBuilder(0);
                        int result = GetCurrentPackageFullName(ref length, sb);

                        sb = new StringBuilder(length);
                        result = GetCurrentPackageFullName(ref length, sb);

                        _isRunningAsUwp = result != APPMODEL_ERROR_NO_PACKAGE;
                    }
                }

                return _isRunningAsUwp.Value;
            }

            private static bool IsWindows7OrLower
            {
                get
                {
                    int versionMajor = Environment.OSVersion.Version.Major;
                    int versionMinor = Environment.OSVersion.Version.Minor;
                    double version = versionMajor + (double)versionMinor / 10;
                    return version <= 6.1;
                }
            }
        }
    }

    /// <summary>
    /// Manages the toast notifications for an app including the ability the clear all toast history and removing individual toasts.
    /// </summary>
    public sealed class DesktopNotificationHistoryCompat
    {
        private string _aumid;
        private ToastNotificationHistory _history;

        /// <summary>
        /// Do not call this. Instead, call <see cref="DesktopNotificationManagerCompat.History"/> to obtain an instance.
        /// </summary>
        /// <param name="aumid"></param>
        internal DesktopNotificationHistoryCompat(string aumid)
        {
            _aumid = aumid;
            _history = ToastNotificationManager.History;
        }

        /// <summary>
        /// Removes all notifications sent by this app from action center.
        /// </summary>
        public void Clear()
        {
            if (_aumid != null)
            {
                _history.Clear(_aumid);
            }
            else
            {
                _history.Clear();
            }
        }

        /// <summary>
        /// Gets all notifications sent by this app that are currently still in Action Center.
        /// </summary>
        /// <returns>A collection of toasts.</returns>
        public IReadOnlyList<ToastNotification> GetHistory()
        {
            return _aumid != null ? _history.GetHistory(_aumid) : _history.GetHistory();
        }

        /// <summary>
        /// Removes an individual toast, with the specified tag label, from action center.
        /// </summary>
        /// <param name="tag">The tag label of the toast notification to be removed.</param>
        public void Remove(string tag)
        {
            if (_aumid != null)
            {
                _history.Remove(tag, string.Empty, _aumid);
            }
            else
            {
                _history.Remove(tag);
            }
        }

        /// <summary>
        /// Removes a toast notification from the action using the notification's tag and group labels.
        /// </summary>
        /// <param name="tag">The tag label of the toast notification to be removed.</param>
        /// <param name="group">The group label of the toast notification to be removed.</param>
        public void Remove(string tag, string group)
        {
            if (_aumid != null)
            {
                _history.Remove(tag, group, _aumid);
            }
            else
            {
                _history.Remove(tag, group);
            }
        }

        /// <summary>
        /// Removes a group of toast notifications, identified by the specified group label, from action center.
        /// </summary>
        /// <param name="group">The group label of the toast notifications to be removed.</param>
        public void RemoveGroup(string group)
        {
            if (_aumid != null)
            {
                _history.RemoveGroup(group, _aumid);
            }
            else
            {
                _history.RemoveGroup(group);
            }
        }
    }

    /// <summary>
    /// Apps must implement this activator to handle notification activation.
    /// </summary>
    public abstract class NotificationActivator : NotificationActivator.INotificationActivationCallback
    {
        public void Activate(string appUserModelId, string invokedArgs, NOTIFICATION_USER_INPUT_DATA[] data, uint dataCount)
        {
            OnActivated(invokedArgs, new NotificationUserInput(data), appUserModelId);
        }

        /// <summary>
        /// This method will be called when the user clicks on a foreground or background activation on a toast. Parent app must implement this method.
        /// </summary>
        /// <param name="arguments">The arguments from the original notification. This is either the launch argument if the user clicked the body of your toast, or the arguments from a button on your toast.</param>
        /// <param name="userInput">Text and selection values that the user entered in your toast.</param>
        /// <param name="appUserModelId">Your AUMID.</param>
        public abstract void OnActivated(string arguments, NotificationUserInput userInput, string appUserModelId);

        // These are the new APIs for Windows 10
        #region NewAPIs
        [StructLayout(LayoutKind.Sequential), Serializable]
        public struct NOTIFICATION_USER_INPUT_DATA
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Key;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string Value;
        }

        [ComImport,
        Guid("53E31837-6600-4A81-9395-75CFFE746F94"), ComVisible(true),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface INotificationActivationCallback
        {
            void Activate(
                [In, MarshalAs(UnmanagedType.LPWStr)]
            string appUserModelId,
                [In, MarshalAs(UnmanagedType.LPWStr)]
            string invokedArgs,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
            NOTIFICATION_USER_INPUT_DATA[] data,
                [In, MarshalAs(UnmanagedType.U4)]
            uint dataCount);
        }
        #endregion
    }

    /// <summary>
    /// Text and selection values that the user entered on your notification. The Key is the ID of the input, and the Value is what the user entered.
    /// </summary>
    public class NotificationUserInput : IReadOnlyDictionary<string, string>
    {
        private NotificationActivator.NOTIFICATION_USER_INPUT_DATA[] _data;

        internal NotificationUserInput(NotificationActivator.NOTIFICATION_USER_INPUT_DATA[] data)
        {
            _data = data;
        }

        public string this[string key] => _data.First(i => i.Key == key).Value;

        public IEnumerable<string> Keys => _data.Select(i => i.Key);

        public IEnumerable<string> Values => _data.Select(i => i.Value);

        public int Count => _data.Length;

        public bool ContainsKey(string key)
        {
            return _data.Any(i => i.Key == key);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _data.Select(i => new KeyValuePair<string, string>(i.Key, i.Value)).GetEnumerator();
        }

        public bool TryGetValue(string key, out string value)
        {
            foreach (var item in _data)
            {
                if (item.Key == key)
                {
                    value = item.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}