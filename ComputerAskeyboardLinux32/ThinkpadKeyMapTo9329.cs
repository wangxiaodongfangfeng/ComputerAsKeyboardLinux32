
using System.Collections.Generic;

namespace ComputerAsKeyboardLinux32
{
    public class ThinkpadKeyMapTo9329
    {
        public Dictionary<int, byte> keyMaps;
        public Dictionary<int, List<byte>> mediaKeyMap;
        public ThinkpadKeyMapTo9329()
        {
            mediaKeyMap = new Dictionary<int, List<byte>>();
            mediaKeyMap.Add(113, new List<byte>() { 0x02, 0x04, 0x00, 0x00 }); //mute;
            mediaKeyMap.Add(115, new List<byte>() { 0x02, 0x01, 0x00, 0x00 }); //V+
            mediaKeyMap.Add(114, new List<byte>() { 0x02, 0x02, 0x00, 0x00 }); //V-;
            mediaKeyMap.Add(164, new List<byte>() { 0x02, 0x08, 0x00, 0x00 }); //Pause; 
            mediaKeyMap.Add(166, new List<byte>() { 0x02, 0x40, 0x00, 0x00 }); //Stop
            mediaKeyMap.Add(163, new List<byte>() { 0x02, 0x10, 0x00, 0x00 }); //NextTrack;
            mediaKeyMap.Add(165, new List<byte>() { 0x02, 0x20, 0x00, 0x00 }); //Previous;

            keyMaps = new Dictionary<int, byte>();
            keyMaps.Add(30, 0x04);//A
            keyMaps.Add(48, 0x05);//B
            keyMaps.Add(46, 0x06);//C
            keyMaps.Add(32, 0x07);//D
            keyMaps.Add(18, 0x08);//E
            keyMaps.Add(33, 0x09);//F
            keyMaps.Add(34, 0x0A);//G
            keyMaps.Add(35, 0x0B);//H
            keyMaps.Add(23, 0x0C);//I
            keyMaps.Add(36, 0x0D);//J
            keyMaps.Add(37, 0x0E);//K
            keyMaps.Add(38, 0x0F);//L
            keyMaps.Add(50, 0x10);//M
            keyMaps.Add(49, 0x11);//N
            keyMaps.Add(24, 0x12);//O
            keyMaps.Add(25, 0x13);//P
            keyMaps.Add(16, 0x14);//Q
            keyMaps.Add(19, 0x15);//R
            keyMaps.Add(31, 0x16);//S
            keyMaps.Add(20, 0x17);//T
            keyMaps.Add(22, 0x18);//U
            keyMaps.Add(47, 0x19);//V
            keyMaps.Add(17, 0x1A);//W
            keyMaps.Add(45, 0x1B);//X
            keyMaps.Add(21, 0x1C);//Y
            keyMaps.Add(44, 0x1D);//Z

            keyMaps.Add(59, 0x3A);//F1
            keyMaps.Add(60, 0x3B);//F2
            keyMaps.Add(61, 0x3C);//F3
            keyMaps.Add(62, 0x3D);//F4
            keyMaps.Add(63, 0x3E);//F5
            keyMaps.Add(64, 0x3F);//F6
            keyMaps.Add(65, 0x40);//F7
            keyMaps.Add(66, 0x41);//F8
            keyMaps.Add(67, 0x42);//F9
            keyMaps.Add(68, 0x43);//F10
            keyMaps.Add(87, 0x44);//F11
            keyMaps.Add(88, 0x45);//F12

            keyMaps.Add(41, 0x35);//Grave
            keyMaps.Add(1, 0x29);//ESC
            keyMaps.Add(2, 0x1E);//1
            keyMaps.Add(3, 0x1F);//2
            keyMaps.Add(4, 0x20);//3
            keyMaps.Add(5, 0x21);//4
            keyMaps.Add(6, 0x22);//5
            keyMaps.Add(7, 0x23);//6
            keyMaps.Add(8, 0x24);//7
            keyMaps.Add(9, 0x25);//8
            keyMaps.Add(10, 0x26);//9
            keyMaps.Add(11, 0x27);//0
            keyMaps.Add(12, 0x2D);//-
            keyMaps.Add(13, 0x2E);//=

            keyMaps.Add(14, 0x2A);// backspace
            keyMaps.Add(15, 0x2B);// tab
            keyMaps.Add(58, 0x39);// Capslock
            keyMaps.Add(28, 0x28);//Enter

            keyMaps.Add(26, 0x2F);//LeftBrace
            keyMaps.Add(27, 0x30);//RightBrace
            keyMaps.Add(43, 0x31);//Backslash
            keyMaps.Add(39, 0x33);//Semicolon
            keyMaps.Add(40, 0x34);//Apostrophe
            keyMaps.Add(51, 0x36);//Comma
            keyMaps.Add(52, 0x37);//Dot
            keyMaps.Add(53, 0x38);//Slash

            keyMaps.Add(42, 0xE1);//LeftShift
            keyMaps.Add(54, 0xE5);//RightShift
            keyMaps.Add(29, 0xE0);//LeftCtrl
            keyMaps.Add(125, 0xE3);//LeftMeta
            keyMaps.Add(56, 0xE2);//LeftAlt
            keyMaps.Add(57, 0x2C);//Space
            keyMaps.Add(100, 0xE6);//RightAlt
            keyMaps.Add(127, 0x04);//Compose
            keyMaps.Add(97, 0xE4);//RightCtrl

            keyMaps.Add(103, 0x52);//Up
            keyMaps.Add(108, 0x51);//Down
            keyMaps.Add(105, 0x50);//Left
            keyMaps.Add(106, 0x4F);//Right
            keyMaps.Add(158, 0x04);//Back
            keyMaps.Add(159, 0x04);//Forward

            keyMaps.Add(110, 0x49);//Insert
            keyMaps.Add(111, 0x4C);//Delete
            keyMaps.Add(102, 0x4A);//Home
            keyMaps.Add(107, 0x4D);//End
            keyMaps.Add(104, 0x4B);//Pageup
            keyMaps.Add(109, 0x4E);//Pagedown
            keyMaps.Add(99, 0x46);//SysRq
            keyMaps.Add(70, 0x47);//ScrollLock
            keyMaps.Add(119, 0x48);//Pause


        }
    }
}
