using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AgarP2P;
using System;

namespace AgarP2P
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new AgarGame())
            {
                game.Run();
            }
        }
    }
}