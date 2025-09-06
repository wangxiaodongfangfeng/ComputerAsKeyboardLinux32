namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public class ThinkpadKeyLayout
    {
        public const string KeyboardLayoutString = """

                                                   |‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾|
                                                   | ESC  │  MT  │  V-  │  V+  │   │  TV  │      │      │      │   │  IO  │ PRTSR│SCRLK │PAUSE │   │INSERT│ HOME │ PGUP │
                                                   |      │      │      │      │   │      │      │      │      │   │      │      │      │      │   │      │      │      │
                                                   |----------------------------   -----------------------------   -----------------------------   ----------------------
                                                   |  F1  │  F2  │  F3  │  F4  │   │  F5  │  F6  │  F7  │  F8  │   │  F9  │  FA  │  FB  │  FC  │   │DELETE│ END  │ PGDN │
                                                   |      │      │      │      │   │      │      │      │      │   │      │      │      │      │   │      │      │      │
                                                   |=====================================================================================================================
                                                   | ~     │ !     │ @     │ #     │  $    │ %     │ ^     │ &     │ *     │ (     │ )     │ _     │ +     │            │
                                                   |       │       │       │       │       │       │       │       │       │       │       │ -     │ =     │ <-BACKSPACE│
                                                   | `     │ 1     │ 2     │ 3     │ 4     │ 5     │ 6     │ 7     │ 8     │ 9     │ 0     │       │       │            │
                                                   |---------------------------------------------------------------------------------------------------------------------
                                                   |         │       │       │       │       │       │       │       │       │       │       │ {     │ }     │ │        │
                                                   |  TAB    │   Q   │   W   │   E   │   R   │   T   │   Y   │   U   │   I   │   O   │   P   │       │       │          │
                                                   |         │       │       │       │       │       │       │       │       │       │       │ [     │ ]     │ \        │
                                                   |---------------------------------------------------------------------------------------------------------------------
                                                   |           │       │       │       │       │       │       │       │       │       │ :     │ ''    │                │
                                                   | CAPSLK    │   A   │   S   │   D   │   F   │   G   │   H   │   J   │   K   │   L   │       │       │   ENTER        │
                                                   |           │       │       │       │       │       │       │       │       │       │ ;     │ '     │                │
                                                   |---------------------------------------------------------------------------------------------------------------------
                                                   |               │       │       │       │       │       │       │       │ <     │ >     │ ?     │                    │
                                                   |   SHIFT       │   Z   │   X   │   C   │   V   │   B   │   N   │   M   │       │       │       │    SHIFT           │
                                                   |               │       │       │       │       │       │       │       │ ,     │ .     │ /     │                    │
                                                   |---------------------------------------------------------------------------------------------------------------------
                                                   |       │       │      │       │                                       │       │       │       │       │   ^  │      │
                                                   |   FN  │  CTRL │  WIN │  ALT  │                SPACE                  │  ALT  │  MENU │ CTRL  │-------│------│------│
                                                   |       │       │      │       │                                       │       │       │       │   <   │      │   >  │
                                                   |---------------------------------------------------------------------------------------------------------------------
                                                   ======================================================================================================================
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   |                                                                                                                    | 
                                                   ======================================================================================================================

                                                   """;

        public static int StartColumn { get; private set; } = 1;

        public static List<List<char>> KeyLayoutChars
        {
            get
            {
                var chars = new List<List<char>>();
                var chartList = new List<char>();
                chars.Add(chartList);
                new List<char>(KeyboardLayoutString.ToCharArray()).ForEach(c =>
                {
                    if (c == '\n')
                    {
                        chartList = [];
                        chars.Add(chartList);
                        return;
                    }

                    chartList.Add(c);
                });
                return chars;
            }
        }

        public static void WriteKeyboardOnScreen()
        {
            Console.CursorVisible = false; //hide 
            Console.Clear(); //

            var screenWidth = Console.WindowWidth;
            var offset = (screenWidth - 118) / 2;
            StartColumn = offset + 1;

            KeyLayoutChars.ForEach(chars =>
            {
                for (var i = 0; i < offset; i++)
                {
                    Console.Write(" ");
                }

                chars.ForEach(Console.Write);
                Console.WriteLine();
            });
            //Console.WriteLine(KeyboardLayoutString);
        }

        private readonly List<List<EventCode>> _keyLayout;

        public ThinkpadKeyLayout()
        {
            var firstRow = new List<EventCode>
            {
                EventCode.Esc, EventCode.Mute, EventCode.VolumeDown, EventCode.VolumeUp, EventCode.Prog1,
                EventCode.Again,
                EventCode.Again, EventCode.Again, EventCode.Power, EventCode.Print, EventCode.ScrollLock,
                EventCode.Pause,
                EventCode.Insert, EventCode.Home, EventCode.Pageup
            };

            var secondRow = new List<EventCode>
            {
                EventCode.F1, EventCode.F2, EventCode.F3, EventCode.F4, EventCode.F5, EventCode.F6, EventCode.F7,
                EventCode.F8,
                EventCode.F9, EventCode.F10, EventCode.F11, EventCode.F12, EventCode.Delete, EventCode.End,
                EventCode.Pagedown
            };

            var thirdRow = new List<EventCode>
            {
                EventCode.Grave, EventCode.Num1, EventCode.Num2, EventCode.Num3, EventCode.Num4, EventCode.Num5,
                EventCode.Num6,
                EventCode.Num7, EventCode.Num8, EventCode.Num9, EventCode.Num0, EventCode.Minus, EventCode.Equal,
                EventCode.Backspace
            };

            var forthRow = new List<EventCode>
            {
                EventCode.Tab, EventCode.Q, EventCode.W, EventCode.E, EventCode.R, EventCode.T, EventCode.Y,
                EventCode.U,
                EventCode.I,
                EventCode.O, EventCode.P, EventCode.LeftBrace, EventCode.RightBrace, EventCode.Backslash
            };

            var fifthRow = new List<EventCode>
            {
                EventCode.Capslock, EventCode.A, EventCode.S, EventCode.D, EventCode.F, EventCode.G, EventCode.H,
                EventCode.J, EventCode.K,
                EventCode.L, EventCode.Semicolon, EventCode.Apostrophe, EventCode.Enter
            };

            var sixthRow = new List<EventCode>
            {
                EventCode.LeftShift, EventCode.Z, EventCode.X, EventCode.C, EventCode.V, EventCode.B, EventCode.N,
                EventCode.M, EventCode.Comma,
                EventCode.Dot, EventCode.Slash, EventCode.RightShift
            };

            var seventhRow = new List<EventCode>
            {
                EventCode.Wakeup, EventCode.LeftCtrl, EventCode.LeftMeta, EventCode.LeftAlt, EventCode.Space,
                EventCode.RightAlt,
                EventCode.Menu, EventCode.RightCtrl, EventCode.Back, EventCode.Up, EventCode.Forward, EventCode.Left,
                EventCode.Down,
                EventCode.Right
            };

            _keyLayout = [firstRow, secondRow, thirdRow, forthRow, fifthRow, sixthRow, seventhRow];
        }

        public Tuple<int, int, int, int> FindKeyPositions(EventCode code)
        {
            var row = _keyLayout.FindIndex(r => r.Contains(code));
            if (row < 0)
            {
                return new Tuple<int, int, int, int>(0, 0, 0, 0);
            }

            var column = _keyLayout[row].FindIndex(c => c == code);

            return row switch
            {
                0 or 1 => FirstTwoRowPosition(row, column),
                2 => ThirdRowPosition(column),
                3 => FourthRowPosition(column),
                4 => FifthRowPosition(column),
                5 => SixthRowPosition(column),
                6 => SeventhRowPosition(column),
                _ => new Tuple<int, int, int, int>(0, 0, 0, 0)
            };
        }

        /// <summary>
        /// calculate first two rows key postion;
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="keyIndex"></param>
        /// <returns></returns>
        private static Tuple<int, int, int, int> FirstTwoRowPosition(int rowIndex, int keyIndex)
        {
            var startColumn = keyIndex * 7 + (keyIndex / 3) * 3 + StartColumn;
            var startRow = rowIndex * 3 + 2;
            var endColomn = startColumn + 6;
            var endRow = startRow + 1;
            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        private static Tuple<int, int, int, int> ThirdRowPosition(int keyIndex)
        {
            var startColumn = keyIndex * 8 + StartColumn;
            var startRow = 8;
            var endColomn = startColumn + 7;
            var endRow = startRow + 2;
            if (keyIndex == 13)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        private static Tuple<int, int, int, int> FourthRowPosition(int keyIndex)
        {
            var startColumn = keyIndex * 8 + StartColumn;
            if (keyIndex > 0)
            {
                startColumn += 2;
            }

            var startRow = 12;
            var endColomn = startColumn + 7;
            if (keyIndex == 0)
            {
                endColomn += 2;
            }

            var endRow = startRow + 2;
            if (keyIndex == 13)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        private static Tuple<int, int, int, int> FifthRowPosition(int keyIndex)
        {
            var startColumn = keyIndex * 8 + StartColumn;
            if (keyIndex > 0)
            {
                startColumn += 4;
            }

            var startRow = 16;
            var endColomn = startColumn + 7;
            if (keyIndex == 0)
            {
                endColomn += 4;
            }

            var endRow = startRow + 2;
            if (keyIndex == 12)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        private static Tuple<int, int, int, int> SixthRowPosition(int keyIndex)
        {
            var startColumn = keyIndex * 8 + StartColumn;
            if (keyIndex > 0)
            {
                startColumn += 8;
            }

            var startRow = 20;
            var endColomn = startColumn + 7;
            if (keyIndex == 0)
            {
                endColomn += 8;
            }

            var endRow = startRow + 2;
            if (keyIndex == 11)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        private static Tuple<int, int, int, int> SeventhRowPosition(int keyIndex)
        {
            var startRow = 24;
            var endRow = startRow + 2;
            var startColumn = 0;
            var endColumn = 0;
            switch (keyIndex)
            {
                case 0:
                    startColumn = 0;
                    endColumn = 6;
                    break;
                case 1:
                    startColumn = 8;
                    endColumn = 14;
                    break;
                case 2:
                    startColumn = 16;
                    endColumn = 21;
                    break;
                case 3:
                    startColumn = 23;
                    endColumn = 29;
                    break;
                case 4:
                    startColumn = 31;
                    endColumn = 69;
                    break;
                case 5:
                    startColumn = 71;
                    endColumn = 77;
                    break;
                case 6:
                    startColumn = 79;
                    endColumn = 85;
                    break;
                case 7:
                    startColumn = 87;
                    endColumn = 93;
                    break;
                case 8:
                    startColumn = 95;
                    endColumn = 101;
                    endRow = startRow;
                    break;
                case 9:
                    startColumn = 103;
                    endColumn = 108;
                    endRow = startRow;
                    break;
                case 10:
                    startColumn = 110;
                    endColumn = 115;
                    endRow = startRow;
                    break;
                case 11:
                    startColumn = 95;
                    endColumn = 101;
                    startRow = startRow + 2;
                    endRow = startRow;
                    break;
                case 12:
                    startColumn = 103;
                    endColumn = 108;
                    startRow = startRow + 2;
                    endRow = startRow;
                    break;
                case 13:
                    startColumn = 110;
                    endColumn = 115;
                    startRow = startRow + 2;
                    endRow = startRow;
                    break;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn + StartColumn, endRow, endColumn);
        }

        public static void ToggleKeys(List<List<char>> chars, Tuple<int, int, int, int> values)
        {
            var sr = values.Item1;
            var sc = values.Item2;
            var er = values.Item3;
            var ec = values.Item4;
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            for (var i = sr; i <= er; i++)
            {
                for (var j = sc; j <= ec; j++)
                {
                    Console.SetCursorPosition(j - 1, i);
                    Console.Write(chars[i][j - ThinkpadKeyLayout.StartColumn]);
                }
            }

            Console.ResetColor();
            //await Task.Delay(50);
            Thread.Sleep(30);
            Console.BackgroundColor = ConsoleColor.Black;
            for (var i = sr; i <= er; i++)
            {
                for (var j = sc; j <= ec; j++)
                {
                    Console.SetCursorPosition(j, i);
                    Console.Write(chars[i][j - ThinkpadKeyLayout.StartColumn]);
                }
            }
        }
    }
}