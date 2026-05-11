use clap::Parser;

#[derive(Parser, Debug, Clone)]
#[command(author, version, about = "ThinkPad Keyboard to USB Controller", long_about = None)]
pub struct Args {
    #[arg(short = 'd', long, default_value = None, help = "Serial port device path (e.g., /dev/ttyUSB0). If not specified, will auto-scan.")]
    pub device: Option<String>,
    
    #[arg(short, long, default_value = "false", help = "Enable macOS mode (swap Alt/Meta)")]
    pub macos: bool,
    
    #[arg(short = 'f', long, default_value = "false", help = "Enable fingerprint support")]
    pub fprint: bool,
    
    #[arg(short = 'm', long, default_value = "false", help = "Mute debug output")]
    pub mute: bool,
    
    #[arg(short, long, default_value = "false", help = "Enable queue mode for packet sending")]
    pub queue: bool,
    
    #[arg(short = 'r', long, default_value = "9600", help = "Baud rate for serial port")]
    pub baud_rate: u32,
    
    #[arg(short, long, default_value = "false", help = "Run in background mode")]
    pub background: bool,
    
    #[arg(short, long, default_value = "false", help = "Run as service")]
    pub service: bool,
    
    #[arg(short = 'x', long, default_value = "9869", help = "XInput port")]
    pub xinput_port: u16,
    
    #[arg(short = 'b', long, default_value = "false", help = "Enable Bluetooth port")]
    pub bluetooth_port: bool,
}
