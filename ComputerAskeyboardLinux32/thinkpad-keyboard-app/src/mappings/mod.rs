use lazy_static::lazy_static;
use std::collections::HashMap;

#[cfg(test)]
mod tests;

pub const LEFT_CTRL: u8 = 0x01;
pub const LEFT_SHIFT: u8 = 0x02;
pub const LEFT_ALT: u8 = 0x04;
pub const LEFT_META: u8 = 0x08;
pub const RIGHT_CTRL: u8 = 0x10;
pub const RIGHT_SHIFT: u8 = 0x20;
pub const RIGHT_ALT: u8 = 0x40;
pub const RIGHT_META: u8 = 0x80;

lazy_static! {
    pub static ref KEY_MAPS: HashMap<u16, u8> = {
        let mut map = HashMap::new();
        map.insert(30, 0x04); // A
        map.insert(48, 0x05); // B
        map.insert(46, 0x06); // C
        map.insert(32, 0x07); // D
        map.insert(18, 0x08); // E
        map.insert(33, 0x09); // F
        map.insert(34, 0x0A); // G
        map.insert(35, 0x0B); // H
        map.insert(23, 0x0C); // I
        map.insert(36, 0x0D); // J
        map.insert(37, 0x0E); // K
        map.insert(38, 0x0F); // L
        map.insert(50, 0x10); // M
        map.insert(49, 0x11); // N
        map.insert(24, 0x12); // O
        map.insert(25, 0x13); // P
        map.insert(16, 0x14); // Q
        map.insert(19, 0x15); // R
        map.insert(31, 0x16); // S
        map.insert(20, 0x17); // T
        map.insert(22, 0x18); // U
        map.insert(47, 0x19); // V
        map.insert(17, 0x1A); // W
        map.insert(45, 0x1B); // X
        map.insert(21, 0x1C); // Y
        map.insert(44, 0x1D); // Z
        map.insert(59, 0x3A); // F1
        map.insert(60, 0x3B); // F2
        map.insert(61, 0x3C); // F3
        map.insert(62, 0x3D); // F4
        map.insert(63, 0x3E); // F5
        map.insert(64, 0x3F); // F6
        map.insert(65, 0x40); // F7
        map.insert(66, 0x41); // F8
        map.insert(67, 0x42); // F9
        map.insert(68, 0x43); // F10
        map.insert(87, 0x44); // F11
        map.insert(88, 0x45); // F12
        map.insert(41, 0x35); // Grave
        map.insert(1, 0x29); // ESC
        map.insert(2, 0x1E); // 1
        map.insert(3, 0x1F); // 2
        map.insert(4, 0x20); // 3
        map.insert(5, 0x21); // 4
        map.insert(6, 0x22); // 5
        map.insert(7, 0x23); // 6
        map.insert(8, 0x24); // 7
        map.insert(9, 0x25); // 8
        map.insert(10, 0x26); // 9
        map.insert(11, 0x27); // 0
        map.insert(12, 0x2D); // -
        map.insert(13, 0x2E); // =
        map.insert(14, 0x2A); // backspace
        map.insert(15, 0x2B); // tab
        map.insert(58, 0x39); // Capslock
        map.insert(28, 0x28); // Enter
        map.insert(26, 0x2F); // LeftBrace
        map.insert(27, 0x30); // RightBrace
        map.insert(43, 0x31); // Backslash
        map.insert(39, 0x33); // Semicolon
        map.insert(40, 0x34); // Apostrophe
        map.insert(51, 0x36); // Comma
        map.insert(52, 0x37); // Dot
        map.insert(53, 0x38); // Slash
        map.insert(42, 0xE1); // LeftShift
        map.insert(54, 0xE5); // RightShift
        map.insert(29, 0xE0); // LeftCtrl
        map.insert(125, 0xE3); // LeftMeta
        map.insert(56, 0xE2); // LeftAlt
        map.insert(57, 0x2C); // Space
        map.insert(100, 0xE6); // RightAlt
        map.insert(127, 0x8F); // Compose
        map.insert(97, 0xE4); // RightCtrl
        map.insert(103, 0x52); // Up
        map.insert(108, 0x51); // Down
        map.insert(105, 0x50); // Left
        map.insert(106, 0x4F); // Right
        map.insert(158, 0x04); // Back
        map.insert(159, 0x04); // Forward
        map.insert(110, 0x49); // Insert
        map.insert(111, 0x4C); // Delete
        map.insert(102, 0x4A); // Home
        map.insert(107, 0x4D); // End
        map.insert(104, 0x4B); // Pageup
        map.insert(109, 0x4E); // Pagedown
        map.insert(99, 0x46); // SysRq
        map.insert(70, 0x47); // ScrollLock
        map.insert(119, 0x48); // Pause
        map
    };

    pub static ref MEDIA_KEY_MAPS: HashMap<u16, [u8; 4]> = {
        let mut map = HashMap::new();
        map.insert(113, [0x02, 0x04, 0x00, 0x00]); // Mute
        map.insert(115, [0x02, 0x01, 0x00, 0x00]); // Volume Up
        map.insert(114, [0x02, 0x02, 0x00, 0x00]); // Volume Down
        map.insert(164, [0x02, 0x08, 0x00, 0x00]); // Play/Pause
        map.insert(166, [0x02, 0x40, 0x00, 0x00]); // Stop
        map.insert(163, [0x02, 0x10, 0x00, 0x00]); // Next Track
        map.insert(165, [0x02, 0x20, 0x00, 0x00]); // Previous Track
        map
    };

    pub static ref SPECIAL_KEY_MAP: HashMap<u16, u8> = {
        let mut map = HashMap::new();
        map.insert(29, LEFT_CTRL);   // LeftCtrl
        map.insert(97, RIGHT_CTRL);  // RightCtrl
        map.insert(42, LEFT_SHIFT);  // LeftShift
        map.insert(54, RIGHT_SHIFT); // RightShift
        map.insert(56, LEFT_ALT);    // LeftAlt
        map.insert(100, RIGHT_ALT);  // RightAlt
        map.insert(125, LEFT_META);  // LeftMeta
        map.insert(126, RIGHT_META); // RightMeta
        map
    };

    pub static ref CHAR_KEY_MAP: HashMap<char, (u8, u8)> = {
        let mut map = HashMap::new();
        // Numbers
        map.insert('0', (0x00, 0x27));
        for i in 1..=9 {
            map.insert((b'0' + i as u8) as char, (0x00, 0x1E + i as u8 - 1));
        }
        // Lowercase letters
        for i in 0..26 {
            map.insert((b'a' + i) as char, (0x00, 0x04 + i as u8));
        }
        // Uppercase letters (with SHIFT)
        for i in 0..26 {
            map.insert((b'A' + i) as char, (0x02, 0x04 + i as u8));
        }
        // Special characters
        map.insert('@', (0x02, 0x1F));
        map.insert('-', (0x00, 0x2D));
        map.insert('^', (0x00, 0x2E));
        map.insert('[', (0x00, 0x30));
        map.insert(']', (0x00, 0x32));
        map.insert(';', (0x00, 0x33));
        map.insert(':', (0x00, 0x34));
        map.insert(',', (0x00, 0x36));
        map.insert('.', (0x00, 0x37));
        map.insert('/', (0x00, 0x38));
        map.insert('\\', (0x00, 0x89));
        map.insert(' ', (0x00, 0x2C));
        map
    };
}
