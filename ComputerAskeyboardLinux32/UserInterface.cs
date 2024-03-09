using Gtk;
public class UserInterface
{
    public static void Run()
    {
        Application.Init();

        // Create the main window
        Window window = new Window("Keyboard Layout");
        window.SetDefaultSize(400, 200);

        // Create a vertical box container to hold the buttons
        Box vbox = new Box(Orientation.Vertical, 5);
        window.Add(vbox);

        // Define the keyboard layout
        string[] row1 = { "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "PrintScr", "ScrollLock", "Pause" };
        string[] row2 = { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Backspace", "Insert", "Home", "PageUp" };
        string[] row3 = { "Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\", "Delete", "End", "PageDown" };
        string[] row4 = { "CapsLock", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter" };
        string[] row5 = { "Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift" };
        string[] row6 = { "Ctrl", "Win", "Alt", "Space", "Alt", "Win", "Menu", "Ctrl" };
        string[] row7 = { "Up", "Left", "Down", "Right" };

        // Create buttons for each row
        CreateButtons(row1, vbox);
        CreateButtons(row2, vbox);
        CreateButtons(row3, vbox);
        CreateButtons(row4, vbox);
        CreateButtons(row5, vbox);
        CreateButtons(row6, vbox);
        CreateButtons(row7, vbox);

        // Show all widgets
        window.ShowAll();

        Application.Run();

    }
    static void CreateButtons(string[] labels, Box container)
    {
        // Create a horizontal box container to hold the buttons for a row
        Box hbox = new Box(Orientation.Horizontal, 5);

        // Create buttons for each label in the row
        foreach (string label in labels)
        {
            Button button = new Button(label);
            hbox.PackStart(button, true, true, 0);
        }

        // Add the row container to the main container
        container.PackStart(hbox, true, true, 0);
    }
}

