using System;
using System.Collections.Generic;
namespace ComputerAsKeyboardLinux32
{

    public class MenuHandler
    {

        public static string Menu { get; set; } = @"
|============================================================|
| E.ExitApplication                                          |
| B.BackToKeyboard                                           |
| P.SetPassowrd                                              |
| R.RefreshKeyboard                                          |
|                                                            |
|============================================================|";

        public static bool CommandMode { get; set; }

        public static Action BeforeExitApplication { get; set; }

        public static List<List<char>> MenuChars
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
                chars.ForEach(c => { Console.Write(c); });
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
        public static void HandleExitProgram()
        {
            if (BeforeExitApplication != null)
            {
                BeforeExitApplication.Invoke();
            }

            Environment.Exit(0);
        }

        public static void HandleBackToKeyboard()
        {
            CommandMode = false;
            ThinkpadKeyLayout.WriteKeyboardOnScreen();
        }
        private static void FunctionForSetPassword()
        {
            Console.Clear();
            Console.CursorVisible = true;
            Console.SetCursorPosition(0, 0);
            MenuHandler.CommandMode = true;
            Console.WriteLine("Please Input Your Password");
            Program.Password = Console.ReadLine();
            Console.WriteLine(string.Format("Your password is {0}, Confirm? (Y/n)", Program.Password));
            var input = Console.ReadLine();
            if (input == "n") FunctionForSetPassword();
            CommandMode = false;
            return;
        }

        public static void HandleRefreshKeyboard()
        {

        }
    }
}