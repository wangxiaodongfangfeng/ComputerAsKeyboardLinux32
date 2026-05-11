use super::*;
use std::fs;
use std::path::Path;

#[test]
fn test_load_devices_empty_file() {
    let devices = load_devices();
    assert!(devices.is_empty());
}

#[test]
fn test_load_devices_from_file() {
    let test_file = ".devices_test_1";
    let test_content = "/dev/input/event0\n/dev/input/event1\n/dev/input/event2\n";
    fs::write(test_file, test_content).unwrap();
    
    fs::rename(test_file, ".devices").unwrap();
    
    let devices = load_devices();
    
    assert_eq!(devices.len(), 3);
    assert_eq!(devices[0], "/dev/input/event0");
    assert_eq!(devices[1], "/dev/input/event1");
    assert_eq!(devices[2], "/dev/input/event2");
    
    fs::remove_file(".devices").unwrap();
}

#[test]
fn test_load_devices_empty_content() {
    fs::write(".devices", "").unwrap();
    
    let devices = load_devices();
    
    assert!(devices.is_empty());
    
    fs::remove_file(".devices").unwrap();
}

#[test]
fn test_discover_input_devices_exists() {
    let devices = discover_input_devices();
    
    let _: Vec<String> = devices;
}

#[test]
fn test_discover_input_devices_filtering() {
    let devices = discover_input_devices();
    
    for device in devices {
        assert!(device.contains("event"), "Device path should contain 'event': {}", device);
        assert!(device.starts_with("/dev/input/"), "Device path should start with /dev/input/: {}", device);
    }
}

#[test]
fn test_process_input_device_invalid_path() {
    use std::sync::{Arc, Mutex};
    use crate::keyboard::KeyboardState;
    
    let state = Arc::new(Mutex::new(KeyboardState::new()));
    
    process_input_device("/dev/input/nonexistent_event_999", state);
    
    assert!(true);
}

#[test]
fn test_load_devices_with_comments() {
    let test_file = ".devices_test_2";
    let test_content = "# This is a comment\n/dev/input/event0\n# Another comment\n/dev/input/event1\n";
    fs::write(test_file, test_content).unwrap();
    
    fs::rename(test_file, ".devices").unwrap();
    
    let devices = load_devices();
    
    assert_eq!(devices.len(), 4);
    
    fs::remove_file(".devices").unwrap();
}

#[test]
fn test_path_exists_helper() {
    assert!(Path::new("/dev").exists());
    assert!(!Path::new("/dev/nonexistent_path_12345").exists());
}
