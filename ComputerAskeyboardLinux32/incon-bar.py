import sys
import subprocess
from PyQt5.QtWidgets import QApplication, QSystemTrayIcon, QMenu, QAction, QStyle, QInputDialog
from PyQt5.QtGui import QIcon
from PyQt5.QtCore import Qt

class KeyboardTrayApp:
    def __init__(self):
        # 初始化应用
        self.app = QApplication(sys.argv)
        if not QSystemTrayIcon.isSystemTrayAvailable():
            print("系统托盘不可用")
            sys.exit(1)

        self.app.setQuitOnLastWindowClosed(False)

        # 创建托盘图标
        self.tray = QSystemTrayIcon()
        self.tray.setIcon(self.app.style().standardIcon(QStyle.SP_ComputerIcon))  # 使用电脑图标
        self.tray.setVisible(True)

        # 创建右键菜单
        self.menu = QMenu()

        # 打开keyboard程序动作
        self.open_action = QAction("打开Keyboard程序")
        self.open_action.triggered.connect(self.open_keyboard)
        self.menu.addAction(self.open_action)

        # 关闭keyboard程序动作
        self.close_action = QAction("关闭Keyboard程序")
        self.close_action.triggered.connect(self.close_keyboard)
        self.menu.addAction(self.close_action)

        # 分隔线
        self.menu.addSeparator()

        # 退出本程序
        self.quit_action = QAction("退出")
        self.quit_action.triggered.connect(self.quit_application)
        self.menu.addAction(self.quit_action)

        # 设置菜单
        self.tray.setContextMenu(self.menu)
        self.tray.showMessage(
            "Keyboard控制器",
            "已在状态栏运行",
            QSystemTrayIcon.Information,
            2000
        )

        # 记录keyboard程序的进程信息（可选）
        self.keyboard_process = None

    def open_keyboard(self):
        """打开keyboard程序，带sudo权限和指定参数"""
        try:
            # 命令参数（根据你的需求调整路径和参数）
            # 注意：需要替换为你的keyboard程序实际路径
            command = [
                "sudo",
                "./keyboard",  # 若程序不在当前目录，需写绝对路径，如"/home/yourname/keyboard"
                "-v", "false",
                "-ba", "115200",
                "-bluetoothport", "true",
                "-background", "true"
            ]

            # 执行命令
            # 若已配置sudo免密，可直接用subprocess.Popen
            # 否则需要处理密码输入
            self.keyboard_process = subprocess.Popen(
                command,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True
            )

            self.tray.showMessage(
                "操作成功",
                "Keyboard程序已启动",
                QSystemTrayIcon.Information,
                1500
            )

        except Exception as e:
            # 处理可能的错误（如权限不足）
            error_msg = f"启动失败: {str(e)}"
            self.tray.showMessage(
                "错误",
                error_msg,
                QSystemTrayIcon.Warning,
                3000
            )
            print(error_msg)  # 调试用

    def close_keyboard(self):
        """关闭keyboard程序"""
        try:
            # 方法1：通过pkill关闭（简单直接）
            subprocess.run(
                ["sudo", "pkill", "-f", "keyboard"],  # -f匹配完整命令行
                check=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True
            )

            self.tray.showMessage(
                "操作成功",
                "Keyboard程序已关闭",
                QSystemTrayIcon.Information,
                1500
            )

        except Exception as e:
            error_msg = f"关闭失败: {str(e)}"
            self.tray.showMessage(
                "错误",
                error_msg,
                QSystemTrayIcon.Warning,
                3000
            )
            print(error_msg)

    def quit_application(self):
        """退出托盘应用"""
        # 退出前先关闭keyboard程序（可选）
        self.close_keyboard()

        self.tray.showMessage(
            "退出",
            "程序已退出",
            QSystemTrayIcon.Information,
            1000
        )
        self.app.quit()

    def run(self):
        sys.exit(self.app.exec_())

if __name__ == "__main__":
    # 字体设置
    font = QApplication.font()
    font.setFamily("Sans")
    QApplication.setFont(font)

    app = KeyboardTrayApp()
    app.run()
