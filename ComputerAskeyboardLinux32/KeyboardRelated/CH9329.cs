using System.Collections.Concurrent;
using System.IO.Ports;
using Gdk;

namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public class Ch9329 : IKeyboard
    {
        //private static SerialPort? _serialPort;
        private static readonly ConcurrentQueue<byte[]> Buffer = new();
        private readonly CancellationTokenSource? _cancellationTokenSource;


        public Ch9329()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CreateCharKeyTable();
            if (Program.UseQueue)
            {
                _ = Task.Run(() => SerialPortDataHandler(_cancellationTokenSource.Token));
            }
        }

        private static Task SerialPortDataHandler(CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                var result = Buffer.TryDequeue(out var data);
                if (!result) continue;
                try
                {
                    if (data != null) SerialPortExtension.CurrentSerialPort?.Write(data, 0, data.Length);
                }
                catch (Exception)
                {
                    Program.WriteLogOnScreen("Failed Send data to serial port");
                }
            }

            return Task.CompletedTask;
        }

        private Dictionary<string, byte[]> _charKeyTable = [];

        /// <summary>
        /// create 109A CharKeyTable
        /// </summary>
        private void CreateCharKeyTable()
        {
            _charKeyTable = new Dictionary<string, byte[]> { { "0", [0, (byte)(0x27)] } };

            for (byte i = 1; i <= 9; i++)
            {
                _charKeyTable.Add(i.ToString(), [0x00, (byte)(0x1E + i - 1)]);
            }

            for (byte i = 0; i < 26; i++)
            {
                //Upper ASCII code
                _charKeyTable.Add(((char)(i + 65)).ToString(), [0x02, (byte)(0x04 + i)]);
                //Lower ASCII code
                _charKeyTable.Add(((char)(i + 97)).ToString(), [0x00, (byte)(0x04 + i)]);
            }

            _charKeyTable.Add("ENTER", [0x00, 0x28]); //001
            _charKeyTable.Add("ESC", [0x00, 0x29]); //002
            _charKeyTable.Add("BACKSPACE", [0x00, 0x2A]); //003
            _charKeyTable.Add("TAB", [0x00, 0x2B]); //004
            _charKeyTable.Add("SPACEBAR", [0x00, 0x2C]); //005
            _charKeyTable.Add("-", [0x00, 0x2D]); //006
            _charKeyTable.Add("^", [0x00, 0x2E]); //007
            _charKeyTable.Add("@", [0x02, 0x1F]); //008
            _charKeyTable.Add("[", [0x00, 0x30]); //009
            _charKeyTable.Add("-----", [0x00, 0x31]); //010
            _charKeyTable.Add("]", [0x00, 0x32]); //011
            _charKeyTable.Add(";", [0x00, 0x33]); //012
            _charKeyTable.Add(":", [0x00, 0x34]); //013
            _charKeyTable.Add("半角/全角", [0x00, 0x35]); //014
            _charKeyTable.Add(",", [0x00, 0x36]); //015
            _charKeyTable.Add(".", [0x00, 0x37]); //016
            _charKeyTable.Add("/", [0x00, 0x38]); //017
            _charKeyTable.Add("CAPS LOCK", [0x00, 0x39]); //018
            _charKeyTable.Add("F1", [0x00, 0x3A]); //019
            _charKeyTable.Add("F2", [0x00, 0x3B]); //020
            _charKeyTable.Add("F3", [0x00, 0x3C]); //021
            _charKeyTable.Add("F4", [0x00, 0x3D]); //022
            _charKeyTable.Add("F5", [0x00, 0x3E]); //023
            _charKeyTable.Add("F6", [0x00, 0x3F]); //024
            _charKeyTable.Add("F7", [0x00, 0x40]); //025
            _charKeyTable.Add("F8", [0x00, 0x41]); //026
            _charKeyTable.Add("F9", [0x00, 0x42]); //027
            _charKeyTable.Add("F10", [0x00, 0x43]); //028
            _charKeyTable.Add("F11", [0x00, 0x44]); //029
            _charKeyTable.Add("F12", [0x00, 0x45]); //030
            _charKeyTable.Add("PRINT SCREEN", [0x00, 0x46]); //031
            _charKeyTable.Add("SCROLL LOCK", [0x00, 0x47]); //032
            _charKeyTable.Add("PAUSE", [0x00, 0x48]); //033
            _charKeyTable.Add("INSERT", [0x00, 0x49]); //034
            _charKeyTable.Add("HOME", [0x00, 0x4A]); //035
            _charKeyTable.Add("PAGE UP", [0x00, 0x4B]); //036
            _charKeyTable.Add("DELETE", [0x00, 0x4C]); //037
            _charKeyTable.Add("END", [0x00, 0x4D]); //038
            _charKeyTable.Add("PAGE DOWN", [0x00, 0x4E]); //039
            _charKeyTable.Add("→", [0x00, 0x4F]); //040
            _charKeyTable.Add("←", [0x00, 0x50]); //041
            _charKeyTable.Add("↓", [0x00, 0x51]); //042
            _charKeyTable.Add("↑", [0x00, 0x52]); //043
            _charKeyTable.Add("NUM LOCK", [0x00, 0x53]); //044
            _charKeyTable.Add("KEYPAD /", [0x00, 0x54]); //045
            _charKeyTable.Add("KEYPAD *", [0x00, 0x55]); //046
            _charKeyTable.Add("KEYPAD -", [0x00, 0x56]); //047
            _charKeyTable.Add("KEYPAD +", [0x00, 0x57]); //048
            _charKeyTable.Add("KEYPAD ENTER", [0x00, 0x58]); //049
            _charKeyTable.Add("KEYPAD 1", [0x00, 0x59]); //050
            _charKeyTable.Add("KEYPAD 2", [0x00, 0x5A]); //051
            _charKeyTable.Add("KEYPAD 3", [0x00, 0x5B]); //052
            _charKeyTable.Add("KEYPAD 4", [0x00, 0x5C]); //053
            _charKeyTable.Add("KEYPAD 5", [0x00, 0x5D]); //054
            _charKeyTable.Add("KEYPAD 6", [0x00, 0x5E]); //055
            _charKeyTable.Add("KEYPAD 7", [0x00, 0x5F]); //056
            _charKeyTable.Add("KEYPAD 8", [0x00, 0x60]); //057
            _charKeyTable.Add("KEYPAD 9", [0x00, 0x61]); //058
            _charKeyTable.Add("KEYPAD 0", [0x00, 0x62]); //059
            _charKeyTable.Add("KEYPAD .", [0x00, 0x63]); //060
            _charKeyTable.Add("APPLICATION", [0x00, 0x65]); //061
            //charKeyTable.Add("＼", new byte[] { 0x00, 0x87 });   //062
            _charKeyTable.Add("ひらがな カタカナ", [0x00, 0x88]); //063
            _charKeyTable.Add("\\", [0x00, 0x89]); //064
            _charKeyTable.Add("変換", [0x00, 0x8A]); //065
            _charKeyTable.Add("無変換", [0x00, 0x8B]); //066
            _charKeyTable.Add("LEFT CTRL", [0x00, 0xE0]); //067
            _charKeyTable.Add("LEFT SHIFT", [0x00, 0xE1]); //068
            _charKeyTable.Add("LEFT ALT", [0x00, 0xE2]); //069
            _charKeyTable.Add("LEFT WINDOWS", [0x00, 0xE3]); //070
            _charKeyTable.Add("RIGHT CTRL", [0x00, 0xE4]); //071
            _charKeyTable.Add("RIGHT SHIFT", [0x00, 0xE5]); //072
            _charKeyTable.Add("RIGHT ALT", [0x00, 0xE6]); //073
            _charKeyTable.Add("RIGHT WINDOWS", [0x00, 0xE7]); //074

            _charKeyTable.Add("!", [0x02, 0x1E]); //075
            _charKeyTable.Add("\"", [0x02, 0x1F]); //076
            _charKeyTable.Add("#", [0x02, 0x20]); //077
            _charKeyTable.Add("$", [0x02, 0x21]); //078
            _charKeyTable.Add("%", [0x02, 0x22]); //079
            _charKeyTable.Add("&", [0x02, 0x23]); //080
            _charKeyTable.Add("'", [0x02, 0x24]); //081
            _charKeyTable.Add("(", [0x02, 0x25]); //082
            _charKeyTable.Add(")", [0x02, 0x26]); //083
            _charKeyTable.Add("=", [0x02, 0x2D]); //084
            _charKeyTable.Add("~", [0x02, 0x2E]); //085
            _charKeyTable.Add("`", [0x02, 0x2F]); //086
            _charKeyTable.Add("{", [0x02, 0x30]); //087
            _charKeyTable.Add("}", [0x02, 0x32]); //088
            _charKeyTable.Add("+", [0x02, 0x33]); //089
            _charKeyTable.Add("*", [0x02, 0x34]); //090
            _charKeyTable.Add("<", [0x02, 0x36]); //091
            _charKeyTable.Add(">", [0x02, 0x37]); //092
            _charKeyTable.Add("?", [0x02, 0x38]); //093
        }


        private static void SendPacket(byte[] data)
        {
            if (Program.UseQueue)
                Buffer.Enqueue(data);
            else
                SendPacketDirectly(data);
        }

        private static void SendPacketDirectly(byte[] data)
        {
            if (SerialPortExtension.CurrentSerialPort == null) return;
            lock (SerialPortExtension.CurrentSerialPort)
            {
                SerialPortExtension.CurrentSerialPort.Write(data, 0, data.Length);
            }
        }

        private static byte[] CreatePacketArray(List<int> arrList, bool addCheckSum)
        {
            var bytePacketList = arrList.ConvertAll(b => (byte)b);
            if (addCheckSum) bytePacketList.Add((byte)(arrList.Sum() & 0xff));
            return bytePacketList.ToArray();
        }

        /// <summary>
        /// charKeyUpPacket
        /// </summary>
        private readonly byte[] _charKeyUpPacket =
            [0x57, 0xAB, 0x00, 0x02, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c];

        /// <summary>
        /// 
        /// mediaKeyUpPacket
        /// </summary>
        private readonly byte[] _mediaKeyUpPacket = [0x57, 0xAB, 0x00, 0x03, 0x04, 0x02, 0x00, 0x00, 0x00, 0x0B];


        // public byte ChipVersion;
        // public byte ChipStatus;
        // public bool NumLock;
        // public bool CapsLock;
        // public bool ScrollLock;

        // public void GetInfo()
        // {
        //     byte[] getInfoPacket = [0x57, 0xAB, 0x00, (byte)CommandCode.GetInfo, 0x00, 0x03];
        //     var resultString = SendPacket(getInfoPacket);
        //
        //     ChipVersion = (byte)resultString[0];
        //     ChipStatus = (byte)resultString[1];
        //     var flagByte = (byte)resultString[2];
        //     NumLock = ((int)(flagByte & 0x01) > 0 ? true : false);
        //     CapsLock = (flagByte & 0x02) > 0;
        //     ScrollLock = (flagByte & 0x04) > 0;
        // }

        /// <summary>
        /// Push key
        /// </summary>
        /// <param name="CMD">KetType</param>
        /// <param name="keyGroup"></param>
        /// <param name="k0">special key code</param>
        /// <param name="k1">key code #1</param>
        /// <param name="k2">key code #2</param>
        /// <param name="k3">key code #3</param>
        /// <param name="k4">key code #4</param>
        /// <param name="k5">key code #5</param>
        /// <param name="k6">key code #6</param>
        public void KeyDown(KeyGroup keyGroup, byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0,
            byte k6 = 0)
        {
            // ========================
            // keyDownPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x02} + LEN{0x08} + DATA{k0, 0x00, k1, k2, k3, k4, k5, k6}
            // CMD = KeyGroup
            // ========================
            List<int> keyDownPacketListInt =
                keyGroup == KeyGroup.CharKey
                    ? [0x57, 0xAB, 0x00, (int)keyGroup, 0x08, k0, 0x00, k1, k2, k3, k4, k5, k6]
                    : [0x57, 0xAB, 0x00, (int)keyGroup, 0x04, k0, k1, k2, k3];

            var keyDownPacket = CreatePacketArray(keyDownPacketListInt, true);
            SendPacket(keyDownPacket);
        }

        public void KeyUpAll()
        {
            KeyUpAll(KeyGroup.CharKey);
        }

        public void KeyUpAll(KeyGroup keyGroup)
        {
            SendPacket(keyGroup == KeyGroup.CharKey ? _charKeyUpPacket : _mediaKeyUpPacket);
        }

        public void KeyDown(SpecialKeyCode specialKeyCode)
        {
            KeyDown(KeyGroup.CharKey, (byte)specialKeyCode, 0x00);
        }

        private void CharKeyType(byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0)
        {
            KeyDown(KeyGroup.CharKey, k0, k1, k2, k3, k4, k5, k6);
            Thread.Sleep(10);
            KeyUpAll(KeyGroup.CharKey);
        }

        public void CharKeyType(string typeString)
        {
            if (typeString.Length < 1) return;

            foreach (var dat in from s in typeString
                     where _charKeyTable.ContainsKey(s.ToString())
                     select _charKeyTable[s.ToString()])
            {
                if (dat[0] == 0x02)
                {
                    KeyDown(KeyGroup.CharKey, 0x00, 0x00, 0xE1, 0, 0, 0, 0);
                    Thread.Sleep(10);
                    KeyDown(KeyGroup.CharKey, 0x00, dat[1], 0xE1, 0, 0, 0, 0);
                    Thread.Sleep(10);
                    KeyDown(KeyGroup.CharKey, 0x00, 0x00, 0xE1, 0, 0, 0, 0);
                }
                else
                {
                    KeyDown(KeyGroup.CharKey, 0x00, dat[1], 0x00, 0, 0, 0, 0);
                }

                Thread.Sleep(10);
                KeyDown(KeyGroup.CharKey, 0x00, 0x00, 0x00, 0, 0, 0, 0);
            }

            KeyDown(KeyGroup.CharKey, 0x00, 0x00, 0x00, 0, 0, 0, 0);
        }

        public void MouseMoveRel(int x, int y, bool keyHold, MouseButtonCode button)
        {
            x = Math.Min(x, 127);
            x = Math.Max(x, -128);
            if (x < 0) x = 0x100 + x;

            y = Math.Min(y, 127);
            y = Math.Max(y, -128);
            if (y < 0) y = 0x100 + y;
            // ========================
            // mouseMoveRelPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01, 0x00}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseMoveRelPacketListInt = [0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00];
            if (keyHold)
            {
                mouseMoveRelPacketListInt[6] = (byte)button;
            }

            mouseMoveRelPacketListInt.Add((byte)(x));
            mouseMoveRelPacketListInt.Add((byte)(y));
            mouseMoveRelPacketListInt.Add(0x00);

            var mouseMoveRelPacket = CreatePacketArray(mouseMoveRelPacketListInt, true);
            SendPacket(mouseMoveRelPacket);
        }

        public void MouseMoveRel(int x, int y)
        {
            MouseMoveRel(x, y, false, MouseButtonCode.Left);
        }

        public void MouseMoveRelNoConvert(byte x, byte y)
        {
            // ========================
            // mouseMoveRelPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01, 0x00}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseMoveRelPacketListInt =
            [
                0x57,
                0xAB,
                0x00,
                0x05,
                0x05,
                0x01,
                0x00,
                (byte)(x),
                (byte)(y),
                0x00
            ];

            var mouseMoveRelPacket = CreatePacketArray(mouseMoveRelPacketListInt, true);
            SendPacket(mouseMoveRelPacket);
        }


        /// <summary>
        /// mouseButtonUpPacket
        /// </summary>
        private readonly byte[] _mouseButtonUpPacketForMac =
            [0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F];

        public bool MouseButtonDownForMac(MouseButtonCode buttonCode)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseButtonDownPacketListInt =
                [0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00];
            mouseButtonDownPacketListInt[6] = (int)buttonCode;

            var mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, true);
            SendPacket(mouseButtonDownPacket);
            return true;
        }

        public bool MouseButtonUpAllForMac()
        {
            SendPacket(_mouseButtonUpPacketForMac);
            return true;
        }

        public void MouseScrollForMac(int value)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseButtonDownPacketListInt =
                [0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00];
            mouseButtonDownPacketListInt[11] = (int)value;
            mouseButtonDownPacketListInt[6] = (int)MouseButtonCode.Middle;

            var mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, true);
            SendPacket(mouseButtonDownPacket);
        }

        /// <summary>
        /// mouseButtonUpPacket
        /// </summary>
        private readonly byte[] _mouseButtonUpPacket =
            [0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 0x0D];

        public bool MouseButtonDown(MouseButtonCode buttonCode)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseButtonDownPacketListInt = [0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00];
            mouseButtonDownPacketListInt[6] = (int)buttonCode;

            var mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, true);
            SendPacket(mouseButtonDownPacket);
            return true;
        }

        public bool ToggleMouseButton(bool keyDown, MouseButtonCode mouseCode)
        {
            return keyDown ? MouseButtonDown(mouseCode) : MouseButtonUpAll();
        }

        public bool ToggleMouseButtonForMac(bool keyDown, MouseButtonCode mouseCode)
        {
            return keyDown ? MouseButtonDownForMac(mouseCode) : MouseButtonUpAllForMac();
        }


        public bool MouseButtonUpAll()
        {
            SendPacket(_mouseButtonUpPacket);
            return true;
        }

        private void MouseClick(MouseButtonCode buttonCode)
        {
            MouseButtonDown(buttonCode);
            MouseButtonUpAll();
        }

        public void MouseDoubleClick()
        {
            MouseClick(MouseButtonCode.Left);
            MouseClick(MouseButtonCode.Left);
        }

        public void MouseScroll(int scrollCount)
        {
            // ========================
            // mouseScrollPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            scrollCount = Math.Min(127, scrollCount);
            scrollCount = Math.Max(-128, scrollCount);
            if (scrollCount < 0)
                scrollCount = 0x100 + scrollCount;


            List<int> mouseScrollPacketListInt =
            [
                0x57,
                0xAB,
                0x00,
                0x05,
                0x05,
                0x01,
                0x00,
                0x00,
                0x00,
                0x00,
                scrollCount
            ];

            var mouseScrollPacket = CreatePacketArray(mouseScrollPacketListInt, true);
            SendPacket(mouseScrollPacket);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            //_serialPort.Close();
            //_serialPort.Dispose();
        }
    }

    public enum SpecialKeyCode : byte
    {
        Enter = 0x28,
        Escape = 0x29,
        Backspace = 0x2A,
        Tab = 0x2B,
        Spacebar = 0x2C,
        CapsLock = 0x39,
        F1 = 0x3A,
        F2 = 0x3B,
        F3 = 0x3C,
        F4 = 0x3D,
        F5 = 0x3E,
        F6 = 0x3F,
        F7 = 0x40,
        F8 = 0x41,
        F9 = 0x42,
        F10 = 0x43,
        F11 = 0x44,
        F12 = 0x45,
        Printscreen = 0x46,
        ScrollLock = 0x47,
        Pause = 0x48,
        Insert = 0x49,
        Home = 0x4A,
        Pageup = 0x4B,
        Delete = 0x4C,
        End = 0x4D,
        Pagedown = 0x4E,
        Rightarrow = 0x4F,
        Leftarrow = 0x50,
        Downarrow = 0x51,
        Uparrow = 0x52,
        Application = 0x65,
        LeftCtrl = 0xE0,
        LeftShift = 0xE1,
        LeftAlt = 0xE2,
        LeftWindows = 0xE3,

        RightCtrl = 0xE4,
        RightShift = 0xE5,
        RightAlt = 0xE6,
        RightWindows = 0xE7,

        Ctrl = 0xE4,
        Shift = 0xE5,
        Alt = 0xE6,
        Windows = 0xE7,
    }

    public enum MouseButtonCode : byte
    {
        Left = 0x01,
        Right = 0x02,
        Middle = 0x04,
    }

    /// <summary>
    /// KeyGroup
    /// </summary>
    public enum KeyGroup : byte
    {
        CharKey = 0x02,
        MediaKey = 0x03,
    }

    public enum MediaKey
    {
        Eject,
        Cdstop,
        Prevtrack,
        Nexttrack,
        Playpause,
        Mute,
        Volumedown,
        Volumeup,
    }
}