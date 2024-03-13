using CH9329NameSpace;
using ComputerAsKeyboardInterface;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using PowerArgs;

public class Program
{
    //initialize keylayout
    const string keyboardLayout = @"
--------------------------------------------------------------------------------------------------------------------
 ESC  │  MT  │  V-  │  V+  │   │  TV  │      │      │      │   │  IO  │ PRTSR│SCRLK │PAUSE │   │INSERT│ HOME │ PGUP │
      │      │      │      │   │      │      │      │      │   │      │      │      │      │   │      │      │      │
----------------------------   -----------------------------   -----------------------------   ----------------------
  F1  │  F2  │  F3  │  F4  │   │  F5  │  F6  │  F7  │  F8  │   │  F9  │  FA  │  FB  │  FC  │   │DELETE│ END  │ PGDN │
      │      │      │      │   │      │      │      │      │   │      │      │      │      │   │      │      │      │
=====================================================================================================================
 ~     │ !     │ @     │ #     │  $    │ %     │ ^     │ &     │ *     │ (     │ )     │ _     │ +     │            │
       │       │       │       │       │       │       │       │       │       │       │ -     │ =     │ <-BACKSPACE│
 `     │ 1     │ 2     │ 3     │ 4     │ 5     │ 6     │ 7     │ 8     │ 9     │ 0     │       │       │            │
---------------------------------------------------------------------------------------------------------------------
         │       │       │       │       │       │       │       │       │       │       │ {     │ }     │ │        │
  TAB    │   Q   │   W   │   E   │   R   │   T   │   Y   │   U   │   I   │   O   │   P   │       │       │          │
         │       │       │       │       │       │       │       │       │       │       │ [     │ ]     │ \        │
---------------------------------------------------------------------------------------------------------------------
           │       │       │       │       │       │       │       │       │       │ :     │ ''    │                │
 CAPSLK    │   A   │   S   │   D   │   F   │   G   │   H   │   J   │   K   │   L   │       │       │   ENTER        │
           │       │       │       │       │       │       │       │       │       │ ;     │ '     │                │
---------------------------------------------------------------------------------------------------------------------
               │       │       │       │       │       │       │       │ <     │ >     │ ?     │                    │
   SHIFT       │   Z   │   X   │   C   │   V   │   B   │   N   │   M   │       │       │       │    SHIFT           │
               │       │       │       │       │       │       │       │ ,     │ .     │ /     │                    │
---------------------------------------------------------------------------------------------------------------------
       │       │      │       │                                       │       │       │       │       │   ^  │      │
   FN  │  CTRL │  WIN │  ALT  │                SPACE                  │  ALT  │  MENU │ CTRL  │-------│------│------│
       │       │      │       │                                       │       │       │       │   <   │      │   >  │
---------------------------------------------------------------------------------------------------------------------
=====================================================================================================================
|                                                                                                                   | 
|                                                                                                                   | 
|                                                                                                                   | 
|                                                                                                                   | 
|                                                                                                                   | 
|                                                                                                                   | 
=====================================================================================================================
";
    private static bool toggle = true;
    private static bool mute = false;
    private static bool switch_alt = false;
    private static bool device_disconnected = false;
    private static bool exit_in_next = false;

    private static CH9329 ch9328;

    private static readonly Dictionary<EventCode, byte> specialKeyMap = new Dictionary<EventCode, byte>();

    private static bool IsSpecialKeyHold(byte specialKeys)
    {
        return specialKeys != 0;
    }

    private static bool IsSpecialKey(EventCode eventCode)
    {
        return specialKeyMap.ContainsKey(eventCode);
    }

    public static void Main(string[] args)
    {

        specialKeyMap.Add(EventCode.RightMeta, 0x80);
        specialKeyMap.Add(EventCode.RightAlt, 0x40);
        specialKeyMap.Add(EventCode.RightShift, 0x20);
        specialKeyMap.Add(EventCode.RightCtrl, 0x10);
        specialKeyMap.Add(EventCode.LeftMeta, 0x08);
        specialKeyMap.Add(EventCode.LeftAlt, 0x04);
        specialKeyMap.Add(EventCode.LeftShift, 0x02);
        specialKeyMap.Add(EventCode.LeftCtrl, 0x01);


        var thinkpadKey = new ThinkpadKeyMapTo9329();
        var thinkpadLayout = new ThinkpadKeyLayout();
        var controlByte = 0x00;

        var choosedDevice = "/dev/ttyUSB0";
        // Path to the directory where ttyUSB devices are located
        string ttyUSBDirectory = "/dev/";
        string mouseDevice = "mouse0";

        StartArgs parsedArgs;

        try
        {
            parsedArgs = Args.Parse<StartArgs>(args);
            choosedDevice = parsedArgs.Device;
            ttyUSBDirectory = parsedArgs.ScanPath;
            mute = !parsedArgs.Verbose;
            switch_alt = parsedArgs.MacOS;
            mouseDevice = parsedArgs.Mouse;


        }
        catch (ArgException ex)
        {
            WriteLogOnScreen(ex.Message);
            Console.WriteLine(ArgUsage.GetUsage<StartArgs>());
            return;
        }


        Console.CursorVisible = false; //hide 
        Console.Clear(); //
        var chars = new List<List<char>>();
        List<char> chartList = new List<char>();
        chars.Add(chartList);
        new List<char>(keyboardLayout.ToCharArray()).ForEach(c =>
       {
           if (c == '\n')
           {
               chartList = new List<char>();
               chars.Add(chartList);
               return;
           }
           chartList.Add(c);
       });

        Console.WriteLine(keyboardLayout);
        Console.TreatControlCAsInput = true;
        using (AggregateInputReader aggHandler1 = new AggregateInputReader())
        {
            WriteLogOnScreen("This is a test log ");
            aggHandler1.OnKeyPress += (e) =>
            {
                if (e.State == KeyState.KeyUp)
                {
                    ToggleKeys(chars, thinkpadLayout.FindKeyPositions(e.Code));
                }
            };

            if (parsedArgs.AutoScan)
            {
                try
                {
                    // Get a list of all files in the /dev/ directory
                    string[] devices = Directory.GetFiles(ttyUSBDirectory);

                    // Filter the list to only include ttyUSB devices
                    var ttyUSBDevices = Array.FindAll(devices, s => s.StartsWith("/dev/ttyUSB", StringComparison.Ordinal));

                    var index = 0;

                    if (ttyUSBDevices.Length <= 0)
                    {
                        WriteLogOnScreen("TTY Devices are not available, please plug in your device and try again");
                    }

                    //if only one device is available ,choose it directly.
                    if (ttyUSBDevices.Length == 1)
                    {
                        choosedDevice = ttyUSBDevices[0];
                    }
                    else
                    {
                        // Output the list of ttyUSB devices
                        WriteLogOnScreen("ttyUSB devices:");
                        foreach (var device in ttyUSBDevices)
                        {
                            WriteLogOnScreen($"{++index}.{device}");
                        }
                        var code = 0;

                        //Try to choose a USB Device we want to use
                        while (code <= 0 || code > ttyUSBDevices.Length + 1)
                        {
                            WriteLogOnScreen("Please Choose the device you want to use");
                            var codeStr = Console.ReadLine();
                            if (codeStr != null)
                            {
                                code = int.TryParse(codeStr, out int result) ? result : 0;
                            }
                        }
                        choosedDevice = devices[code - 1];
                    }

                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur
                    WriteLogOnScreen($"An error occurred: {ex.Message}");
                }
            }
            WriteLogOnScreen($"device is {choosedDevice}");
            using (AggregateInputReader aggHandler = new AggregateInputReader())
            {



                if (File.Exists(choosedDevice))
                {
                    ch9328 = new CH9329(PortName: choosedDevice);
                }

                var watcher = new FileSystemWatcher(ttyUSBDirectory)
                {
                    NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName
                | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                watcher.Created += (sender, e) =>
                {
                    WriteLogOnScreen($"File created:{e.FullPath}");
                    try
                    {
                        if (e.FullPath.Contains("ttyUSB"))
                        {
                            ch9328 = new CH9329(PortName: e.FullPath);
                            WriteLogOnScreen($"ConnectToAnother:{e.FullPath}");
                            device_disconnected = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLogOnScreen(ex.Message);
                    }

                };
                watcher.Deleted += (sender, e) =>
                {
                    WriteLogOnScreen($"File Deleted:{e.FullPath}");
                    if (e.FullPath == choosedDevice)
                    {
                        WriteLogOnScreen("Device is removed , can't use now");
                        device_disconnected = true;
                    }
                };
                bool individual_special_key = false;
                aggHandler.OnKeyPress += (KeyPressEvent e) =>
                {
                    if (!mute)
                    {
                        WriteLogOnScreen($"Code:{e.Code} State:{e.State},Event:{e.DevicePath}");
                    }
                    // take no action when toggle is off
                    if (!toggle || device_disconnected)
                    {
                        return;
                    }

                    var keyCode = e.Code;

                    if ((e.Code == EventCode.Back || e.Code == EventCode.Forward) & switch_alt)
                    {
                        HandleBackAndFowardForMacOS(keyCode, e.State);
                        return;
                    }
                    if (e.Code == EventCode.LeftMouse || e.Code == EventCode.RightMouse || e.Code == EventCode.MiddleMouse)
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
                        //when a specialkey is down ,we can't send another speclial
                        // key as an individual key, we have to wait other nospecial key 
                        if (IsSpecialKeyHold((byte)controlByte) && IsSpecialKey(keyCode))
                        {
                            // do nothing
                        }
                        else
                        {
                            if (thinkpadKey.keyMaps.TryGetValue((int)keyCode, out var value))
                            {
                                if (IsSpecialKey(keyCode))
                                {
                                    individual_special_key = true;
                                }
                                else
                                {
                                    ch9328?.keyDown(CH9329.KeyGroup.CharKey, (byte)controlByte, value);
                                    individual_special_key = false;
                                    if (controlByte == 0x01 && keyCode == EventCode.Compose)
                                    {
                                        exit_in_next = true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (specialKeyMap.TryGetValue(keyCode, out byte value))
                        {
                            controlByte -= value;
                        }
                        if (IsSpecialKey(keyCode) && individual_special_key)
                        {
                            if (thinkpadKey.keyMaps.TryGetValue((int)keyCode, out var value1))
                            {
                                ch9328?.keyDown(CH9329.KeyGroup.CharKey, 0x00, value1);
                            }
                        }
                        ch9328?.keyUpAll();

                    }
                    if (e.State is KeyState.KeyDown)
                    {
                        if (specialKeyMap.TryGetValue(keyCode, out byte value))
                        {
                            controlByte += value;
                        }
                    }
                };
                //handle toggle event ,sometime ,we want to turn off the keyboard
                aggHandler.OnKeyPress += (e) =>
                {
                    if (e.Code == EventCode.Prog1 && e.State == KeyState.KeyUp)
                    {
                        toggle = !toggle;
                        WriteLogOnScreen($"Toggle is {(toggle ? "on" : "off")} now");
                    }
                };
                //handle mute event, we don't like log to be printed
                aggHandler.OnKeyPress += (e) =>
                {
                    if (e.Code == EventCode.Mute && e.State == KeyState.KeyUp)
                    {
                        mute = !mute;
                        WriteLogOnScreen($"Log is {(mute ? "on" : "off")} now");
                    }
                };

                var mouseReader = new MouseReader($"/dev/input/{mouseDevice}");
                mouseReader.OnMouseMove += (e) =>
                {
                    if (MouseKeyHold)
                    {
                        ch9328?.mouseMoveRel(e.X, e.Y, true, HoldMouseKey);
                    }
                    else
                    {
                        ch9328?.mouseMoveRel(e.X, e.Y);
                    }
                };
                mouseReader.OnMouseScroll += (e) =>
                {
                    ch9328?.mouseScrollForMac(e.ScrollCount);
                };
                System.Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    ch9328?.keyUpAll();
                };
                while (true)
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    if (exit_in_next)
                    {
                        break;
                    }
                }
                ch9328?.keyUpAll();
            }
        }
    }
    /// <summary>
    /// if the Mousekey is Hold now
    /// </summary>
    private static bool MouseKeyHold { get; set; }

    private static CH9329.MouseButtonCode HoldMouseKey { get; set; }
    private static void HandleMouseKey(KeyPressEvent e, bool macos)
    {
        CH9329.MouseButtonCode mouse = CH9329.MouseButtonCode.LEFT;
        switch (e.Code)
        {
            case EventCode.LeftMouse:
                mouse = CH9329.MouseButtonCode.LEFT;
                break;
            case EventCode.RightMouse:
                mouse = CH9329.MouseButtonCode.RIGHT;
                break;
            case EventCode.MiddleMouse:
                mouse = CH9329.MouseButtonCode.MIDDLE;
                break;
        }


        if (e.State == KeyState.KeyDown || e.State == KeyState.KeyHold)
        {
            if (macos) { ch9328?.mouseButtonDownForMac(mouse); }
            else { ch9328?.mouseButtonDown(mouse); }
            MouseKeyHold = true;
            HoldMouseKey = mouse;
        }
        else
        {
            if (macos) { ch9328?.mouseButtonUpAllForMac(); }
            else { ch9328?.mouseButtonUpAll(); }
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
    public static void HandleBackAndFowardForMacOS(EventCode code, KeyState keyState)
    {
        if (keyState == KeyState.KeyDown)
        {
            var value = code == EventCode.Back ? 0x50 : 0x4F;
            //send Ctrl+<- or Ctrl + ->
            ch9328?.keyDown(CH9329.KeyGroup.CharKey, (byte)0x01, (byte)value);
        }
        else
        {
            ch9328?.keyUpAll();
        }

    }
    private static void ToggleKeys(List<List<char>> chars, (int, int, int, int) values)
    {

        var (sr, sc, er, ec) = values;
        Console.BackgroundColor = ConsoleColor.DarkCyan;
        for (var i = sr; i <= er; i++)
        {
            for (var j = sc; j <= ec; j++)
            {
                Console.SetCursorPosition(j, i);
                Console.Write(chars[i][j]);
            }
        }
        Console.ResetColor();
        Thread.Sleep(30);
        Console.BackgroundColor = ConsoleColor.Black;
        for (var i = sr; i <= er; i++)
        {
            for (var j = sc; j <= ec; j++)
            {
                Console.SetCursorPosition(j, i);
                Console.Write(chars[i][j]);
            }
        }
    }


    private static readonly Queue<string> logs = new Queue<string>();

    private static void WriteLogOnScreen(string log)
    {

        if (logs.Count >= 5)
        {
            logs.Dequeue();
        }

        logs.Enqueue(log);

        var index = 0;

        foreach (var content in logs)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Console.SetCursorPosition(0, 28 + (++index));
            Console.WriteLine(content);

        }

    }
}
