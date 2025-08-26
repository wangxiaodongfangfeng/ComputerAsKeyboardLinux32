using ComputerAsKeyboardInterface.FingerPrint;
using ComputerAsKeyboardInterface.KeyboardRelated;
using ComputerAsKeyboardInterface.MouseRelated;
using PowerArgs;

namespace ComputerAsKeyboardInterface;

public static class Program
{
  //initialize key-layout
  private const string KeyboardLayout = ThinkpadKeyLayout.KeyboardLayoutString;

  private static bool _toggle = true;
  private static bool _mute = false;
  private static bool _switchAlt = false;
  private static bool _deviceDisconnected = false;
  private const bool ExitInNext = false;
  private static IKeyboard? _keyboard;
  private static bool _fingerPrint = false;
  private static bool CommandMode { get; set; }

  public static string? Password { get; set; }

  private static readonly Dictionary<EventCode, byte> SpecialKeyMap = new();

  private static readonly Dictionary<EventCode, bool> SpecialKeyStatus = new();
  private static bool _bluetooth = false;
  private static string _currentPort = "/dev/ttyUSB0";
  private static readonly byte[] KeySlots = new byte[6];

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

    var chosenDevice = "/dev/ttyUSB0";
    // Path to the directory where ttyUSB devices are located
    var ttyUsbDirectory = "/dev/";
    var mouseDevice = "mouse0";
    var bluetooth = false;

    StartArgs parsedArgs;

    try
    {
      parsedArgs = Args.Parse<StartArgs>(args);
      chosenDevice = parsedArgs.Device;
      ttyUsbDirectory = parsedArgs.ScanPath;
      _mute = !parsedArgs.Verbose;
      _switchAlt = parsedArgs.MacOs;
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
    using var aggHandler1 = new AggregateInputReader();

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


        switch (ttyUsbDevices.Count)
        {
          case <= 0:
            WriteLogOnScreen("TTY Devices are not available, please plug in your device and try again");
            return;
          //if only one device is available ,choose it directly.
          case 1:
            chosenDevice = ttyUsbDevices[0];
            break;
          default:
            // Output the list of ttyUSB devices
            WriteLogOnScreen(
              "There are more than one device in the folder, please specify the device you want to use by command");
            return;
        }
      }
      catch (Exception ex)
      {
        // Handle any exceptions that may occur
        WriteLogOnScreen($"An error occurred: {ex.Message}");
      }
    }

    #endregion

    WriteLogOnScreen($"device is {chosenDevice}");

    using var aggHandler = new AggregateInputReader();
    if (File.Exists(chosenDevice))
    {
      _keyboard = GenerateKeyboard(bluetooth, chosenDevice);
      _bluetooth = bluetooth;
      _currentPort = chosenDevice;
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
      WriteLogOnScreen($"File created:{e.FullPath}");
      try
      {
        if (!(e.FullPath.Contains("ttyUSB") || e.FullPath.Contains("rfcomm"))) return;
        _keyboard?.Dispose();
        _keyboard = GenerateKeyboard(bluetooth, e.FullPath);
        _bluetooth = bluetooth;
        _currentPort = e.FullPath;
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
      if (e.FullPath != chosenDevice) return;
      WriteLogOnScreen("Device is removed , can't use now");
      _deviceDisconnected = true;
    };

    #endregion

    aggHandler.OnKeyPress += (KeyPressEvent e) =>
    {
      if (!_mute)
      {
        WriteLogOnScreen($"Code:{e.Code} State:{e.State},Event:{e.DevicePath}");
      }

      // take no action when toggle is off
      if (!_toggle || _deviceDisconnected)
      {
        return;
      }

      var keyCode = e.Code;
      switch (e.Code)
      {
        //MacOS use back and forward to switch screen
        case EventCode.Back or EventCode.Forward when _switchAlt:
          HandleBackAndForwardForMacOs(keyCode, e.State);
          return;
        //MacOS use Compose to show menu
        case EventCode.Compose when _switchAlt:
          break;
        case EventCode.LeftMouse:
        case EventCode.RightMouse:
        case EventCode.MiddleMouse:
          HandleMouseKey(e, _switchAlt);
          return;
      }

      //When I was working in MacOS ,I should swith the left alt and meta
      //TODO: we should refator this code, because it looks ugly now.
      if (_switchAlt && (keyCode == EventCode.LeftMeta || keyCode == EventCode.LeftAlt))
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
        if (thinkpadKey.KeyMaps.TryGetValue((int)keyCode, out keyByte))
        {
          //if don't have duplicated key,find a new slot
          if (!KeySlots.Contains(keyByte))
          {
            var index = KeySlots.ToList().FindIndex(key => key == 0);
            KeySlots[index] = keyByte;
          }

          _keyboard.KeyDown(KeyGroup.CharKey, 0x00, KeySlots[0], KeySlots[1], KeySlots[2], KeySlots[3],
            KeySlots[4], KeySlots[5]);
          //WriteLogOnScreen(string.Format("{0},{1},{3},{4},{5}", _keyslots[0], _keyslots[1], _keyslots[2], _keyslots[3], _keyslots[4], _keyslots[5]));
        }

        List<byte> mediaKeyByte;
        if (thinkpadKey.MediaKeyMap.TryGetValue((int)keyCode, out mediaKeyByte))
        {
          _keyboard.KeyDown(KeyGroup.MediaKey, mediaKeyByte[0], mediaKeyByte[1], mediaKeyByte[2], mediaKeyByte[3]);
        }

        if (IsSpecialKey(keyCode))
        {
          SpecialKeyStatus[keyCode] = true;
        }
      }
      else
      {
        byte keyByte;
        if (thinkpadKey.KeyMaps.TryGetValue((int)keyCode, out keyByte))
        {
          var index = KeySlots.ToList().FindIndex(key => key == keyByte);
          KeySlots[index] = 0;
          _keyboard.KeyDown(KeyGroup.CharKey, 0x00, KeySlots[0], KeySlots[1], KeySlots[2], KeySlots[3],
            KeySlots[4], KeySlots[5]);
          //WriteLogOnScreen(string.Format("{0},{1},{3},{4},{5}", _keyslots[0], _keyslots[1], _keyslots[2], _keyslots[3], _keyslots[4], _keyslots[5]));
        }

        List<byte> mediaKeyByte;
        if (thinkpadKey.MediaKeyMap.TryGetValue((int)keyCode, out mediaKeyByte))
        {
          _keyboard.KeyDown(KeyGroup.MediaKey, 0x02, 0, 0, 0);
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
      _toggle = !_toggle;
      WriteLogOnScreen(String.Format("Toggle is {0} now", (_toggle ? "on" : "off")));
    };

    #endregion

    #region handle mute

    //handle mute event, we don't like log to be printed
    aggHandler.OnKeyPress += (e) =>
    {
      if (e.Code != EventCode.Mute || e.State != KeyState.KeyUp) return;
      _mute = !_mute;
      WriteLogOnScreen(String.Format("Log is {0} now", (_mute ? "on" : "off")));
    };
    MenuHandler.BeforeExitApplication = () => { _keyboard?.KeyUpAll(); };
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
        _keyboard.MouseMoveRel(e.X, e.Y, true, HoldMouseKey);
      }
      else
      {
        _keyboard.MouseMoveRel(e.X, e.Y);
      }
    };

    mouseReader.OnMouseScroll += (e) =>
    {
      if (_keyboard == null) return;
      _keyboard.MouseScrollForMac(e.ScrollCount);
    };

    System.Console.CancelKeyPress += (sender, eventArgs) =>
    {
      if (_keyboard == null) return;
      _keyboard.KeyUpAll();
    };
    while (true)
    {
      var keyInfo = Console.ReadKey(intercept: true);
      if (ExitInNext)
      {
        break;
      }
    }

    _keyboard.KeyUpAll();
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

    MouseButtonCode mouse = MouseButtonCode.Left;
    switch (e.Code)
    {
      case EventCode.LeftMouse:
        mouse = MouseButtonCode.Left;
        break;
      case EventCode.RightMouse:
        mouse = MouseButtonCode.Right;
        break;
      case EventCode.MiddleMouse:
        mouse = MouseButtonCode.Middle;
        break;
    }

    if (e.State == KeyState.KeyDown || e.State == KeyState.KeyHold)
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
    _keyboard = GenerateKeyboard(_bluetooth, _currentPort);
    WriteLogOnScreen($"Refreshed the keyboard with {_bluetooth},{_currentPort}");
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