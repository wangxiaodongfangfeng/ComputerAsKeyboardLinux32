using System;

namespace ComputerAsKeyboardLinux32
{
    public class LogKeyboard:IKeyboard
    {
        public void mouseButtonUpAll()
        {
        }

        public void mouseButtonDown(MouseButtonCode buttonCode)
        {
        }

        public void mouseButtonUpAllForMac()
        {
        }

        public void mouseButtonDownForMac(MouseButtonCode buttonCode)
        {
        }

        public void mouseMoveRel(int x, int y, bool keyHold, MouseButtonCode button)
        {
        }

        public void mouseMoveRel(int x, int y)
        {
        }

        public void keyDown(SpecialKeyCode specialKeyCode)
        {
        }

        public void keyDown(KeyGroup keyGroup, byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0)
        {
        }

        public void keyUpAll(KeyGroup keyGroup)
        {
        }

        public void keyUpAll()
        {
        }

        public void mouseScrollForMac(int value)
        {
        }

        public void charKeyType(string typeString)
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}