using System;

public class KeyPressEvent : EventArgs
{
    public KeyPressEvent(EventCode code, KeyState state)
    {
        Code = code;
        State = state;
    }

    public string DevicePath { get; set; }

    public EventCode Code { get; }

    public KeyState State { get; }
}