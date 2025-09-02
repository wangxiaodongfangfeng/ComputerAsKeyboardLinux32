using System.Collections.Concurrent;
using ComputerAsKeyboardInterface.MouseRelated;
using PowerArgs;

namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public class AggregateInputReader : IDisposable
    {
        private ConcurrentBag<InputReader>? _readers = [];
        public event InputReader.RaiseKeyPress? OnKeyPress;
        public event InputReader.RaiseMouseMove? OnMouseMove;
    
        public AggregateInputReader(List<string> inputs)
        {
            if (inputs.Count == 0)
            {
                inputs = Directory.GetFiles("/dev/input/", "event*").ToList();
            }

            inputs.AsParallel().ForEach(file =>
            {
                var reader = new InputReader(file);
                reader.OnKeyPress += ReaderOnOnKeyPress;
                reader.OnMouseMove += ReaderOnOnMouseMove;
                _readers?.Add(reader);
            });
        }

        public AggregateInputReader()
        {
            var files = Directory.GetFiles("/dev/input/", "event*");

            files.AsParallel().ForEach(file =>
            {
                var reader = new InputReader(file);
                reader.OnKeyPress += ReaderOnOnKeyPress;
                reader.OnMouseMove += ReaderOnOnMouseMove;
                _readers?.Add(reader);
            });
        }

        private void ReaderOnOnKeyPress(KeyPressEvent e)
        {
            OnKeyPress?.Invoke(e);
        }

        private void ReaderOnOnMouseMove(MouseMoveEvent e)
        {
            OnMouseMove?.Invoke(e);
        }

        public void Dispose()
        {
            if (_readers != null)
                foreach (var d in _readers)
                {
                    d.OnKeyPress -= ReaderOnOnKeyPress;
                    d.Dispose();
                }

            _readers = null;
        }
    }
}