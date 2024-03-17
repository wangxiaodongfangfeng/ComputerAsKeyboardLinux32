using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CH9329NameSpace;

namespace ComputerAskeyboardLinux32
{
    public interface IKeyboard
    {

        void mouseButtonUpAll();
        void mouseButtonDown(MouseButtonCode buttonCode);
        void mouseButtonUpAllForMac();
        void mouseButtonDownForMac(MouseButtonCode buttonCode);
        void mouseMoveRel(int x, int y, bool keyHold, MouseButtonCode button);
        void mouseMoveRel(int x, int y);
        void keyDown(SpecialKeyCode specialKeyCode);
        /// <summary>
        /// Push key
        /// </summary>
        /// <param name="CMD">KetType</param>
        /// <param name="k0">special key code</param>
        /// <param name="k1">key code #1</param>
        /// <param name="k2">key code #2</param>
        /// <param name="k3">key code #3</param>
        /// <param name="k4">key code #4</param>
        /// <param name="k5">key code #5</param>
        /// <param name="k6">key code #6</param>
        void keyDown(KeyGroup keyGroup, byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0);
        void keyUpAll(KeyGroup keyGroup);
        void keyUpAll();
        void mouseScrollForMac(int value);
    }
}
