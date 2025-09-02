import tkinter as tk
from tkinter import ttk
from PIL import Image, ImageTk
import socket
import threading
import io
import time

class ScreenshotClient:
    def __init__(self, root):
        # 配置参数
        self.server_ip = "172.20.10.2"    # 服务器IP地址
        self.server_port = 12345        # 服务器端口
        self.reconnect_delay = 5        # 重连延迟(秒)
        self.buffer_size = 4096         # 接收缓冲区大小
        
        self.root = root
        self.client_socket = None
        self.connected = False
        self.current_image = None
        
        # 初始化窗口
        self._setup_window()
        
        # 启动连接线程
        self._start_connection_thread()

    def _setup_window(self):
        """设置窗口属性"""
        self.root.title("截图接收客户端")
        
        # 窗口设置：全屏、置顶、无边界
        self.root.attributes("-fullscreen", True)
        self.root.attributes("-topmost", True)
        self.root.attributes("-alpha", 1.0)  # 不透明
        self.root.overrideredirect(True)     # 无标题栏
        self.root.protocol("WM_DELETE_WINDOW", self._do_nothing)
                # 获取屏幕尺寸
        screen_width = self.root.winfo_screenwidth()
        screen_height = self.root.winfo_screenheight()
        
        # 强制窗口大小与屏幕一致（关键修复）
        self.root.geometry(f"{screen_width}x{screen_height}+0+0")
        # 黑色背景
        self.root.configure(bg="#000000")
        
        # 创建图片显示标签
        self.image_label = ttk.Label(self.root, background="#000000")
        self.image_label.place(relx=0.5, rely=0.5, anchor="center")
        
        # 初始文本
        self._show_text(f"连接到服务器...\n{self.server_ip}:{self.server_port}")
        
        # 拦截输入事件
        self.root.bind("<Key>", lambda e: "break")
        self.root.bind("<Button-1>", lambda e: "break")
        
        # 监听窗口大小变化
        self.root.bind("<Configure>", self._on_resize)

    def _do_nothing(self):
        """忽略关闭事件"""
        pass

    def _on_resize(self, event):
        """窗口大小变化时重新显示图片"""
        if self.current_image:
            self._display_image(self.current_image)

    def _show_text(self, text):
        """显示文本信息"""
        self.image_label.config(
            text=text,
            font=("Arial", 18),
            foreground="#ffffff"
        )

    def _display_image(self, image_data):
        """显示图片"""
        try:
            # 从二进制数据加载图片
            img = Image.open(io.BytesIO(image_data))
            self.current_image = image_data

            # 获取屏幕尺寸
            screen_width = self.root.winfo_screenwidth()
            screen_height = self.root.winfo_screenheight()

            # 缩放图片以适应全屏
            img = img.resize((screen_width, screen_height), Image.LANCZOS)

            # 转换为Tkinter可用格式
            tk_img = ImageTk.PhotoImage(image=img)

            # 显示图片
            self.image_label.config(image=tk_img, text="")
            self.image_label.image = tk_img  # 保持引用
            print(f"[{time.strftime('%H:%M:%S')}] 图片显示成功 ({img.size[0]}x{img.size[1]})")

        except Exception as e:
            error_msg = f"显示错误: {str(e)}"
            self._show_text(error_msg)
            print(f"[{time.strftime('%H:%M:%S')}] {error_msg}")

    def _receive_images(self):
        """接收服务器推送的图片"""
        while self.connected and self.client_socket:
            try:
                # 接收图片大小(4字节，大端模式)
                size_data = b""
                while len(size_data) < 4:
                    chunk = self.client_socket.recv(4 - len(size_data))
                    if not chunk:
                        raise ConnectionResetError("服务器断开连接")
                    size_data += chunk
                
                image_size = int.from_bytes(size_data, byteorder='big')
                print(f"[{time.strftime('%H:%M:%S')}] 接收图片，大小: {image_size}字节")
                
                # 接收图片数据
                image_data = b""
                while len(image_data) < image_size:
                    chunk = self.client_socket.recv(
                        min(self.buffer_size, image_size - len(image_data))
                    )
                    if not chunk:
                        raise ConnectionResetError("服务器断开连接")
                    image_data += chunk
                
                # 在主线程中显示图片
                self.root.after(0, self._display_image, image_data)
                
            except Exception as e:
                error_msg = f"接收错误: {str(e)}"
                self.root.after(0, self._show_text, error_msg)
                print(f"[{time.strftime('%H:%M:%S')}] {error_msg}")
                
                # 断开连接并重连
                self.connected = False
                if self.client_socket:
                    try:
                        self.client_socket.close()
                    except:
                        pass

    def _connect_to_server(self):
        """连接到服务器并保持连接"""
        while True:
            if not self.connected:
                try:
                    # 显示连接状态
                    self.root.after(0, self._show_text, 
                                   f"正在连接 {self.server_ip}:{self.server_port}...")
                    
                    # 创建新连接
                    self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                    self.client_socket.connect((self.server_ip, self.server_port))
                    self.connected = True
                    
                    # 连接成功
                    self.root.after(0, self._show_text, "已连接，等待接收图片...")
                    print(f"[{time.strftime('%H:%M:%S')}] 已连接到服务器")
                    
                    # 开始接收图片
                    self._receive_images()
                    
                except Exception as e:
                    error_msg = f"连接失败: {str(e)}\n{self.reconnect_delay}秒后重试"
                    self.root.after(0, self._show_text, error_msg)
                    print(f"[{time.strftime('%H:%M:%S')}] {error_msg}")
                    
                    # 关闭 socket
                    if self.client_socket:
                        try:
                            self.client_socket.close()
                        except:
                            pass
                    
                    # 等待重连
                    time.sleep(self.reconnect_delay)
            else:
                time.sleep(1)

    def _start_connection_thread(self):
        """启动连接线程"""
        self.connect_thread = threading.Thread(target=self._connect_to_server, daemon=True)
        self.connect_thread.start()

if __name__ == "__main__":
    root = tk.Tk()
    app = ScreenshotClient(root)
    root.mainloop()
