using System;
using System.Collections.Generic;
using System.IO;

namespace ComputerAsKeyboardInterface
{
    public class AggregateMouseReader : IDisposable
    {
        private List<MouseReader> _readers = new List<MouseReader>();

        public event MouseReader.RaiseMouseMove OnMouseMove;

        public AggregateMouseReader(List<string> events)
        {
            foreach (var file in events)
            {
                var reader = new MouseReader(file);
                reader.OnMouseMove += ReaderOnOnMouseMove;
                _readers.Add(reader);
            }
        }

        public AggregateMouseReader()
        {
            var files = Directory.GetFiles("/dev/input/", "mouse*");
            foreach (var file in files)
            {
                var reader = new MouseReader(file);
                reader.OnMouseMove += ReaderOnOnMouseMove;
                _readers.Add(reader);
            }
        }

        private void ReaderOnOnMouseMove(MouseEvent e)
        {
            OnMouseMove?.Invoke(e);
        }

        public void Dispose()
        {
            foreach (var d in _readers)
            {
                d.OnMouseMove -= this.ReaderOnOnMouseMove;
                d.Dispose();
            }

            _readers = null;
        }
    }
}
