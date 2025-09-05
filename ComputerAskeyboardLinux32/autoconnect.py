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
LOG_FILE = "/var/log/bluetooth_auto_connect.log"  # 主日志文件
RFCOMM_LOG_FILE = "/var/log/bluetooth_rfcomm_details.log"  # rfcomm命令详细日志

# 确保脚本以root权限运行
def check_root():
    if not os.geteuid() == 0:
        print("此脚本需要root权限运行。请使用sudo执行。")
        sys.exit(1)

# 记录主日志
def log(message):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_entry = f"[{timestamp}] {message}\n"

    print(log_entry.strip())  # 同时输出到控制台

    with open(LOG_FILE, "a") as f:
        f.write(log_entry)

# 记录rfcomm命令详细日志
def log_rfcomm_output(message):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    log_entry = f"[{timestamp}] {message}\n"

    with open(RFCOMM_LOG_FILE, "a") as f:
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
        result = subprocess.run(
            ["sudo", "rfcomm", "-a"],
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True
        )
        rfcomm_output = result.stdout

        # 记录rfcomm -a的输出
        log_rfcomm_output(f"rfcomm -a 输出:\n{rfcomm_output}")

        if mac in rfcomm_output and "CONNECTED" in rfcomm_output:
            return True

        # 检查蓝牙连接状态
        btctl_output = subprocess.check_output(
            ["bluetoothctl", "info", mac],
            stderr=subprocess.STDOUT,
            text=True
        )

        return "Connected: yes" in btctl_output
    except subprocess.CalledProcessError as e:
        log(f"检查连接状态出错: {e.output}")
        log_rfcomm_output(f"检查连接状态出错: {e.output}")
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

        # 记录l2ping的输出
        log_rfcomm_output(f"l2ping {mac} 输出: 退出码={result.returncode}\nstdout: {result.stdout}\nstderr: {result.stderr}")

        return result.returncode == 0
    except Exception as e:
        error_msg = f"检查设备 {mac} 范围时出错: {str(e)}"
        log(error_msg)
        log_rfcomm_output(error_msg)
        return False

# 找到可用的rfcomm端口
def find_available_rfcomm_port():
    try:
        result = subprocess.run(
            ["sudo", "rfcomm", "-a"],
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True
        )
        output = result.stdout

        used_ports = []
        # 解析已使用的端口
        pattern = re.compile(r"rfcomm(\d+):")
        for line in output.splitlines():
            match = pattern.search(line)
            if match:
                # 检查端口是否处于连接状态
                if "CONNECTED" in line:
                    used_ports.append(int(match.group(1)))

        # 从起始端口开始查找第一个可用端口
        port = RFCOMM_START_PORT
        while port in used_ports:
            port += 1

        return port
    except subprocess.CalledProcessError as e:
        log(f"检查可用端口时出错: {e.output}")
        log_rfcomm_output(f"检查可用端口时出错: {e.output}")
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
    log_rfcomm_output(f"开始尝试连接设备 {name} ({mac}) 到 rfcomm{port}")

    retries = 0
    while retries < MAX_RETRIES:
        try:
            # 首先确保设备已信任
            trust_result = subprocess.run(
                ["bluetoothctl", "trust", mac],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True
            )
            log_rfcomm_output(f"bluetoothctl trust {mac} 输出:\nstdout: {trust_result.stdout}\nstderr: {trust_result.stderr}")
            if trust_result.returncode != 0:
                log(f"设置设备信任失败: {trust_result.stderr}")
                raise subprocess.CalledProcessError(trust_result.returncode, "bluetoothctl trust")

            # 连接设备
            connect_result = subprocess.run(
                ["bluetoothctl", "connect", mac],
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                timeout=10  # 添加超时避免卡住
            )
            log_rfcomm_output(f"bluetoothctl connect {mac} 输出:\nstdout: {connect_result.stdout}\nstderr: {connect_result.stderr}")
            if connect_result.returncode != 0:
                log(f"bluetoothctl连接失败: {connect_result.stderr}")
                raise subprocess.CalledProcessError(connect_result.returncode, "bluetoothctl connect")

            # 使用带sudo的rfcomm connect命令，并捕获输出
            cmd = f"sudo rfcomm connect {port} {mac}"
            log_rfcomm_output(f"执行命令: {cmd}")

            # 执行rfcomm connect并捕获输出
            rfcomm_result = subprocess.run(
                cmd.split(),  # 不使用shell=True以更好地捕获输出
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                text=True,
                timeout=15  # 连接超时时间
            )

            # 记录rfcomm命令的完整输出
            log_rfcomm_output(f"rfcomm connect 输出 (退出码: {rfcomm_result.returncode}):\n{rfcomm_result.stdout}")

            # 后台运行连接进程
            bg_cmd = f"sudo rfcomm connect {port} {mac} > /dev/null 2>&1 &"
            subprocess.run(bg_cmd, shell=True)

            # 等待连接建立
            time.sleep(5)

            # 检查是否连接成功
            if is_connected(mac):
                log(f"成功连接设备 {name} ({mac}) 到 rfcomm{port}")
                return True
            else:
                log(f"连接rfcomm{port}未成功建立")
                # 清理可能的残留进程
                subprocess.run(f"sudo pkill -f 'rfcomm connect {port} {mac}'", shell=True)

        except subprocess.CalledProcessError as e:
            error_msg = f"连接尝试 {retries + 1} 失败: {str(e)}"
            log(error_msg)
            log_rfcomm_output(error_msg)
        except subprocess.TimeoutExpired:
            error_msg = f"连接尝试 {retries + 1} 超时"
            log(error_msg)
            log_rfcomm_output(error_msg)
        except Exception as e:
            error_msg = f"连接尝试 {retries + 1} 发生未知错误: {str(e)}"
            log(error_msg)
            log_rfcomm_output(error_msg)

        retries += 1
        if retries < MAX_RETRIES:
            time.sleep(5)  # 重试前等待

    log(f"达到最大重试次数，无法连接设备 {name} ({mac})")
    return False

# 处理程序退出
def handle_exit(signal, frame):
    log("脚本正在退出...")
    # 清理所有rfcomm连接
    subprocess.run(["sudo", "rfcomm", "release", "all"], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    sys.exit(0)

# 主循环
def main():
    check_root()
    log("蓝牙自动连接脚本启动")
    log_rfcomm_output("蓝牙自动连接脚本启动 - 详细日志开始")

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
        error_msg = f"脚本出错: {str(e)}"
        log(error_msg)
        log_rfcomm_output(error_msg)
        # 出错时清理连接
        subprocess.run(["sudo", "rfcomm", "release", "all"], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        raise

if __name__ == "__main__":
    main()
    