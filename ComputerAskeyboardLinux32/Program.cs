using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using ComputerAsKeyboardInterface.Bluetooth;
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
    private static bool Background { get; set; } = false;

    public static string? Password { get; set; }

    private static readonly Dictionary<EventCode, byte> SpecialKeyMap = new();

    private static readonly Dictionary<EventCode, bool> SpecialKeyStatus = new();
    private static bool _bluetooth = false;
    private static readonly byte[] KeySlots = new byte[6];
    public static bool UseQueue { get; private set; } = false;
    private static List<string>? InputDevices { get; set; } = [];
    private static List<string> MouseDevices { get; set; } = [];
    private static int BaudRate { get; set; } = 9600;
    private static bool HasXInput { get; set; } = false;
    private static bool RunAsService { get; set; } = false;
    private static int XinputServicePort { get; set; } = 9869;

    private static List<int> ToggleInputDeviceIds { get; set; } = [];
    private static readonly ManualResetEventSlim ManualResetEventSlim = new(false);

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
        MouseDevices = devices
            .Where(d => DeviceResolver.MouseDevicesMapping.ContainsKey(d))
            .Select(c => DeviceResolver.MouseDevicesMapping[c]).ToList();
    }

    private static void WithKeyTogglingOnScreen(this AggregateInputReader reader)
    {
        var thinkpadLayout = new ThinkpadKeyLayout();
        var chars = ThinkpadKeyLayout.KeyLayoutChars;
        reader.OnKeyPress += (e) =>
        {
            if (MenuHandler.CommandMode) return;
            if (e.State == KeyState.KeyUp)
            {
                ThinkpadKeyLayout.ToggleKeys(chars, thinkpadLayout.FindKeyPositions(e.Code));
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

            ToggleDevices(_toggle);
        };
    }

    private static void ToggleDevices(bool toggle)
    {
        if (!HasXInput || ToggleInputDeviceIds.Count == 0) return;
        var command = toggle ? "disable" : "enable";
        if (!RunAsService)
        {
            try
            {
                ToggleInputDeviceIds.ForEach(id => { LinuxCommandHelper.ExecuteCommand($"xinput {command} {id}"); });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        else
        {
            ToggleInputDeviceIds.ForEach(id => { _ = SendXInputCommand(command, id, XinputServicePort); });
        }
    }

    /// <summary>
    /// 发送xinput命令到服务器
    /// </summary>
    /// <param name="operation">操作类型：disable或enable</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="serverPort"></param>
    /// <returns>服务器响应结果</returns>
    private static async Task<string> SendXInputCommand(string operation, int deviceId, int serverPort)
    {
        if (operation != "disable" && operation != "enable")
        {
            Console.WriteLine("操作必须是'disable'或'enable' {0}", nameof(operation));
            return "error";
        }

        try
        {
            // 创建TCP客户端并连接服务器
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", serverPort);
            Console.WriteLine($"已连接到服务器 localhost:{serverPort}");
            // 获取网络流
            await using var stream = client.GetStream();
            // 构建命令字符串
            var command = $"xinput {operation} {deviceId}";
            var data = Encoding.UTF8.GetBytes(command);

            // 发送命令到服务器
            await stream.WriteAsync(data);
            Console.WriteLine($"已发送命令: {command}");

            // 接收服务器响应
            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
        catch (Exception ex)
        {
            return $"发送命令时出错: {ex.Message}";
        }
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

    private static void HandleXinputRelated()
    {
        if (!RunAsService)
        {
            HasXInput = LinuxCommandChecker.IsCommandExists("xinput");
            if (!HasXInput) return;
        }
        else
        {
            HasXInput = true;
        }

        const string toggleDevicesFile = ".toggle_devices";
        var allxInputDevices = DeviceResolver.GetXInputDevices(RunAsService);
        if (allxInputDevices.Count != 0 && !File.Exists(toggleDevicesFile))
        {
            Console.WriteLine("Please choose the input device you want to toggle:");
            var inputDevices = new List<string>();
            var index = 0;
            allxInputDevices.ForEach(kv =>
            {
                Console.WriteLine($"{index++}. {kv.Name}");
                inputDevices.Add(kv.Name);
            });
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line)) Console.WriteLine("Invalid input");

            if (line != null && line.Split(',').Any(s => !int.TryParse(s, out _)))
                Console.WriteLine("Invalid input");

            if (line != null)
            {
                var chosenIndexes = line.Split(',').Select(int.Parse).ToList();

                var chosenList = new List<string>();

                chosenIndexes.ForEach(i =>
                {
                    if (i < 0 || i >= inputDevices.Count) return;
                    chosenList.Add(inputDevices[i]);
                });

                File.WriteAllLines(toggleDevicesFile, chosenList);
            }
        }

        if (allxInputDevices.Count == 0 || !File.Exists(toggleDevicesFile)) return;
        var devices = File.ReadAllLines(toggleDevicesFile);
        var ids = devices.Select(d => { return allxInputDevices.FirstOrDefault(xi => xi.Name == d)?.Id ?? -1; })
            .Where(d => d != -1);
        ToggleInputDeviceIds = ids.ToList();
    }

    private static string? _chosenDevice = "/dev/ttyUSB0";

    // Path to the directory where ttyUSB devices are located
    private static string _ttyUsbDirectory = "/dev/";

    internal static void Main(string[] args)
    {
        MainFuncton(args);
    }

    public static void MainFuncton(string[] args)
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
            _ttyUsbDirectory = parsedArgs.ScanPath ?? "/dev/";
            _mute = !parsedArgs.Verbose;
            _switchAlt = parsedArgs.MacOs;
            _bluetooth = parsedArgs.Bluetooth;
            _fingerPrint = parsedArgs.Fprint;
            Background = parsedArgs.Background;
            UseQueue = parsedArgs.Queue;
            BaudRate = parsedArgs.BaudRate;
            RunAsService = parsedArgs.RunAsService;
            XinputServicePort = parsedArgs.XInputServicePort;
        }
        catch (ArgException ex)
        {
            WriteLogOnScreen(ex.Message);
            return;
        }

        HandleXinputRelated();
        Console.TreatControlCAsInput = true;

        if (!parsedArgs.Background)
        {
            ThinkpadKeyLayout.WriteKeyboardOnScreen();
            using var aggHandler1 = new AggregateInputReader(InputDevices);
            aggHandler1.WithKeyTogglingOnScreen();
        }

        SerialPortExtension.BaudRate = parsedArgs.BaudRate;

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
        EnableMouseTrack(isMacOs: parsedArgs.MacOs);

        Console.CancelKeyPress += (sender, eventArgs) => { _keyboard?.KeyUpAll(); };

        _ = SerialPortExtension.EnableAutoDetectAsync(parsedArgs.BluetoothPort);
        // while (true)
        // {
        //     //Console.ReadKey(intercept: true);
        //     if (ExitInNext) break;
        // }

        ManualResetEventSlim.Wait();

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
            case EventCode.Back or EventCode.Forward when !isMacOs:
                if (!KeyboardDisabled) HandleBackAndForwardForWindowsAndLinux(keyCode, e.State);
                return true;

            //MacOS use Compose to show menu
            case EventCode.Compose when isMacOs:
                break;
            case EventCode.LeftMouse:
            case EventCode.RightMouse:
            case EventCode.MiddleMouse:
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

    private static bool FnDown { get; set; }

    private static void SwitchKeySlot(EventCode keyCode, bool push, ThinkpadKeyMapTo9329 thinkpadKey)
    {
        if (!thinkpadKey.KeyMaps.TryGetValue((int)keyCode, out var keyByte)) return;
        if (push)
        {
            //push
            //if we don't have a duplicated key,find a new slot
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

    private static bool IsFnCompositeKey()
    {
        return false;
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

    private static void Beep()
    {
        try
        {
            if (!File.Exists("/usr/local/bin/safe-beep")) return;
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/local/bin/safe-beep",
                Arguments = "", // 传递beep的参数
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            // 3. 执行命令
            using var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            var error = process?.StandardError.ReadToEnd();
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"异常：{ex.Message}");
        }
    }

    private static void EnableMenuFunction(this AggregateInputReader reader)
    {
        MenuHandler.BeforeExitApplication = () => { _keyboard?.KeyUpAll(); };
        // long touch fn will show menu;
        var lastCodeCount = 0;
        var waitingForCommand = false;
        reader.OnKeyPress += (e) =>
        {
            if (waitingForCommand)
            {
                if (e is { Code: EventCode.E })
                {
                    if (LinuxCommandChecker.IsCommandExists("beep"))
                    {
                        Beep();
                        Task.Delay(200).Wait();
                        Beep();
                        Task.Delay(200).Wait();
                    }

                    _keyboard?.KeyUpAll(KeyGroup.CharKey);
                    ToggleDevices(false);
                    Environment.Exit(0);
                }

                if (e is not { Code: EventCode.Wakeup })
                {
                    waitingForCommand = false;
                    MenuHandler.CommandMode = false;
                }
            }

            if (e is { Code: EventCode.Wakeup, State: KeyState.KeyDown or KeyState.KeyHold })
            {
                lastCodeCount++;
                if (lastCodeCount <= 20) return;
                switch (Background)
                {
                    case true:
                        if (LinuxCommandChecker.IsCommandExists("beep")) Beep();
                        break;
                    case false:
                        MenuHandler.StartMenu();
                        break;
                }

                if (Background)
                {
                    MenuHandler.CommandMode = true;
                    waitingForCommand = true;
                }
            }

            lastCodeCount = 0;
        };
    }

    private static void EnableMouseTrack(bool isMacOs)
    {
        var mouseReader =
            new AggregateMouseReader(MouseDevices.ToList());
        mouseReader.OnMouseMove += (e) =>
        {
            if (KeyboardDisabled) return;
            _keyboard?.MouseMoveRel(e.X, e.Y, MouseKeyHold, MouseKeyHold ? HoldMouseKey : MouseButtonCode.Left);
        };
        mouseReader.OnMouseScroll += (e) =>
        {
            if (KeyboardDisabled) return;
            if (isMacOs)
                _keyboard?.MouseScrollForMac(e.ScrollCount);
            else if (_keyboard is Ch9329 ch9329)
            {
                ch9329.MouseScroll(e.ScrollCount);
            }
        };
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
            _ => MouseButtonCode.Left
        };
        MouseKeyHold = e.State is KeyState.KeyDown or KeyState.KeyHold;
        _ = macos
            ? _keyboard.ToggleMouseButtonForMac(MouseKeyHold, mouse)
            : _keyboard.ToggleMouseButton(MouseKeyHold, mouse);
        HoldMouseKey = mouse;
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

    private static void HandleBackAndForwardForWindowsAndLinux(EventCode code, KeyState keyState)
    {
        if (_keyboard == null)
            return;
        if (keyState == KeyState.KeyDown)
        {
            var value = code == EventCode.Back ? 0x50 : 0x4F;
            //send Ctrl+Meta+<- or Ctrl+Meta + ->
            _keyboard.KeyDown(KeyGroup.CharKey, (byte)0x09, (byte)value);
        }
        else
        {
            _keyboard.KeyUpAll();
        }
    }

    private static readonly ConcurrentQueue<string> Logs = new();

    public static void WriteLogOnScreen(string log)
    {
        if (Background) Console.WriteLine(log);
        if (Background) return;

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
                Console.WriteLine(content.PadRight(116));
            }
        }
    }

    private static IKeyboard GenerateKeyboard(bool bluetooth, string port)
    {
        return bluetooth
            ? new Btk05(port, baudRate: BaudRate)
            : new Ch9329();
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
            return;
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