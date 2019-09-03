using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleGameEngine
{
    public abstract class olcConsoleGameEngine
    {
        protected int m_nScreenWidth = 80;
        protected int m_nScreenHeight = 30;
        protected CHAR_INFO[] m_bufScreen;
        protected bool m_bAtomActive;
        protected sKeyState[] m_keys;
        protected sKeyState[] m_mouse;
        protected double m_mousePosX;
        protected double m_mousePosY;

        public short[] m_keyNewState, m_keyOldState;
        public bool m_bEnableSound = false;
        public IntPtr m_hConsole, m_hConsoleIn;
        public string m_sAppName = "Default";

        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_INPUT_HANDLE = -10;
        private SMALL_RECT m_rectWindow;
        private bool[] m_mouseOldState = new bool[5];
        private bool[] m_mouseNewState = new bool[5];


        public olcConsoleGameEngine()
        {

            m_keyNewState = new short[256 * sizeof(short)];
            m_keyOldState = new short[256 * sizeof(short)];
            m_keys = new sKeyState[256 * sizeof(short)];
            m_mouse = new sKeyState[256 * sizeof(short)];

            m_hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
            m_hConsoleIn = GetStdHandle(STD_INPUT_HANDLE);
        }

        public unsafe int ConstructConsole(int width, int height, short fontw = 12, short fonth = 12)
        {

            m_nScreenHeight = height;
            m_nScreenWidth = width;

            m_rectWindow.Left = 0;
            m_rectWindow.Top = 0;
            m_rectWindow.Right = 1;
            m_rectWindow.Bottom = 1;

            if (!SetConsoleWindowInfo(m_hConsole, true, ref m_rectWindow))
                throw new Exception("SetConsoleWindowInfo");

            COORD buffer;
            buffer.X = (short)m_nScreenWidth;
            buffer.Y = (short)m_nScreenHeight;

            if (!SetConsoleScreenBufferSize(m_hConsole, buffer))
                throw new Exception("SetConsoleScreenBufferSize");

            // Assign screen buffer to the console
            if (!SetConsoleActiveScreenBuffer(m_hConsole))
                throw new Exception("SetConsoleActiveScreenBuffer");


            CONSOLE_FONT_INFOEX cfi;
            cfi.cbSize = (uint)Marshal.SizeOf<CONSOLE_FONT_INFOEX>();
            cfi.nFont = 0;
            cfi.dwFontSize.X = fontw;
            cfi.dwFontSize.Y = fonth;
            cfi.FontFamily = 0;
            cfi.FontWeight = 400;
            cfi.FaceName = "Consolas";
            if (!SetCurrentConsoleFontEx(m_hConsole, false, cfi))
                throw new Exception("SetCurrentConsoleFontEx");


            COORD coordLargest = GetLargestConsoleWindowSize(m_hConsole);

            if (height > coordLargest.Y)
                throw new Exception("Screen Height / Font Height Too Big | SCR: " + height + " | LRG: " + coordLargest.Y);
            if (width > coordLargest.X)
                throw new Exception("Screen Width / Font Width Too Big | SCR: " + width + " | LRG: " + coordLargest.X);

            m_rectWindow.Left = 0;
            m_rectWindow.Top = 0;
            m_rectWindow.Right = (short)(m_nScreenWidth - 1);
            m_rectWindow.Bottom = (short)(m_nScreenHeight - 1);

            if (!SetConsoleWindowInfo(m_hConsole, true, ref m_rectWindow))
                throw new Exception("SetConsoleWindowInfo");

            uint args = (uint)(ConsoleModes.ENABLE_EXTENDED_FLAGS | ConsoleModes.ENABLE_WINDOW_INPUT | ConsoleModes.ENABLE_MOUSE_INPUT);
            if (!SetConsoleMode(m_hConsoleIn, args))
                throw new Exception("SetConsoleMode");

            m_bufScreen = new CHAR_INFO[m_nScreenWidth * m_nScreenHeight];

            return 1;
        }

        // User MUST OVERRIDE THESE!!
        public abstract bool OnUserCreate();
        public abstract bool OnUserUpdate(float fElapsedTime);

        #region Drawing / Visuals

        public int ScreenWidth()
        {
            return m_nScreenWidth;
        }

        public int ScreenHeight()
        {
            return m_nScreenHeight;
        }

        public virtual void Draw(int x, int y, PIXEL_TYPE pixel = PIXEL_TYPE.PIXEL_SOLID, COLOR col = COLOR.FG_WHITE)
        {
            if (x >= 0 && x < m_nScreenWidth && y >= 0 && y < m_nScreenHeight)
            {
                m_bufScreen[y * m_nScreenWidth + x].UnicodeChar = (char)pixel;
                m_bufScreen[y * m_nScreenWidth + x].Attributes = (ushort)col;
            }
        }

        public virtual void Draw(int x, int y, char pixel, COLOR col = COLOR.FG_WHITE)
        {
            if (x >= 0 && x < m_nScreenWidth && y >= 0 && y < m_nScreenHeight)
            {
                m_bufScreen[y * m_nScreenWidth + x].UnicodeChar = pixel;
                m_bufScreen[y * m_nScreenWidth + x].Attributes = (ushort)col;
            }
        }

        public void Draw(float x, float y, char pixel, COLOR col = COLOR.FG_WHITE) { Draw((int)x, (int)y, pixel, col); }
        public void Draw(double x, double y, char pixel, COLOR col = COLOR.FG_WHITE) { Draw((int)x, (int)y, pixel, col); }
        public void Fill(int x1, int y1, int x2, int y2, PIXEL_TYPE pixel = PIXEL_TYPE.PIXEL_SOLID, COLOR col = COLOR.FG_WHITE)
        {
            Clip(x1, y1);
            Clip(x2, y2);
            for (int x = x1; x < x2; x++)
                for (int y = y1; y < y2; y++)
                    Draw(x, y, pixel, col);
        }

        public void DrawString(int x, int y, string c, COLOR col = COLOR.FG_WHITE)
        {
            for (int i = 0; i < c.Length; i++)
            {
                m_bufScreen[y * m_nScreenWidth + x + i].UnicodeChar = c[i];
                m_bufScreen[y * m_nScreenWidth + x + i].Attributes = (ushort)col;
            }
        }

        public void DrawStringAlpha(int x, int y, string c, COLOR col = COLOR.FG_WHITE)
        {
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] != ' ')
                {
                    m_bufScreen[y * m_nScreenWidth + x + i].UnicodeChar = c[i];
                    m_bufScreen[y * m_nScreenWidth + x + i].Attributes = (ushort)col;
                }
            }
        }

        public void Clip(int x, int y)
        {
            if (x < 0) x = 0;
            if (x >= m_nScreenWidth) x = m_nScreenWidth;
            if (y < 0) y = 0;
            if (y >= m_nScreenHeight) y = m_nScreenHeight;
        }

        public void DrawLine(int x1, int y1, int x2, int y2, PIXEL_TYPE pixel = PIXEL_TYPE.PIXEL_SOLID, COLOR col = COLOR.FG_WHITE)
        {
            int x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
            dx = x2 - x1;
            dy = y2 - y1;
            dx1 = Math.Abs(dx);
            dy1 = Math.Abs(dy);
            px = 2 * dy1 - dx1;
            py = 2 * dx1 - dy1;
            if (dy1 <= dx1)
            {
                if (dx >= 0)
                {
                    x = x1;
                    y = y1;
                    xe = x2;
                }
                else
                {
                    x = x2;
                    y = y2;
                    xe = x1;
                }
                Draw(x, y, pixel, col);
                for (i = 0; x < xe; i++)
                {
                    x = x + 1;
                    if (px < 0)
                        px = px + 2 * dy1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0))
                            y = y + 1;
                        else
                            y = y - 1;
                        px = px + 2 * (dy1 - dx1);
                    }
                    Draw(x, y, pixel, col);
                }
            }
            else
            {
                if (dy >= 0)
                {
                    x = x1;
                    y = y1;
                    ye = y2;
                }
                else
                {
                    x = x2;
                    y = y2;
                    ye = y1;
                }
                Draw(x, y, pixel, col);
                for (i = 0; y < ye; i++)
                {
                    y = y + 1;
                    if (py <= 0)
                        py = py + 2 * dx1;
                    else
                    {
                        if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0))
                            x = x + 1;
                        else
                            x = x - 1;
                        py = py + 2 * (dx1 - dy1);
                    }
                    Draw(x, y, pixel, col);
                }
            }
        }

        public void DrawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, PIXEL_TYPE pixel = PIXEL_TYPE.PIXEL_SOLID, COLOR col = COLOR.FG_WHITE)
        {
            DrawLine(x1, y1, x2, y2, pixel, col);
            DrawLine(x2, y2, x3, y3, pixel, col);
            DrawLine(x3, y3, x1, y1, pixel, col);
        }

        public void DrawCircle(int xc, int yc, int r, PIXEL_TYPE pixel = PIXEL_TYPE.PIXEL_SOLID, COLOR col = COLOR.FG_WHITE)
        {
            int x = 0;
            int y = r;
            int p = 3 - 2 * r;
            if (r == 0) return;

            while (y >= x) // only formulate 1/8 of circle
            {
                Draw(xc - x, yc - y, pixel, col);//upper left left
                Draw(xc - y, yc - x, pixel, col);//upper upper left
                Draw(xc + y, yc - x, pixel, col);//upper upper right
                Draw(xc + x, yc - y, pixel, col);//upper right right
                Draw(xc - x, yc + y, pixel, col);//lower left left
                Draw(xc - y, yc + x, pixel, col);//lower lower left
                Draw(xc + y, yc + x, pixel, col);//lower lower right
                Draw(xc + x, yc + y, pixel, col);//lower right right
                if (p < 0) p += 4 * x++ + 6;
                else p += 4 * (x++ - y--) + 10;
            }
        }

        public void DrawSprite(int x, int y, olcSprite sprite)
        {
            if (sprite == null) return;

            for (int i = 0; i < sprite.nWidth; i++)
            {
                for (int j = 0; j < sprite.nHeight; j++)
                {
                    if (sprite.GetGlyph(i, j) != ' ')
                        Draw(x + i, y + j, sprite.GetGlyph(i, j), sprite.getColor(i, j));
                }
            }
        }

        public void DrawPartialSprite(int x, int y, olcSprite sprite, int ox, int oy, int w, int h)
        {
            if (sprite == null)
                return;

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    if (sprite.GetGlyph(i + ox, j + oy) != ' ')
                        Draw(x + i, y + j, sprite.GetGlyph(i + ox, j + oy), sprite.getColor(i + ox, j + oy));
                }
            }
        }

        #endregion

        #region Thread Management...

        public void Start()
        {
            m_bAtomActive = true;
            Thread thread = new Thread(GameThread);
            thread.Start();
            thread.Join();
        }

        private void GameThread()
        {
            if (!OnUserCreate())
                return;

            var t1 = DateTimeOffset.Now;
            var t2 = DateTimeOffset.Now;
            while (m_bAtomActive)
            {
                t2 = DateTimeOffset.Now;
                var elapsedtime = t2 - t1;
                t1 = t2;
                float elapsed = elapsedtime.Milliseconds * 2;

                #region Input Handling

                #region Keyboard
                for (int i = 0; i < 256; i++)
                {
                    m_keyNewState[i] = GetAsyncKeyState(i);
                    m_keys[i].bPressed = false;
                    m_keys[i].bReleased = false;

                    if (m_keyNewState[i] != m_keyOldState[i])
                    {
                        if (Convert.ToBoolean(m_keyNewState[i] & 0x8000))
                        {
                            m_keys[i].bPressed = !m_keys[i].bHeld;
                            m_keys[i].bHeld = true;
                        }
                        else
                        {
                            m_keys[i].bReleased = true;
                            m_keys[i].bHeld = false;
                        }
                    }
                    m_keyOldState[i] = m_keyNewState[i];
                }
                #endregion

                #region Mouse
                INPUT_RECORD[] inBuf = new INPUT_RECORD[32];
                uint events = 0;
                GetNumberOfConsoleInputEvents(m_hConsoleIn, out events);
                if (events > 0)
                    ReadConsoleInput(m_hConsoleIn, inBuf, events, out events);

                for (uint i = 0; i < events; i++)
                {
                    if (inBuf[i].EventType == 0x0002 || (inBuf[i].MouseEvent.dwEventFlags == 0 || inBuf[i].MouseEvent.dwEventFlags == 0x0001))
                    {
                        if (inBuf[i].MouseEvent.dwEventFlags == 0)
                        {
                            for (int m = 0; m < 5; m++)
                                m_mouseNewState[m] = (inBuf[i].MouseEvent.dwButtonState & (1 << m)) > 0;
                        }
                        else
                        {
                            m_mousePosX = inBuf[i].MouseEvent.dwMousePosition.X;
                            m_mousePosY = inBuf[i].MouseEvent.dwMousePosition.Y;
                        }
                    }
                }

                for (int m = 0; m < 5; m++)
                {
                    m_mouse[m].bPressed = false;
                    m_mouse[m].bReleased = false;

                    if (m_mouseNewState[m] != m_mouseOldState[m])
                    {
                        if (m_mouseNewState[m])
                        {
                            m_mouse[m].bPressed = true;
                            m_mouse[m].bHeld = true;
                        }
                        else
                        {
                            m_mouse[m].bReleased = true;
                            m_mouse[m].bHeld = false;
                        }
                    }
                    m_mouseOldState[m] = m_mouseNewState[m];
                }

                #endregion

                #endregion
                if (!OnUserUpdate(elapsed))
                    m_bAtomActive = false;
                SetConsoleTitle(string.Format("OneLoneCoder.com - Console Game Engine - {0} s - FPS: {1}", m_sAppName, elapsed));
                COORD coord;
                coord.X = (short)m_nScreenWidth;
                coord.Y = (short)m_nScreenHeight;

                COORD c;
                c.X = 0;
                c.Y = 0;
                Console.OutputEncoding = Encoding.Unicode;
                Console.CursorVisible = false;
                WriteConsoleOutput(m_hConsole, m_bufScreen, coord, c, ref m_rectWindow);
            }
        }

        #endregion

        #region PImports

        #region Kernel32

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleActiveScreenBuffer(IntPtr hConsoleOutput);

        [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX ConsoleScreenBufferInfo);
        [DllImport("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)] static extern bool ReadConsoleInput(IntPtr hConsoleInput, [Out] INPUT_RECORD[] lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

        [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetNumberOfConsoleInputEvents(IntPtr hConsoleInput, out uint lpcNumberOfEvents);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", EntryPoint = "WriteConsoleOutputW", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO[] lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetConsoleTitle(string lpConsoleTitle);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetCurrentConsoleFontEx(IntPtr consoleOutput, bool maximumWindow, CONSOLE_FONT_INFOEX consoleCurrentFontEx);
        [DllImport("kernel32.dll", SetLastError = true)] static extern COORD GetLargestConsoleWindowSize(IntPtr hConsoleOutput);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD dwSize);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput, bool bAbsolute, [In] ref SMALL_RECT lpConsoleWindow);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetConsoleCP(uint wCodePageID);
        #endregion
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);

        #region User-Defined Types

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            public uint cbSize;
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;

            public ushort wPopupAttributes;
            public bool bFullscreenSupported;

            private COLORREF black;
            private COLORREF darkBlue;
            private COLORREF darkGreen;
            private COLORREF darkCyan;
            private COLORREF darkRed;
            private COLORREF darkMagenta;
            private COLORREF darkYellow;
            private COLORREF gray;
            private COLORREF darkGray;
            private COLORREF blue;
            private COLORREF green;
            private COLORREF cyan;
            private COLORREF red;
            private COLORREF magenta;
            private COLORREF yellow;
            private COLORREF white;

            // has been a while since I did this, test before use
            // but should be something like:
            //
            // [MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
            // public COLORREF[] ColorTable;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct COLORREF
        {
            public byte R;
            public byte G;
            public byte B;
        }

        #region Records

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_RECORD
        {
            [FieldOffset(0)]
            public ushort EventType;
            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;
            [FieldOffset(4)]
            public MOUSE_EVENT_RECORD MouseEvent;
            [FieldOffset(4)]
            public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
            [FieldOffset(4)]
            public MENU_EVENT_RECORD MenuEvent;
            [FieldOffset(4)]
            public FOCUS_EVENT_RECORD FocusEvent;
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct KEY_EVENT_RECORD
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.Bool)]
            public bool bKeyDown;
            [FieldOffset(4), MarshalAs(UnmanagedType.U2)]
            public ushort wRepeatCount;
            [FieldOffset(6), MarshalAs(UnmanagedType.U2)]
            //public VirtualKeys wVirtualKeyCode;
            public ushort wVirtualKeyCode;
            [FieldOffset(8), MarshalAs(UnmanagedType.U2)]
            public ushort wVirtualScanCode;
            [FieldOffset(10)]
            public char UnicodeChar;
            [FieldOffset(12), MarshalAs(UnmanagedType.U4)]
            //public ControlKeyState dwControlKeyState;
            public uint dwControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSE_EVENT_RECORD
        {
            public COORD dwMousePosition;
            public uint dwButtonState;
            public uint dwControlKeyState;
            public uint dwEventFlags;
        }

        public struct WINDOW_BUFFER_SIZE_RECORD
        {
            public COORD dwSize;

            public WINDOW_BUFFER_SIZE_RECORD(short x, short y)
            {
                dwSize = new COORD();
                dwSize.X = x;
                dwSize.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MENU_EVENT_RECORD
        {
            public uint dwCommandId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FOCUS_EVENT_RECORD
        {
            public uint bSetFocus;
        }

        #endregion

        [Flags]
        private enum ConsoleModes : uint
        {
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_AUTO_POSITION = 0x0100,

            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
            DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
            ENABLE_LVB_GRID_WORLDWIDE = 0x0010
        }

        public struct SMALL_RECT
        {

            public short Left;
            public short Top;
            public short Right;
            public short Bottom;

        }

        //CHAR_INFO struct, which was a union in the old days
        // so we want to use LayoutKind.Explicit to mimic it as closely
        // as we can
        [StructLayout(LayoutKind.Explicit)]
        public struct CHAR_INFO
        {
            [FieldOffset(0)]
            public char UnicodeChar;
            [FieldOffset(0)]
            public char AsciiChar;
            [FieldOffset(2)] //2 bytes seems to work properly
            public UInt16 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            public short X;
            public short Y;

            public COORD(short x, short y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CONSOLE_FONT_INFOEX
        {
            public uint cbSize;
            public uint nFont;
            public COORD dwFontSize;
            public int FontFamily;
            public int FontWeight;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FaceName;
        }
        #endregion

        #endregion

        public struct sKeyState
        {
            public bool bPressed;
            public bool bReleased;
            public bool bHeld;
        }
    }
}
