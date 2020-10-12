using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Numerics;
using Microsoft.Graphics.Canvas.UI;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using System.Linq;
using Microsoft.Graphics.Canvas.Text;

namespace BlocksAndSpaces
{
    partial class Game
    {
        public int Score { get; set; }
        public int Lines { get; set; }
        public int Level { get; private set; }
        public bool GameOver { get; private set; }

        public Action OnGameOver { get; set; }
        public Action OnDroppingShape { get; set; }
        public Action OnRemovingLines { get; set; }
        public Action OnLevelUp { get; set; }
        public Action OnRotate { get; set; }
        public Action OnMove { get; set; }
        public Action OnBlocked { get; set; }

        const int blocksAcross = 10;
        const int blocksDown = 18;
        const double baseBetweenBlockUpdatesSeconds = 1;
        const double levelReductionSeconds = 0.1;
        const int scoreBase = 100;
        const double levelThreshold = 1000;

        Vector2 blockSize;
        List<Block> blocks;
        Shape currentShape;
        Vector2 shapeOrigin = new Vector2 { X = (float)Math.Floor(blocksAcross / 2d), Y = 0 };

        TimeSpan betweenBlockUpdates = TimeSpan.FromSeconds(baseBetweenBlockUpdatesSeconds);
        TimeSpan fastBetweenBlockUpdates = TimeSpan.FromSeconds(0.1);
        TimeSpan lastBlockUpdate;

        List<Block> blocksToRemove;
        DateTime blockRemovalStart;
        TimeSpan timeToRemove = TimeSpan.FromSeconds(1);
        
        Random random = new Random();
        Color levelColour = Colors.DarkGray;

        public bool SpecialLevel { get; set; }

        public Game(double width, double height)
        {
            blockSize = new Vector2 { X = (float)width / blocksAcross, Y = (float)height / blocksDown };
            blocks = new List<Block>();
            Level = 1;

            SpecialLevel = true;

            OnGameOver = () => { };
            OnDroppingShape = () => { };
            OnRemovingLines = () => { };
            OnLevelUp = () => { };
            OnRotate = () => { };
            OnMove = () => { };
            OnBlocked = () => { };
        }

        private void RandomiseLevelColour()
        {
            levelColour = Color.FromArgb((byte)255, (byte)random.Next(100), (byte)random.Next(100), (byte)random.Next(100));
        }

        internal void Update(CanvasTimingInformation timing, KeyboardState keyboardState)
        {
            if (GameOver)
                return;

            if(currentShape == null)
            {
                if(blocksToRemove != null)
                    RemoveBlocks();
                else
                    CreateShape();
                return;
            }

            if (keyboardState.LeftPressed)
                TryMove(currentShape.Blocks, -1, 0);
            if (keyboardState.RightPressed)
                TryMove(currentShape.Blocks, 1, 0);
            if (keyboardState.RotatePressed)
                TryRotate();

            var timeBetweenUpdates = betweenBlockUpdates;
            if (keyboardState.DownPressed)
                timeBetweenUpdates = fastBetweenBlockUpdates;

            if (lastBlockUpdate == null || timing.TotalTime.Subtract(lastBlockUpdate) >= timeBetweenUpdates)
            {
                lastBlockUpdate = timing.TotalTime;

                if (currentShape.Blocks.All(o => IsFree(o.X, o.Y + 1)))
                {
                    OnDroppingShape();
                    foreach (var block in currentShape.Blocks)
                        block.Y += 1;
                }
                else
                {
                    CheckForLines();
                    currentShape = null;
                }
            }
        }

        internal bool IsFree(float x, float y)
        {
            if (x < 0 || x >= blocksAcross)
                return false;
            if (y < 0 || y >= blocksDown)
                return false;

            return !blocks
                .Where(o => !currentShape.Blocks.Contains(o))
                .Any(o => o.X == x && o.Y == y);
        }

        internal bool TryMove(List<Block> blocks, float dX, float dY)
        {
            if (!blocks.All(o => IsFree(o.X + dX, o.Y + dY)))
            {
                OnBlocked();
                return false;
            }

            OnMove();

            foreach (var block in blocks)
            {
                block.X += dX;
                block.Y += dY;
            }

            return true;
        }

        private void TryRotate()
        {
            var currentRotation = currentShape.Rotations[currentShape.RotationIndex];
            var originIndex = Array.IndexOf(currentRotation, new Vector2 { X = 0, Y = 0 });
            var originBlock = currentShape.Blocks[originIndex].AsVector();

            var newIndex = (currentShape.RotationIndex + 1) % currentShape.Rotations.Length;
            var candidateRotation = currentShape.Rotations[newIndex];

            var paired = currentShape.Blocks.Select((o, i) => new { block = o, rotation = candidateRotation[i] }).ToArray();
            if (!paired.All(o => IsFree(originBlock.X + o.rotation.X, originBlock.Y + o.rotation.Y)))
            {
                OnBlocked();
                return;
            }

            OnRotate();

            currentShape.RotationIndex = newIndex;
            foreach (var pair in paired)
            {
                pair.block.X = originBlock.X + pair.rotation.X;
                pair.block.Y = originBlock.Y + pair.rotation.Y;
            }
        }

        private ShapeTemplate GetNextTemplate()
        {
            var index = random.Next(0, ShapeTemplate.DefaultTemplates.Length);
            return ShapeTemplate.DefaultTemplates[index];
        }

        private void CreateShape()
        {
            var index = random.Next(0, ShapeTemplate.DefaultTemplates.Length);
            var template = ShapeTemplate.DefaultTemplates[index];

            if(nextShapeSpecial)
            {
                template = specialTemplate;
                nextShapeSpecial = false;
            }

            currentShape = new Shape
            {
                Name = template.Name,
                Rotations = template.Rotations
            };

            foreach (var vector in currentShape.Rotations[0])
                currentShape.Blocks.Add(new Block { X = shapeOrigin.X + vector.X, Y = shapeOrigin.Y + vector.Y, Colour = template.Colour });

            blocks.AddRange(currentShape.Blocks);

            if (blocks.Where(o => !currentShape.Blocks.Contains(o))
                .Any(o => currentShape.Blocks.Any(b => b.X == o.X && b.Y == o.Y)))
            {
                GameOver = true;
                OnGameOver();
            }
        }

        private void CheckForLines()
        {
            var lines = blocks.GroupBy(o => o.Y)
                .Where(o => o.Count() == blocksAcross)
                .OrderByDescending(o => o.Key);

            var lineCount = lines.Count();
            if (lineCount == 0)
                return;

            OnRemovingLines();
            blockRemovalStart = DateTime.Now;

            Score += lineCount * scoreBase * lineCount;
            Lines += lineCount;

            CheckForLevelUp();

            blocksToRemove = lines.SelectMany(o => o).ToList();
            blocksToRemove.ForEach(o => o.Colour = Colors.White);
        }

        private void CheckForLevelUp()
        {
            var currentLevel = Level;
            Level = (int)Math.Floor(Score / levelThreshold) + 1;

            if (Level == currentLevel)
                return;
            
            var reduction = baseBetweenBlockUpdatesSeconds - ((Level - 1) * levelReductionSeconds);
            if (reduction < fastBetweenBlockUpdates.Seconds)
                reduction = fastBetweenBlockUpdates.Seconds;
            betweenBlockUpdates = TimeSpan.FromSeconds(reduction);

            OnLevelUp();
            RandomiseLevelColour();

            if (SpecialLevel)
                ApplySpecialLevel();
        }

        private void RemoveBlocks()
        {
            if (DateTime.Now.Subtract(blockRemovalStart) < timeToRemove)
                return;

            foreach (var block in blocksToRemove)
            {
                foreach (var higherBlock in blocks.Where(o => o.X == block.X && o.Y < block.Y))
                    higherBlock.Y += 1;
                blocks.Remove(block);
            }

            blocksToRemove = null;
        }

        internal void Draw(CanvasDrawingSession graphics)
        {
            graphics.FillRectangle(0, 0, blocksAcross * blockSize.X, blocksDown * blockSize.Y, levelColour);

            var specialBlocks = blocks.Where(o => o.Colour == specialTemplate.Colour).OrderBy(o => o.Y).ThenBy(o => o.X).ToArray();

            foreach (var block in blocks)
            {
                graphics.DrawRectangle(block.X * blockSize.X, block.Y * blockSize.Y, blockSize.X, blockSize.Y, Colors.Black);
                graphics.FillRectangle(block.X * blockSize.X + 1, block.Y * blockSize.Y + 1, blockSize.X - 2, blockSize.Y - 2, block.Colour);

                if (specialBlocks.Contains(block))
                    graphics.DrawText(specialMessage[Array.IndexOf(specialBlocks, block)].ToString(), block.X * blockSize.X + 5, block.Y * blockSize.Y, blockSize.X, blockSize.Y, Colors.Black, null);
            }
        }
    }

    class KeyboardState
    {
        public bool LeftPressed { get; set; }
        public bool RotatePressed { get; set; }
        public bool RightPressed { get; set; }
        public bool DownPressed { get; set; }

        public bool AnyPressed
        {
            get { return LeftPressed || RotatePressed || RightPressed || DownPressed; }
        }
    }

    class Block
    {
        public Color Colour { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        internal Vector2 AsVector()
        {
            return new Vector2 { X = X, Y = Y };
        }

        public override string ToString()
        {
            return AsVector().ToString();
        }
    }

    class Shape
    {
        public string Name { get; set; }
        public List<Block> Blocks { get; set; }

        public Vector2[][] Rotations { get; set; }
        public int RotationIndex { get; set; }

        public Shape()
        {
            Blocks = new List<Block>();
        }
    }

    public class ShapeTemplate
    {
        public string Name { get; set; }
        public Color Colour { get; set; }
        public Vector2[][] Rotations { get; set; }

        public static ShapeTemplate[] DefaultTemplates = new[]
        {
            new ShapeTemplate
            {
                Name = "Line", 
                Colour = Colors.Aquamarine,
                Rotations = new Vector2[][] 
                { 
                    new [] { V(-1, 0), V(0, 0), V(1, 0), V(2, 0) },
                    new [] { V(0, -1), V(0, 0), V(0, 1), V(0, 2) }
                }
            },
            new ShapeTemplate
            {
                Name = "Square", 
                Colour = Colors.Yellow,
                Rotations = new Vector2[][]
                { 
                    new [] { V(-1, 0), V(0, 0), V(0, 1), V(-1, 1) }
                }
            },
            new ShapeTemplate
            {
                Name = "S", 
                Colour = Colors.Red,
                Rotations = new Vector2[][]
                { 
                    new [] { V(1, 0), V(0, 0), V(0, 1), V(-1, 1) },
                    new [] { V(0, -1), V(0, 0), V(1, 0), V(1, 1) }
                }
            },
            new ShapeTemplate
            {
                Name = "Z", 
                Colour = Colors.Orange,
                Rotations = new Vector2[][]
                { 
                    new [] { V(-1, 0), V(0, 0), V(0, 1), V(1, 1) },
                    new [] { V(0, -1), V(0, 0), V(-1, 0), V(-1, 1) }
                }
            },
            new ShapeTemplate
            {
                Name = "L", 
                Colour = Colors.LawnGreen,
                Rotations = new Vector2[][]
                { 
                    new [] { V(0, 0), V(0, 1), V(0, 2), V(1, 2) },
                    new [] { V(1, 0), V(0, 0), V(-1, 0), V(-1, 1) },
                    new [] { V(0, 2), V(0, 1), V(0, 0), V(-1, 0) },
                    new [] { V(-1, 0), V(0, 0), V(1, 0), V(1, -1) }
                }
            },
            new ShapeTemplate
            {
                Name = "J", 
                Colour = Colors.Khaki,
                Rotations = new Vector2[][]
                { 
                    new [] { V(0, 0), V(0, 1), V(0, 2), V(-1, 2) },
                    new [] { V(-1, -1), V(-1, 0), V(0, 0), V(1, 0) },
                    new [] { V(1, 0), V(0, 0), V(0, 1), V(0, 2) },
                    new [] { V(1, 1), V(1, 0), V(0, 0), V(-1, 0) }
                }
            },
            new ShapeTemplate
            {
                Name = "T", 
                Colour = Colors.LightBlue,
                Rotations = new Vector2[][]
                { 
                    new [] { V(-1, 1), V(0, 1), V(1, 1), V(0, 0) },
                    new [] { V(0, -1), V(0, 0), V(1, 0), V(0, 1) },
                    new [] { V(-1, 0), V(0, 0), V(1, 0), V(0, 1) },
                    new [] { V(0, -1), V(0, 0), V(0, 1), V(-1, 0) }
                }
            }
        };

        public static Vector2 V(float x, float y)
        {
            return new Vector2 { X = x, Y = y };
        }
    }

    static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }
}
