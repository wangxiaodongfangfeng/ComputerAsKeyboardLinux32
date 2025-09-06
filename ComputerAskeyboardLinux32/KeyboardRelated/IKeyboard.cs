namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public interface IKeyboard:IDisposable
    {

        void MouseButtonUpAll();
        void MouseButtonDown(MouseButtonCode buttonCode);
        void MouseButtonUpAllForMac();
        void MouseButtonDownForMac(MouseButtonCode buttonCode);
        void MouseMoveRel(int x, int y, bool keyHold, MouseButtonCode button);
        void MouseMoveRel(int x, int y);
        void KeyDown(SpecialKeyCode specialKeyCode);

        /// <summary>
        /// Push key
        /// </summary>
        /// <param name="keyGroup"></param>
        /// <param name="k0">special key code</param>
        /// <param name="k1">key code #1</param>
        /// <param name="k2">key code #2</param>
        /// <param name="k3">key code #3</param>
        /// <param name="k4">key code #4</param>
        /// <param name="k5">key code #5</param>
        /// <param name="k6">key code #6</param>
        void KeyDown(KeyGroup keyGroup, byte k0, byte k1, byte k2 = 0, byte k3 = 0, byte k4 = 0, byte k5 = 0, byte k6 = 0);
        void KeyUpAll(KeyGroup keyGroup);
        void KeyUpAll();
        void MouseScrollForMac(int value);
        void CharKeyType(string typeString);
    }
}
