using System.IO.Ports;

namespace ComputerAsKeyboardInterface.KeyboardRelated
{
  public class Ch9329 : IKeyboard
  {
    public string PortName;
    public int BaudRate;
    private readonly int _xSize;
    private readonly int _ySize;

    private readonly SerialPort _serialPort;

    private readonly object _lockObject = new object();
    private readonly Queue<string> _messageLog = new Queue<string>();

    private const int MessageLogCount = 32;

    private void AddMessageLog(string message)
    {
      if (_messageLog.Count > MessageLogCount)
        _messageLog.Dequeue();
      _messageLog.Enqueue(message);
    }

    public string GetMessageLog() => string.Join("\r\n", _messageLog);


    public Ch9329(string portName = "COM5", int xSize = 1920, int ySize = 1080, int baudRate = 9600)
    {
      this.PortName = portName;
      this.BaudRate = baudRate;
      this._xSize = xSize;
      this._ySize = ySize;

      _serialPort = new SerialPort(portName, baudRate);

      _serialPort.Open();
      CreateCharKeyTable();
      CreateMediaKeyTable();
      CreateKeyTable();
    }

    private Dictionary<MediaKey, byte[]> _mediaKeyTable;


    private void CreateMediaKeyTable()
    {
      _mediaKeyTable = new Dictionary<MediaKey, byte[]>
      {
        { MediaKey.Eject, [0x02, 0x80, 0x00, 0x00] },
        { MediaKey.Cdstop, [0x02, 0x40, 0x00, 0x00] },
        { MediaKey.Prevtrack, [0x02, 0x20, 0x00, 0x00] },
        { MediaKey.Nexttrack, [0x02, 0x10, 0x00, 0x00] },
        { MediaKey.Playpause, [0x02, 0x08, 0x00, 0x00] },
        { MediaKey.Mute, [0x02, 0x04, 0x00, 0x00] },
        { MediaKey.Volumedown, [0x02, 0x02, 0x00, 0x00] },
        { MediaKey.Volumeup, [0x02, 0x01, 0x00, 0x00] }
      };
    }

    private Dictionary<byte[], string> _keyTable = [];

    //private void create

    /// <summary>
    /// create 109A KeyTable
    /// </summary>
    private void CreateKeyTable()
    {
      _keyTable = new Dictionary<byte[], string>();

      _keyTable.Add([0x00, 0x04], "A"); //001
      _keyTable.Add([0x00, 0x05], "B"); //002
      _keyTable.Add([0x00, 0x06], "C"); //003
      _keyTable.Add([0x00, 0x07], "D"); //004
      _keyTable.Add([0x00, 0x08], "E"); //005
      _keyTable.Add([0x00, 0x09], "F"); //006
      _keyTable.Add([0x00, 0x0A], "G"); //007
      _keyTable.Add([0x00, 0x0B], "H"); //008
      _keyTable.Add([0x00, 0x0C], "I"); //009
      _keyTable.Add([0x00, 0x0D], "J"); //010
      _keyTable.Add([0x00, 0x0E], "K"); //011
      _keyTable.Add([0x00, 0x0F], "L"); //012
      _keyTable.Add([0x00, 0x10], "M"); //013
      _keyTable.Add([0x00, 0x11], "N"); //014
      _keyTable.Add([0x00, 0x12], "O"); //015
      _keyTable.Add([0x00, 0x13], "P"); //016
      _keyTable.Add([0x00, 0x14], "Q"); //017
      _keyTable.Add([0x00, 0x15], "R"); //018
      _keyTable.Add([0x00, 0x16], "S"); //019
      _keyTable.Add([0x00, 0x17], "T"); //020
      _keyTable.Add([0x00, 0x18], "U"); //021
      _keyTable.Add([0x00, 0x19], "V"); //022
      _keyTable.Add([0x00, 0x1A], "W"); //023
      _keyTable.Add([0x00, 0x1B], "X"); //024
      _keyTable.Add([0x00, 0x1C], "Y"); //025
      _keyTable.Add([0x00, 0x1D], "Z"); //026
      _keyTable.Add([0x00, 0x1E], "1"); //027
      _keyTable.Add([0x00, 0x1F], "2"); //028
      _keyTable.Add([0x00, 0x20], "3"); //029
      _keyTable.Add([0x00, 0x21], "4"); //030
      _keyTable.Add([0x00, 0x22], "5"); //031
      _keyTable.Add([0x00, 0x23], "6"); //032
      _keyTable.Add([0x00, 0x24], "7"); //033
      _keyTable.Add([0x00, 0x25], "8"); //034
      _keyTable.Add([0x00, 0x26], "9"); //035
      _keyTable.Add([0x00, 0x27], "0"); //036
      _keyTable.Add([0x00, 0x28], "Enter"); //037
      _keyTable.Add([0x00, 0x29], "Esc"); //038
      _keyTable.Add([0x00, 0x2A], "Backspace"); //039
      _keyTable.Add([0x00, 0x2B], "Tab"); //040
      _keyTable.Add([0x00, 0x2C], "Spacebar"); //041
      _keyTable.Add([0x00, 0x2D], "-"); //042
      _keyTable.Add([0x00, 0x2E], "^"); //043
      _keyTable.Add([0x00, 0x2F], "@"); //044
      _keyTable.Add([0x00, 0x30], "["); //045
      _keyTable.Add([0x00, 0x31], "-----"); //046
      _keyTable.Add([0x00, 0x32], "]"); //047
      _keyTable.Add([0x00, 0x33], ";"); //048
      _keyTable.Add([0x00, 0x34], ":"); //049
      _keyTable.Add([0x00, 0x35], "半角/全角"); //050
      _keyTable.Add([0x00, 0x36], ","); //051
      _keyTable.Add([0x00, 0x37], "."); //052
      _keyTable.Add([0x00, 0x38], "/"); //053
      _keyTable.Add([0x00, 0x39], "Caps Lock"); //054
      _keyTable.Add([0x00, 0x3A], "F1"); //055
      _keyTable.Add([0x00, 0x3B], "F2"); //056
      _keyTable.Add([0x00, 0x3C], "F3"); //057
      _keyTable.Add([0x00, 0x3D], "F4"); //058
      _keyTable.Add([0x00, 0x3E], "F5"); //059
      _keyTable.Add([0x00, 0x3F], "F6"); //060
      _keyTable.Add([0x00, 0x40], "F7"); //061
      _keyTable.Add([0x00, 0x41], "F8"); //062
      _keyTable.Add([0x00, 0x42], "F9"); //063
      _keyTable.Add([0x00, 0x43], "F10"); //064
      _keyTable.Add([0x00, 0x44], "F11"); //065
      _keyTable.Add([0x00, 0x45], "F12"); //066
      _keyTable.Add([0x00, 0x46], "Print Screen"); //067
      _keyTable.Add([0x00, 0x47], "Scroll Lock"); //068
      _keyTable.Add([0x00, 0x48], "Pause"); //069
      _keyTable.Add([0x00, 0x49], "Insert"); //070
      _keyTable.Add([0x00, 0x4A], "Home"); //071
      _keyTable.Add([0x00, 0x4B], "Page Up"); //072
      _keyTable.Add([0x00, 0x4C], "Delete"); //073
      _keyTable.Add([0x00, 0x4D], "End"); //074
      _keyTable.Add([0x00, 0x4E], "Page Down"); //075
      _keyTable.Add([0x00, 0x4F], "→"); //076
      _keyTable.Add([0x00, 0x50], "←"); //077
      _keyTable.Add([0x00, 0x51], "↓"); //078
      _keyTable.Add([0x00, 0x52], "↑"); //079
      _keyTable.Add([0x00, 0x53], "Num Lock"); //080
      _keyTable.Add([0x00, 0x54], "Keypad /"); //081
      _keyTable.Add([0x00, 0x55], "Keypad *"); //082
      _keyTable.Add([0x00, 0x56], "Keypad -"); //083
      _keyTable.Add([0x00, 0x57], "Keypad +"); //084
      _keyTable.Add([0x00, 0x58], "Keypad Enter"); //085
      _keyTable.Add([0x00, 0x59], "Keypad 1"); //086
      _keyTable.Add([0x00, 0x5A], "Keypad 2"); //087
      _keyTable.Add([0x00, 0x5B], "Keypad 3"); //088
      _keyTable.Add([0x00, 0x5C], "Keypad 4"); //089
      _keyTable.Add([0x00, 0x5D], "Keypad 5"); //090
      _keyTable.Add([0x00, 0x5E], "Keypad 6"); //091
      _keyTable.Add([0x00, 0x5F], "Keypad 7"); //092
      _keyTable.Add([0x00, 0x60], "Keypad 8"); //093
      _keyTable.Add([0x00, 0x61], "Keypad 9"); //094
      _keyTable.Add([0x00, 0x62], "Keypad 0"); //095
      _keyTable.Add([0x00, 0x63], "Keypad ."); //096
      _keyTable.Add([0x00, 0x65], "Application"); //097
      _keyTable.Add([0x00, 0x87], "\\"); //098
      _keyTable.Add([0x00, 0x88], "ひらがな カタカナ"); //099
      _keyTable.Add([0x00, 0x89], "\\"); //100
      _keyTable.Add([0x00, 0x8A], "変換"); //101
      _keyTable.Add([0x00, 0x8B], "無変換"); //102
      _keyTable.Add([0x00, 0xE0], "Left Ctrl"); //103
      _keyTable.Add([0x00, 0xE1], "Left Shift"); //104
      _keyTable.Add([0x00, 0xE2], "Left Alt"); //105
      _keyTable.Add([0x00, 0xE3], "Left Windows"); //106
      _keyTable.Add([0x00, 0xE4], "Right Ctrl"); //107
      _keyTable.Add([0x00, 0xE5], "Right Shift"); //108
      _keyTable.Add([0x00, 0xE6], "Right Alt"); //109
      _keyTable.Add([0x00, 0xE7], "Right Windows"); //110

      _keyTable.Add([0x02, 0x1E], "!"); //111
      _keyTable.Add([0x02, 0x1F], "\""); //112
      _keyTable.Add([0x02, 0x20], "#"); //113
      _keyTable.Add([0x02, 0x21], "$"); //114
      _keyTable.Add([0x02, 0x22], "%"); //115
      _keyTable.Add([0x02, 0x23], "&"); //116
      _keyTable.Add([0x02, 0x24], "'"); //117
      _keyTable.Add([0x02, 0x25], "("); //118
      _keyTable.Add([0x02, 0x26], ")"); //119
      _keyTable.Add([0x02, 0x2D], "="); //120
      _keyTable.Add([0x02, 0x2E], "~"); //121
      _keyTable.Add([0x02, 0x2F], "`"); //122
      _keyTable.Add([0x02, 0x30], "{"); //123
      _keyTable.Add([0x02, 0x32], "}"); //124
      _keyTable.Add([0x02, 0x33], "+"); //125
      _keyTable.Add([0x02, 0x34], "*"); //126
      _keyTable.Add([0x02, 0x36], "<"); //127
      _keyTable.Add([0x02, 0x37], ">"); //128
      _keyTable.Add([0x02, 0x38], "?"); //129
      _keyTable.Add([0x00, 0x87], "_"); //130
      _keyTable.Add([0x00, 0x89], "｜"); //131
    }

    private Dictionary<string, byte[]> _charKeyTable;

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


    private string SendPacket(byte[] data)
    {
      lock (_lockObject)
      {
        _serialPort.Write(data, 0, data.Length);
        Thread.Sleep(20);
        return "";
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


    public byte ChipVersion;
    public byte ChipStatus;
    public bool NumLock;
    public bool CapsLock;
    public bool ScrollLock;

    public void GetInfo()
    {
      byte[] getInfoPacket = [0x57, 0xAB, 0x00, (byte)CommandCode.GetInfo, 0x00, 0x03];
      string resultString = SendPacket(getInfoPacket);

      ChipVersion = (byte)resultString[0];
      ChipStatus = (byte)resultString[1];
      byte flagByte = (byte)resultString[2];
      NumLock = ((int)(flagByte & 0x01) > 0 ? true : false);
      CapsLock = (flagByte & 0x02) > 0;
      ScrollLock = (flagByte & 0x04) > 0;
    }

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
      // ========================
      // keyDownPacketContents
      // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x02} + LEN{0x08} + DATA{k0, 0x00, k1, k2, k3, k4, k5, k6}
      // CMD = KeyGroup
      // ========================
      List<int> keyDownPacketListInt =
        keyGroup == KeyGroup.CharKey
          ? [0x57, 0xAB, 0x00, (int)keyGroup, 0x08, k0, 0x00, k1, k2, k3, k4, k5, k6]
          : [0x57, 0xAB, 0x00, (int)keyGroup, 0x04, k0, k1, k2, k3];

      byte[] keyDownPacket = CreatePacketArray(keyDownPacketListInt, true);

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
      int xAbs = (int)(4096 * x / _xSize);
      int yAbs = (int)(4096 * y / _ySize);

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

      byte[] mouseMoveRelPacket = CreatePacketArray(mouseMoveRelPacketListInt, true);
      SendPacket(mouseMoveRelPacket);
    }


    /// <summary>
    /// mouseButtonUpPacket
    /// </summary>
    private readonly byte[] _mouseButtonUpPacketForMac =
      [0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F];

    public void MouseButtonDownForMac(MouseButtonCode buttonCode)
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
      List<int> mouseButtonDownPacketListInt =
        [0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00];
      mouseButtonDownPacketListInt[11] = (int)value;
      mouseButtonDownPacketListInt[6] = (int)MouseButtonCode.Middle;

      byte[] mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, true);
      SendPacket(mouseButtonDownPacket);
    }

    /// <summary>
    /// mouseButtonUpPacket
    /// </summary>
    private readonly byte[] _mouseButtonUpPacket = [0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 0x0D];

    public void MouseButtonDown(MouseButtonCode buttonCode)
    {
      // ========================
      // mouseClickPacketContents
      // HEAD{0x57, 0xAB} + ADDR{0x00} + CMD{0x05} + LEN{0x05} + DATA{0x01}
      // CMD = 0x05 : USB mouse relative mode
      // ========================
      List<int> mouseButtonDownPacketListInt = [0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00];
      mouseButtonDownPacketListInt[6] = (int)buttonCode;

      byte[] mouseButtonDownPacket = CreatePacketArray(mouseButtonDownPacketListInt, true);
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

      byte[] mouseScrollPacket = CreatePacketArray(mouseScrollPacketListInt, true);
      return SendPacket(mouseScrollPacket);
    }

    public void Dispose()
    {
      this._serialPort.Close();
      ;
      this._serialPort.Dispose();
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

  public enum CommandCode : byte
  {
    GetInfo = 0x01,
    SendKbGeneralData = 0x02,
    SendKbMediaData = 0x03,
    SendMsAbsData = 0x04,
    SendMsRelData = 0x05,
    ReadMyHidData = 0x07,
    GetParaCfg = 0x08,
    GetUsbString = 0x0A,
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