namespace ComputerAsKeyboardInterface.MouseRelated
{
    public class MouseEvent : EventArgs
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte Bx { get; set; }
        public byte By { get; set; }
        public string DevicePath { get; set; }

        public int ScrollCount { get; set; }
    }
}