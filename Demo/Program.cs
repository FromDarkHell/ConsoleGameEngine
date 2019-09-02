using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ConsoleGameEngine;
namespace Demo
{
    class Program
    {
        public class Demo : olcConsoleGameEngine
        {


            public override bool OnUserCreate()
            {
                m_sAppName = "FPS Test";
                return true;
            }

            public override bool OnUserUpdate(float fElapsedTime)
            {
                Fill(0, 0, ScreenWidth(), ScreenHeight(), PIXEL_TYPE.PIXEL_SOLID, COLOR.FG_BLACK);

                if (m_mouse[0].bHeld)
                {
                    Fill(20, 20, 50, 50);
                }

                return true;
            }

            private bool isKeyPushedDown(Keys key)
            {
                return m_keys[(int)key].bHeld;
            }

            static T RandomEnumValue<T>()
            {
                var v = Enum.GetValues(typeof(T));
                return (T)v.GetValue(new Random().Next(v.Length));
            }
        }

        public class FPSDemo : olcConsoleGameEngine
        {
            private static readonly int mapW = 32;
            private static readonly int mapH = 32;
            private static readonly int depth = 64;
            private static readonly double fov = Math.PI / 4.0;

            // The default X value of the player  
            static double playerX = 8.0d;
            // The default Y value of the player
            static double playerY = 8.0d;

            static double playerA = 0.0d;

            static double fSpeed = 1.25f;
            static double sprintModifier = 1.00f;

            string[] mapString;
            char[] map;

            private static bool showMinimap = true;
            private static bool bIsPlayingGame = true;

            private static double lastMouseX, lastMouseY = 0;

            public override bool OnUserCreate()
            {
                mapString = new string[]
                {
                    "#########.......#########.......",
                    "#...............#...............",
                    "#.......#########.......########",
                    "#..............##..............#",
                    "#......##......##......##......#",
                    "#......##..............##......#",
                    "#..............##..............#",
                    "###............####............#",
                    "##.............###.............#",
                    "#............####............###",
                    "#..............................#",
                    "#..............##..............#",
                    "#..............##..............#",
                    "#...........#####...........####",
                    "#..............................#",
                    "###..####....########....#######",
                    "####.####.......######..........",
                    "#...............#...............",
                    "#.......#########.......##..####",
                    "#..............##..............#",
                    "#......##......##.......#......#",
                    "#......##......##......##......#",
                    "#..............##..............#",
                    "###............####............#",
                    "##.............###.............#",
                    "#............####............###",
                    "#..............................#",
                    "#..............................#",
                    "#..............##..............#",
                    "#...........##..............####",
                    "#..............##..............#",
                    "################################"
                };

                map = new char[mapH * mapW];
                int index = 0;
                foreach (string m in mapString)
                {
                    char[] chars = m.ToCharArray();
                    foreach (char c in chars)
                    {
                        map[index] = c;
                        index++;
                    }
                }

                return true;
            }

            public override bool OnUserUpdate(float elapsed)
            {
                Fill(0, 0, ScreenWidth(), ScreenHeight(), PIXEL_TYPE.PIXEL_SOLID, COLOR.FG_BLACK);

                #region Input Detection

                // The X Coord before any movement
                double frmX = playerX;
                // The Y Coord before any movement
                double frmY = playerY;

                #region Rotation
                // Rotate Cam Left
                if (isKeyPushedDown(Keys.Left))
                    playerA -= (0.5f) / (elapsed);
                // Rotate Cam Right
                if (isKeyPushedDown(Keys.Right))
                    playerA += (0.5f) / (elapsed);

                if (m_mousePosX - lastMouseX < 0) playerA -= (0.75 / elapsed) * (2 * Math.Abs(m_mousePosX - lastMouseX));
                else if (m_mousePosX - lastMouseX > 0) playerA += (0.75 / elapsed) * (m_mousePosX - lastMouseX);

                #endregion

                #region Proper Movement
                // Up
                if (isKeyPushedDown(Keys.W))
                {
                    playerX += (Math.Sin(playerA) * fSpeed) / elapsed;
                    playerY += (Math.Cos(playerA) * fSpeed) / elapsed;
                }
                // Down
                if (isKeyPushedDown(Keys.S))
                {
                    playerX -= (Math.Sin(playerA) * fSpeed) / elapsed;
                    playerY -= (Math.Cos(playerA) * fSpeed) / elapsed;

                }

                // Strafe Left
                if (isKeyPushedDown(Keys.A))
                {
                    playerX -= (Math.Cos(playerA) * fSpeed) / elapsed;
                    playerY += (Math.Sin(playerA) * fSpeed) / elapsed;
                }
                // Strafe Right
                if (isKeyPushedDown(Keys.D))
                {
                    playerX += (Math.Cos(playerA) * fSpeed) / elapsed;
                    playerY -= (Math.Sin(playerA) * fSpeed) / elapsed;
                }

                if (isKeyPushedDown(Keys.H))
                    showMinimap = !showMinimap;

                if (isKeyPushedDown(Keys.Escape))
                    bIsPlayingGame = false;

                if (m_keys[(int)Keys.LShiftKey].bReleased)
                    fSpeed -= sprintModifier;
                else if (m_keys[(int)Keys.LShiftKey].bPressed)
                    fSpeed += sprintModifier;

                #region Collision Detection
                if (map[(int)playerY * mapW + (int)playerX] == '#')
                {
                    playerX = frmX;
                    playerY = frmY;
                }
                #endregion

                #endregion


                #endregion

                #region Rendering / Calculations

                for (int x = 0; x < ScreenWidth(); x++)
                {
                    #region General Rendering Math Formulae

                    double rayAngle = (playerA - fov / 2.0d) + ((double)x / ScreenWidth()) * fov;

                    // Unit vectors for our ray
                    double eyeX = Math.Sin(rayAngle);
                    double eyeY = Math.Cos(rayAngle);

                    // The distance to a wall, obviously...
                    double distanceToWall = 0;

                    // If we've hit a wall... Pretty obvious huh.
                    bool hitWall = false;
                    // A variable to help do some visibility stuff, if its the edge of a cell
                    bool edgeOfCell = false;

                    // Saw an enemy
                    bool hitEnemy = false;

                    while ((!hitWall && !hitEnemy) && distanceToWall < depth)
                    {
                        // Increment distanceToWall to see if we'll hit a wall
                        distanceToWall += 0.05d;

                        // The testing X + Y value if we're in a wall
                        int testX = (int)(playerX + eyeX * distanceToWall);
                        int testY = (int)(playerY + eyeY * distanceToWall);

                        // Test if our ray is OOB
                        if (testX < 0 | testX >= mapW || testY < 0 || testY >= mapH)
                        {
                            // Our wall is OOB, return back.
                            hitWall = true;
                            distanceToWall = depth;
                        }
                        else
                        {
                            // Its a wall as well
                            if (map[testY * mapW + testX] == '#')
                            {
                                // Make sure we hit a wall
                                hitWall = true;

                                #region Distance / Dot Formula for extra shading
                                List<Tuple<double, double>> p = new List<Tuple<double, double>>(); // Distance, Dot formula

                                for (int tx = 0; tx < 2; tx++)
                                {
                                    for (int ty = 0; ty < 2; ty++)
                                    {
                                        double vy = (float)testY + ty - playerY;
                                        double vx = (float)testX + tx - playerX;

                                        double d = Math.Sqrt(vx * vx + vy * vy);
                                        double dot = (eyeX * vx / d) + (eyeY * vy / d);
                                        p.Add(new Tuple<double, double>(d, dot));
                                    }
                                }
                                p.Sort((a, b) => b.Item1.CompareTo(a.Item1));

                                double bound = 0.01d;

                                if (Math.Acos(p.ElementAt(0).Item2) < bound) edgeOfCell = true;
                                if (Math.Acos(p.ElementAt(1).Item2) < bound) edgeOfCell = true;
                                #endregion

                            }

                            if (map[testY * mapW + testX] == 'e')
                            {
                                hitEnemy = true;
                                edgeOfCell = false;
                            }
                        }
                    }

                    // Calculate the distance to the ceiling, currently unused for any rendering.
                    int ceiling = (int)((float)(ScreenHeight() / 2.0) - ScreenHeight() / (distanceToWall));
                    // The floor... duh.
                    int floor = ScreenHeight() - ceiling;

                    char shade = ' ';
                    if (distanceToWall <= depth / 4.0d) shade = (char)PIXEL_TYPE.PIXEL_SOLID;
                    else if (distanceToWall <= depth / 3.0d) shade = (char)PIXEL_TYPE.PIXEL_THREEQUARTERS;
                    else if (distanceToWall <= depth / 2.0d) shade = (char)PIXEL_TYPE.PIXEL_HALF;
                    else if (distanceToWall <= depth) shade = (char)PIXEL_TYPE.PIXEL_QUARTER;

                    if (edgeOfCell) shade = '#';
                    if (hitEnemy) shade = '!';

                    for (int y = 0; y < ScreenHeight(); y++)
                    {
                        // A ceiling
                        if (y < ceiling) Draw(x, y, PIXEL_TYPE.PIXEL_QUARTER, COLOR.FG_BLACK);
                        // A wall
                        else if (y >= ceiling && y <= floor)
                            if (edgeOfCell)
                                Draw(x, y, '#', COLOR.FG_DARK_GREY);
                            else
                                Draw(x, y, shade, (y != ceiling) ? COLOR.FG_DARK_RED : COLOR.FG_GREY);
                        // The floor
                        else
                        {

                            float b = 1.0f - ((y - ScreenHeight() / 2.0f) / (ScreenHeight() / 2.0f));
                            if (b < 0.25) shade = '#';
                            else if (b < 0.5) shade = 'x';
                            else if (b < 0.75) shade = '-';
                            else if (b < 0.9) shade = '.';
                            Draw(x, y, shade, COLOR.FG_DARK_GREEN);
                        }

                    }

                    string stats = String.Format("X={0:0.00}, Y={1:0.00}, A={2:0.00}, SP: {3:0.00}, mX: {4:0.00}, oX: {5:0.00}, dX: {6:0.00}",
                    playerX, playerY, playerA, fSpeed, m_mousePosX, lastMouseX, m_mousePosX - lastMouseX);
                    DrawString(0, 0, stats);

                    if (showMinimap)
                    {
                        int mapX = 0;
                        int mapY = 2;
                        foreach (string mapLine in mapString)
                        {
                            DrawString(mapX, mapY, mapLine);
                            mapY += 1;
                        }
                        char rotatedChar = 'p';
                        m_bufScreen[((int)playerY + 2) * ScreenWidth() + (int)playerX].UnicodeChar = rotatedChar;
                    }
                    #endregion
                }

                #endregion

                if (m_mousePosX != lastMouseX) lastMouseX = m_mousePosX;
                if (m_mousePosY != lastMouseY) lastMouseY = m_mousePosY;

                return bIsPlayingGame;
            }

            public bool isKeyPushedDown(Keys key)
            {
                return m_keys[(int)key].bHeld;
            }

        }
        static void Main(string[] args)
        {
            var game = new FPSDemo();
            game.ConstructConsole(200, 120, 8, 8);
            game.Start();
        }
    }
}
