using PowerArgs;

namespace ComputerAsKeyboardInterface
{
    internal class StartArgs
    {

        [ArgDescription("Specifies whether to enable verbose mode")]
        [DefaultValue(false)]
        public bool Verbose { get; set; }

        [ArgDescription("if you use MacOS, means switch the left meta/leftwin and alt")]
        [DefaultValue(false)]
        public bool MacOs { get; set; }

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

        [ArgDescription("specify the application running as a servie")]
        [DefaultValue(false)]
        public bool RunAsService { get; set; }

        [ArgDescription("specify the port to use to communicate with xinput service")]
        [DefaultValue(9869)]
        public int XInputServicePort { get; set; }
    }
}