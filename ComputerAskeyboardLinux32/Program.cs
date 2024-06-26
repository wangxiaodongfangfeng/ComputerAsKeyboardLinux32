﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using PowerArgs;
using ComputerAsKeyboardLinux32;

public static class Program
{
    //initialize keylayout
    private const string KeyboardLayout = ThinkpadKeyLayout.KeyboardLayoutString;

    private static bool toggle = true;
    private static bool mute = false;
    private static bool switch_alt = false;
    private static bool device_disconnected = false;
    private static bool exit_in_next = false;
    private static IKeyboard _keyboard;
    private static bool _fingerPrint = false;
    private static bool CommandMode { get; set; }

    public static string Password { get; set; }

    private static readonly Dictionary<EventCode, byte> SpecialKeyMap = new Dictionary<EventCode, byte>();

    private static readonly Dictionary<EventCode, bool> SpecialKeyStatus = new Dictionary<EventCode, bool>();

    private static bool _bluetooth = false;

    private static string _currentPort = "/dev/ttyUSB0";

    private static byte[] _keyslots = new byte[6];

    private static int ControlBytes
    {
        get
        {
            var value = SpecialKeyStatus
                .Where(specialKeyStatue => specialKeyStatue.Value)
                .Aggregate(0x00,
                    (current, specialKeyStatue) => (int)(current | SpecialKeyMap[specialKeyStatue.Key]));
            return value;
        }
    }

    private static bool IsSpecialKey(EventCode eventCode)
    {
        return SpecialKeyMap.ContainsKey(eventCode);
    }

    public static void Main(string[] args)
    {
        SpecialKeyMap.Add(EventCode.RightMeta, 0x80);
        SpecialKeyMap.Add(EventCode.RightAlt, 0x40);
        SpecialKeyMap.Add(EventCode.RightShift, 0x20);
        SpecialKeyMap.Add(EventCode.RightCtrl, 0x10);
        SpecialKeyMap.Add(EventCode.LeftMeta, 0x08);
        SpecialKeyMap.Add(EventCode.LeftAlt, 0x04);
        SpecialKeyMap.Add(EventCode.LeftShift, 0x02);
        SpecialKeyMap.Add(EventCode.LeftCtrl, 0x01);


        var thinkpadKey = new ThinkpadKeyMapTo9329();
        var thinkpadLayout = new ThinkpadKeyLayout();
        Password = "Xinyuan@199109062337";

        var choosedDevice = "/dev/ttyUSB0";
        // Path to the directory where ttyUSB devices are located
        var ttyUsbDirectory = "/dev/";
        var mouseDevice = "mouse0";
        var bluetooth = false;

        StartArgs parsedArgs;

        try
        {
            parsedArgs = Args.Parse<StartArgs>(args);
            choosedDevice = parsedArgs.Device;
            ttyUsbDirectory = parsedArgs.ScanPath;
            mute = !parsedArgs.Verbose;
            switch_alt = parsedArgs.MacOS;
            mouseDevice = parsedArgs.Mouse;
            bluetooth = parsedArgs.Bluetooth;
            _fingerPrint = parsedArgs.Fingerprint;
        }
        catch (ArgException ex)
        {
            WriteLogOnScreen(ex.Message);
            //Console.WriteLine(ArgUsage.GetUsage<StartArgs>());
            return;
        }

        ThinkpadKeyLayout.WriteKeyboardOnScreen();

        var chars = ThinkpadKeyLayout.KeyLayoutChars;

        Console.TreatControlCAsInput = true;
        using (AggregateInputReader aggHandler1 = new AggregateInputReader())
        {
            #region ToggleKeyImplementation

            aggHandler1.OnKeyPress += (e) =>
            {
                if (MenuHandler.CommandMode) return;
                if (e.State == KeyState.KeyUp)
                {
                    ThinkpadKeyLayout.ToggleKeys(chars, thinkpadLayout.FindKeyPositions(e.Code));
                }
            };

            #endregion

            #region AutoScan Region

            if (parsedArgs.AutoScan)
            {
                try
                {
                    // Get a list of all files in the /dev/ directory
                    var devices = Directory.GetFiles(ttyUsbDirectory);

                    // Filter the list to only include ttyUSB devices
                    var ttyUsbDevices = Directory.GetFiles(ttyUsbDirectory)
                        .Where(device => device.StartsWith("/dev/ttyUSB", StringComparison.Ordinal) ||
                                         device.StartsWith("/dev/rfcomm", StringComparison.Ordinal)).ToList();


                    if (ttyUsbDevices.Count <= 0)
                    {
                        WriteLogOnScreen("TTY Devices are not available, please plug in your device and try again");
                        return;
                    }

                    //if only one device is available ,choose it directly.
                    if (ttyUsbDevices.Count == 1)
                    {
                        choosedDevice = ttyUsbDevices[0];
                    }
                    else
                    {
                        // Output the list of ttyUSB devices
                        WriteLogOnScreen(
                            "There are more than one device in the folder, please specify the device you want to use by command");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur
                    WriteLogOnScreen(string.Format("An error occurred: {0}", ex.Message));
                }
            }

            #endregion

            WriteLogOnScreen(String.Format("device is {0}", choosedDevice));

            using (var aggHandler = new AggregateInputReader())
            {
                if (File.Exists(choosedDevice))
                {
                    _keyboard = GenerateKeyboard(bluetooth, choosedDevice);
                    _bluetooth = bluetooth;
                    _currentPort = choosedDevice;
                }

                #region WatchFileChange auto detect serial port

                var watcher = new FileSystemWatcher(ttyUsbDirectory)
                {
                    NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime |
                                   NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite |
                                   NotifyFilters.Security | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                watcher.Created += (sender, e) =>
                {
                    WriteLogOnScreen(string.Format("File created:{0}", e.FullPath));
                    try
                    {
                        if (!(e.FullPath.Contains("ttyUSB") || e.FullPath.Contains("rfcomm"))) return;
                        _keyboard.Dispose();
                        _keyboard = GenerateKeyboard(bluetooth, e.FullPath);
                        _bluetooth = bluetooth;
                        _currentPort = e.FullPath;
                        WriteLogOnScreen(String.Format("ConnectToAnother:{0}", e.FullPath));
                        device_disconnected = false;
                    }
                    catch (Exception ex)
                    {
                        WriteLogOnScreen(ex.Message);
                    }
                };
                watcher.Deleted += (sender, e) =>
                {
                    WriteLogOnScreen(string.Format("File Deleted:{0}", e.FullPath));
                    if (e.FullPath != choosedDevice) return;
                    WriteLogOnScreen("Device is removed , can't use now");
                    device_disconnected = true;
                };

                #endregion

                aggHandler.OnKeyPress += (KeyPressEvent e) =>
                {
                    if (!mute)
                    {
                        WriteLogOnScreen(String.Format("Code:{0} State:{1},Event:{2}", e.Code, e.State,
                            e.DevicePath));
                    }

                    // take no action when toggle is off
                    if (!toggle || device_disconnected)
                    {
                        return;
                    }

                    var keyCode = e.Code;
                    //MacOS use back and forward to switch screen
                    if ((e.Code == EventCode.Back || e.Code == EventCode.Forward) && switch_alt)
                    {
                        HandleBackAndFowardForMacOS(keyCode, e.State);
                        return;
                    }
                    //MacOS use Compose to show menu
                    if (e.Code == EventCode.Compose && switch_alt)
                    {

                    }

                    if (e.Code == EventCode.LeftMouse || e.Code == EventCode.RightMouse ||
                        e.Code == EventCode.MiddleMouse)
                    {
                        HandleMouseKey(e, switch_alt);
                        return;
                    }

                    //When I was working in MacOS ,I should swith the left alt and meta
                    //TODO: we should refator this code, because it looks ugly now.
                    if (switch_alt && (keyCode == EventCode.LeftMeta || keyCode == EventCode.LeftAlt))
                    {
                        keyCode = keyCode == EventCode.LeftMeta ? EventCode.LeftAlt : EventCode.LeftMeta;
                    }

                    if (e.State == KeyState.KeyDown || e.State == KeyState.KeyHold)
                    {
                        #region HandleAutoInputPassword When Ctrl+F1 and Ctrl + F2 Happen

                        switch (e.Code)
                        {
                            case EventCode.F1 when ControlBytes == 0x01:
                                HandleInputPassword();
                                return;
                            case EventCode.F10 when ControlBytes == 0x01:
                                HandleRefreshKeyboard();
                                return;
                        }

                        #endregion

                        byte keyByte = 0;
                        if (thinkpadKey.keyMaps.TryGetValue((int)keyCode, out keyByte))
                        {
                            //if don't have duplicated key,find a new slot
                            if (!_keyslots.Contains(keyByte))
                            {
                                var index = _keyslots.ToList().FindIndex(key => key == 0);
                                _keyslots[index] = keyByte;
                            }
                            _keyboard.keyDown(KeyGroup.CharKey, 0x00, _keyslots[0], _keyslots[1], _keyslots[2], _keyslots[3], _keyslots[4], _keyslots[5]);
                            //WriteLogOnScreen(string.Format("{0},{1},{3},{4},{5}", _keyslots[0], _keyslots[1], _keyslots[2], _keyslots[3], _keyslots[4], _keyslots[5]));
                        }
                        List<byte> mediaKeyByte;
                        if (thinkpadKey.mediaKeyMap.TryGetValue((int)keyCode, out mediaKeyByte))
                        {
                            _keyboard.keyDown(KeyGroup.MediaKey, mediaKeyByte[0], mediaKeyByte[1], mediaKeyByte[2], mediaKeyByte[3]);
                        }
                        if (IsSpecialKey(keyCode))
                        {
                            SpecialKeyStatus[keyCode] = true;
                        }
                    }
                    else
                    {
                        byte keyByte;
                        if (thinkpadKey.keyMaps.TryGetValue((int)keyCode, out keyByte))
                        {
                            var index = _keyslots.ToList().FindIndex(key => key == keyByte);
                            _keyslots[index] = 0;
                            _keyboard.keyDown(KeyGroup.CharKey, 0x00, _keyslots[0], _keyslots[1], _keyslots[2], _keyslots[3], _keyslots[4], _keyslots[5]);
                            //WriteLogOnScreen(string.Format("{0},{1},{3},{4},{5}", _keyslots[0], _keyslots[1], _keyslots[2], _keyslots[3], _keyslots[4], _keyslots[5]));
                        }
                        List<byte> mediaKeyByte;
                        if (thinkpadKey.mediaKeyMap.TryGetValue((int)keyCode, out mediaKeyByte))
                        {
                            _keyboard.keyDown(KeyGroup.MediaKey, 0x02, 0, 0, 0);
                        }
                        if (IsSpecialKey(keyCode))
                        {
                            SpecialKeyStatus[keyCode] = false;
                        }
                    }
                };

                #region handle toggle

                //handle toggle event ,sometime ,we want to turn off the keyboard
                aggHandler.OnKeyPress += (e) =>
                {
                    if (e.Code != EventCode.Prog1 || e.State != KeyState.KeyUp) return;
                    toggle = !toggle;
                    WriteLogOnScreen(String.Format("Toggle is {0} now", (toggle ? "on" : "off")));
                };

                #endregion

                #region handle mute

                //handle mute event, we don't like log to be printed
                aggHandler.OnKeyPress += (e) =>
                {
                    if (e.Code != EventCode.Mute || e.State != KeyState.KeyUp) return;
                    mute = !mute;
                    WriteLogOnScreen(String.Format("Log is {0} now", (mute ? "on" : "off")));
                };
                MenuHandler.BeforeExitApplication = () => { _keyboard?.keyUpAll(); };
                // long touch fn will show menu;
                int lastCodeCount = 0;
                aggHandler.OnKeyPress += (e) =>
                {
                    if (e.Code != EventCode.Wakeup)
                    {
                        lastCodeCount = 0;
                        return;
                    }
                    else if (e.State == KeyState.KeyDown || e.State == KeyState.KeyHold)
                    {
                        lastCodeCount++;
                        if (lastCodeCount > 20)
                        {
                            MenuHandler.StartMenu();
                            lastCodeCount = 0;
                        }
                    }
                    else
                    {
                        lastCodeCount = 0;
                    }
                };
                #endregion

                var mouseReader = new MouseReader(String.Format("/dev/input/{0}", mouseDevice));
                mouseReader.OnMouseMove += (e) =>
                {
                    if (_keyboard == null) return;
                    if (MouseKeyHold)
                    {
                        _keyboard.mouseMoveRel(e.X, e.Y, true, HoldMouseKey);
                    }
                    else
                    {
                        _keyboard.mouseMoveRel(e.X, e.Y);
                    }
                };

                mouseReader.OnMouseScroll += (e) =>
                {
                    if (_keyboard == null) return;
                    _keyboard.mouseScrollForMac(e.ScrollCount);
                };

                System.Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    if (_keyboard == null) return;
                    _keyboard.keyUpAll();
                };
                while (true)
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    if (exit_in_next)
                    {
                        break;
                    }
                }

                _keyboard.keyUpAll();
            }
        }
    }

    /// <summary>
    /// if the Mousekey is Hold now
    /// </summary>
    private static bool MouseKeyHold { get; set; }

    private static MouseButtonCode HoldMouseKey { get; set; }

    private static void HandleMouseKey(KeyPressEvent e, bool macos)
    {
        if (_keyboard == null)
            return;

        MouseButtonCode mouse = MouseButtonCode.LEFT;
        switch (e.Code)
        {
            case EventCode.LeftMouse:
                mouse = MouseButtonCode.LEFT;
                break;
            case EventCode.RightMouse:
                mouse = MouseButtonCode.RIGHT;
                break;
            case EventCode.MiddleMouse:
                mouse = MouseButtonCode.MIDDLE;
                break;
        }

        if (e.State == KeyState.KeyDown || e.State == KeyState.KeyHold)
        {
            if (macos)
            {
                _keyboard.mouseButtonDownForMac(mouse);
            }
            else
            {
                _keyboard.mouseButtonDown(mouse);
            }

            MouseKeyHold = true;
            HoldMouseKey = mouse;
        }
        else
        {
            if (macos)
            {
                _keyboard.mouseButtonUpAllForMac();
            }
            else
            {
                _keyboard.mouseButtonUpAll();
            }

            MouseKeyHold = false;
            HoldMouseKey = mouse;
        }

        return;
    }

    /// <summary>
    /// if the system is macos ,I can use forward and backword
    /// to mimic the desktop switch shortcut.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="keyState"></param>
    private static void HandleBackAndFowardForMacOS(EventCode code, KeyState keyState)
    {
        if (_keyboard == null)
            return;
        if (keyState == KeyState.KeyDown)
        {
            var value = code == EventCode.Back ? 0x50 : 0x4F;
            //send Ctrl+<- or Ctrl + ->
            _keyboard.keyDown(KeyGroup.CharKey, (byte)0x01, (byte)value);
        }
        else
        {
            _keyboard.keyUpAll();
        }
    }

    private static readonly Queue<string> Logs = new Queue<string>();

    public static void WriteLogOnScreen(string log)
    {
        if (MenuHandler.CommandMode) return;
        if (Logs.Count >= 10)
        {
            Logs.Dequeue();
        }

        Logs.Enqueue(log);
        var index = 0;
        foreach (var content in Logs)
        {
            Console.SetCursorPosition(ThinkpadKeyLayout.StartColumn, 28 + (++index));
            Console.WriteLine(content);
        }
    }

    private static IKeyboard GenerateKeyboard(bool bluetooth, string port)
    {
        return bluetooth ? (IKeyboard)new BTK05(port) : (IKeyboard)new CH9329(port);
    }

    /// <summary>
    /// RefreshKeyboard
    /// Sometime when the computer wake up from sleep
    /// bluetooth will be disconnected.
    /// we should reconnect the bluetooth by reopen the port
    /// </summary>
    private static void HandleRefreshKeyboard()
    {
        _keyboard.Dispose();
        _keyboard = GenerateKeyboard(_bluetooth, _currentPort);
        WriteLogOnScreen(String.Format("Refreshed the keyboard with {0},{1}", _bluetooth, _currentPort));
    }

    private static void HandleInputPassword()
    {
        if (!_fingerPrint)
        {
            if (_keyboard != null)
                _keyboard.charKeyType(Password);
        }
        var attempts = 0;
        var finger = new FingerPrintHelper();
        var matched = false;
        while (attempts < 3)
        {
            matched = FingerPrintHelper.VerifyFinger("ford");
            if (matched)
            {
                break;
            }
            attempts++;
        }

        if (_keyboard != null && matched)
        {
            WriteLogOnScreen("Your fingerprint is matched");
            _keyboard.charKeyType(Password);
        }
        else
        {
            WriteLogOnScreen("Your fingerprint is not matched");
        }
    }
}