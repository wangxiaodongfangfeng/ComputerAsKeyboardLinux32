﻿using System;

namespace ComputerAsKeyboardLinux32
{
    public class MouseEvent : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte BX { get; set; }
        public byte BY { get; set; }
        public string DevicePath { get; set; }

        public int ScrollCount { get; set; }
    }
}