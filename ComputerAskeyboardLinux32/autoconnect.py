#!/usr/bin/env python3
import subprocess
import time
import re
import os
import signal
import sys
from datetime import datetime

# 配置
SCAN_INTERVAL = 60  # 扫描间隔(秒)
RFCOMM_START_PORT = 0  # 起始端口
MAX_RETRIES = 3  # 最大重试次数
LOG_FILE = "/var/log/bluetooth_auto_connect.log"  # 日志文件

# 确保脚本以root权限运行
def check_root():
    if not os.geteuid() == 0:
        print("此脚本需要root权限运行。请使用sudo执行。")
        sys.exit(1)

# 记录日志
def log(message):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_entry = f"[{timestamp}] {message}\n"

    print(log_entry.strip())  # 同时输出到控制台

    with open(LOG_FILE, "a") as f:
        f.write(log_entry)

# 获取已配对的蓝牙设备
def get_paired_devices():
    try:
        output = subprocess.check_output(
            ["bluetoothctl", "paired-devices"],
            stderr=subprocess.STDOUT,
            text=True
        )

        devices = []
        # 解析输出，格式通常为 "Device XX:XX:XX:XX:XX:XX 设备名称"
        pattern = re.compile(r"Device ([0-9A-Fa-f:]+) (.+)")
        for line in output.splitlines():
            match = pattern.match(line.strip())
            if match:
                mac = match.group(1)
                name = match.group(2)
                devices.append({"mac": mac, "name": name})

        return devices
    except subprocess.CalledProcessError as e:
        log(f"获取已配对设备失败: {e.output}")
        return []

# 检查设备是否已连接
def is_connected(mac):
    try:
        # 使用rfcomm -a检查连接状态
        rfcomm_output = subprocess.check_output(
            ["rfcomm", "-a"],
            stderr=subprocess.STDOUT,
            text=True
        )

        if mac in rfcomm_output and "connected" in rfcomm_output:
            return True

        # 检查蓝牙连接状态
        btctl_output = subprocess.check_output(
            ["bluetoothctl", "info", mac],
            stderr=subprocess.STDOUT,
            text=True
        )

        return "Connected: yes" in btctl_output
    except subprocess.CalledProcessError:
        return False

# 检查设备是否在范围内（可连接）
def is_in_range(mac):
    try:
        # 尝试ping蓝牙设备
        result = subprocess.run(
            ["l2ping", "-c", "1", "-t", "10", mac],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )

        return result.returncode == 0
    except Exception as e:
        log(f"检查设备 {mac} 范围时出错: {str(e)}")
        return False

# 找到可用的rfcomm端口
def find_available_rfcomm_port():
    try:
        output = subprocess.check_output(
            ["rfcomm", "-a"],
            stderr=subprocess.STDOUT,
            text=True
        )

        used_ports = []
        # 解析已使用的端口
        pattern = re.compile(r"rfcomm(\d+):")
        for line in output.splitlines():
            match = pattern.search(line)
            if match:
                # 检查端口是否处于连接状态
                if "connected" in line:
                    used_ports.append(int(match.group(1)))

        # 从起始端口开始查找第一个可用端口
        port = RFCOMM_START_PORT
        while port in used_ports:
            port += 1

        return port
    except subprocess.CalledProcessError as e:
        log(f"检查可用端口时出错: {e.output}")
        # 如果rfcomm -a失败，假设起始端口可用
        return RFCOMM_START_PORT

# 连接到设备
def connect_device(mac, name):
    if is_connected(mac):
        log(f"设备 {name} ({mac}) 已连接，无需操作")
        return True

    if not is_in_range(mac):
        log(f"设备 {name} ({mac}) 不在范围内，无法连接")
        return False

    port = find_available_rfcomm_port()
    log(f"尝试连接设备 {name} ({mac}) 到 rfcomm{port}")

    retries = 0
    while retries < MAX_RETRIES:
        try:

            # 使用rfcomm connect命令连接到指定端口
            # 注意：rfcomm connect会阻塞，所以我们需要在后台运行
            process = subprocess.Popen(
                ["rfcomm", "connect", str(port), mac, "&"],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                shell=True  # 需要shell=True来支持后台运行符号&
            )

            # 等待连接建立
            time.sleep(3)

            # 检查是否连接成功
            if is_connected(mac):
                log(f"成功连接设备 {name} ({mac}) 到 rfcomm{port}")
                return True
            else:
                log(f"连接rfcomm{port}未成功建立")

        except subprocess.CalledProcessError as e:
            log(f"连接尝试 {retries + 1} 失败: {str(e)}")

        retries += 1
        if retries < MAX_RETRIES:
            time.sleep(5)  # 重试前等待

    log(f"达到最大重试次数，无法连接设备 {name} ({mac})")
    return False

# 处理程序退出
def handle_exit(signal, frame):
    log("脚本正在退出...")
    sys.exit(0)

# 主循环
def main():
    check_root()
    log("蓝牙自动连接脚本启动")

    # 设置信号处理
    signal.signal(signal.SIGINT, handle_exit)
    signal.signal(signal.SIGTERM, handle_exit)

    try:
        while True:
            log("开始扫描已配对设备...")
            devices = get_paired_devices()

            if not devices:
                log("未发现已配对的蓝牙设备")
            else:
                log(f"发现 {len(devices)} 个已配对设备")
                for device in devices:
                    connect_device(device["mac"], device["name"])

            log(f"等待 {SCAN_INTERVAL} 秒后再次扫描...")
            time.sleep(SCAN_INTERVAL)

    except Exception as e:
        log(f"脚本出错: {str(e)}")
        raise

if __name__ == "__main__":
    main()
    