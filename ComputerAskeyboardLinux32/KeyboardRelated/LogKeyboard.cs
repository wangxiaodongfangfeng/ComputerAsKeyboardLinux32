namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public class LogKeyboard:IKeyboard
    {
        public void MouseButtonUpAll()
        {
        }

        public void MouseButtonDown(MouseButtonCode buttonCode)
        {
        }

        public void MouseButtonUpAllForMac()
        {
        }

        public void MouseButtonDownForMac(MouseButtonCode buttonCode)
        {
        }

        public void MouseMoveRel(int x, int y, bool keyHold, MouseButtonCode button)
        {
        }

        public void MouseMoveRel(int x, int y)
        {
        }

        public void KeyDown(SpecialKeyCode specialKeyCode)
        {
        }

        public void KeyDown(KeyGroup keyGroup, byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0)
        {
        }

        public void KeyUpAll(KeyGroup keyGroup)
        {
        }

        public void KeyUpAll()
        {
        }

        public void MouseScrollForMac(int value)
        {
        }

        public void CharKeyType(string typeString)
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}