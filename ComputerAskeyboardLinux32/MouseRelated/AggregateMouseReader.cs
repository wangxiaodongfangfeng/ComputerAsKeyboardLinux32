namespace ComputerAsKeyboardInterface.MouseRelated
{
    public class AggregateMouseReader : IDisposable
    {
        private List<MouseReader>? _readers = [];

        public event MouseReader.RaiseMouseMove? OnMouseMove;

        public AggregateMouseReader(List<string> events)
        {
            foreach (var reader in events.Select(file => new MouseReader(file)))
            {
                reader.OnMouseMove += ReaderOnOnMouseMove;
                _readers?.Add(reader);
            }
        }

        public AggregateMouseReader()
        {
            var files = Directory.GetFiles("/dev/input/", "mouse*");
            foreach (var file in files)
            {
                var reader = new MouseReader(file);
                reader.OnMouseMove += ReaderOnOnMouseMove;
                _readers?.Add(reader);
            }
        }

        private void ReaderOnOnMouseMove(MouseEvent e)
        {
            OnMouseMove?.Invoke(e);
        }

        public void Dispose()
        {
            if (_readers != null)
                foreach (var d in _readers)
                {
                    d.OnMouseMove -= this.ReaderOnOnMouseMove;
                    d.Dispose();
                }

            _readers = null;
        }
    }
}
