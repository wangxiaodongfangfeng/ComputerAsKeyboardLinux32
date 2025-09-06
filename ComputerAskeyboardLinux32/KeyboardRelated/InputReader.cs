using ComputerAsKeyboardInterface.MouseRelated;

namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public class InputReader : IDisposable
    {
        public delegate void RaiseKeyPress(KeyPressEvent e);

        public delegate void RaiseMouseMove(MouseMoveEvent e);

        public event RaiseKeyPress? OnKeyPress;
        public event RaiseMouseMove? OnMouseMove;

        private readonly int _bufferLength;
        private readonly byte[] _buffer;
        private FileStream? _stream;
        private bool _disposing;
        private readonly string _path;

        private static bool Platform64 => Environment.Is64BitOperatingSystem;
        private const int LengthOfX64 = 24;
        private const int LengthOfX86 = 16;
        private const int OffsetOfX64 = 15;
        private const int OffsetOfX86 = 7;

        public InputReader(string path)
        {
            _bufferLength = Platform64 ? LengthOfX64 : LengthOfX86;
            _buffer = new byte[_bufferLength];
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _path = path;
            _ = Run();
        }

        private async Task Run()
        {
            var offset = _buffer.Length == LengthOfX64 ? OffsetOfX64 : OffsetOfX86;
            while (true)
            {
                if (_disposing || _stream == null)
                    break;

                await _stream.ReadExactlyAsync(_buffer, 0, _bufferLength);


                var type = BitConverter.ToInt16([_buffer[offset + 1], _buffer[offset + 2]], 0);
                var code = BitConverter.ToInt16([_buffer[offset + 3], _buffer[offset + 4]], 0);
                var value = BitConverter.ToInt32(
                    [_buffer[offset + 5], _buffer[offset + 6], _buffer[offset + 7], _buffer[offset + 8]], 0);

                var eventType = (EventType)type;

                switch (eventType)
                {
                    case EventType.EvKey:
                        HandleKeyPressEvent(code, value);
                        break;
                    case EventType.EvRel:
                        var axis = (MouseAxis)code;
                        var e = new MouseMoveEvent(axis, value);
                        OnMouseMove?.Invoke(e);
                        break;
                    case EventType.EvSyn:
                    case EventType.EvAbs:
                    case EventType.EvMsc:
                    case EventType.EvSw:
                    case EventType.EvLed:
                    case EventType.EvSnd:
                    case EventType.EvRep:
                    case EventType.EvFf:
                    case EventType.EvPwr:
                    case EventType.EvFfStatus:
                    default:
                        break;
                }
            }
        }

        private void HandleKeyPressEvent(short code, int value)
        {
            var c = (EventCode)code;
            var s = (KeyState)value;
            var e = new KeyPressEvent(c, s)
            {
                DevicePath = _path
            };
            OnKeyPress?.Invoke(e);
        }

        public void Dispose()
        {
            _disposing = true;
            _stream?.Dispose();
            _stream = null;
        }
    }
}