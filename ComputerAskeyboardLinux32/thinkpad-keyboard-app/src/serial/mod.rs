use serialport::SerialPort;

#[allow(unused_imports)]
use std::path::Path;

#[cfg(test)]
mod tests;

pub fn auto_scan_serial_port() -> Option<String> {
    let ports = match serialport::available_ports() {
        Ok(p) => p,
        Err(e) => {
            eprintln!("Failed to list serial ports: {}", e);
            return None;
        }
    };

    for port in ports {
        let port_name = &port.port_name;
        if port_name.contains("ttyUSB") || port_name.contains("rfcomm") {
            return Some(port_name.to_string());
        }
    }

    None
}

pub fn create_packet(arr_list: &[u8], add_check_sum: bool) -> Vec<u8> {
    let mut packet = arr_list.to_vec();
    if add_check_sum {
        let sum: u8 = arr_list.iter().fold(0u8, |acc, &x| acc.wrapping_add(x));
        packet.push(sum);
    }
    packet
}

pub fn send_packet(port: &mut (dyn SerialPort + Send), data: &[u8]) -> bool {
    const MAX_RETRIES: usize = 3;

    for attempt in 1..=MAX_RETRIES {
        match port.write_all(data) {
            Ok(_) => {
                match port.flush() {
                    Ok(_) => return true,
                    Err(e) => {
                        eprintln!("Failed to flush serial port (attempt {}/{}): {}", attempt, MAX_RETRIES, e);
                    }
                }
            }
            Err(e) => {
                eprintln!("Failed to send data (attempt {}/{}): {}", attempt, MAX_RETRIES, e);
            }
        }

        if attempt < MAX_RETRIES {
            std::thread::sleep(std::time::Duration::from_millis(50));
        }
    }

    false
}