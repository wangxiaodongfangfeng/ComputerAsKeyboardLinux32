namespace ComputerAsKeyboardInterface.MouseRelated
{
    public class AggregateMouseReader : IDisposable
    {
        private List<MouseReader>? _readers = [];

        public event MouseReader.RaiseMouseMove? OnMouseMove;
        public event MouseReader.RaiseMouseScroll? OnMouseScroll;

        public AggregateMouseReader(List<string> events)
        {
            foreach (var reader in events.Select(file => new MouseReader(file)))
            {
                reader.OnMouseMove += ReaderOnOnMouseMove;
                reader.OnMouseScroll += ReaderOnOnMouseScroll;
                _readers?.Add(reader);
            }
        }

        private void ReaderOnOnMouseMove(MouseEvent e) => OnMouseMove?.Invoke(e);
        private void ReaderOnOnMouseScroll(MouseEvent e) => OnMouseScroll?.Invoke(e);


        public void Dispose()
        {
            if (_readers != null)
                foreach (var d in _readers)
                {
                    d.OnMouseMove -= this.ReaderOnOnMouseMove;
                    d.OnMouseScroll -= this.ReaderOnOnMouseScroll;
                    d.Dispose();
                }

            _readers = null;
        }
    }
}