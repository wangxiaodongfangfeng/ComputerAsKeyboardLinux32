using System.Text.RegularExpressions;

namespace ComputerAsKeyboardInterface
{
    internal static partial class DeviceResolver
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


        /// <summary>
        /// 执行xinput list命令并解析结果
        /// </summary>
        /// <returns>设备信息列表</returns>
        public static List<XInputDevice> GetXInputDevices()
        {
            var devices = new List<XInputDevice>();

            try
            {
                // 执行xinput list命令
                var result = LinuxCommandHelper.ExecuteCommand("xinput list");

                if (!result.Success)
                {
                    Console.WriteLine($"执行xinput命令失败: {result.Error}");
                    return devices;
                }

                // 解析命令输出
                if (result.Output != null) devices = ParseXInputOutput(result.Output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取xinput设备信息时发生错误: {ex.Message}");
            }

            return devices;
        }

        /// <summary>
        /// 解析xinput list命令的输出
        /// </summary>
        /// <param name="output">命令输出字符串</param>
        /// <returns>解析后的设备列表</returns>
        private static List<XInputDevice> ParseXInputOutput(string output)
        {
            var devices = new List<XInputDevice>();
            if (string.IsNullOrEmpty(output))
                return devices;

            // 正则表达式匹配设备行，正确提取id号前面的设备名称
            // 例如:
            // ⎜   ↳ Virtual core XTEST pointer           	id=4	[pointer  (2)]
            // 设备名称是 "Virtual core XTEST pointer"，id是 4
            const string pattern = @"↳\s(?<name>[^\n]+?)\s+id=(?<id>\d+)\s+\[";
            var matches = MyRegex2().Matches(output);

            foreach (Match match in matches)
            {
                // 提取设备ID
                if (!int.TryParse(match.Groups["id"].Value, out var id)) continue;
                // 提取并清理设备名称（去除前后空白字符）
                var name = match.Groups["name"].Value.Trim();
                // 去除名称中可能存在的特殊前缀字符（如⎜、↳等）
                //name = Regex.Replace(name, @"^[\s\x{231C}\x{239C}]+", "", RegexOptions.ECMAScript);
                devices.Add(new XInputDevice { Id = id, Name = name });
            }

            return devices;
        }

        [GeneratedRegex(@"↳\s(?<name>[^\n]+?)\s+id=(?<id>\d+)\s+\[", RegexOptions.Multiline)]
        private static partial Regex MyRegex2();
    }

    public class XInputDevice
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"ID: {Id}, Name: {Name}";
        }
    }
}