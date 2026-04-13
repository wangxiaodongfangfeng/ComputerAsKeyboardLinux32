import tkinter as tk
from tkinter import ttk
import sys
import math

class LinuxVirtualKeyboard:
    def __init__(self, root):
        self.root = root
        self.root.title("105-Key Virtual Keyboard")
        
        # Set fullscreen mode with escape key to exit
        # self.root.attributes('-fullscreen', True)
        self.root.bind('<Escape>', lambda e: self.root.destroy())
        self.root.geometry("1920x1080")  # Initial size, can be adjusted
        self.root.minsize(800, 300)  # Minimum size to ensure usability
        # Configure grid for fullscreen resizing
        self.root.grid_rowconfigure(0, weight=1)
        self.root.grid_columnconfigure(0, weight=1)
        
        # Main container with padding
        self.main_container = ttk.Frame(root, padding="10")
        self.main_container.grid(sticky="nsew")
        self.main_container.grid_rowconfigure(0, weight=1)
        self.main_container.grid_columnconfigure(0, weight=1)
        
        # Create a canvas for scrolling (in case window is resized smaller)
        self.canvas = tk.Canvas(self.main_container)
        self.scrollbar = ttk.Scrollbar(self.main_container, orient="horizontal", command=self.canvas.xview)
        self.keyboard_frame = ttk.Frame(self.canvas)
        
        # Configure scrolling
        self.keyboard_frame.bind(
            "<Configure>",
            lambda e: self.canvas.configure(scrollregion=self.canvas.bbox("all"))
        )
        
        self.canvas.create_window((0, 0), window=self.keyboard_frame, anchor="nw")
        self.canvas.configure(xscrollcommand=self.scrollbar.set)
        
        # Layout canvas and scrollbar
        self.canvas.grid(row=0, column=0, sticky="nsew")
        self.scrollbar.grid(row=1, column=0, sticky="ew")
        
        # Configure keyboard frame grid
        for i in range(6):  # 6 rows of keys
            self.keyboard_frame.grid_rowconfigure(i, weight=1)
        self.keyboard_frame.grid_columnconfigure(0, weight=1)
        
        # Key styles - adapted for Linux display
        self.normal_style = ttk.Style()
        self.normal_style.configure("Key.TButton", 
                                   font=("Ubuntu", 16),
                                   padding=(5,10))
        
        self.pressed_style = ttk.Style()
        self.pressed_style.configure("Pressed.TButton", 
                                    font=("Ubuntu", 16, "bold"),
                                    padding=(5,10),
                                    background="#3daee9",
                                    foreground="white")
        
        # Create keyboard rows with proper spacing
        self.create_keyboard_rows()
        
        # Track pressed keys
        self.pressed_keys = set()
    


    def create_keyboard_rows(self):
        """Create all keyboard rows with proper grid layout"""
        # Row 0 - Function keys
        row0 = [
            ('Esc', 5), ('F1', 3), ('F2', 3), ('F3', 3), ('F4', 3), 
            ('F5', 3), ('F6', 3), ('F7', 3), ('F8', 3), ('F9', 3), 
            ('F10', 3), ('F11', 3), ('F12', 3), ('PrtSc', 5), 
            ('ScrLk', 5), ('Pause', 5)
        ]
        self.create_row(0, row0)
        
        # Row 1 - Top number row
        row1 = [
            ('~', 5), ('1', 5), ('2', 5), ('3', 5), ('4', 5), ('5', 5),
            ('6', 5), ('7', 5), ('8', 5), ('9', 5), ('0', 5), ('-', 5),
            ('=', 5), ('Backspace', 10), ('Ins', 5), ('Home', 5),
            ('PgUp', 5), ('NumLk', 5), ('/', 5), ('*', 5), ('-', 5)
        ]
        self.create_row(1, row1)
        
        # Row 2 - QWERTY row
        row2 = [
            ('Tab', 7), ('Q', 5), ('W', 5), ('E', 5), ('R', 5), ('T', 5),
            ('Y', 5), ('U', 5), ('I', 5), ('O', 5), ('P', 5), ('[', 5),
            (']', 5), ('\\', 7), ('Del', 5), ('End', 5), ('PgDn', 5),
            ('7', 5), ('8', 5), ('9', 5), ('+', 5)
        ]
        self.create_row(2, row2)
        
        # Row 3 - A row
        row3 = [
            ('Caps', 8), ('A', 5), ('S', 5), ('D', 5), ('F', 5), ('G', 5),
            ('H', 5), ('J', 5), ('K', 5), ('L', 5), (';', 5), ("'", 5),
            ('Enter', 11), ('', 5), ('', 5), ('', 5),
            ('4', 5), ('5', 5), ('6', 5), ('', 5)
        ]
        self.create_row(3, row3)
        
        # Row 4 - Z row
        row4 = [
            ('Shift', 10), ('Z', 5), ('X', 5), ('C', 5), ('V', 5), ('B', 5),
            ('N', 5), ('M', 5), (',', 5), ('.', 5), ('/', 5), ('Shift', 12),
            ('↑', 5), ('', 5), ('', 5),
            ('1', 5), ('2', 5), ('3', 5), ('Enter', 5)
        ]
        self.create_row(4, row4)
        
        # Row 5 - Control row
        row5 = [
            ('Ctrl', 6), ('Win', 6), ('Alt', 6), (' ', 40), 
            ('Alt', 6), ('Win', 6), ('Menu', 6), ('Ctrl', 6),
            ('←', 5), ('↓', 5), ('→', 5), ('0', 5), ('.', 5)
        ]
        self.create_row(5, row5)

    def create_row(self, row_num, key_definitions):
        """Create a single row of keys"""
        row_frame = ttk.Frame(self.keyboard_frame)
        row_frame.grid(row=row_num, column=0, sticky="ew", pady=2)
        
        # Configure row for resizing
        row_frame.grid_rowconfigure(0, weight=1)
        
        for col, (key_text, width) in enumerate(key_definitions):
            if key_text:  # Only create button if there's text
                self.create_key(row_frame, key_text, 0, col, width)
            row_frame.grid_columnconfigure(col, weight=1)

    def create_key(self, parent, text, row, col, width):
        """Create an individual key button"""
        # Special handling for spacebar
        if text == ' ':
            key = ttk.Button(
                parent,
                text='Space',  # Label for spacebar
                width=width,
                style="Key.TButton",
                command=lambda t=text: self.press_key(t)
            )
        else:
            key = ttk.Button(
                parent,
                text=text,
                width=width,
                style="Key.TButton",
                command=lambda t=text: self.press_key(t)
            )
        
        key.grid(row=row, column=col, padx=1, pady=1, sticky="nsew")
        
        # Bind mouse events for visual feedback
        key.bind("<ButtonPress-1>", lambda e, t=text, k=key: self.on_press(t, k))
        key.bind("<ButtonRelease-1>", lambda e, t=text, k=key: self.on_release(t, k))
        
        # Bind keyboard events to match physical keyboard
        self.bind_physical_keyboard(text, key)
        
        return key

    def bind_physical_keyboard(self, virtual_key, button):
        """Bind physical keyboard presses to virtual keys"""
        # Simple mapping for common keys
        key_mappings = {
            '~': 'grave', '1': '1', '2': '2', '3': '3', '4': '4', '5': '5',
            '6': '6', '7': '7', '8': '8', '9': '9', '0': '0', '-': 'minus',
            '=': 'equal', 'Backspace': 'BackSpace', 'Tab': 'Tab', 'Q': 'q',
            'W': 'w', 'E': 'e', 'R': 'r', 'T': 't', 'Y': 'y', 'U': 'u',
            'I': 'i', 'O': 'o', 'P': 'p', '[': 'bracketleft', ']': 'bracketright',
            '\\': 'backslash', 'Caps': 'Caps_Lock', 'A': 'a', 'S': 's', 'D': 'd',
            'F': 'f', 'G': 'g', 'H': 'h', 'J': 'j', 'K': 'k', 'L': 'l', ';': 'semicolon',
            "'": 'apostrophe', 'Enter': 'Return', 'Shift': 'Shift_L', 'Z': 'z',
            'X': 'x', 'C': 'c', 'V': 'v', 'B': 'b', 'N': 'n', 'M': 'm', ',': 'comma',
            '.': 'period', '/': 'slash', 'Ctrl': 'Control_L', 'Win': 'Super_L',
            'Alt': 'Alt_L', ' ': 'space', 'Menu': 'Menu', '←': 'Left', '↓': 'Down',
            '→': 'Right', '↑': 'Up', 'Ins': 'Insert', 'Home': 'Home', 'PgUp': 'Prior',
            'Del': 'Delete', 'End': 'End', 'PgDn': 'Next', 'NumLk': 'Num_Lock',
            'Esc': 'Escape'
        }
        
        if virtual_key in key_mappings:
            physical_key = key_mappings[virtual_key]
            self.root.bind(f'<KeyPress-{physical_key}>', lambda e, t=virtual_key, k=button: self.on_press(t, k))
            self.root.bind(f'<KeyRelease-{physical_key}>', lambda e, t=virtual_key, k=button: self.on_release(t, k))

    def press_key(self, key):
        """Handle key press action"""
        print(f"Key pressed: {key}")
        # Add actual key sending functionality here if needed
        # For example, using pynput: from pynput.keyboard import Controller; Controller().press(key)

    def on_press(self, key, button):
        """Visual feedback when key is pressed"""
        button.config(style="Pressed.TButton")
        self.pressed_keys.add(key)

    def on_release(self, key, button):
        """Visual feedback when key is released"""
        button.config(style="Key.TButton")
        if key in self.pressed_keys:
            self.pressed_keys.remove(key)

if __name__ == "__main__":
    root = tk.Tk()
    
    # Linux-specific DPI handling
    if sys.platform.startswith('linux'):
        root.tk.call('tk', 'scaling', 1.0)  # Force proper scaling
    
    app = LinuxVirtualKeyboard(root)
    root.mainloop()
