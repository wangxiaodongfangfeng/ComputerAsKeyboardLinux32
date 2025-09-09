namespace ComputerAsKeyboardInterface
{
    public class ThinkpadKeyMapTo9329
    {
        public readonly Dictionary<int, byte> KeyMaps;
        public readonly Dictionary<int, List<byte>> MediaKeyMap;

        public ThinkpadKeyMapTo9329()
        {
            MediaKeyMap = new Dictionary<int, List<byte>>
            {
                { 113, [0x02, 0x04, 0x00, 0x00] }, //mute;
                { 115, [0x02, 0x01, 0x00, 0x00] }, //V+
                { 114, [0x02, 0x02, 0x00, 0x00] }, //V-;
                { 164, [0x02, 0x08, 0x00, 0x00] }, //Pause; 
                { 166, [0x02, 0x40, 0x00, 0x00] }, //Stop
                { 163, [0x02, 0x10, 0x00, 0x00] }, //NextTrack;
                { 165, [0x02, 0x20, 0x00, 0x00] } //Previous;
            };

            KeyMaps = new Dictionary<int, byte>
            {
                { 30, 0x04 }, //A
                { 48, 0x05 }, //B
                { 46, 0x06 }, //C
                { 32, 0x07 }, //D
                { 18, 0x08 }, //E
                { 33, 0x09 }, //F
                { 34, 0x0A }, //G
                { 35, 0x0B }, //H
                { 23, 0x0C }, //I
                { 36, 0x0D }, //J
                { 37, 0x0E }, //K
                { 38, 0x0F }, //L
                { 50, 0x10 }, //M
                { 49, 0x11 }, //N
                { 24, 0x12 }, //O
                { 25, 0x13 }, //P
                { 16, 0x14 }, //Q
                { 19, 0x15 }, //R
                { 31, 0x16 }, //S
                { 20, 0x17 }, //T
                { 22, 0x18 }, //U
                { 47, 0x19 }, //V
                { 17, 0x1A }, //W
                { 45, 0x1B }, //X
                { 21, 0x1C }, //Y
                { 44, 0x1D }, //Z
                { 59, 0x3A }, //F1
                { 60, 0x3B }, //F2
                { 61, 0x3C }, //F3
                { 62, 0x3D }, //F4
                { 63, 0x3E }, //F5
                { 64, 0x3F }, //F6
                { 65, 0x40 }, //F7
                { 66, 0x41 }, //F8
                { 67, 0x42 }, //F9
                { 68, 0x43 }, //F10
                { 87, 0x44 }, //F11
                { 88, 0x45 }, //F12
                { 41, 0x35 }, //Grave
                { 1, 0x29 }, //ESC
                { 2, 0x1E }, //1
                { 3, 0x1F }, //2
                { 4, 0x20 }, //3
                { 5, 0x21 }, //4
                { 6, 0x22 }, //5
                { 7, 0x23 }, //6
                { 8, 0x24 }, //7
                { 9, 0x25 }, //8
                { 10, 0x26 }, //9
                { 11, 0x27 }, //0
                { 12, 0x2D }, //-
                { 13, 0x2E }, //=
                { 14, 0x2A }, // backspace
                { 15, 0x2B }, // tab
                { 58, 0x39 }, // Capslock
                { 28, 0x28 }, //Enter
                { 26, 0x2F }, //LeftBrace
                { 27, 0x30 }, //RightBrace
                { 43, 0x31 }, //Backslash
                { 39, 0x33 }, //Semicolon
                { 40, 0x34 }, //Apostrophe
                { 51, 0x36 }, //Comma
                { 52, 0x37 }, //Dot
                { 53, 0x38 }, //Slash
                { 42, 0xE1 }, //LeftShift
                { 54, 0xE5 }, //RightShift
                { 29, 0xE0 }, //LeftCtrl
                { 125, 0xE3 }, //LeftMeta
                { 56, 0xE2 }, //LeftAlt
                { 57, 0x2C }, //Space
                { 100, 0xE6 }, //RightAlt
                { 127, 0x04 }, //Compose
                { 97, 0xE4 }, //RightCtrl
                { 103, 0x52 }, //Up
                { 108, 0x51 }, //Down
                { 105, 0x50 }, //Left
                { 106, 0x4F }, //Right
                { 158, 0x04 }, //Back
                { 159, 0x04 }, //Forward
                { 110, 0x49 }, //Insert
                { 111, 0x4C }, //Delete
                { 102, 0x4A }, //Home
                { 107, 0x4D }, //End
                { 104, 0x4B }, //Pageup
                { 109, 0x4E }, //Pagedown
                { 99, 0x46 }, //SysRq
                { 70, 0x47 }, //ScrollLock
                { 119, 0x48 }, //Pause
                { 143, 0x8F }
            };
        }
    }
}