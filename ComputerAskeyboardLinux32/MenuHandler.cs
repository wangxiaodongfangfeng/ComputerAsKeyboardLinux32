namespace ComputerAsKeyboardInterface
{
    public static class MenuHandler
    {
        private static string Menu { get; set; } = """

                                                   |============================================================|
                                                   | E.ExitApplication                                          |
                                                   | B.BackToKeyboard                                           |
                                                   | P.SetPassword                                              |
                                                   | R.RefreshKeyboard                                          |
                                                   |                                                            |
                                                   |============================================================|
                                                   """;

        public static bool CommandMode { get; private set; }

        public static Action? BeforeExitApplication { get; set; }

        private static List<List<char>> MenuChars
        {
            get
            {
                var chars = new List<List<char>>();
                var chartList = new List<char>();
                chars.Add(chartList);
                new List<char>(Menu.ToCharArray()).ForEach(c =>
                {
                    if (c == '\n')
                    {
                        chartList = new List<char>();
                        chars.Add(chartList);
                        return;
                    }
                    chartList.Add(c);
                });
                return chars;
            }
        }

        public static void StartMenu()
        {
        showMenu:
            Console.Clear();
            Console.CursorVisible = true;
            Console.SetCursorPosition(0, 0);
            CommandMode = true;
            Console.BackgroundColor = ConsoleColor.Black;
            var width = Console.WindowWidth;
            var height = Console.WindowHeight;

            var leftOffset = (width - MenuChars[1].Count) / 2;
            var topOffset = (height - MenuChars.Count) / 2;
            for (var i = 0; i < topOffset; i++) { Console.WriteLine(); }
            MenuChars.ForEach(chars =>
            {
                for (var i = 0; i < leftOffset; i++)
                {
                    Console.Write(" ");
                }
                chars.ForEach(Console.Write);
                Console.WriteLine();
            });
        readagin:
            var menu = Console.ReadKey();
            switch (menu.KeyChar)
            {
                case 'e':
                case 'E':
                    HandleExitProgram();
                    return;
                case 'b':
                case 'B':
                    HandleBackToKeyboard();
                    return;
                case 'p':
                case 'P':
                    FunctionForSetPassword();
                    goto showMenu;
                case 'r':
                case 'R':
                    HandleRefreshKeyboard();
                    return;
                default:
                    goto readagin;
            }
        }

        private static void HandleExitProgram()
        {
            BeforeExitApplication?.Invoke();
            Program.ExitInNext = true;
            Environment.Exit(0);
        }

        private static void HandleBackToKeyboard()
        {
            CommandMode = false;
            ThinkpadKeyLayout.WriteKeyboardOnScreen();
        }
        private static void FunctionForSetPassword()
        {
            Console.Clear();
            Console.CursorVisible = true;
            Console.SetCursorPosition(0, 0);
            CommandMode = true;
            Console.WriteLine("Please Input Your Password");
            Program.Password = Console.ReadLine();
            Console.WriteLine($"Your password is {Program.Password}, Confirm? (Y/n)");
            var input = Console.ReadLine();
            if (input == "n") FunctionForSetPassword();
            CommandMode = false;
            return;
        }

        private static void HandleRefreshKeyboard()
        {

        }
    }
}