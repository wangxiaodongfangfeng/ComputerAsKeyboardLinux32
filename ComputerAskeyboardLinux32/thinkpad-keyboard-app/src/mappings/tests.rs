#[test]
fn test_key_codes_are_valid() {
    use crate::mappings::KEY_MAPS;
    
    for (key_code, key_byte) in KEY_MAPS.iter() {
        assert!(*key_code < 1000, "Key code {} seems too large", key_code);
        assert!(*key_byte <= 255, "Key byte 0x{:x} seems too large", key_byte);
    }
}

#[test]
fn test_special_key_flags_values() {
    use crate::mappings::{LEFT_CTRL, LEFT_SHIFT, LEFT_ALT, LEFT_META, RIGHT_CTRL, RIGHT_SHIFT, RIGHT_ALT, RIGHT_META};
    
    // Test that special key flag values are correct bit positions
    assert_eq!(LEFT_CTRL, 0x01);
    assert_eq!(LEFT_SHIFT, 0x02);
    assert_eq!(LEFT_ALT, 0x04);
    assert_eq!(LEFT_META, 0x08);
    assert_eq!(RIGHT_CTRL, 0x10);
    assert_eq!(RIGHT_SHIFT, 0x20);
    assert_eq!(RIGHT_ALT, 0x40);
    assert_eq!(RIGHT_META, 0x80);
}