using System.Text.RegularExpressions;

namespace ComputerAsKeyboardInterface
{
    internal partial class DeviceResolver
    {
        // 用于匹配设备名称的正则表达式（N: Name="..."）
        private static readonly Regex NameRegex = MyRegex();

        // 用于匹配event路径的正则表达式（H: Handlers=...eventX...）
        private static readonly Regex EventRegex = MyRegex1();

        internal static Dictionary<string, string> InputDevicesMapping = new Dictionary<string, string>();

        /// <summary>
        /// 从/proc/bus/input/devices解析设备名称与event路径的映射
        /// </summary>
        public static Dictionary<string, string> GetInputDevicesFromProc()
        {
            var deviceMap = new Dictionary<string, string>();
            const string procPath = "/proc/bus/input/devices";

            if (!File.Exists(procPath)) return deviceMap;

            var lines = File.ReadAllLines(procPath);
            string? currentName = null;
            string? currentEvent = null;

            foreach (var line in lines)
            {
                // 解析设备名称（N: Name="..."）
                var nameMatch = NameRegex.Match(line);
                if (nameMatch.Success)
                {
                    currentName = nameMatch.Groups[1].Value;
                    continue;
                }

                // 解析Handlers中的event节点（如event3）
                if (line.StartsWith("H: Handlers="))
                {
                    var eventMatch = EventRegex.Match(line);
                    if (eventMatch.Success)
                    {
                        currentEvent = $"/dev/input/{eventMatch.Value}";
                    }
                }

                // 空行表示一个设备信息块结束，添加到字典
                if (!string.IsNullOrWhiteSpace(line) || currentName == null || currentEvent == null) continue;
                deviceMap.TryAdd(currentName, currentEvent);

                // 重置当前设备信息
                currentName = null;
                currentEvent = null;
            }

            InputDevicesMapping = deviceMap;
            return deviceMap;
        }

        [GeneratedRegex("""^N: Name="(.*)"$""")]
        private static partial Regex MyRegex();

        [GeneratedRegex(@"event\d+")]
        private static partial Regex MyRegex1();
    }
}