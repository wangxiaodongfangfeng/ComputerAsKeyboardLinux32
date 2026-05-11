use clap::Parser;
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::Duration;

mod args;
mod keyboard;
mod mappings;
mod serial;
mod input;
#[cfg(test)]
mod test_utils;

use args::Args;
use keyboard::KeyboardState;
use serial::auto_scan_serial_port;
use input::{discover_mouse_devices, discover_input_devices, load_devices, process_input_device, process_mouse_device};

fn main() {
    let cli = Args::parse();

    let state = Arc::new(Mutex::new(KeyboardState::new()));

    {
        let mut state_guard = state.lock().unwrap();
        state_guard.macos = cli.macos;
        state_guard.mute = cli.mute;
        state_guard.queue = cli.queue;
        state_guard.xinput_port = cli.xinput_port;
        state_guard.service = cli.service;
        state_guard.load_password();
    }

    let serial_port_path = if let Some(ref device) = cli.device {
        device.clone()
    } else {
        match auto_scan_serial_port() {
            Some(p) => p,
            None => {
                eprintln!("No serial ports found");
                return;
            }
        }
    };

    {
        let baud_rate = if serial_port_path.contains("ttyUSB") {
            println!("ttyUSB device detected, forcing baud rate to 9600");
            9600
        } else {
            cli.baud_rate
        };

        match serialport::new(&serial_port_path, baud_rate)
            .data_bits(serialport::DataBits::Eight)
            .stop_bits(serialport::StopBits::One)
            .parity(serialport::Parity::None)
            .flow_control(serialport::FlowControl::None)
            .timeout(Duration::from_millis(0))
            .open() {
            Ok(port) => {
                let mut state_guard = state.lock().unwrap();
                let _ = port.clear(serialport::ClearBuffer::All);
                std::thread::sleep(Duration::from_millis(150));
                state_guard.start_sender_thread(port);
                println!("Connected to serial port: {} at {} baud", serial_port_path, baud_rate);
            }
            Err(e) => {
                eprintln!("Failed to open serial port: {}", e);
                return;
            }
        }
    }

    let mut input_devices = load_devices();
    if input_devices.is_empty() {
        input_devices = discover_input_devices();
    }

    println!("Monitoring {} input devices", input_devices.len());

    for device in input_devices {
        let state_clone = Arc::clone(&state);
        thread::spawn(move || {
            process_input_device(&device, state_clone);
        });
    }

    let mouse_devices = discover_mouse_devices();
    if !mouse_devices.is_empty() {
        println!("Monitoring {} mouse devices (high-speed mode)", mouse_devices.len());
        for device in mouse_devices {
            let state_clone = Arc::clone(&state);
            thread::spawn(move || {
                process_mouse_device(&device, state_clone);
            });
        }
    }

    if !cli.background {
        println!("Press Ctrl+C to exit");
        let _ = std::io::stdin().read_line(&mut String::new());
    } else {
        loop {
            thread::sleep(Duration::from_secs(1));
        }
    }

    {
        let mut state_guard = state.lock().unwrap();
        state_guard.key_up_all(keyboard::KeyGroup::CharKey);
    }
}