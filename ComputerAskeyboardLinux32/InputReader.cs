using System;
using System.IO;
using System.Threading.Tasks;

public class InputReader : IDisposable
{
    public delegate void RaiseKeyPress(KeyPressEvent e);

    public delegate void RaiseMouseMove(MouseMoveEvent e);

    public event RaiseKeyPress OnKeyPress;
    public event RaiseMouseMove OnMouseMove;

    private const int BufferLength = 16;

    private readonly byte[] _buffer = new byte[BufferLength];

    private FileStream _stream;
    private bool _disposing;

    private string _path = "";

    public InputReader(string path)
    {
        _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        this._path = path;
        Task.Run(new Action(Run));
    }

    private void Run()
    {
        while (true)
        {
            if (_disposing)
                break;

            _stream.Read(_buffer, 0, BufferLength);

            var type = BitConverter.ToInt16(new[] { _buffer[8], _buffer[9] }, 0);
            var code = BitConverter.ToInt16(new[] { _buffer[10], _buffer[11] }, 0);
            var value = BitConverter.ToInt32(new[] { _buffer[12], _buffer[13], _buffer[14], _buffer[15] }, 0);

            var eventType = (EventType)type;

            switch (eventType)
            {
                case EventType.EV_KEY:
                    HandleKeyPressEvent(code, value);
                    break;
                case EventType.EV_REL:
                    var axis = (MouseAxis)code;
                    var e = new MouseMoveEvent(axis, value);
                    OnMouseMove?.Invoke(e);
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
        OnKeyPress?.Invoke(e);
    }

    public void Dispose()
    {
        _disposing = true;
        _stream.Dispose();
        _stream = null;
    }
}