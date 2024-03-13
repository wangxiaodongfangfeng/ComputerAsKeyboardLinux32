using System;
using System.IO;
using System.Threading.Tasks;

namespace ComputerAsKeyboardInterface
{
    public class MouseReader : IDisposable
    {
        public delegate void RaiseMouseMove(MouseEvent e);

        public delegate void RaiseMouseScroll(MouseEvent e);

        public event RaiseMouseMove OnMouseMove;
        public event RaiseMouseScroll OnMouseScroll;

        private const int BufferLength = 3;

        private readonly byte[] _buffer = new byte[BufferLength];

        private FileStream _stream;
        private bool _disposing;

        private string _path;

        public MouseReader(string path)
        {
            _path = path;
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Task.Run(new Action(Run));
        }

        private void Run()
        {
            while (true)
            {
                if (_disposing)
                    break;

                try
                {
                    _stream.Read(_buffer, 0, BufferLength);
                    int dx = _buffer[1] - ((_buffer[0] & 0x10) != 0 ? 256 : 0);
                    int dy = _buffer[2] - ((_buffer[0] & 0x20) != 0 ? 256 : 0);
                    int button = _buffer[0] & 0x04; // Extract button state
                    //middle button is down
                    if (button > 0)
                    {
                        OnMouseScroll?.Invoke(new MouseEvent() { ScrollCount = Math.Abs(dy) > 12 ? -dy / 4 : -dy });
                    }
                    else
                    {
                        OnMouseMove?.Invoke(new MouseEvent() { X = dx, Y = -dy, BX = _buffer[0], BY = _buffer[1], DevicePath = _path });
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
