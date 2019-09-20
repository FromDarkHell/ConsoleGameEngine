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
            public enum spriteType
            {
                TYPE_PROJECTILE = 0x00,
                TYPE_OBJECT = 0x01
            }

            public struct sObject
            {
                public float x;
                public float y;
                public float vx;
                public float vy;
                public bool bRemove;
                public spriteType spriteType;
                public olcSprite sprite;
            };

            private static readonly int mapW = 32;
            private static readonly int mapH = 32;
            private static readonly int depth = 32;
            private static readonly double fov = Math.PI / 4.0;

            // The default X value of the player  
            static double playerX = 8.0d;
            // The default Y value of the player
            static double playerY = 8.0d;

            static double playerA = 0.0d;

            static double fSpeed = 4.25f;
            static double sprintModifier = 1.00f;

            string[] mapString;
            char[] map;

            private static bool showMinimap = true;
            private static bool bIsPlayingGame = true;
            private static bool bEnableFog = true;

            private static double lastMouseX, lastMouseY = 0;

            static olcSprite spriteWall = new olcSprite(Environment.CurrentDirectory + "\\FPSSprites\\fps_wall1.spr");
            static olcSprite spriteLamp = new olcSprite(Environment.CurrentDirectory + "\\FPSSprites\\fps_lamp1.spr");
            static olcSprite spriteFireball = new olcSprite(Environment.CurrentDirectory + "\\FPSSprites\\fps_fireball1.spr");

            double[] fDepthBuffer;

            List<sObject> listObjects = new List<sObject>();
            public override bool OnUserCreate()
            {
                listObjects.Add(new sObject() { x = 8.5f, y = 8.5f, vx = 0.0f, vy = 0.0f, bRemove = false, spriteType = spriteType.TYPE_OBJECT, sprite = spriteLamp });
                listObjects.Add(new sObject() { x = 7.5f, y = 7.5f, vx = 0.0f, vy = 0.0f, bRemove = false, spriteType = spriteType.TYPE_OBJECT, sprite = spriteLamp });
                listObjects.Add(new sObject() { x = 10.5f, y = 3.5f, vx = 0.0f, vy = 0.0f, bRemove = false, spriteType = spriteType.TYPE_OBJECT, sprite = spriteLamp });
                listObjects.Add(new sObject() { x = 3f, y = 13f, vx = 0.0f, vy = 0.0f, bRemove = false, spriteType = spriteType.TYPE_OBJECT, sprite = spriteLamp });

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
                fDepthBuffer = new double[ScreenWidth()];
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

                if (isKeyPushedDown(Keys.Space) || m_mouse[0].bHeld)
                {
                    sObject o;
                    o.x = (float)playerX;
                    o.y = (float)playerY;
                    float noise = (((new Random().Next()) - 0.5f) / int.MaxValue);
                    o.vx = (float)Math.Sin(playerA);
                    o.vy = (float)Math.Cos(playerA);
                    o.sprite = spriteFireball;
                    o.bRemove = false;
                    o.spriteType = spriteType.TYPE_PROJECTILE;
                    listObjects.Add(o);
                }

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

                    double fSampleX = 0.0d;

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

                                // Determine where ray has hit wall. Break Block boundary
                                // int 4 line segments
                                double fBlockMidX = testX + 0.5f;
                                double fBlockMidY = testY + 0.5f;

                                double fTestPointX = (playerX + eyeX * distanceToWall);
                                double fTestPointY = (playerY + eyeY * distanceToWall);

                                double fTestAngle = Math.Atan2((fTestPointY - fBlockMidY), (fTestPointX - fBlockMidX));

                                if (fTestAngle >= -3.14159f * 0.25f && fTestAngle < 3.14159f * 0.25f) fSampleX = fTestPointY - testY;
                                if (fTestAngle >= 3.14159f * 0.25f && fTestAngle < 3.14159f * 0.75f) fSampleX = fTestPointX - testX;
                                if (fTestAngle < -3.14159f * 0.25f && fTestAngle >= -3.14159f * 0.75f) fSampleX = fTestPointX - testX;
                                if (fTestAngle >= 3.14159f * 0.75f || fTestAngle < -3.14159f * 0.75f) fSampleX = fTestPointY - testY;
                            }
                        }
                    }

                    // Calculate the distance to the ceiling, currently unused for any rendering.
                    int ceiling = (int)((float)(ScreenHeight() / 2.0) - ScreenHeight() / (distanceToWall));
                    // The floor... duh.
                    int floor = ScreenHeight() - ceiling;

                    fDepthBuffer[x] = distanceToWall;

                    for (int y = 0; y < ScreenHeight(); y++)
                    {
                        // Each Row
                        if (y <= ceiling)
                        {
                            Draw(x, y, '.', COLOR.FG_BLUE);
                        }
                        else if (y > ceiling && y <= floor)
                        {
                            // Draw Wall
                            if (distanceToWall < depth)
                            {
                                float fSampleY = ((float)y - (float)ceiling) / ((float)floor - (float)ceiling);
                                Draw(x, y, spriteWall.SampleGlyph((float)fSampleX, fSampleY), spriteWall.SampleColor((float)fSampleX, fSampleY));
                            }
                            else
                                Draw(x, y, PIXEL_TYPE.PIXEL_SOLID, COLOR.FG_BLACK);
                        }
                        else // Floor
                        {
                            Draw(x, y, PIXEL_TYPE.PIXEL_SOLID, COLOR.FG_DARK_GREEN);
                        }

                    }


                }

                for (int i = 0; i < listObjects.Count; i++)
                {
                    sObject obj = listObjects[i];
                    // Update obj Physics
                    obj.x += (obj.vx);
                    obj.y += (obj.vy);
                    try
                    {
                        // Check if obj is inside wall - set flag for removal
                        if (map[(int)obj.x * mapW + (int)obj.y] == '#')
                            obj.bRemove = true;
                        if (obj.spriteType == spriteType.TYPE_PROJECTILE)
                        {
                            for (int o = 0; o < listObjects.Count; o++)
                            {
                                sObject sObj = listObjects[o];
                                double dVecX = obj.x - sObj.x;
                                double dVecY = obj.y - sObj.y;
                                double dDistanceFromObj = Math.Sqrt(dVecX * dVecX + dVecY * dVecY);
                                if (dDistanceFromObj > -1 && dDistanceFromObj < 0.6)
                                    obj.bRemove = true;
                            }
                        }
                    }
                    catch (Exception) { obj.bRemove = true; }

                    // Can obj be seen?
                    double fVecX = obj.x - playerX;
                    double fVecY = obj.y - playerY;
                    double fDistanceFromPlayer = Math.Sqrt(fVecX * fVecX + fVecY * fVecY);

                    double fEyeX = Math.Sin(playerA);
                    double fEyeY = Math.Cos(playerA);

                    // Calculate angle between lamp and players feet, and players looking direction
                    // to determine if the lamp is in the players field of view
                    double fobjAngle = Math.Atan2(fEyeY, fEyeX) - Math.Atan2(fVecY, fVecX);
                    if (fobjAngle < -3.14159f) fobjAngle += 2.0f * 3.14159f;
                    if (fobjAngle > 3.14159f) fobjAngle -= 2.0f * 3.14159f;
                    bool bInPlayerFOV = Math.Abs(fobjAngle) < fov / 2.0f;

                    if (bInPlayerFOV && fDistanceFromPlayer >= 0.5f && fDistanceFromPlayer < depth && !obj.bRemove)
                    {
                        double fObjectCeiling = (ScreenHeight() / 2.0) - ScreenHeight() / (fDistanceFromPlayer);
                        double fObjectFloor = ScreenHeight() - fObjectCeiling;
                        double fObjectHeight = fObjectFloor - fObjectCeiling;
                        double fObjectAspectRatio = obj.sprite.nHeight / obj.sprite.nWidth;
                        double fObjectWidth = fObjectHeight / fObjectAspectRatio;
                        double fMiddleOfObject = (0.5f * (fobjAngle / (fov / 2.0f)) + 0.5f) * ScreenWidth();

                        // Draw Lamps / other objects
                        for (double lx = 0; lx < fObjectWidth; lx++)
                        {
                            for (double ly = 0; ly < fObjectHeight; ly++)
                            {
                                double floatSampleX = lx / fObjectWidth;
                                double floatSampleY = ly / fObjectHeight;

                                char c = obj.sprite.SampleGlyph((float)floatSampleX, (float)floatSampleY);
                                int objectColumn = (int)(fMiddleOfObject + lx - (fObjectWidth / 2.0f));
                                if (objectColumn >= 0 && objectColumn < ScreenWidth())
                                {
                                    if (c != ' ' && fDepthBuffer[objectColumn] >= fDistanceFromPlayer)
                                    {
                                        Draw(objectColumn, fObjectCeiling + ly, c, obj.sprite.SampleColor((float)floatSampleX, (float)floatSampleY));
                                        fDepthBuffer[objectColumn] = fDistanceFromPlayer;
                                    }
                                }
                            }
                        }
                    }

                    if (obj.spriteType == spriteType.TYPE_OBJECT && fDistanceFromPlayer > -1 && fDistanceFromPlayer < 0.6)
                    {
                        playerX = frmX;
                        playerY = frmY;
                    }

                    listObjects[i] = obj;
                }

                listObjects = listObjects.Where(obj => !obj.bRemove).ToList();

                string stats = String.Format("X={0:0.00}, Y={1:0.00}, A={2:0.00}, SP: {3:0.00}, mX: {4:0.00}, oX: {5:0.00}, dX: {6:0.00}, #obj: {7}",
                playerX, playerY, playerA, fSpeed, m_mousePosX, lastMouseX, m_mousePosX - lastMouseX, listObjects.Count);
                DrawString(0, 0, stats);

                if (showMinimap)
                {
                    // Display Map & Player
                    for (int nx = 0; nx < mapW; nx++)
                        for (int ny = 0; ny < mapW; ny++)
                            Draw(nx + 1, ny + 1, map[ny * mapW + nx]);
                    Draw(1 + (int)playerY, 1 + (int)playerX, 'P');
                }
                #endregion

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
            game.ConstructConsole(400, 151, 4, 6);
            game.Start();
        }
    }
}
