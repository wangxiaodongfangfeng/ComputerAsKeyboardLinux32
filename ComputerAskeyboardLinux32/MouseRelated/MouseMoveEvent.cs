namespace ComputerAsKeyboardInterface.MouseRelated
{
    public class MouseMoveEvent(MouseAxis axis, int amount) : EventArgs
    {
        public MouseAxis Axis { get; set; } = axis;

        public int Amount { get; set; } = amount;
    }
}