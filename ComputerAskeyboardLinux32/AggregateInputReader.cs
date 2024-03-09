using System;
using System.Collections.Generic;
using System.IO;

public class AggregateInputReader : IDisposable
{
    private List<InputReader> _readers = new List<InputReader>();

    public event InputReader.RaiseKeyPress OnKeyPress;

    public event InputReader.RaiseMouseMove OnMouseMove;


    public AggregateInputReader(List<string> events)
    {
        foreach (var file in events)
        {
            var reader = new InputReader(file);

            reader.OnKeyPress += ReaderOnOnKeyPress;

            reader.OnMouseMove += ReaderOnOnMouseMove;

            _readers.Add(reader);
        }
    }

    public AggregateInputReader()
    {
        var files = Directory.GetFiles("/dev/input/", "event*");

        foreach (var file in files)
        {
            var reader = new InputReader(file);

            reader.OnKeyPress += ReaderOnOnKeyPress;

            reader.OnMouseMove += ReaderOnOnMouseMove;

            _readers.Add(reader);
        }
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
        foreach (var d in _readers)
        {
            d.OnKeyPress -= ReaderOnOnKeyPress;
            d.Dispose();
        }

        _readers = null;
    }
}