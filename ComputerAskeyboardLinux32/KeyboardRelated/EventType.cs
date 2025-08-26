namespace ComputerAsKeyboardInterface.KeyboardRelated
{
    public enum EventType
    {
        /// <summary>
        /// Used as markers to separate events. Events may be separated in time or in space, such as with the multitouch protocol.
        /// </summary>
        EvSyn,

        /// <summary>
        /// Used to describe state changes of keyboards, buttons, or other key-like devices.
        /// </summary>
        EvKey,

        /// <summary>
        /// Used to describe relative axis value changes, e.g. moving the mouse 5 units to the left.
        /// </summary>
        EvRel,

        /// <summary>
        /// Used to describe absolute axis value changes, e.g. describing the coordinates of a touch on a touchscreen.
        /// </summary>
        EvAbs,

        /// <summary>
        /// Used to describe miscellaneous input data that do not fit into other types.
        /// </summary>
        EvMsc,

        /// <summary>
        /// Used to describe binary state input switches.
        /// </summary>
        EvSw,

        /// <summary>
        /// Used to turn LEDs on devices on and off.
        /// </summary>
        EvLed,

        /// <summary>
        /// Used to output sound to devices.
        /// </summary>
        EvSnd,

        /// <summary>
        /// Used for autorepeating devices.
        /// </summary>
        EvRep,

        /// <summary>
        /// Used to send force feedback commands to an input device.
        /// </summary>
        EvFf,

        /// <summary>
        /// A special type for power button and switch input.
        /// </summary>
        EvPwr,

        /// <summary>
        /// Used to receive force feedback device status.
        /// </summary>
        EvFfStatus,
    }
}