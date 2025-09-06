using PowerArgs;

namespace ComputerAsKeyboardInterface
{
    internal class StartArgs
    {
        [ArgDescription("whether scan device automatically")]
        [DefaultValue(true)]
        public bool AutoScan { get; set; }

        [ArgDescription("where to scan when Device is not specified")]
        [DefaultValue("/dev/")]
        public string? ScanPath { get; set; }

        [ArgDescription("Specifies whether to enable verbose mode")]
        [DefaultValue(true)]
        public bool Verbose { get; set; }

        [ArgDescription("The device we want to use, default value is /dev/ttyUSB0")]
        [DefaultValue("/dev/ttyUSB0")]
        public string? Device { get; set; }

        [ArgDescription("if you use MacOS, means switch the left meta/leftwin and alt")]
        [DefaultValue(false)]
        public bool MacOs { get; set; }

        [ArgDescription("mouse device")]
        [DefaultValue("mouse0")]
        public string? Mouse { get; set; }


        [ArgDescription("start-bluetooth-version")]
        [DefaultValue(false)]
        public bool Bluetooth { get; set; }

        [ArgDescription("use fingerprint")]
        [DefaultValue(false)]
        public bool Fprint { get; set; }

        [ArgDescription("If use queue to send command")]
        [DefaultValue(false)]
        public bool Queue { get; set; }

        [ArgDescription("specify baudRate of the chip")]
        [DefaultValue(9600)]
        public int BaudRate { get; set; }

        [ArgDescription("if use bluetooth serial port")]
        [DefaultValue(false)]
        public bool BluetoothPort { get; set; }

        [ArgDescription("running this program in background")]
        [DefaultValue(false)]
        public bool Background { get; set; }
    }
}