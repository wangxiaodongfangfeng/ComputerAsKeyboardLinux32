using System;

namespace ComputerAsKeyboardLinux32
{
    public class MouseMoveEvent : EventArgs
    {
        public MouseMoveEvent(MouseAxis axis, int amount)
        {
            Axis = axis;
            Amount = amount;
        }

        public MouseAxis Axis { get; set; }

        public int Amount { get; set; }
    }
}