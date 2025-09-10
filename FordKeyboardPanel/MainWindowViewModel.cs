using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Timers;
using Avalonia.Data.Converters;

namespace FordKeyboardPanel
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        private string? _currentDate; // 日期和星期
        private string? _currentTime;
        private readonly Timer? _timer;

        public string? CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        // 日期和星期（如：2023-10-05 星期四）
        public string? CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<KeyboardDevice> KeyboardDevices { get; }

        // 常用波特率列表，用于下拉选择
        public int[] CommonBaudRates { get; } = [9600, 19200, 38400, 57600, 115200, 230400];

        public MainWindowViewModel()
        {
            // 初始化设备列表并添加示例数据
            KeyboardDevices =
            [
                new KeyboardDevice { Name = "USB Keyboard", Path = "/dev/ttyUSB0", BaudRate = 9600 },
                new KeyboardDevice { Name = "Bluetooth Keyboard", Path = "/dev/rfcomm0", BaudRate = 115200 },
                new KeyboardDevice { Name = "Wireless Keyboard", Path = "/dev/ttyACM0", BaudRate = 57600 }
            ];

            // 初始化时钟定时器
            _timer = new Timer(1000); // 每秒更新一次
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();

            // 初始设置时间
            UpdateCurrentTime();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            // 日期格式：年-月-日 星期几（dddd会显示完整星期名称）
            CurrentDate = now.ToString("yyyy-MM-dd dddd");
            // 时间格式：时:分:秒.毫秒
            CurrentTime = now.ToString("HH:mm:ss");
        }

        private void UpdateCurrentTime()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BoolToEnableDisableConverter : IValueConverter
    {
        public static BoolToEnableDisableConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "Disable" : "Enable";
            }

            return "Enable"; // 默认值
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}