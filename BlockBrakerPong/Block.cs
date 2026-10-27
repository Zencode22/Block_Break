using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlockBrakerPong
{
    /// <summary>
    /// A single destructible brick. Tougher bricks (higher HP) render with a
    /// brighter face and a chunky retro bevel, like 70s arcade art.
    /// </summary>
    public class Block
    {
        public Vector2 Position;
        public Point Size;
        public int MaxHp { get; private set; }
        public int Hp { get; private set; }
        public Color BaseColor { get; private set; }

        public Block(Vector2 position, Point size, int hp, Color color)
        {
            Position = position;
            Size = size;
            Hp = hp;
            MaxHp = hp;
            BaseColor = color;
        }

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        /// <summary>
        /// Applies one hit. Returns true if the block was destroyed by this hit.
        /// </summary>
        public bool Hit()
        {
            Hp--;
            return Hp <= 0;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            var b = Bounds;

            // Dark outline / drop shadow for that chunky arcade look.
            spriteBatch.Draw(pixel, new Rectangle(b.X + 3, b.Y + 3, b.Width, b.Height),
                new Color(25, 25, 35));

            // Body: damaged bricks fade toward the dim court color.
            float healthFactor = (float)Hp / MaxHp;
            Color body = Color.Lerp(Game1.Bg * 0.9f, BaseColor, healthFactor);
            spriteBatch.Draw(pixel, b, body);

            // Top bevel highlight + bottom shade line.
            spriteBatch.Draw(pixel, new Rectangle(b.X, b.Y, b.Width, 4),
                Color.White * (0.25f + 0.35f * healthFactor));
            spriteBatch.Draw(pixel, new Rectangle(b.X, b.Bottom - 4, b.Width, 4),
                Color.Black * 0.35f);
        }
    }
}
