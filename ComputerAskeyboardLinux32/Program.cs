using System.Collections.Concurrent;
using ComputerAsKeyboardInterface.FingerPrint;
using ComputerAsKeyboardInterface.KeyboardRelated;
using ComputerAsKeyboardInterface.MouseRelated;
using PowerArgs;

namespace ComputerAsKeyboardInterface;

public static class Program
{
    //initialize key-layout
    private static bool _toggle = true;
    private static bool _mute = false;
    private static bool _switchAlt = false;
    private static bool _deviceDisconnected = false;
    public static bool ExitInNext { get; set; } = false;
    private static IKeyboard? _keyboard;
    private static bool _fingerPrint = false;
    private static bool CommandMode { get; set; }

    public static string? Password { get; set; }

    private static readonly Dictionary<EventCode, byte> SpecialKeyMap = new();

    private static readonly Dictionary<EventCode, bool> SpecialKeyStatus = new();
    private static bool _bluetooth = false;
    private static readonly byte[] KeySlots = new byte[6];
    public static bool UseQueue { get; private set; } = false;

    public static List<string>? InputDevices { get; private set; } = [];

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

    private static void InitPassword()
    {
        Password = "Xinyuan@199109062337";
        if (File.Exists(".password"))
            Password = File.ReadAllText(".password");
    }

    private static void LoadDevicesMapping()
    {
        DeviceResolver.GetInputDevicesFromProc();
        if (!File.Exists(".devices")) return;
        var devices = File.ReadAllLines(".devices");
        InputDevices = devices.Select(c => DeviceResolver.InputDevicesMapping[c]).ToList();
    }

    private static void WithKeyTogglingOnScreen(this AggregateInputReader reader)
    {
        var thinkpadLayout = new ThinkpadKeyLayout();
        var chars = ThinkpadKeyLayout.KeyLayoutChars;
        reader.OnKeyPress += async (e) =>
        {
            if (MenuHandler.CommandMode) return;
            if (e.State == KeyState.KeyUp)
            {
                await ThinkpadKeyLayout.ToggleKeys(chars, thinkpadLayout.FindKeyPositions(e.Code));
            }
        };
    }

    private static string? AutoScanAvailablePortDevice()
    {
        try
        {
            // Filter the list to only include ttyUSB devices
            var ttyUsbDevices = Directory.GetFiles(_ttyUsbDirectory)
                .Where(device => device.StartsWith("/dev/ttyUSB", StringComparison.Ordinal) ||
                                 device.StartsWith("/dev/rfcomm", StringComparison.Ordinal)).ToList();


            switch (ttyUsbDevices.Count)
            {
                case <= 0:
                    WriteLogOnScreen("TTY Devices are not available, please plug in your device and try again");
                    return null;
                default:
                    ttyUsbDevices.ForEach(SerialPortExtension.AddSerialPort);
                    break;
            }

            return ttyUsbDevices.FirstOrDefault();
        }
        catch (Exception ex)
        {
            // Handle any exceptions that may occur
            WriteLogOnScreen($"An error occurred: {ex.Message}");
            return null;
        }
    }

    private static void WithSerialPortChangeDetection()
    {
        var watcher = new FileSystemWatcher(_ttyUsbDirectory)
        {
            NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime |
                           NotifyFilters.DirectoryName
                           | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite |
                           NotifyFilters.Security | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Created += (sender, e) =>
        {
            WriteLogOnScreen($"File created:{e.FullPath}");
            try
            {
                if (!(e.FullPath.Contains("ttyUSB") || e.FullPath.Contains("rfcomm"))) return;

                SerialPortExtension.AddSerialPort(e.FullPath);
                _chosenDevice = SerialPortExtension.CurrentSerialPort?.PortName;
                WriteLogOnScreen($"ConnectToAnother:{e.FullPath}");
                _deviceDisconnected = false;
            }
            catch (Exception ex)
            {
                WriteLogOnScreen(ex.Message);
            }
        };
        watcher.Deleted += (sender, e) =>
        {
            WriteLogOnScreen($"File Deleted:{e.FullPath}");
            SerialPortExtension.RemoveSerialPort(e.FullPath);
            if (SerialPortExtension.CurrentSerialPort != null) return;
            WriteLogOnScreen("Device is removed , can't use now");
            _deviceDisconnected = true;
        };
    }

    private static void EnableKeyboardSwitch(this AggregateInputReader reader)
    {
        //handle toggle event ,sometime ,we want to turn off the keyboard
        reader.OnKeyPress += (e) =>
        {
            if (e.Code != EventCode.Prog1 || e.State != KeyState.KeyUp) return;
            _toggle = !_toggle;
            WriteLogOnScreen($"Toggle is {(_toggle ? "on" : "off")} now");
        };
    }

    private static void EnableMuteSwitch(this AggregateInputReader reader)
    {
        reader.OnKeyPress += (e) =>
        {
            if (e.Code != EventCode.Mute || e.State != KeyState.KeyUp) return;
            _mute = !_mute;
            WriteLogOnScreen($"Log is {(_mute ? "on" : "off")} now");
        };
    }

    private static bool HandleInitCommandsWhenInit(string[] args)
    {
        if (args.Length == 0 || args[0] != "init") return false;
        Console.WriteLine("Please choose the input device you want to monitor:");
        Console.WriteLine("Multi chosen please use comma to split");
        var inputDevices = new List<string>();
        var index = 0;
        DeviceResolver.InputDevicesMapping.ForEach(kv =>
        {
            Console.WriteLine($"{index++}. {kv.Key}");
            inputDevices.Add(kv.Key);
        });
        var line = Console.ReadLine();
        if (string.IsNullOrEmpty(line)) Console.WriteLine("Invalid input");

        if (line != null && line.Split(',').Any(s => !int.TryParse(s, out _)))
            Console.WriteLine("Invalid input");

        var chosenIndexes = line.Split(',').Select(int.Parse).ToList();

        var chosenList = new List<string>();

        chosenIndexes.ForEach(i =>
        {
            if (i < 0 || i >= inputDevices.Count) return;
            chosenList.Add(inputDevices[i]);
        });

        File.WriteAllLines(".devices", chosenList);

        InputDevices = chosenList.Select(c => DeviceResolver.InputDevicesMapping[c]).ToList();

        return true;
    }

    private static string? _chosenDevice = "/dev/ttyUSB0";

    // Path to the directory where ttyUSB devices are located
    private static string _ttyUsbDirectory = "/dev/";
    private static string _mouseDevice = "mouse0";

    public static void Main(string[] args)
    {
        InitSpecialKeyMap();
        InitPassword();
        LoadDevicesMapping();


        if (HandleInitCommandsWhenInit(args)) return;


        StartArgs parsedArgs;

        try
        {
            parsedArgs = Args.Parse<StartArgs>(args);
            _chosenDevice = parsedArgs.Device;
            _ttyUsbDirectory = parsedArgs.ScanPath;
            _mute = !parsedArgs.Verbose;
            _switchAlt = parsedArgs.MacOs;
            _mouseDevice = parsedArgs.Mouse;
            _bluetooth = parsedArgs.Bluetooth;
            _fingerPrint = parsedArgs.Fingerprint;
            UseQueue = parsedArgs.Queue;
        }
        catch (ArgException ex)
        {
            WriteLogOnScreen(ex.Message);
            return;
        }

        ThinkpadKeyLayout.WriteKeyboardOnScreen();


        Console.TreatControlCAsInput = true;
        using var aggHandler1 = new AggregateInputReader(InputDevices);
        aggHandler1.WithKeyTogglingOnScreen();

        if (parsedArgs.AutoScan)
        {
            _chosenDevice = AutoScanAvailablePortDevice();
            if (_chosenDevice == null) return;
        }
        else
        {
            SerialPortExtension.AddSerialPort(_chosenDevice);
        }

        if (!File.Exists(_chosenDevice)) return;
        WriteLogOnScreen($"device is {_chosenDevice}");
        using var aggHandler = new AggregateInputReader(InputDevices);
        _keyboard = GenerateKeyboard(_bluetooth, _chosenDevice);
        WithSerialPortChangeDetection();

        aggHandler.EnableMainFunction();
        aggHandler.EnableKeyboardSwitch();
        aggHandler.EnableMuteSwitch();
        aggHandler.EnableMenuFunction();
        EnableMouseTrack(_mouseDevice);

        Console.CancelKeyPress += (sender, eventArgs) => { _keyboard?.KeyUpAll(); };
        while (true)
        {
            Console.ReadKey(intercept: true);
            if (ExitInNext) break;
        }

        _keyboard?.KeyUpAll();
    }

    private static bool InterceptSpecialKey(KeyPressEvent e, bool isMacOs)
    {
        var keyCode = e.Code;
        switch (e.Code)
        {
            //MacOS use back and forward to switch screen
            case EventCode.Back or EventCode.Forward when isMacOs:
                if (!KeyboardDisabled) HandleBackAndForwardForMacOs(keyCode, e.State);
                return true;
            //MacOS use Compose to show menu
            case EventCode.Compose when isMacOs:
                break;
            case EventCode.LeftMouse:
            case EventCode.RightMouse:
            case EventCode.MiddleMouse:
            case EventCode.Touch:
                if (!KeyboardDisabled) HandleMouseKey(e, isMacOs);
                return true;
        }

        return false;
    }

    private static bool InterceptSpecialKeyComposite(KeyPressEvent e)
    {
        #region HandleAutoInputPassword When Ctrl+F1 and Ctrl + F2 Happen

        switch (e.Code)
        {
            case EventCode.F1 when ControlBytes == 0x01:
                HandleInputPassword();
                return true;
            case EventCode.F10 when ControlBytes == 0x01:
                HandleRefreshKeyboard();
                return true;
            case EventCode.Num1 when ControlBytes == 0x03:
                SerialPortExtension.SwitchSerialPort(0);
                return true;
            case EventCode.Num2 when ControlBytes == 0x03:
                SerialPortExtension.SwitchSerialPort(1);
                return true;
            case EventCode.Num3 when ControlBytes == 0x03:
                SerialPortExtension.SwitchSerialPort(2);
                return true;
            case EventCode.Tab when ControlBytes == 0x03:
                SerialPortExtension.SwitchSerialPort();
                return true;
            default:
                break;
        }

        #endregion

        return false;
    }

    private static bool KeyboardDisabled => !_toggle || _deviceDisconnected;

    private static void EnableMainFunction(this AggregateInputReader reader)
    {
        var thinkpadKey = new ThinkpadKeyMapTo9329();
        reader.OnKeyPress += (e) =>
        {
            if (!_mute) WriteLogOnScreen($"Code:{e.Code} State:{e.State},Event:{e.DevicePath}");

            // take no action when toggle is off
            //if (!_toggle || _deviceDisconnected) return;

            var result = InterceptSpecialKey(e, _switchAlt);

            if (result) return;
            var keyCode = e.Code;
            //When I was working in MacOS ,I should switch the left alt and meta
            keyCode = _switchAlt ? SwitchMetaAndAlt(keyCode) : keyCode;

            var isPush = e.State is KeyState.KeyDown or KeyState.KeyHold;
            if (isPush)
            {
                if (KeyboardDisabled)
                {
                    result = InterceptSpecialKeyComposite(e);
                    if (result) return;
                }
            }

            if (!KeyboardDisabled)
            {
                SwitchKeySlot(keyCode, isPush, thinkpadKey);
                SwitchMedialKey(keyCode, isPush, thinkpadKey);
            }

            if (IsSpecialKey(keyCode)) SpecialKeyStatus[keyCode] = isPush;
        };
    }

    private static void SwitchMedialKey(EventCode keyCode, bool push, ThinkpadKeyMapTo9329 thinkpadKey)
    {
        if (!thinkpadKey.MediaKeyMap.TryGetValue((int)keyCode, out var mediaKeyByte)) return;
        if (push)
            _keyboard?.KeyDown(KeyGroup.MediaKey, mediaKeyByte[0], mediaKeyByte[1], mediaKeyByte[2], mediaKeyByte[3]);
        else
            _keyboard?.KeyDown(KeyGroup.MediaKey, 0x02, 0, 0, 0);
    }

    private static void SwitchKeySlot(EventCode keyCode, bool push, ThinkpadKeyMapTo9329 thinkpadKey)
    {
        if (!thinkpadKey.KeyMaps.TryGetValue((int)keyCode, out var keyByte)) return;
        if (push)
        {
            //push
            //if we don't have duplicated key,find a new slot
            if (!KeySlots.Contains(keyByte))
            {
                var index = KeySlots.ToList().FindIndex(key => key == 0);
                KeySlots[index] = keyByte;
            }
        }
        else
        {
            //pop
            var index = KeySlots.ToList().FindIndex(key => key == keyByte);
            KeySlots[index] = 0;
        }

        SendCharKeyDown();
    }

    private static void SendCharKeyDown()
    {
        _keyboard?.KeyDown(KeyGroup.CharKey, 0x00, KeySlots[0], KeySlots[1], KeySlots[2], KeySlots[3],
            KeySlots[4], KeySlots[5]);
    }

    private static EventCode SwitchMetaAndAlt(EventCode keyCode)
    {
        if (keyCode is EventCode.LeftMeta or EventCode.LeftAlt)
        {
            keyCode = keyCode == EventCode.LeftMeta ? EventCode.LeftAlt : EventCode.LeftMeta;
        }

        return keyCode;
    }

    private static void EnableMenuFunction(this AggregateInputReader reader)
    {
        MenuHandler.BeforeExitApplication = () => { _keyboard?.KeyUpAll(); };
        // long touch fn will show menu;
        var lastCodeCount = 0;
        reader.OnKeyPress += (e) =>
        {
            if (e.Code != EventCode.Wakeup)
            {
            }
            else if (e.State is KeyState.KeyDown or KeyState.KeyHold)
            {
                lastCodeCount++;
                if (lastCodeCount <= 20) return;
                MenuHandler.StartMenu();
            }

            lastCodeCount = 0;
        };
    }

    private static void EnableMouseTrack(string mouseDevice)
    {
        var mouseReader = new MouseReader($"/dev/input/{mouseDevice}");
        mouseReader.OnMouseMove += (e) =>
            _keyboard?.MouseMoveRel(e.X, e.Y, MouseKeyHold, MouseKeyHold ? HoldMouseKey : MouseButtonCode.Left);
        mouseReader.OnMouseScroll += (e) => { _keyboard?.MouseScrollForMac(e.ScrollCount); };
    }

    private static void InitSpecialKeyMap()
    {
        SpecialKeyMap.Add(EventCode.RightMeta, 0x80);
        SpecialKeyMap.Add(EventCode.RightAlt, 0x40);
        SpecialKeyMap.Add(EventCode.RightShift, 0x20);
        SpecialKeyMap.Add(EventCode.RightCtrl, 0x10);
        SpecialKeyMap.Add(EventCode.LeftMeta, 0x08);
        SpecialKeyMap.Add(EventCode.LeftAlt, 0x04);
        SpecialKeyMap.Add(EventCode.LeftShift, 0x02);
        SpecialKeyMap.Add(EventCode.LeftCtrl, 0x01);
    }

    /// <summary>
    /// if the MouseKey is Hold now
    /// </summary>
    private static bool MouseKeyHold { get; set; }

    private static MouseButtonCode HoldMouseKey { get; set; }

    private static void HandleMouseKey(KeyPressEvent e, bool macos)
    {
        if (_keyboard == null) return;

        var mouse = e.Code switch
        {
            EventCode.LeftMouse => MouseButtonCode.Left,
            EventCode.RightMouse => MouseButtonCode.Right,
            EventCode.MiddleMouse => MouseButtonCode.Middle,
            EventCode.Touch => MouseButtonCode.Left,
            _ => MouseButtonCode.Left
        };

        if (e.State is KeyState.KeyDown or KeyState.KeyHold)
        {
            if (macos)
            {
                _keyboard.MouseButtonDownForMac(mouse);
            }
            else
            {
                _keyboard.MouseButtonDown(mouse);
            }

            MouseKeyHold = true;
            HoldMouseKey = mouse;
        }
        else
        {
            if (macos)
            {
                _keyboard.MouseButtonUpAllForMac();
            }
            else
            {
                _keyboard.MouseButtonUpAll();
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
    private static void HandleBackAndForwardForMacOs(EventCode code, KeyState keyState)
    {
        if (_keyboard == null)
            return;
        if (keyState == KeyState.KeyDown)
        {
            var value = code == EventCode.Back ? 0x50 : 0x4F;
            //send Ctrl+<- or Ctrl + ->
            _keyboard.KeyDown(KeyGroup.CharKey, (byte)0x01, (byte)value);
        }
        else
        {
            _keyboard.KeyUpAll();
        }
    }

    private static readonly ConcurrentQueue<string> Logs = new();

    public static void WriteLogOnScreen(string log)
    {
        lock (Logs)
        {
            if (MenuHandler.CommandMode) return;
            if (Logs.Count >= 10)
            {
                Logs.TryDequeue(out var result);
            }

            Logs.Enqueue(log);
            var index = 0;
            foreach (var content in Logs.ToList())
            {
                Console.SetCursorPosition(ThinkpadKeyLayout.StartColumn, 28 + (++index));
                Console.WriteLine(content);
            }
        }
    }

    private static IKeyboard GenerateKeyboard(bool bluetooth, string port)
    {
        return bluetooth ? (IKeyboard)new Btk05(port) : (IKeyboard)new Ch9329(port);
    }

    /// <summary>
    /// RefreshKeyboard
    /// Sometime when the computer wake up from sleep
    /// bluetooth will be disconnected.
    /// we should reconnect the bluetooth by reopen the port
    /// </summary>
    private static void HandleRefreshKeyboard()
    {
        _keyboard?.Dispose();
        if (_chosenDevice == null) return;
        _keyboard = GenerateKeyboard(_bluetooth, _chosenDevice);
        WriteLogOnScreen($"Refreshed the keyboard with {_bluetooth},{_chosenDevice}");
    }

    private static void HandleInputPassword()
    {
        if (!_fingerPrint)
        {
            if (Password != null) _keyboard?.CharKeyType(Password);
        }

        var attempts = 0;
        var finger = new FingerPrintHelper();
        var matched = false;
        while (attempts < 3)
        {
            matched = FingerPrintHelper.VerifyFinger("ford");
            if (matched) break;
            attempts++;
        }

        if (_keyboard != null && matched)
        {
            WriteLogOnScreen("Your fingerprint is matched");
            if (Password != null) _keyboard.CharKeyType(Password);
        }
        else
        {
            WriteLogOnScreen("Your fingerprint is not matched");
        }
    }
}