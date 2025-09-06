namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public class KeyPressEvent(EventCode code, KeyState state) : EventArgs
    {
        public string? DevicePath { get; init; }

        public EventCode Code { get; set; } = code;

        public KeyState State { get; set; } = state;
    }
}