using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BlockBrakerPong
{
    /// <summary>
    /// The classic Pong paddle: a solid white bar that slides horizontally
    /// along the bottom of the court. Controlled with arrow keys or A/D.
    /// </summary>
    public class Paddle
    {
        public const int Width = 120;
        public const int Height = 16;

        private const float MoveSpeed = 520f; // pixels per second

        public Vector2 Position;
        public float VelocityX { get; private set; }

        private readonly Game1 _game;

        public Paddle(Game1 game)
        {
            _game = game;
            Position = new Vector2(Game1.CourtWidth / 2f - Width / 2f,
                                   Game1.CourtHeight - Game1.WallThickness - Height - 26);
        }

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public void Update(float elapsed, KeyboardState keys)
        {
            float dir = 0;
            if (keys.IsKeyDown(Keys.Left) || keys.IsKeyDown(Keys.A)) dir -= 1;
            if (keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.D)) dir += 1;

            VelocityX = dir * MoveSpeed;
            Position.X += VelocityX * elapsed;

            // Keep the paddle inside the side walls.
            float minX = Game1.WallThickness;
            float maxX = Game1.CourtWidth - Game1.WallThickness - Width;
            if (Position.X < minX) Position.X = minX;
            if (Position.X > maxX) Position.X = maxX;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // White body with a subtle dark base, like a CRT-lit Pong paddle.
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X + 3, (int)Position.Y + 4, Width, Height),
                new Color(30, 30, 40));
            spriteBatch.Draw(pixel, Bounds, Color.White);
        }
    }
}
