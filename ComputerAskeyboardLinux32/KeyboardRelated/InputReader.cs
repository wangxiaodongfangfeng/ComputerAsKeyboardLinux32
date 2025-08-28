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

        public InputReader(string path)
        {
            _bufferLength = Platform64 ? 24 : 16;
            _buffer = new byte[_bufferLength];
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this._path = path;
            new Task(Run).Start();
            //Task.Run(new Action(Run));
        }

        private void Run()
        {
            var offset = _buffer.Length == 24 ? 15 : 7;
            Program.WriteLogOnScreen($"Start Monitoring Program for input {_path}");
            var started = false;
            while (true)
            {
                if (_disposing)
                    break;
                if (!started) Program.WriteLogOnScreen($"Start to Read data from input stream {_path}");

                _stream?.ReadExactly(_buffer, 0, _bufferLength);

                if (!started)
                {
                    Program.WriteLogOnScreen($"Read data from input stream {_path}");
                    started = true;
                }


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
                DevicePath = this._path
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