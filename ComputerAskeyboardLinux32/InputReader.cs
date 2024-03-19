using System;
using System.IO;
using System.Threading.Tasks;

public class InputReader : IDisposable
{
    public delegate void RaiseKeyPress(KeyPressEvent e);

    public delegate void RaiseMouseMove(MouseMoveEvent e);

    public event RaiseKeyPress OnKeyPress;
    public event RaiseMouseMove OnMouseMove;

    private int BufferLength = 16;

    private byte[] _buffer;

    private FileStream _stream;
    private bool _disposing;

    private string _path = "";

    private bool Platform64
    {
        get { return BitConverter.GetBytes((long)(16)).Length == 8; }
    }

    public InputReader(string path)
    {
        BufferLength = Platform64 ? 24 : 16;
        _buffer = new byte[BufferLength];
        _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        this._path = path;
        new Task(new Action(Run)).Start();
        //Task.Run(new Action(Run));
    }

    private void Run()
    {
        var offset = _buffer.Length == 24 ? 15 : 7;
        while (true)
        {
            if (_disposing)
                break;

            _stream.Read(_buffer, 0, BufferLength);

            var type = BitConverter.ToInt16(new[] { _buffer[offset + 1], _buffer[offset + 2] }, 0);
            var code = BitConverter.ToInt16(new[] { _buffer[offset + 3], _buffer[offset + 4] }, 0);
            var value = BitConverter.ToInt32(
                new[] { _buffer[offset + 5], _buffer[offset + 6], _buffer[offset + 7], _buffer[offset + 8] }, 0);

            var eventType = (EventType)type;

            switch (eventType)
            {
                case EventType.EV_KEY:
                    HandleKeyPressEvent(code, value);
                    break;
                case EventType.EV_REL:
                    var axis = (MouseAxis)code;
                    var e = new MouseMoveEvent(axis, value);
                    if (OnMouseMove != null)
                    {
                        OnMouseMove.Invoke(e);
                    }

                    break;
            }
        }
    }

    private void HandleKeyPressEvent(short code, int value)
    {
        var c = (EventCode)code;
        var s = (KeyState)value;
        var e = new KeyPressEvent(c, s);
        e.DevicePath = this._path;
        if (OnKeyPress != null)
        {
            OnKeyPress.Invoke(e);
        }
    }

    public void Dispose()
    {
        _disposing = true;
        _stream.Dispose();
        _stream = null;
    }
}