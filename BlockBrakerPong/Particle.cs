using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlockBrakerPong
{
    /// <summary>
    /// Tiny square of debris that flies off when a block is destroyed.
    /// Fades out under gravity, like phosphor dots burning away on a CRT.
    /// </summary>
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Tint;
        public float Life;      // seconds remaining
        public float MaxLife;

        private const float Gravity = 480f;
        private const int Size = 5;

        public Particle(Vector2 position, Vector2 velocity, Color tint, float life)
        {
            Position = position;
            Velocity = velocity;
            Tint = tint;
            Life = life;
            MaxLife = life;
        }

        public void Update(float elapsed)
        {
            Velocity.Y += Gravity * elapsed;
            Position += Velocity * elapsed;
            Life -= elapsed;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel) =>
            spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, Size, Size),
                Tint * Mathf.Clamp(Life / MaxLife, 0f, 1f));
    }
}
