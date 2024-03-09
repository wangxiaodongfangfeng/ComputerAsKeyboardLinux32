
using PowerArgs;


public class StartArgs
{

    [ArgDescription("whether scan device automatically")]
    [DefaultValue(false)]
    public bool AutoScan { get; set; }

    [ArgDescription("where to scan when Device is not specified")]
    [DefaultValue("/dev/")]
    public string ScanPath { get; set; }

    [ArgDescription("Specifies whether to enable verbose mode")]
    [DefaultValue(true)]
    public bool Verbose { get; set; }

    [ArgDescription("The device we want to use, default value is /dev/ttyUSB0")]
    [DefaultValue("/dev/ttyUSB0")]
    public string Device { get; set; }

    [ArgDescription("if you use MacOS, means switch the left meta/leftwin and alt")]
    [DefaultValue(false)]
    public bool MacOS { get; set; }

    [ArgDescription("mouse device")]
    [DefaultValue("mouse0")]
    public string Mouse { get; set; }

}
