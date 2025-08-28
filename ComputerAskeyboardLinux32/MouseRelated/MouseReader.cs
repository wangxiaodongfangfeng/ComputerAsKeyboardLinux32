namespace ComputerAsKeyboardInterface.MouseRelated
{
    public class MouseReader : IDisposable
    {
        public delegate void RaiseMouseMove(MouseEvent e);
        public event RaiseMouseMove? OnMouseMove;
        public delegate void RaiseMouseScroll(MouseEvent e);

        public event RaiseMouseScroll? OnMouseScroll;

        private const int BufferLength = 3;
        private readonly byte[] _buffer = new byte[BufferLength];
        private FileStream _stream;
        private bool _disposing;
        private readonly string _path;

        public MouseReader(string path)
        {
            _path = path;
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            new Task(Run).Start();
        }

        private void Run()
        {
            while (true)
            {
                if (_disposing)
                    break;

                try
                {
                    _stream.ReadExactly(_buffer, 0, BufferLength);
                    var dx = _buffer[1] - ((_buffer[0] & 0x10) != 0 ? 256 : 0);
                    var dy = _buffer[2] - ((_buffer[0] & 0x20) != 0 ? 256 : 0);
                    var button = _buffer[0] & 0x04; // Extract button state
                    //middle button is down
                    if (button > 0)
                    {
                        OnMouseScroll?.Invoke(new MouseEvent() { ScrollCount = -dy });
                    }
                    else
                    {
                        OnMouseMove?.Invoke(new MouseEvent() { X = dx, Y = -dy, Bx = _buffer[0], By = _buffer[1], DevicePath = _path });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public void Dispose()
        {
            _disposing = true;
            _stream.Dispose();
            _stream = null;
        }
    }
}
