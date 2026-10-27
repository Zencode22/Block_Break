using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BlockBrakerPong
{
    /// <summary>
    /// The ball: a chunky square (as befits 1972 hardware) that bounces off
    /// walls, the paddle and blocks. Paddle speed and hit position influence
    /// the bounce angle, just like arcade Pong.
    /// </summary>
    public class Ball
    {
        public const int Size = 14;              // drawn as a square sprite
        private const float BaseSpeed = 380f;    // pixels per second
        private const float MaxSpeed = 620f;
        private const float SpeedRampPerBlock = 1.5f;

        public Vector2 Position;
        public Vector2 Velocity;
        public bool AttachedToPaddle { get; private set; } = true;
        public bool Missed { get; private set; }

        /// <summary>Short history of positions for the phosphor trail effect.</summary>
        private readonly System.Collections.Generic.Queue<Vector2> _trail =
            new System.Collections.Generic.Queue<Vector2>();

        private readonly Game1 _game;
        private float _speed = BaseSpeed;

        public Ball(Game1 game)
        {
            _game = game;
        }

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Size, Size);

        public void AttachTo(Paddle paddle)
        {
            AttachedToPaddle = true;
            Missed = false;
            _speed = BaseSpeed;
            Velocity = Vector2.Zero;
            PlaceOn(paddle);
        }

        private void PlaceOn(Paddle paddle)
        {
            Position = new Vector2(paddle.Position.X + paddle.Bounds.Width / 2f - Size / 2f,
                                   paddle.Position.Y - Size - 4);
        }

        /// <summary>Launches the ball upward, with a slight random horizontal tilt.</summary>
        public void Launch(float directionY = -1f)
        {
            AttachedToPaddle = false;
            float angle = Mathf.RandomRange(-0.45f, 0.45f); // radians off vertical
            Velocity = new Vector2(Mathf.Sin(angle), directionY * Mathf.Cos(angle)) * _speed;
        }

        public void Update(float elapsed)
        {
            if (AttachedToPaddle)
            {
                PlaceOn(_game.PlayerPaddle);
                return;
            }

            _trail.Enqueue(Position);
            if (_trail.Count > 6) _trail.Dequeue();

            // Move in small steps so fast balls never tunnel through bricks.
            float remaining = elapsed;
            while (remaining > 0 && !Missed)
            {
                float step = System.Math.Min(remaining, 0.004f);
                remaining -= step;
                Move(step);
                if (Velocity == Vector2.Zero) break;
            }
        }

        private void Move(float elapsed)
        {
            Position += Velocity * elapsed;
            var bounds = Bounds;

            // --- Wall collisions (left / right / top) ---
            if (bounds.Left <= Game1.WallThickness && Velocity.X < 0)
            {
                Position.X = Game1.WallThickness;
                Velocity.X = -Velocity.X;
                _game.Sounds.PlayWallBounce();
            }
            else if (bounds.Right >= Game1.CourtWidth - Game1.WallThickness && Velocity.X > 0)
            {
                Position.X = Game1.CourtWidth - Game1.WallThickness - Size;
                Velocity.X = -Velocity.X;
                _game.Sounds.PlayWallBounce();
            }

            if (bounds.Top <= Game1.WallThickness && Velocity.Y < 0)
            {
                Position.Y = Game1.WallThickness;
                Velocity.Y = -Velocity.Y;
                _game.Sounds.PlayWallBounce();
            }

            // --- Missed: fell past the bottom wall ---
            if (bounds.Bottom >= Game1.CourtHeight - 2)
            {
                Missed = true;
                Velocity = Vector2.Zero;
                return;
            }

            // --- Paddle collision ---
            var paddle = _game.PlayerPaddle;
            var pb = paddle.Bounds;
            if (Velocity.Y > 0 && bounds.Intersects(pb))
            {
                // Where on the paddle did we hit? (-1 left edge .. +1 right edge)
                float hitPos = (Position.X + Size / 2f - (pb.X + pb.Width / 2f)) / (pb.Width / 2f);
                hitPos = Mathf.Clamp(hitPos, -1f, 1f);

                float maxBounce = 1.1f; // ~63 degrees
                float angle = hitPos * maxBounce;

                // Paddle motion adds english, classic Pong style.
                Velocity.X += paddle.VelocityX * 0.25f;
                float speed = Mathf.Min(_speed * 1.02f, MaxSpeed);
                _speed = speed;

                Velocity = new Vector2(Mathf.Sin(angle), -Mathf.Abs(Mathf.Cos(angle))) * speed;
                Position.Y = pb.Top - Size - 1;

                _game.Sounds.PlayPaddleHit();
            }

            // --- Block collisions ---
            var blocks = _game.Blocks;
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var block = blocks[i];
                if (!bounds.Intersects(block.Bounds)) continue;

                ResolveBlockCollision(block);

                bool destroyed = block.Hit();
                if (destroyed)
                {
                    blocks.RemoveAt(i);
                    _game.SpawnDebris(new Vector2(block.Bounds.X + block.Bounds.Width / 2f,
                                                  block.Bounds.Y + block.Bounds.Height / 2f),
                                      block.BaseColor);
                    _game.Scores.AddForBlock(block.MaxHp);
                    _game.Sounds.PlayBlockBreak();

                    _speed = System.Math.Min(_speed + SpeedRampPerBlock, MaxSpeed);
                    NormalizeSpeed();
                }
                else
                {
                    _game.Sounds.PlayWallBounce();
                }
                break; // one block per step keeps physics stable
            }
        }

        /// <summary>Bounces the ball off whichever side of the block it entered from.</summary>
        private void ResolveBlockCollision(Block block)
        {
            var b = Bounds;
            var r = block.Bounds;

            // Previous position tells us which face we came from.
            var prev = _trail.Count > 0 ? _trail.Peek() : Position;

            float overlapLeft = b.Right - r.Left;     // came from the left
            float overlapRight = r.Right - b.Left;    // came from the right
            float overlapTop = b.Bottom - r.Top;      // came from above
            float overlapBottom = r.Bottom - b.Top;   // came from below

            float min = System.Math.Min(System.Math.Min(overlapLeft, overlapRight),
                                        System.Math.Min(overlapTop, overlapBottom));

            if (min == overlapTop && Velocity.Y > 0)
            {
                Position.Y = r.Top - Size - 1;
                Velocity.Y = -Velocity.Y;
            }
            else if (min == overlapBottom && Velocity.Y < 0)
            {
                Position.Y = r.Bottom + 1;
                Velocity.Y = -Velocity.Y;
            }
            else if (min == overlapLeft && Velocity.X > 0)
            {
                Position.X = r.Left - Size - 1;
                Velocity.X = -Velocity.X;
            }
            else if (min == overlapRight && Velocity.X < 0)
            {
                Position.X = r.Right + 1;
                Velocity.X = -Velocity.X;
            }
            else
            {
                // Fallback: reverse whichever axis points into the block.
                if (prev.X + Size / 2 < r.X + r.Width / 2f) Velocity.X = -System.Math.Abs(Velocity.X);
                else Velocity.X = System.Math.Abs(Velocity.X);
                if (prev.Y + Size / 2 < r.Y + r.Height / 2f) Velocity.Y = -System.Math.Abs(Velocity.Y);
                else Velocity.Y = System.Math.Abs(Velocity.Y);
            }
        }

        private void NormalizeSpeed()
        {
            if (Velocity == Vector2.Zero) return;
            Velocity.Normalize();
            Velocity *= _speed;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Phosphor trail
            int i = 0;
            foreach (var t in _trail)
            {
                float alpha = 0.08f + 0.1f * i;
                spriteBatch.Draw(pixel, new Rectangle((int)t.X, (int)t.Y, Size, Size),
                    Game1.BallTrail * (alpha * 4f));
                i++;
            }

            // Soft glow behind the ball
            spriteBatch.Draw(pixel, new Rectangle(Bounds.X - 4, Bounds.Y - 4, Size + 8, Size + 8),
                Color.White * 0.12f);

            // The ball itself — a white square, because real Pong had no circles.
            spriteBatch.Draw(pixel, Bounds, Color.White);
        }
    }
}
