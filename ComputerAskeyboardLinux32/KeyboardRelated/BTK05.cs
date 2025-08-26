using System.IO.Ports;

namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public class Btk05 : IKeyboard
    {
        public string PortName;
        public int BaudRate;
        public int XSize;
        public int YSize;

        readonly SerialPort _serialPort;
        private readonly object _lockObject = new object();
        public Queue<string> MessageLog = new Queue<string>();

        public int MessageLogCount = 32;

        private void AddMessageLog(string message)
        {
            if (MessageLog.Count > MessageLogCount)
            {
                MessageLog.Dequeue();
            }

            ;
            MessageLog.Enqueue(message);
        }

        public string GetMessageLog()
        {
            return String.Join("\r\n", MessageLog);
        }


        public Btk05(string portName = "COM5", int xSize = 1920, int ySize = 1080, int baudRate = 9600)
        {
            this.PortName = portName;
            this.BaudRate = baudRate;
            this.XSize = xSize;
            this.YSize = ySize;

            _serialPort = new SerialPort(portName, baudRate);

            _serialPort.Open();
            CreateCharKeyTable();
            CreateMediaKeyTable();
            CreateKeyTable();
        }

        private Dictionary<MediaKey, byte[]> _mediaKeyTable;


        private void CreateMediaKeyTable()
        {
            _mediaKeyTable = new Dictionary<MediaKey, byte[]>();

            _mediaKeyTable.Add(MediaKey.Eject, [0x02, 0x80, 0x00, 0x00]);
            _mediaKeyTable.Add(MediaKey.Cdstop, [0x02, 0x40, 0x00, 0x00]);
            _mediaKeyTable.Add(MediaKey.Prevtrack, [0x02, 0x20, 0x00, 0x00]);
            _mediaKeyTable.Add(MediaKey.Nexttrack, [0x02, 0x10, 0x00, 0x00]);
            _mediaKeyTable.Add(MediaKey.Playpause, [0x02, 0x08, 0x00, 0x00]);
            _mediaKeyTable.Add(MediaKey.Mute, [0x02, 0x04, 0x00, 0x00]);
            _mediaKeyTable.Add(MediaKey.Volumedown, [0x02, 0x02, 0x00, 0x00]);
            _mediaKeyTable.Add(MediaKey.Volumeup, [0x02, 0x01, 0x00, 0x00]);
        }

        public Dictionary<byte[], string> KeyTable;

        //private void create

        /// <summary>
        /// create 109A KeyTable
        /// </summary>
        private void CreateKeyTable()
        {
            KeyTable = new Dictionary<byte[], string>();

            KeyTable.Add([0x00, 0x04], "A"); //001
            KeyTable.Add([0x00, 0x05], "B"); //002
            KeyTable.Add([0x00, 0x06], "C"); //003
            KeyTable.Add([0x00, 0x07], "D"); //004
            KeyTable.Add([0x00, 0x08], "E"); //005
            KeyTable.Add([0x00, 0x09], "F"); //006
            KeyTable.Add([0x00, 0x0A], "G"); //007
            KeyTable.Add([0x00, 0x0B], "H"); //008
            KeyTable.Add([0x00, 0x0C], "I"); //009
            KeyTable.Add([0x00, 0x0D], "J"); //010
            KeyTable.Add([0x00, 0x0E], "K"); //011
            KeyTable.Add([0x00, 0x0F], "L"); //012
            KeyTable.Add([0x00, 0x10], "M"); //013
            KeyTable.Add([0x00, 0x11], "N"); //014
            KeyTable.Add([0x00, 0x12], "O"); //015
            KeyTable.Add([0x00, 0x13], "P"); //016
            KeyTable.Add([0x00, 0x14], "Q"); //017
            KeyTable.Add([0x00, 0x15], "R"); //018
            KeyTable.Add([0x00, 0x16], "S"); //019
            KeyTable.Add([0x00, 0x17], "T"); //020
            KeyTable.Add([0x00, 0x18], "U"); //021
            KeyTable.Add([0x00, 0x19], "V"); //022
            KeyTable.Add([0x00, 0x1A], "W"); //023
            KeyTable.Add([0x00, 0x1B], "X"); //024
            KeyTable.Add([0x00, 0x1C], "Y"); //025
            KeyTable.Add([0x00, 0x1D], "Z"); //026
            KeyTable.Add([0x00, 0x1E], "1"); //027
            KeyTable.Add([0x00, 0x1F], "2"); //028
            KeyTable.Add([0x00, 0x20], "3"); //029
            KeyTable.Add([0x00, 0x21], "4"); //030
            KeyTable.Add([0x00, 0x22], "5"); //031
            KeyTable.Add([0x00, 0x23], "6"); //032
            KeyTable.Add([0x00, 0x24], "7"); //033
            KeyTable.Add([0x00, 0x25], "8"); //034
            KeyTable.Add([0x00, 0x26], "9"); //035
            KeyTable.Add([0x00, 0x27], "0"); //036
            KeyTable.Add([0x00, 0x28], "Enter"); //037
            KeyTable.Add([0x00, 0x29], "Esc"); //038
            KeyTable.Add([0x00, 0x2A], "Backspace"); //039
            KeyTable.Add([0x00, 0x2B], "Tab"); //040
            KeyTable.Add([0x00, 0x2C], "Spacebar"); //041
            KeyTable.Add([0x00, 0x2D], "-"); //042
            KeyTable.Add([0x00, 0x2E], "^"); //043
            KeyTable.Add([0x00, 0x2F], "@"); //044
            KeyTable.Add([0x00, 0x30], "["); //045
            KeyTable.Add([0x00, 0x31], "-----"); //046
            KeyTable.Add([0x00, 0x32], "]"); //047
            KeyTable.Add([0x00, 0x33], ";"); //048
            KeyTable.Add([0x00, 0x34], ":"); //049
            KeyTable.Add([0x00, 0x35], "半角/全角"); //050
            KeyTable.Add([0x00, 0x36], ","); //051
            KeyTable.Add([0x00, 0x37], "."); //052
            KeyTable.Add([0x00, 0x38], "/"); //053
            KeyTable.Add([0x00, 0x39], "Caps Lock"); //054
            KeyTable.Add([0x00, 0x3A], "F1"); //055
            KeyTable.Add([0x00, 0x3B], "F2"); //056
            KeyTable.Add([0x00, 0x3C], "F3"); //057
            KeyTable.Add([0x00, 0x3D], "F4"); //058
            KeyTable.Add([0x00, 0x3E], "F5"); //059
            KeyTable.Add([0x00, 0x3F], "F6"); //060
            KeyTable.Add([0x00, 0x40], "F7"); //061
            KeyTable.Add([0x00, 0x41], "F8"); //062
            KeyTable.Add([0x00, 0x42], "F9"); //063
            KeyTable.Add([0x00, 0x43], "F10"); //064
            KeyTable.Add([0x00, 0x44], "F11"); //065
            KeyTable.Add([0x00, 0x45], "F12"); //066
            KeyTable.Add([0x00, 0x46], "Print Screen"); //067
            KeyTable.Add([0x00, 0x47], "Scroll Lock"); //068
            KeyTable.Add([0x00, 0x48], "Pause"); //069
            KeyTable.Add([0x00, 0x49], "Insert"); //070
            KeyTable.Add([0x00, 0x4A], "Home"); //071
            KeyTable.Add([0x00, 0x4B], "Page Up"); //072
            KeyTable.Add([0x00, 0x4C], "Delete"); //073
            KeyTable.Add([0x00, 0x4D], "End"); //074
            KeyTable.Add([0x00, 0x4E], "Page Down"); //075
            KeyTable.Add([0x00, 0x4F], "→"); //076
            KeyTable.Add([0x00, 0x50], "←"); //077
            KeyTable.Add([0x00, 0x51], "↓"); //078
            KeyTable.Add([0x00, 0x52], "↑"); //079
            KeyTable.Add([0x00, 0x53], "Num Lock"); //080
            KeyTable.Add([0x00, 0x54], "Keypad /"); //081
            KeyTable.Add([0x00, 0x55], "Keypad *"); //082
            KeyTable.Add([0x00, 0x56], "Keypad -"); //083
            KeyTable.Add([0x00, 0x57], "Keypad +"); //084
            KeyTable.Add([0x00, 0x58], "Keypad Enter"); //085
            KeyTable.Add([0x00, 0x59], "Keypad 1"); //086
            KeyTable.Add([0x00, 0x5A], "Keypad 2"); //087
            KeyTable.Add([0x00, 0x5B], "Keypad 3"); //088
            KeyTable.Add([0x00, 0x5C], "Keypad 4"); //089
            KeyTable.Add([0x00, 0x5D], "Keypad 5"); //090
            KeyTable.Add([0x00, 0x5E], "Keypad 6"); //091
            KeyTable.Add([0x00, 0x5F], "Keypad 7"); //092
            KeyTable.Add([0x00, 0x60], "Keypad 8"); //093
            KeyTable.Add([0x00, 0x61], "Keypad 9"); //094
            KeyTable.Add([0x00, 0x62], "Keypad 0"); //095
            KeyTable.Add([0x00, 0x63], "Keypad ."); //096
            KeyTable.Add([0x00, 0x65], "Application"); //097
            KeyTable.Add([0x00, 0x87], "\\"); //098
            KeyTable.Add([0x00, 0x88], "ひらがな カタカナ"); //099
            KeyTable.Add([0x00, 0x89], "\\"); //100
            KeyTable.Add([0x00, 0x8A], "変換"); //101
            KeyTable.Add([0x00, 0x8B], "無変換"); //102
            KeyTable.Add([0x00, 0xE0], "Left Ctrl"); //103
            KeyTable.Add([0x00, 0xE1], "Left Shift"); //104
            KeyTable.Add([0x00, 0xE2], "Left Alt"); //105
            KeyTable.Add([0x00, 0xE3], "Left Windows"); //106
            KeyTable.Add([0x00, 0xE4], "Right Ctrl"); //107
            KeyTable.Add([0x00, 0xE5], "Right Shift"); //108
            KeyTable.Add([0x00, 0xE6], "Right Alt"); //109
            KeyTable.Add([0x00, 0xE7], "Right Windows"); //110

            KeyTable.Add([0x02, 0x1E], "!"); //111
            KeyTable.Add([0x02, 0x1F], "\""); //112
            KeyTable.Add([0x02, 0x20], "#"); //113
            KeyTable.Add([0x02, 0x21], "$"); //114
            KeyTable.Add([0x02, 0x22], "%"); //115
            KeyTable.Add([0x02, 0x23], "&"); //116
            KeyTable.Add([0x02, 0x24], "'"); //117
            KeyTable.Add([0x02, 0x25], "("); //118
            KeyTable.Add([0x02, 0x26], ")"); //119
            KeyTable.Add([0x02, 0x2D], "="); //120
            KeyTable.Add([0x02, 0x2E], "~"); //121
            KeyTable.Add([0x02, 0x2F], "`"); //122
            KeyTable.Add([0x02, 0x30], "{"); //123
            KeyTable.Add([0x02, 0x32], "}"); //124
            KeyTable.Add([0x02, 0x33], "+"); //125
            KeyTable.Add([0x02, 0x34], "*"); //126
            KeyTable.Add([0x02, 0x36], "<"); //127
            KeyTable.Add([0x02, 0x37], ">"); //128
            KeyTable.Add([0x02, 0x38], "?"); //129
            KeyTable.Add([0x00, 0x87], "_"); //130
            KeyTable.Add([0x00, 0x89], "｜"); //131
        }

        private Dictionary<string, byte[]> _charKeyTable;

        /// <summary>
        /// create 109A CharKeyTable
        /// </summary>
        private void CreateCharKeyTable()
        {
            _charKeyTable = new Dictionary<string, byte[]>();

            _charKeyTable.Add("0", [0, (byte)(0x27)]);
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

        private string SendPacket(byte[] data)
        {
            lock (_lockObject)
            {
                _serialPort.Write(data, 0, data.Length);
                Thread.Sleep(20);
            }

            // lock (lockObject)
            // {
            //     string resultMessage = serialPort.ReadExisting();
            //
            //     addMessageLog(data.ToString() + "|" + resultMessage);
            //     return resultMessage;
            // }
            return "";
        }

        private byte[] CreatePacketArray(List<int> arrList, bool addCheckSum)
        {
            List<byte> bytePacketList = arrList.ConvertAll(b => (byte)b);
            if (addCheckSum) bytePacketList.Add((byte)(arrList.Sum() & 0xff));
            return bytePacketList.ToArray();
        }

        /// <summary>
        /// charKeyUpPacket
        /// </summary>
        readonly byte[] _charKeyUpPacket = [0x0C, 0x00, 0xA1, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        /// <summary>
        /// 
        /// mediaKeyUpPacket
        /// </summary>
        readonly byte[] _mediaKeyUpPacket = [0x57, 0xAB, 0x00, 0x03, 0x04, 0x02, 0x00, 0x00, 0x00, 0x0B];


        /// <summary>
        /// Push key
        /// </summary>
        /// <param name="CMD">KetType</param>
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
            List<int> keyDownPacketListInt = [0x0C, 0x00, 0xA1, 0x01, k0, 0x00, k1, k2, k3, k4, k5, k6];
            byte[] keyDownPacket = CreatePacketArray(keyDownPacketListInt, false);
            SendPacket(keyDownPacket);
        }


        public void KeyUpAll()
        {
            KeyUpAll(KeyGroup.CharKey);
        }

        public void KeyUpAll(KeyGroup keyGroup)
        {
            if (keyGroup == KeyGroup.CharKey)
            {
                SendPacket(_charKeyUpPacket);
            }
            else
            {
                SendPacket(_mediaKeyUpPacket);
            }

            ;
        }

        public void KeyDown(SpecialKeyCode specialKeyCode)
        {
            KeyDown(KeyGroup.CharKey, (byte)specialKeyCode, 0x00);
        }

        public void CharKeyType(byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0)
        {
            KeyDown(KeyGroup.CharKey, k0, k1, k2, k3, k4, k5, k6);
            KeyUpAll(KeyGroup.CharKey);
        }

        public void MediaKeyType(MediaKey mediaKey)
        {
            byte[] dat = _mediaKeyTable[mediaKey];
            KeyDown(KeyGroup.MediaKey, dat[0], dat[1], dat[2], dat[3]);
            KeyUpAll(KeyGroup.MediaKey);
        }

        public void CharKeyType(string typeString)
        {
            if (typeString.Length < 1) return;

            foreach (char s in typeString)
            {
                if (_charKeyTable.ContainsKey(s.ToString()))
                {
                    byte[] dat = _charKeyTable[s.ToString()];
                    CharKeyType(dat[0], dat[1]);
                }
            }
        }

        public void MouseMoveAbs(int x, int y)
        {
            int xAbs = (int)(4096 * x / XSize);
            int yAbs = (int)(4096 * y / YSize);

            // ========================
            // mouseMoveAbsPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x04} + LEN{0x07} + DATA{0x02, 0x00, [x],[x],[y],[y], 0x00}
            // CMD = 0x04 : USB mouse absolute mode
            // ========================
            List<int> mouseMoveAbsPacketListInt =
            [
                0x57,
                0xAB,
                0x00,
                0x04,
                0x07,
                0x02,
                0x00,
                (byte)(xAbs & 0xff),
                (byte)(xAbs >> 8),
                (byte)(yAbs & 0xff),
                (byte)(yAbs >> 8),
                0x00
            ];

            byte[] mouseMoveAbsPacket = CreatePacketArray(mouseMoveAbsPacketListInt, true);
            SendPacket(mouseMoveAbsPacket);
        }


        public void MouseMoveRel(int x, int y, bool keyHold, MouseButtonCode button)
        {
            if (x > 127)
            {
                x = 127;
            }

            ;
            if (x < -128)
            {
                x = -128;
            }

            ;

            if (y > 127)
            {
                y = 127;
            }

            ;
            if (y < -128)
            {
                y = -128;
            }

            ;

            // ========================
            // mouseMoveRelPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01, 0x00}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseMoveRelPacketListInt = [0x0B, 0x00, 0xA1, 0x05, 0x00];
            if (keyHold)
            {
                mouseMoveRelPacketListInt[4] = (byte)button;
            }

            byte[] bytesx = BitConverter.GetBytes((short)x);
            byte[] bytesy = BitConverter.GetBytes((short)y);


            mouseMoveRelPacketListInt.Add((byte)(bytesx[0]));
            mouseMoveRelPacketListInt.Add((byte)(bytesx[1]));
            mouseMoveRelPacketListInt.Add((byte)(bytesy[0]));
            mouseMoveRelPacketListInt.Add((byte)(bytesy[1]));
            mouseMoveRelPacketListInt.Add(0x00);
            mouseMoveRelPacketListInt.Add(0x00);

            byte[] mouseMoveRelPacket = CreatePacketArray(mouseMoveRelPacketListInt, false);
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

            byte[] mouseMoveRelPacket = CreatePacketArray(mouseMoveRelPacketListInt, true);
            SendPacket(mouseMoveRelPacket);
        }


        /// <summary>
        /// mouseButtonUpPacket
        /// </summary>
        readonly byte[] _mouseButtonUpPacketForMac =
            [0x0B, 0x00, 0xA1, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        public void MouseButtonDownForMac(MouseButtonCode buttonCode)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseButtonDownPacketListInt = [0x0B, 0x00, 0xA1, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
            mouseButtonDownPacketListInt[4] = (int)buttonCode;

            byte[] mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, false);
            SendPacket(mouseButtonDownPacket);
        }

        public void MouseButtonUpAllForMac()
        {
            SendPacket(_mouseButtonUpPacketForMac);
        }

        public void MouseScrollForMac(int value)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseButtonDownPacketListInt = [0x0B, 0x00, 0xA1, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
            byte[] bytes = BitConverter.GetBytes((short)value);
            mouseButtonDownPacketListInt[9] = bytes[0];
            mouseButtonDownPacketListInt[10] = bytes[1];
            mouseButtonDownPacketListInt[4] = (int)MouseButtonCode.Middle;

            byte[] mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, false);
            SendPacket(mouseButtonDownPacket);
        }


        /// <summary>
        /// mouseButtonUpPacket
        /// </summary>
        readonly byte[] _mouseButtonUpPacket = [0x0B, 0x00, 0xA1, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        public void MouseButtonDown(MouseButtonCode buttonCode)
        {
            // ========================
            // mouseClickPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            List<int> mouseButtonDownPacketListInt = [0x0B, 0x00, 0xA1, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
            mouseButtonDownPacketListInt[4] = (int)buttonCode;

            byte[] mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, false);
            SendPacket(mouseButtonDownPacket);
        }

        public void MouseButtonUpAll()
        {
            SendPacket(_mouseButtonUpPacket);
        }

        public void MouseClick(MouseButtonCode buttonCode)
        {
            MouseButtonDown(buttonCode);
            MouseButtonUpAll();
        }

        public void MouseDoubleClick()
        {
            MouseClick(MouseButtonCode.Left);
            MouseClick(MouseButtonCode.Left);
        }

        public string MouseScroll(int scrollCount)
        {
            // ========================
            // mouseScrollPacketContents
            // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
            // CMD = 0x05 : USB mouse relative mode
            // ========================
            if (scrollCount > 127)
            {
                scrollCount = 127;
            }

            ;
            if (scrollCount < -128)
            {
                scrollCount = -128;
            }

            ;
            if (scrollCount < 0)
            {
                scrollCount = 0x100 + scrollCount;
            }

            ;

            List<int> mouseScrollPacketListInt = [0x0B, 0x00, 0xA1, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00];

            short value = (short)scrollCount;
            byte[] bytes = BitConverter.GetBytes(value);

            mouseScrollPacketListInt.Add(bytes[1]);
            mouseScrollPacketListInt.Add(bytes[0]);

            byte[] mouseScrollPacket = CreatePacketArray(mouseScrollPacketListInt, false);
            return SendPacket(mouseScrollPacket);
        }

        public void Dispose()
        {
            this._serialPort.Close();
            this._serialPort.Dispose();
        }
    }
}