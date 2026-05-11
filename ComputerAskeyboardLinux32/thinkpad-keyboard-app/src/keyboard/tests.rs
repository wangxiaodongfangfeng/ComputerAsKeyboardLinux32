use super::*;
use crate::test_utils::MockSerialPort;

#[test]
fn test_keyboard_state_new() {
    let state = KeyboardState::new();
    
    // Check initial state
    assert!(state.serial_port.is_none());
    assert!(state.sender.is_none());
    assert_eq!(state.key_slots, [0; 6]);
    assert_eq!(state.special_key_status, 0);
    assert!(state.toggle);
    assert!(!state.macos);
    assert!(!state.mute);
    assert!(!state.queue);
    assert!(!state.mouse_button_hold);
    assert_eq!(state.hold_mouse_button, MouseButtonCode::Left);
    assert!(!state.device_disconnected);
    assert_eq!(state.password, "Xinyuan@199109062337");
}

#[test]
fn test_keyboard_state_switch_meta_and_alt() {
    // Test meta/alt switching
    assert_eq!(KeyboardState::switch_meta_and_alt(125), 56);  // LeftMeta -> LeftAlt
    assert_eq!(KeyboardState::switch_meta_and_alt(56), 125);  // LeftAlt -> LeftMeta
    assert_eq!(KeyboardState::switch_meta_and_alt(30), 30);   // A stays A
    assert_eq!(KeyboardState::switch_meta_and_alt(28), 28);   // Enter stays Enter
}

#[test]
fn test_handle_key_event_basic() {
    let mut state = KeyboardState::new();
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // Simulate pressing and releasing 'A' key
    state.handle_key_event(30, 1);  // Key down
    state.handle_key_event(30, 0);  // Key up
    
    // Should have written packets
    assert!(!state.device_disconnected);
}

#[test]
fn test_handle_key_event_special_keys() {
    let mut state = KeyboardState::new();
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // Test left ctrl press
    state.handle_key_event(29, 1);  // LeftCtrl down
    assert_eq!(state.special_key_status, 0x01);  // LEFT_CTRL flag set
    
    // Test left ctrl release
    state.handle_key_event(29, 0);  // LeftCtrl up
    assert_eq!(state.special_key_status, 0x00);  // LEFT_CTRL flag cleared
}

#[test]
fn test_handle_key_event_macos_mode() {
    let mut state = KeyboardState::new();
    state.macos = true;
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // In macOS mode, LeftMeta (125) should be treated as LeftAlt (56)
    state.handle_key_event(125, 1);  // LeftMeta down
    assert_eq!(state.special_key_status, 0x04);  // LEFT_ALT flag set (not LEFT_META)
}

#[test]
fn test_mouse_move_rel() {
    let mut state = KeyboardState::new();
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // Test mouse movement
    state.mouse_move_rel(10, 0);   // X movement
    state.mouse_move_rel(0, -5);   // Y movement
    
    // Should have written mouse packets
    assert!(!state.device_disconnected);
}

#[test]
fn test_mouse_scroll() {
    let mut state = KeyboardState::new();
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // Test mouse scroll
    state.mouse_scroll(1);    // Scroll up
    state.mouse_scroll(-1);   // Scroll down
    
    // Should have written scroll packets
    assert!(!state.device_disconnected);
}

#[test]
fn test_key_up_all() {
    let mut state = KeyboardState::new();
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // Press some keys first
    state.handle_key_event(30, 1);  // A down
    state.handle_key_event(48, 1);  // B down
    
    // Release all keys
    state.key_up_all(KeyGroup::CharKey);
    
    // Should have released all keys
    assert!(!state.device_disconnected);
}

#[test]
fn test_keyboard_state_toggle() {
    let mut state = KeyboardState::new();
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // Toggle starts as true
    assert!(state.toggle);
    
    // Pressing keys should work
    state.handle_key_event(30, 1);  // A down
    
    // Disable toggle
    state.toggle = false;
    
    // Pressing keys should be ignored
    state.handle_key_event(48, 1);  // B down (should be ignored)
    
    // Re-enable toggle
    state.toggle = true;
    
    // Pressing keys should work again
    state.handle_key_event(32, 1);  // D down
    
    assert!(!state.device_disconnected);
}

#[test]
fn test_mouse_button_events() {
    let mut state = KeyboardState::new();
    let mock_port = MockSerialPort::new();
    state.serial_port = Some(Box::new(mock_port));
    
    // Test left mouse button
    state.handle_key_event(272, 1);  // Left button down
    state.handle_key_event(272, 0);  // Left button up
    
    // Test right mouse button
    state.handle_key_event(273, 1);  // Right button down
    state.handle_key_event(273, 0);  // Right button up
    
    // Test middle mouse button
    state.handle_key_event(274, 1);  // Middle button down
    state.handle_key_event(274, 0);  // Middle button up
    
    assert!(!state.device_disconnected);
}
