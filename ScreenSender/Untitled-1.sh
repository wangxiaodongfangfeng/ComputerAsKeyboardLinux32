#!/bin/bash

# Bluetooth Device Scanner and RFCOMM Binder
# Requires: bluez, bluez-tools, sudo privileges

# Check for required tools
check_dependencies() {
    local dependencies=("bluetoothctl" "rfcomm" "hciconfig")
    for dep in "${dependencies[@]}"; do
        if ! command -v "$dep" &> /dev/null; then
            echo "Error: Required tool '$dep' not found. Installing..."
            sudo apt update && sudo apt install -y bluez bluez-tools
            return 0
        fi
    done
}

# Enable Bluetooth adapter
enable_bluetooth() {
    echo "Enabling Bluetooth adapter..."
    sudo hciconfig hci0 up
    if [ $? -ne 0 ]; then
        echo "Error: Failed to enable Bluetooth adapter"
        exit 1
    fi
}

# Scan for nearby Bluetooth devices
scan_devices() {
    echo -e "\nScanning for Bluetooth devices (will take 10 seconds)..."
    bluetoothctl -- power on
    bluetoothctl -- scan on &
    local scan_pid=$!
    sleep 10
    kill $scan_pid
    bluetoothctl -- scan off
}

# List paired devices
list_paired_devices() {
    echo -e "\nPaired Bluetooth devices:"
    bluetoothctl -- paired-devices | while read -r line; do
        if [[ $line == Device* ]]; then
            local mac=$(echo "$line" | awk '{print $2}')
            local name=$(echo "$line" | cut -d ' ' -f 3-)
            echo "MAC: $mac, Name: $name"
        fi
    done
}

# Bind device to RFCOMM port
bind_to_rfcomm() {
    local mac=$1
    local port=$2
    
    if [ -z "$mac" ] || [ -z "$port" ]; then
        echo "Error: MAC address and port number required"
        return 1
    fi

    # Check if port is available
    if sudo rfcomm show | grep -q "rfcomm$port"; then
        echo "Port $port is already in use"
        return 1
    fi

    # Create RFCOMM binding
    echo -e "\nBinding $mac to /dev/rfcomm$port..."
    sudo rfcomm bind "$port" "$mac"

    if [ $? -eq 0 ]; then
        echo "Successfully bound to /dev/rfcomm$port"
        echo "You can access this device as a serial port at: /dev/rfcomm$port"
    else
        echo "Failed to bind device to port $port"
    fi
}

# Main script execution
main() {
    check_dependencies
    enable_bluetooth
    scan_devices
    list_paired_devices

    read -p $'\nEnter MAC address of device to bind: ' device_mac
    read -p 'Enter RFCOMM port number (0-255): ' port_number

    bind_to_rfcomm "$device_mac" "$port_number"

    echo -e "\nTo unbind later, use: sudo rfcomm release $port_number"
}

# Run main script
main
