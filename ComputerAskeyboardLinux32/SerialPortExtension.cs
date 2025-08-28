using System.IO.Ports;

namespace ComputerAsKeyboardInterface;

public static class SerialPortExtension
{
    private static Dictionary<string, SerialPort> AllAvailablePorts { get; set; } = new();

    public static SerialPort? CurrentSerialPort { get; set; }


    public static SerialPort? GetSerialPort(string portName)
    {
        if (!AllAvailablePorts.ContainsKey(portName)) return null;
        var kvp = AllAvailablePorts.FirstOrDefault(kvp => kvp.Key == portName);
        return kvp.Value;
    }

    public static void AddSerialPort(string portName)
    {
        if (AllAvailablePorts.TryGetValue(portName, out var port))
        {
            port.Dispose();
            AllAvailablePorts.Remove(portName);
        }

        port = new SerialPort(portName);
        port.Open();
        AllAvailablePorts.Add(portName, port);
        CurrentSerialPort ??= port;
    }

    public static void RemoveSerialPort(string portName)
    {
        var port = AllAvailablePorts[portName];
        AllAvailablePorts.Remove(portName);
        if (CurrentSerialPort?.PortName == port.PortName)
        {
            CurrentSerialPort = AllAvailablePorts.Count > 0 ? AllAvailablePorts.Values.First() : null;
        }
    }

    public static void SwitchSerialPort(int index)
    {
        CurrentSerialPort = AllAvailablePorts.Values.ToList()[(index) % (AllAvailablePorts.Count)];
        Program.WriteLogOnScreen($"Current Serial Port Switched {CurrentSerialPort.PortName}");
    }

    public static void SwitchSerialPort()
    {
        if (CurrentSerialPort == null)
        {
            CurrentSerialPort = AllAvailablePorts.Values.First();
            return;
        }

        var index = 0;
        foreach (var port in AllAvailablePorts.Values)
        {
            if (port.PortName == CurrentSerialPort.PortName)
            {
                index = (index + 1) % AllAvailablePorts.Count;
                break;
            }

            index++;
        }

        SwitchSerialPort(index);
    }
}