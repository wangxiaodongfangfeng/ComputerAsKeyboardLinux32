use super::*;

#[test]
fn test_create_packet_with_checksum() {
    let data = [0x57, 0xAB, 0x00, 0x02, 0x08, 0x00, 0x00, 0x00];
    let packet = create_packet(&data, true);
    
    // Should have original data plus checksum
    assert_eq!(packet.len(), data.len() + 1);
    
    // Checksum should be sum of all bytes using wrapping addition
    let expected_checksum: u8 = data.iter().fold(0u8, |acc, &x| acc.wrapping_add(x));
    assert_eq!(packet[packet.len() - 1], expected_checksum);
    
    // First bytes should be original data
    for i in 0..data.len() {
        assert_eq!(packet[i], data[i]);
    }
}

#[test]
fn test_create_packet_without_checksum() {
    let data = [0x57, 0xAB, 0x00, 0x02];
    let packet = create_packet(&data, false);
    
    // Should have exactly the same data
    assert_eq!(packet.len(), data.len());
    assert_eq!(packet, data);
}

#[test]
fn test_create_packet_empty() {
    let data = [];
    let packet = create_packet(&data, true);
    
    // Empty packet with checksum should just be [0]
    assert_eq!(packet, [0]);
}

#[test]
fn test_create_packet_single_byte() {
    let data = [0xAA];
    let packet = create_packet(&data, true);
    
    // Should be [0xAA, 0xAA] (data + checksum)
    assert_eq!(packet, [0xAA, 0xAA]);
}

#[test]
fn test_create_packet_checksum_overflow() {
    // Test checksum overflow behavior
    let data = [0xFF, 0xFF];
    let packet = create_packet(&data, true);
    
    // Checksum should wrap around: 0xFF + 0xFF = 0xFE (mod 256)
    assert_eq!(packet[2], 0xFE);
}
