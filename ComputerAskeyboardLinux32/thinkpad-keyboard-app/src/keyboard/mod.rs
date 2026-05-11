use serialport::SerialPort;
use std::io::{Read, Write};
use std::net::TcpStream;
use std::path::Path;
use std::sync::mpsc::{self, Sender};
use std::thread;
use std::time::{Instant, Duration};

use crate::mappings::{CHAR_KEY_MAP, KEY_MAPS, MEDIA_KEY_MAPS, SPECIAL_KEY_MAP};
use crate::serial::{create_packet, send_packet};

#[cfg(test)]
mod tests;

#[repr(u8)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub enum KeyGroup {
    CharKey = 0x02,
    MediaKey = 0x03,
}

#[repr(u8)]
#[derive(Debug, Copy, Clone, PartialEq, Eq)]
pub enum MouseButtonCode {
    Left = 0x01,
    Right = 0x02,
    Middle = 0x04,
}

pub struct KeyboardState {
    pub serial_port: Option<Box<dyn SerialPort>>,
    sender: Option<Sender<Vec<u8>>>,
    key_slots: [u8; 6],
    special_key_status: u8,
    pub toggle: bool,
    pub macos: bool,
    pub mute: bool,
    pub queue: bool,
    pub xinput_port: u16,
    pub service: bool,
    mouse_button_hold: bool,
    hold_mouse_button: MouseButtonCode,
    pub device_disconnected: bool,
    pub password: String,
    // Mouse event throttling
    mouse_accum_x: i32,
    mouse_accum_y: i32,
    last_mouse_send_time: Instant,
    mouse_send_interval: Duration,
}

impl KeyboardState {
    pub fn new() -> Self {
        KeyboardState {
            serial_port: None,
            sender: None,
            key_slots: [0; 6],
            special_key_status: 0,
            toggle: true,
            macos: false,
            mute: false,
            queue: false,
            xinput_port: 9869,
            service: false,
            mouse_button_hold: false,
            hold_mouse_button: MouseButtonCode::Left,
            device_disconnected: false,
            password: String::from("Xinyuan@199109062337"),
            mouse_accum_x: 0,
            mouse_accum_y: 0,
            last_mouse_send_time: Instant::now(),
            mouse_send_interval: Duration::from_millis(15), // ~67Hz send rate
        }
    }

    pub fn load_password(&mut self) {
        if Path::exists(Path::new(".password")) {
            if let Ok(password) = std::fs::read_to_string(".password") {
                self.password = password.trim().to_string();
            }
        }
    }

    fn send_xinput_command(&mut self, operation: &str) {
        let command = format!("xinput {}", operation);

        match TcpStream::connect(format!("127.0.0.1:{}", self.xinput_port)) {
            Ok(mut stream) => {
                if let Err(e) = stream.write_all(command.as_bytes()) {
                    eprintln!("Failed to send xinput command: {}", e);
                } else {
                    let mut buffer = [0u8; 1024];
                    if let Ok(bytes_read) = stream.read(&mut buffer) {
                        let response = String::from_utf8_lossy(&buffer[..bytes_read]);
                        println!("XInput response: {}", response);
                    }
                }
            }
            Err(e) => eprintln!("Failed to connect to xinput service: {}", e),
        }
    }

    fn toggle_devices(&mut self, toggle: bool) {
        let operation = if toggle { "disable" } else { "enable" };
        self.send_xinput_command(operation);
    }

    pub fn start_sender_thread(&mut self, port: Box<dyn SerialPort>) {
        let (tx, rx) = mpsc::channel::<Vec<u8>>();
        self.sender = Some(tx);

        thread::spawn(move || {
            let mut port = port;
            loop {
                match rx.recv() {
                    Ok(data) => {
                        if !send_packet(&mut *port, &data) {
                            eprintln!("Serial port write failed");
                            break;
                        }
                    }
                    Err(_) => {
                        break;
                    }
                }
            }
        });
    }

    fn key_down(&mut self, key_group: KeyGroup, k0: u8, k1: u8, k2: u8, k3: u8, k4: u8, k5: u8, k6: u8) {
        let packet = if key_group == KeyGroup::CharKey {
            create_packet(&[0x57, 0xAB, 0x00, key_group as u8, 0x08, k0, 0x00, k1, k2, k3, k4, k5, k6], true)
        } else {
            create_packet(&[0x57, 0xAB, 0x00, key_group as u8, 0x04, k0, k1, k2, k3], true)
        };
        self.send_packet_internal(packet);
    }

    pub fn key_up_all(&mut self, key_group: KeyGroup) {
        let packet = if key_group == KeyGroup::CharKey {
            vec![0x57, 0xAB, 0x00, 0x02, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c]
        } else {
            vec![0x57, 0xAB, 0x00, 0x03, 0x04, 0x02, 0x00, 0x00, 0x00, 0x0B]
        };
        self.send_packet_internal(packet);
    }

    fn switch_key_slot(&mut self, key_code: u16, push: bool) {
        if let Some(&key_byte) = KEY_MAPS.get(&key_code) {
            if push {
                if !self.key_slots.contains(&key_byte) {
                    if let Some(index) = self.key_slots.iter().position(|&x| x == 0) {
                        self.key_slots[index] = key_byte;
                    }
                }
            } else {
                if let Some(index) = self.key_slots.iter().position(|&x| x == key_byte) {
                    self.key_slots[index] = 0;
                }
            }
            self.send_char_key_down();
        }
    }

    fn send_char_key_down(&mut self) {
        self.key_down(
            KeyGroup::CharKey,
            self.special_key_status,
            self.key_slots[0],
            self.key_slots[1],
            self.key_slots[2],
            self.key_slots[3],
            self.key_slots[4],
            self.key_slots[5],
        );
    }

    fn switch_media_key(&mut self, key_code: u16, push: bool) {
        if let Some(&media_key_byte) = MEDIA_KEY_MAPS.get(&key_code) {
            if push {
                self.key_down(
                    KeyGroup::MediaKey,
                    media_key_byte[0],
                    media_key_byte[1],
                    media_key_byte[2],
                    media_key_byte[3],
                    0, 0, 0,
                );
            } else {
                self.key_down(KeyGroup::MediaKey, 0x02, 0, 0, 0, 0, 0, 0);
            }
        }
    }

    fn handle_mouse_key(&mut self, code: u16, key_down: bool) {
        let mouse_code = match code {
            272 => MouseButtonCode::Left,
            273 => MouseButtonCode::Right,
            274 => MouseButtonCode::Middle,
            _ => return,
        };
        
        self.mouse_button_hold = key_down;
        self.hold_mouse_button = mouse_code;
        
        if self.macos {
            if key_down {
                self.mouse_button_down_mac(mouse_code);
            } else {
                self.mouse_button_up_all_mac();
            }
        } else {
            if key_down {
                self.mouse_button_down(mouse_code);
            } else {
                self.mouse_button_up_all();
            }
        }
    }

    fn mouse_button_down(&mut self, button_code: MouseButtonCode) {
        let mut packet = vec![0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, button_code as u8, 0x00, 0x00, 0x00];
        let sum: u8 = packet.iter().fold(0u8, |acc, &x| acc.wrapping_add(x));
        packet.push(sum);
        self.send_packet_internal(packet);
    }

    fn mouse_button_up_all(&mut self) {
        self.send_packet_internal(vec![0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 0x0D]);
    }

    fn mouse_button_down_mac(&mut self, button_code: MouseButtonCode) {
        let mut packet = vec![0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, button_code as u8, 0x00, 0x00, 0x00, 0x00, 0x00];
        let sum: u8 = packet.iter().fold(0u8, |acc, &x| acc.wrapping_add(x));
        packet.push(sum);
        self.send_packet_internal(packet);
    }

    fn mouse_button_up_all_mac(&mut self) {
        self.send_packet_internal(vec![0x57, 0xAB, 0x00, 0x04, 0x07, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0F]);
    }

    pub fn mouse_move_rel(&mut self, x: i32, y: i32) {
        // Accumulate mouse movement
        self.mouse_accum_x += x;
        self.mouse_accum_y += y;
        
        let now = Instant::now();
        let elapsed = now.duration_since(self.last_mouse_send_time);
        
        // Send accumulated movement if:
        // 1. Enough time has passed (throttling)
        // 2. Or accumulated movement is large enough
        if elapsed >= self.mouse_send_interval || 
           self.mouse_accum_x.abs() >= 15 || 
           self.mouse_accum_y.abs() >= 15 {
            
            let x_clamped = std::cmp::max(-128, std::cmp::min(127, self.mouse_accum_x)) as i8;
            let y_clamped = std::cmp::max(-128, std::cmp::min(127, self.mouse_accum_y)) as i8;
            
            let button_byte = if self.mouse_button_hold {
                self.hold_mouse_button as u8
            } else {
                0x00
            };
            
            let mut packet = vec![
                0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, button_byte, 
                x_clamped as u8, y_clamped as u8, 0x00
            ];
            let sum: u8 = packet.iter().fold(0u8, |acc, &x| acc.wrapping_add(x));
            packet.push(sum);
            self.send_packet_internal(packet);
            
            // Reset accumulators
            self.mouse_accum_x -= x_clamped as i32;
            self.mouse_accum_y -= y_clamped as i32;
            self.last_mouse_send_time = now;
        }
    }

    pub fn mouse_scroll(&mut self, scroll_count: i32) {
        let scroll_clamped = std::cmp::max(-128, std::cmp::min(127, scroll_count)) as i8;
        
        let mut packet = vec![
            0x57, 0xAB, 0x00, 0x05, 0x05, 0x01, 0x00, 0x00, 0x00, 0x00, 
            scroll_clamped as u8
        ];
        let sum: u8 = packet.iter().fold(0u8, |acc, &x| acc.wrapping_add(x));
        packet.push(sum);
        self.send_packet_internal(packet);
    }

    fn switch_meta_and_alt(key_code: u16) -> u16 {
        match key_code {
            125 => 56,  // LeftMeta -> LeftAlt
            56 => 125,  // LeftAlt -> LeftMeta
            _ => key_code,
        }
    }

    pub fn type_string(&mut self, s: &str) {
        for c in s.chars() {
            if let Some(&(modifier, key)) = CHAR_KEY_MAP.get(&c) {
                self.key_down(KeyGroup::CharKey, modifier, key, 0, 0, 0, 0, 0);
                self.key_up_all(KeyGroup::CharKey);
                std::thread::sleep(std::time::Duration::from_millis(10));
            }
        }
    }

    pub fn handle_key_event(&mut self, code: u16, value: i32) {
        let is_push = value == 1 || value == 2;
        
        if !self.mute {
            println!("Code: {} State: {}", code, if is_push { "DOWN/HOLD" } else { "UP" });
        }
        
        let mut key_code = code;
        if self.macos {
            key_code = Self::switch_meta_and_alt(key_code);
        }
        
        // Always update special key status first (even when toggle is off)
        if SPECIAL_KEY_MAP.contains_key(&key_code) {
            let flag = SPECIAL_KEY_MAP[&key_code];
            if is_push {
                self.special_key_status = self.special_key_status | flag;
            } else {
                self.special_key_status = self.special_key_status & !flag;
            }
        }
        
        // Handle Prog1 key for toggle switch (KEY_PROG1 = 148)
        if code == 148 && value == 0 { // Key up
            self.toggle = !self.toggle;
            println!("Toggle is {} now", if self.toggle { "on" } else { "off" });
            self.toggle_devices(self.toggle);
            return;
        }
        
        // Handle Ctrl+F1 shortcut for password input (F1 = 59) - only when toggle is off (local mode)
        if code == 59 && value == 0 && (self.special_key_status & 0x01) != 0 && !self.toggle { // F1 key up with Ctrl pressed and toggle off
            let password = self.password.clone();
            self.type_string(&password);
            return;
        }
        
        if !self.toggle || self.device_disconnected {
            return;
        }
        
        if matches!(code, 272 | 273 | 274) {
            if !self.device_disconnected {
                self.handle_mouse_key(code, is_push);
            }
            return;
        }
        
        self.switch_media_key(key_code, is_push);
        self.switch_key_slot(key_code, is_push);
    }

    fn send_packet_internal(&mut self, data: Vec<u8>) {
        if let Some(ref sender) = self.sender {
            let _ = sender.send(data);
        } else {
            self.send_packet_directly(&data);
        }
    }

    fn send_packet_directly(&mut self, data: &[u8]) {
        if let Some(ref mut port) = self.serial_port {
            if !send_packet(&mut **port, data) {
                self.device_disconnected = true;
            }
        }
    }
}