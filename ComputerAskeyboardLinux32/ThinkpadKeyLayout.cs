using System;
using System.Collections.Generic;

namespace ComputerAsKeyboardLinux32
{
    public class ThinkpadKeyLayout
    {
        public const string KeyboardLayoutString = @"
|--------------------------------------------------------------------------------------------------------------------|
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
";

        public static int StartColumn { get; set; } = 1;

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
                        chartList = new List<char>();
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

            int screenWidth = Console.WindowWidth;
            int offset = (screenWidth - 118) / 2;
            StartColumn = offset + 1;

            KeyLayoutChars.ForEach(chars =>
            {
                for (var i = 0; i < offset; i++)
                {
                    Console.Write(" ");
                }
                chars.ForEach(c =>
                {
                    Console.Write(c);
                });
                Console.WriteLine();
            });
            //Console.WriteLine(KeyboardLayoutString);
        }


        public List<List<EventCode>> keyLayout;

        public ThinkpadKeyLayout()
        {
            List<EventCode> firstRow = new List<EventCode>
            {
                EventCode.Esc, EventCode.Mute, EventCode.VolumeDown, EventCode.VolumeUp, EventCode.Prog1,
                EventCode.Again,
                EventCode.Again, EventCode.Again, EventCode.Power, EventCode.Print, EventCode.ScrollLock,
                EventCode.Pause,
                EventCode.Insert, EventCode.Home, EventCode.Pageup
            };

            List<EventCode> secondRow = new List<EventCode>
            {
                EventCode.F1, EventCode.F2, EventCode.F3, EventCode.F4, EventCode.F5, EventCode.F6, EventCode.F7,
                EventCode.F8,
                EventCode.F9, EventCode.F10, EventCode.F11, EventCode.F12, EventCode.Delete, EventCode.End,
                EventCode.Pagedown
            };

            List<EventCode> thirdRow = new List<EventCode>
            {
                EventCode.Grave, EventCode.Num1, EventCode.Num2, EventCode.Num3, EventCode.Num4, EventCode.Num5,
                EventCode.Num6,
                EventCode.Num7, EventCode.Num8, EventCode.Num9, EventCode.Num0, EventCode.Minus, EventCode.Equal,
                EventCode.Backspace
            };

            List<EventCode> forthRow = new List<EventCode>
            {
                EventCode.Tab, EventCode.Q, EventCode.W, EventCode.E, EventCode.R, EventCode.T, EventCode.Y,
                EventCode.U,
                EventCode.I,
                EventCode.O, EventCode.P, EventCode.LeftBrace, EventCode.RightBrace, EventCode.Backslash
            };

            List<EventCode> fifthRow = new List<EventCode>
            {
                EventCode.Capslock, EventCode.A, EventCode.S, EventCode.D, EventCode.F, EventCode.G, EventCode.H,
                EventCode.J, EventCode.K,
                EventCode.L, EventCode.Semicolon, EventCode.Apostrophe, EventCode.Enter
            };

            List<EventCode> sixthRow = new List<EventCode>
            {
                EventCode.LeftShift, EventCode.Z, EventCode.X, EventCode.C, EventCode.V, EventCode.B, EventCode.N,
                EventCode.M, EventCode.Comma,
                EventCode.Dot, EventCode.Slash, EventCode.RightShift
            };

            List<EventCode> seventhRow = new List<EventCode>
            {
                EventCode.Wakeup, EventCode.LeftCtrl, EventCode.LeftMeta, EventCode.LeftAlt, EventCode.Space,
                EventCode.RightAlt,
                EventCode.Menu, EventCode.RightCtrl, EventCode.Back, EventCode.Up, EventCode.Forward, EventCode.Left,
                EventCode.Down,
                EventCode.Right
            };

            keyLayout = new List<List<EventCode>>
            {
                firstRow, secondRow, thirdRow, forthRow, fifthRow, sixthRow, seventhRow
            };
        }

        public Tuple<int, int, int, int> FindKeyPositions(EventCode code)
        {
            int row = keyLayout.FindIndex(r => r.Contains(code));
            if (row < 0)
            {
                return new Tuple<int, int, int, int>(0, 0, 0, 0);
            }

            int column = keyLayout[row].FindIndex(c => c == code);

            switch (row)
            {
                case 0:
                case 1:
                    return FirstTwoRowPosition(row, column);
                case 2:
                    return ThirdRowPosition(column);
                case 3:
                    return FourthRowPosition(column);
                case 4:
                    return FifthRowPosition(column);
                case 5:
                    return SixthRowPosition(column);
                case 6:
                    return SeventhRowPosition(column);
                default:
                    return new Tuple<int, int, int, int>(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// calculate first two rows key postion;
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="keyIndex"></param>
        /// <returns></returns>
        public Tuple<int, int, int, int> FirstTwoRowPosition(int rowIndex, int keyIndex)
        {
            int startColumn = keyIndex * 7 + (keyIndex / 3) * 3 + StartColumn;
            int startRow = rowIndex * 3 + 2;
            int endColomn = startColumn + 6;
            int endRow = startRow + 1;
            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        public Tuple<int, int, int, int> ThirdRowPosition(int keyIndex)
        {
            int startColumn = keyIndex * 8 + StartColumn;
            int startRow = 8;
            int endColomn = startColumn + 7;
            int endRow = startRow + 2;
            if (keyIndex == 13)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        public Tuple<int, int, int, int> FourthRowPosition(int keyIndex)
        {
            int startColumn = keyIndex * 8 + StartColumn;
            if (keyIndex > 0)
            {
                startColumn += 2;
            }

            int startRow = 12;
            int endColomn = startColumn + 7;
            if (keyIndex == 0)
            {
                endColomn += 2;
            }

            int endRow = startRow + 2;
            if (keyIndex == 13)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        public Tuple<int, int, int, int> FifthRowPosition(int keyIndex)
        {
            int startColumn = keyIndex * 8 + StartColumn;
            if (keyIndex > 0)
            {
                startColumn += 4;
            }

            int startRow = 16;
            int endColomn = startColumn + 7;
            if (keyIndex == 0)
            {
                endColomn += 4;
            }

            int endRow = startRow + 2;
            if (keyIndex == 12)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        public Tuple<int, int, int, int> SixthRowPosition(int keyIndex)
        {
            int startColumn = keyIndex * 8 + StartColumn;
            if (keyIndex > 0)
            {
                startColumn += 8;
            }

            int startRow = 20;
            int endColomn = startColumn + 7;
            if (keyIndex == 0)
            {
                endColomn += 8;
            }

            int endRow = startRow + 2;
            if (keyIndex == 11)
            {
                endColomn = 116;
            }

            return new Tuple<int, int, int, int>(startRow, startColumn, endRow, endColomn);
        }

        public Tuple<int, int, int, int> SeventhRowPosition(int keyIndex)
        {
            int startRow = 24;
            int endRow = startRow + 2;
            int startColumn = 0;
            int endColumn = 0;
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
    }
}