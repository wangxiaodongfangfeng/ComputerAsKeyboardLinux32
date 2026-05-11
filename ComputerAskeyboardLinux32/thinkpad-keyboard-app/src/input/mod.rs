use std::fs::{self, File};
use std::io::Read;
use std::path::Path;
use std::sync::{Arc, Mutex};

use crate::keyboard::KeyboardState;

#[cfg(test)]
mod tests;

const EV_KEY: u16 = 1;

pub fn load_devices() -> Vec<String> {
    if Path::exists(Path::new(".devices")) {
        if let Ok(lines) = fs::read_to_string(".devices") {
            return lines.lines().map(|s| s.to_string()).collect();
        }
    }
    
    Vec::new()
}

pub fn discover_input_devices() -> Vec<String> {
    let mut devices = Vec::new();
    
    if let Ok(files) = fs::read_dir("/dev/input/") {
        devices = files
            .filter_map(|entry| {
                let entry = entry.ok()?;
                let path = entry.path();
                if path.to_string_lossy().contains("event") {
                    Some(path.to_string_lossy().to_string())
                } else {
                    None
                }
            })
            .collect();
    }
    
    devices
}

pub fn process_input_device(device_path: &str, state: Arc<Mutex<KeyboardState>>) {
    let mut file = match File::open(device_path) {
        Ok(f) => f,
        Err(e) => {
            eprintln!("Failed to open {}: {}", device_path, e);
            return;
        }
    };
    
    let mut buf = vec![0u8; 24]; // Linux input event size (struct input_event)
    
    loop {
        match file.read_exact(&mut buf) {
            Ok(_) => {
                let type_ = u16::from_le_bytes([buf[16], buf[17]]);
                let code = u16::from_le_bytes([buf[18], buf[19]]);
                let value = i32::from_le_bytes([buf[20], buf[21], buf[22], buf[23]]);
                
                let mut state_guard = state.lock().unwrap();
                
                // state_guard is a MutexGuard that provides exclusive access to the KeyboardState
                // It is obtained by locking the Arc<Mutex<KeyboardState>> to safely share
                // mutable state between multiple threads. The guard automatically releases
                // the lock when it goes out of scope.

                if type_ == EV_KEY {
                    state_guard.handle_key_event(code, value);
                }
            }
            Err(e) => {
                eprintln!("Error reading from {}: {}", device_path, e);
                std::thread::sleep(std::time::Duration::from_millis(10));
            }
        }
    }
}

pub fn process_mouse_device(device_path: &str, state: Arc<Mutex<KeyboardState>>) {
    let mut file = match File::open(device_path) {
        Ok(f) => f,
        Err(e) => {
            eprintln!("Failed to open mouse {}: {}", device_path, e);
            return;
        }
    };

    let mut buf = [0u8; 3]; // Mouse protocol: 3 bytes (button, X, Y)

    loop {
        match file.read_exact(&mut buf) {
            Ok(_) => {
                let button = buf[0] & 0x07;
                let dx = buf[1] as i8 as i32;
                let dy = -(buf[2] as i8 as i32);

                let mut state_guard = state.lock().unwrap();

                if button & 0x04 != 0 {
                    state_guard.mouse_scroll(dy);
                } else {
                    if dx != 0 {
                        state_guard.mouse_move_rel(dx, 0);
                    }
                    if dy != 0 {
                        state_guard.mouse_move_rel(0, dy);
                    }
                }
            }
            Err(_e) => {
                std::thread::sleep(std::time::Duration::from_millis(1));
            }
        }
    }
}

pub fn discover_mouse_devices() -> Vec<String> {
    let mut devices = Vec::new();
    
    if let Ok(files) = fs::read_dir("/dev/input/") {
        devices = files
            .filter_map(|entry| {
                let entry = entry.ok()?;
                let path = entry.path();
                let path_str = path.to_string_lossy().to_string();
                if path_str.contains("mouse") || path_str.contains("mice") {
                    Some(path_str)
                } else {
                    None
                }
            })
            .collect();
    }
    
    devices
}
