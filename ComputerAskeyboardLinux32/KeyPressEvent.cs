using System;

public class KeyPressEvent : EventArgs
{
    public KeyPressEvent(EventCode code, KeyState state)
    {
        this.Code = code;
        this.State = state;
    }

    public string DevicePath { get; set; }

    public EventCode Code { get; set; }

    public KeyState State { get; set; }
}