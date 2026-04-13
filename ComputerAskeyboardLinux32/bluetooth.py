import os
import time
import subprocess
import re
import signal
import threading
from datetime import datetime

# 配置参数
SCAN_INTERVAL = 30  # 扫描间隔(秒)
CLEANUP_INTERVAL = 300  # 清理间隔(秒)
RFCOMM_RANGE = range(0, 10)  # 可用的rfcomm端口范围

class BluetoothDaemon:
    def __init__(self):
        self.running = True
        self.connected_devices = {}  # 存储当前连接: {设备地址: (端口号, 进程ID)}
        self.paired_devices = self.get_paired_devices()
        
        # 启动清理线程
        self.cleanup_thread = threading.Thread(target=self.cleanup_loop, daemon=True)
        self.cleanup_thread.start()
        
        # 注册信号处理
        signal.signal(signal.SIGINT, self.handle_signal)
        signal.signal(signal.SIGTERM, self.handle_signal)

    def handle_signal(self, signum, frame):
        """处理终止信号，优雅退出"""
        print(f"\n接收到信号 {signum}，正在关闭守护进程...")
        self.running = False
        self.cleanup(force=True)
        print("守护进程已关闭")

    def get_paired_devices(self):
        """获取系统中已配对的蓝牙设备"""
        try:
            result = subprocess.check_output(
                ["bluetoothctl", "paired-devices"], 
                stderr=subprocess.STDOUT,
                text=True
            )
            
            # 解析输出，格式类似: "Device AA:BB:CC:DD:EE:FF 设备名称"
            paired = {}
            pattern = r"Device ([0-9A-Fa-f:]+) (.+)"
            for line in result.splitlines():
                match = re.match(pattern, line.strip())
                if match:
                    addr = match.group(1).upper()
                    name = match.group(2)
                    paired[addr] = name
                    
            print(f"已加载 {len(paired)} 个已配对设备")
            return paired
        except subprocess.CalledProcessError as e:
            print(f"获取已配对设备失败: {e.output}")
            return {}

    def scan_devices(self):
        """扫描周围可见的蓝牙设备"""
        try:
            # 使用hcitool扫描设备，超时10秒
            result = subprocess.check_output(
                ["hcitool", "scan"], 
                stderr=subprocess.STDOUT,
                text=True,
                timeout=10
            )
            
            # 解析输出，格式类似: "AA:BB:CC:DD:EE:FF 设备名称"
            devices = {}
            pattern = r"([0-9A-Fa-f:]+)\s+(.+)"
            for line in result.splitlines()[1:]:  # 跳过第一行"Scanning ..."
                match = re.match(pattern, line.strip())
                if match:
                    addr = match.group(1).upper()
                    name = match.group(2)
                    devices[addr] = name
                    
            print(f"扫描到 {len(devices)} 个可见设备")
            return devices
        except subprocess.CalledProcessError as e:
            print(f"扫描设备失败: {e.output}")
            return {}
        except subprocess.TimeoutExpired:
            print("扫描超时")
            return {}

    def find_available_rfcomm(self):
        """查找可用的rfcomm端口"""
        # 检查当前已使用的端口
        used_ports = set()
        try:
            result = subprocess.check_output(
                ["rfcomm", "show"], 
                stderr=subprocess.STDOUT,
                text=True
            )
            
            pattern = r"rfcomm(\d+):"
            for line in result.splitlines():
                match = re.search(pattern, line)
                if match:
                    used_ports.add(int(match.group(1)))
        except subprocess.CalledProcessError:
            # 没有任何端口在使用
            pass
            
        # 查找第一个可用端口
        for port in RFCOMM_RANGE:
            if port not in used_ports:
                return port
        return None

    def connect_device(self, device_addr):
        """连接设备到rfcomm端口"""
        # 检查设备是否已连接
        if device_addr in self.connected_devices:
            print(f"设备 {device_addr} 已连接，无需重复连接")
            return True
            
        # 查找可用端口
        port = self.find_available_rfcomm()
        if port is None:
            print("没有可用的rfcomm端口")
            return False
            
        try:
            # 绑定并连接设备
            cmd = f"rfcomm bind {port} {device_addr}"
            subprocess.check_output(
                cmd.split(),
                stderr=subprocess.STDOUT,
                text=True
            )
            
            # 记录连接信息
            self.connected_devices[device_addr] = {
                "port": port,
                "connected_at": datetime.now()
            }
            
            print(f"成功连接 {device_addr} 到 /dev/rfcomm{port}")
            return True
        except subprocess.CalledProcessError as e:
            print(f"连接 {device_addr} 失败: {e.output}")
            return False

    def cleanup_rfcomm(self, force=False):
        """清理无用的rfcomm连接"""
        cleaned = 0
        try:
            result = subprocess.check_output(
                ["rfcomm", "show"], 
                stderr=subprocess.STDOUT,
                text=True
            )
            
            pattern = r"rfcomm(\d+):\s+(.+)\s+Channel\s+\d+"
            for line in result.splitlines():
                match = re.search(pattern, line)
                if match:
                    port = int(match.group(1))
                    device_addr = match.group(2).strip()
                    
                    # 检查设备是否仍在已配对列表中或是否活跃
                    if force or device_addr not in self.paired_devices:
                        try:
                            subprocess.check_output(
                                ["rfcomm", "release", str(port)],
                                stderr=subprocess.STDOUT,
                                text=True
                            )
                            cleaned += 1
                            print(f"已释放端口 rfcomm{port} ({device_addr})")
                            
                            # 从连接列表中移除
                            if device_addr in self.connected_devices:
                                del self.connected_devices[device_addr]
                        except subprocess.CalledProcessError as e:
                            print(f"释放端口 rfcomm{port} 失败: {e.output}")
                            
        except subprocess.CalledProcessError:
            # 没有任何端口在使用
            pass
            
        return cleaned

    def cleanup(self, force=False):
        """执行清理操作"""
        print("开始清理操作...")
        cleaned_ports = self.cleanup_rfcomm(force)
        
        # 刷新已配对设备列表
        self.paired_devices = self.get_paired_devices()
        
        print(f"清理完成，共释放 {cleaned_ports} 个端口")

    def cleanup_loop(self):
        """定期清理循环"""
        while self.running:
            time.sleep(CLEANUP_INTERVAL)
            if self.running:  # 再次检查，防止在sleep时收到终止信号
                self.cleanup()

    def run(self):
        """运行守护进程主循环"""
        print("蓝牙守护进程已启动")
        print(f"扫描间隔: {SCAN_INTERVAL}秒，清理间隔: {CLEANUP_INTERVAL}秒")
        
        try:
            while self.running:
                # 扫描设备
                visible_devices = self.scan_devices()
                
                # 检查可见设备中是否有已配对设备需要连接
                for addr, name in visible_devices.items():
                    if addr in self.paired_devices and addr not in self.connected_devices:
                        print(f"发现已配对设备: {name} ({addr})，尝试连接...")
                        self.connect_device(addr)
                
                # 等待下一次扫描
                wait_time = 0
                while wait_time < SCAN_INTERVAL and self.running:
                    time.sleep(1)
                    wait_time += 1
                    
        except Exception as e:
            print(f"守护进程发生错误: {str(e)}")
            self.cleanup(force=True)

if __name__ == "__main__":
    # 检查是否以root权限运行
    if os.geteuid() != 0:
        print("错误: 该程序需要以root权限运行")
        exit(1)
        
    daemon = BluetoothDaemon()
    daemon.run()

