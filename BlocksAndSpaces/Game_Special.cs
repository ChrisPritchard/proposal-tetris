using Microsoft.Graphics.Canvas.Numerics;
using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace BlocksAndSpaces
{
    partial class Game
    {
        const string specialMessage = "WILL YOUMARRY  ME?";

        static Func<float, float, Vector2> v = ShapeTemplate.V;
        bool nextShapeSpecial;

        ShapeTemplate specialTemplate = new ShapeTemplate
        {
            Name = "Special",
            Colour = Colors.Magenta,
            Rotations = new Vector2[][]
            { 
                new [] 
                { 
                    v(-4, 0), v(-3, 0), v(-2, 0), v(-1, 0), v(0, 0), v(1, 0), v(2, 0), v(3, 0),
                                v(-3, 1), v(-2, 1), v(-1, 1), v(0, 1), v(1, 1), v(2, 1),
                                        v(-2, 2), v(-1, 2), v(0, 2), v(1, 2)
                }
            }
        };

        private void ApplySpecialLevel()
        {
            blocks = new List<Block>();
            betweenBlockUpdates = TimeSpan.FromSeconds(2);
            levelColour = Colors.White;
            nextShapeSpecial = true;
        }
    }
}
