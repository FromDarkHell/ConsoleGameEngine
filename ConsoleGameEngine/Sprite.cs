using System;
using System.Collections.Generic;
using System.IO;


namespace ConsoleGameEngine
{
    #region Enums

    public enum COLOR
    {
        FG_BLACK = 0x00,
        FG_DARK_BLUE = 0x01,
        FG_DARK_GREEN = 0x02,
        FG_DARK_CYAN = 0x03,
        FG_DARK_RED = 0x04,
        FG_DARK_MAGENTA = 0x05,
        FG_DARK_YELLOW = 0x06,
        FG_GREY = 0x07,
        FG_DARK_GREY = 0x08,
        FG_BLUE = 0x09,
        FG_GREEN = 0x0A,
        FG_CYAN = 0x0B,
        FG_RED = 0x0C,
        FG_MAGENTA = 0x0D,
        FG_YELLOW = 0x0E,
        FG_WHITE = 0x0F
    };

    public enum BGCOLOR
    {
        BG_BLACK = 0x00,
        BG_DARK_BLUE = 0x10,
        BG_DARK_GREEN = 0x20,
        BG_DARK_CYAN = 0x30,
        BG_DARK_RED = 0x40,
        BG_DARK_MAGENTA = 0x50,
        BG_DARK_YELLOW = 0x60,
        BG_GREY = 0x70,
        BG_DARK_GREY = 0x80,
        BG_BLUE = 0x90,
        BG_GREEN = 0xA0,
        BG_CYAN = 0xB0,
        BG_RED = 0xC0,
        BG_MAGENTA = 0xD0,
        BG_YELLOW = 0xE0,
        BG_WHITE = 0xF0,
    }

    public enum PIXEL_TYPE
    {
        PIXEL_SOLID = '█',
        PIXEL_THREEQUARTERS = '▓',
        PIXEL_HALF = '▒',
        PIXEL_QUARTER = '░',
        PIXEL_NONE = ' '
    };


    #endregion

    public class olcSprite
    {
        public int nWidth = 0;
        public int nHeight = 0;

        #region Initializers
        public olcSprite()
        {

        }

        public olcSprite(int w, int h)
        {
            Create(w, h);
        }

        public olcSprite(string sFile)
        {
            if (!Load(sFile))
            {
                Create(8, 8);
            }
        }
        #endregion

        #region Creation

        private char[] m_Glyphs;
        private COLOR[] m_Colours;

        private void Create(int w, int h)
        {
            nWidth = w;
            nHeight = h;
            m_Glyphs = new char[w * h];
            m_Colours = new COLOR[w * h];

            for (int i = 0; i < (w * h); i++)
            {
                m_Glyphs[i] = ' ';
                m_Colours[i] = COLOR.FG_BLACK;
            }
        }
        #endregion

        #region Glyph / Color Management

        public void SetGlyph(int x, int y, char c)
        {
            if (x < 0 || x >= nWidth || y < 0 || y >= nHeight)
                return;
            else
                m_Glyphs[y * nWidth + x] = c;
        }

        public void SetColor(int x, int y, COLOR c)
        {
            if (x < 0 || x >= nWidth || y < 0 || y >= nHeight)
                return;
            else
                m_Colours[y * nWidth + x] = c;
        }

        public char GetGlyph(int x, int y)
        {
            if (x < 0 || x >= nWidth || y < 0 || y >= nHeight)
                return ' ';
            else
                return m_Glyphs[y * nWidth + x];
        }

        public COLOR getColor(int x, int y)
        {
            if (x < 0 || x >= nWidth || y < 0 || y >= nHeight)
                return COLOR.FG_BLACK;
            else
                return m_Colours[y * nWidth + x];
        }

        public char SampleGlyph(float x, float y)
        {
            int sx = (int)(x * nWidth);
            int sy = (int)(y * nHeight - 1.0f);
            if (sx < 0 || sx >= nWidth || sy < 0 || sy >= nHeight)
                return ' ';
            else
                return m_Glyphs[sy * nWidth + sx];
        }

        public COLOR SampleColor(float x, float y)
        {
            int sx = (int)(x * nWidth);
            int sy = (int)(y * nHeight - 1.0f);
            if (sx < 0 || sx >= nWidth || sy < 0 || sy >= nHeight)
                return COLOR.FG_BLACK;
            else
                return m_Colours[sy * nWidth + sx];
        }

        #endregion

        #region Save / Load

        public bool Save(string sFile)
        {
            List<string> spriteContent = new List<string>();
            spriteContent.Add(nWidth.ToString() + "\n");
            spriteContent.Add(nHeight.ToString() + "\n");
            spriteContent.Add("--COLORS--" + "\n");
            foreach (COLOR color in m_Colours)
                spriteContent.Add(((short)color).ToString() + "\n");
            spriteContent.Add("--GLYPHS--" + "\n");
            foreach (char glyph in m_Glyphs)
                spriteContent.Add(glyph.ToString() + "\n");
            File.WriteAllLines(sFile, spriteContent);

            return true;
        }

        public bool Load(string sFile)
        {
            m_Glyphs = null;
            m_Colours = null;
            nWidth = 0;
            nHeight = 0;

            string[] fileData = File.ReadAllLines(sFile);
            if (fileData == null || fileData.Length == 0)
                return false;

            nWidth = int.Parse(fileData[0]);
            nHeight = int.Parse(fileData[1]);
            Create(nWidth, nHeight);
            List<COLOR> colors = new List<COLOR>();
            List<char> glyphs = new List<char>();

            string lastSegment = "";
            foreach (string fileLine in fileData)
            {
                if (lastSegment == "--COLORS--")
                {
                    colors.Add((COLOR)Enum.Parse(typeof(COLOR), fileLine));
                }
                else if (lastSegment == "--GLYPHS--")
                {
                    glyphs.Add(char.Parse(fileLine));
                }
                if (fileLine.Contains("--"))
                    lastSegment = fileLine;
            }

            m_Colours = colors.ToArray();
            m_Glyphs = glyphs.ToArray();
            return true;
        }

        #endregion
    }
}
