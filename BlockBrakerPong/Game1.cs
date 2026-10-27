using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;

namespace BlockBrakerPong
{
    /// <summary>
    /// Block Braker, Pong edition: Breakout-style brick clearing played with a
    /// classic Pong aesthetic — black court, chunky white paddle, dotted center
    /// line, and retro blip/blop sound effects.
    /// </summary>
    public class Game1 : Game
    {
        // ---- Court layout -------------------------------------------------
        public const int CourtWidth = 800;
        public const int CourtHeight = 600;
        public const int WallThickness = 12;      // left / right / top walls
        public const int HudHeight = 52;          // score strip below the top wall
        public const int ServeZoneTop = HudHeight + 24;

        // ---- Colors (Pong palette) ----------------------------------------
        public static readonly Color Bg = new Color(8, 8, 12);
        public static readonly Color WallColor = new Color(235, 235, 235);
        public static readonly Color DimWhite = new Color(90, 90, 100);
        public static readonly Color TextColor = new Color(235, 235, 235);
        public static readonly Color Accent = new Color(255, 214, 61);   // amber highlight
        public static readonly Color BallTrail = new Color(60, 60, 70);

        // Brick rows use a cool->warm Pong CRT gradient; tougher rows are brighter.
        public static readonly Color[] RowColors =
        {
            new Color(255, 92, 92),    // red      (toughest, inner rows)
            new Color(255, 158, 74),   // orange
            new Color(255, 214, 61),   // yellow
            new Color(122, 222, 130),  // green
            new Color(94, 189, 245),   // cyan
            new Color(136, 132, 255),  // violet   (weakest, outer row)
        };

        // ---- Game states ---------------------------------------------------
        public enum GameState { Title, Serving, Playing, LevelClear, Paused, GameOver, Win }
        public GameState State { get; private set; } = GameState.Title;

        // ---- Managers ------------------------------------------------------
        public GraphicsDeviceManager Graphics { get; private set; } = null!;
        public SpriteBatch SpriteBatch { get; private set; } = null!;
        public SoundManager Sounds { get; private set; } = null!;
        public ScoreManager Scores { get; private set; } = null!;

        public Paddle PlayerPaddle { get; private set; } = null!;
        public Ball TheBall { get; private set; } = null!;
        public List<Block> Blocks { get; private set; } = new List<Block>();
        public List<Particle> Particles { get; private set; } = new List<Particle>();

        public int CurrentLevel { get; private set; } = 1;
        public const int MaxLevel = 4;

        private Texture2D _white = null!;       // 1x1 pixel used for every rectangle
        private SpriteFont _gameFont = null!;   // big HUD font
        private SpriteFont _buttonFont = null!; // small info font
        private KeyboardState _prevKeys;
        private float _flashTimer;      // brief screen flash on level clear
        private double _serveDelay;     // countdown between serve and launch

        public Game1()
        {
            Graphics = new GraphicsDeviceManager(this);
            Graphics.PreferredBackBufferWidth = CourtWidth;
            Graphics.PreferredBackBufferHeight = CourtHeight;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();

            PlayerPaddle = new Paddle(this);
            TheBall = new Ball(this);
            Scores = new ScoreManager();
            BuildLevel(CurrentLevel);
            ResetServe();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Sounds = new SoundManager(Content);

            _white = new Texture2D(GraphicsDevice, 1, 1);
            _white.SetData(new[] { Color.White });

            _gameFont = Content.Load<SpriteFont>("gamefont");
            _buttonFont = Content.Load<SpriteFont>("buttonfont");
        }

        // ---- Level construction -------------------------------------------

        /// <summary>Builds the brick field for the given level (1..MaxLevel).</summary>
        public void BuildLevel(int level)
        {
            Blocks.Clear();

            int cols = 10;
            int rows = Mathf.Clamp(3 + level, 4, 7);

            float topY = ServeZoneTop + 30;
            float blockW = 58;
            float blockH = 22;
            float gap = 8;

            float totalW = cols * blockW + (cols - 1) * gap;
            float startX = (CourtWidth - totalW) / 2f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // Skip some blocks on later levels to carve Pong-ish patterns.
                    if (level >= 3 && (r + c) % 7 == 3) continue;
                    if (level >= 4 && ((r * cols + c) % 11 == 5)) continue;

                    var pos = new Vector2(startX + c * (blockW + gap),
                                          topY + r * (blockH + gap));

                    // Outer rows are weak (1 hit), inner rows tougher.
                    int distFromEdge = System.Math.Min(r, rows - 1 - r);
                    int hp = 1 + System.Math.Min(distFromEdge, 2);

                    Color color = RowColors[r % RowColors.Length];
                    Blocks.Add(new Block(pos, new Point((int)blockW, (int)blockH), hp, color));
                }
            }
        }

        /// <summary>Puts the ball back on the player paddle, waiting to be launched.</summary>
        public void ResetServe()
        {
            TheBall.AttachTo(PlayerPaddle);
            _serveDelay = 0;
        }

        public void StartNewGame()
        {
            // Persist the high score across restarts within this session.
            int previousHigh = Scores.HighScore;
            Scores.Reset();
            Scores.HighScore = System.Math.Max(previousHigh, Scores.HighScore);
            CurrentLevel = 1;
            BuildLevel(CurrentLevel);
            PlayerPaddle.Position = new Vector2(CourtWidth / 2f - Paddle.Width / 2f,
                                                CourtHeight - WallThickness - Paddle.Height - 26);
            Particles.Clear();
            ResetServe();
            State = GameState.Serving;
        }

        // ---- Input helpers --------------------------------------------------

        private static bool KeyPressed(KeyboardState now, KeyboardState prev, Keys key) =>
            now.IsKeyDown(key) && !prev.IsKeyDown(key);

        protected override void Update(GameTime gameTime)
        {
            var keys = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float elapsed = dt;

            // Global hotkeys
            if (keys.IsKeyDown(Keys.Escape)) Exit();

            switch (State)
            {
                case GameState.Title:
                    if (KeyPressed(keys, _prevKeys, Keys.Enter) ||
                        KeyPressed(keys, _prevKeys, Keys.Space))
                    {
                        StartNewGame();
                        Sounds.PlayPing();
                    }
                    break;

                case GameState.Serving:
                    PlayerPaddle.Update(elapsed, keys);
                    TheBall.Update(elapsed); // keeps ball glued to the paddle
                    if (KeyPressed(keys, _prevKeys, Keys.Space) ||
                        Mouse.GetState().LeftButton == ButtonState.Pressed &&
                        _prevMouse.LeftButton == ButtonState.Released)
                    {
                        TheBall.Launch(-1f); // upward by default
                        State = GameState.Playing;
                        Sounds.PlayPaddleHit();
                    }
                    break;

                case GameState.Playing:
                    PlayerPaddle.Update(elapsed, keys);
                    TheBall.Update(elapsed);
                    UpdateParticles(elapsed);

                    if (_flashTimer > 0) _flashTimer -= elapsed;

                    if (Blocks.Count == 0)
                    {
                        Sounds.PlayLevelClear();
                        if (CurrentLevel >= MaxLevel)
                        {
                            Scores.AddBonusForWin();
                            State = GameState.Win;
                        }
                        else
                        {
                            _serveDelay = 1.2;
                            State = GameState.LevelClear;
                        }
                    }
                    else if (TheBall.Missed)
                    {
                        Sounds.PlayGameOverJingle();
                        State = Scores.LoseLife() ? GameState.GameOver : GameState.Serving;
                        if (State == GameState.Serving)
                        {
                            PlayerPaddle.Position = new Vector2(CourtWidth / 2f - Paddle.Width / 2f, PlayerPaddle.Position.Y);
                            ResetServe();
                        }
                    }
                    else if (KeyPressed(keys, _prevKeys, Keys.P))
                    {
                        State = GameState.Paused;
                    }
                    break;

                case GameState.LevelClear:
                    UpdateParticles(elapsed);
                    _serveDelay -= elapsed;
                    if (_serveDelay <= 0)
                    {
                        CurrentLevel++;
                        BuildLevel(CurrentLevel);
                        ResetServe();
                        _flashTimer = 0.35f;
                        State = GameState.Serving;
                    }
                    break;

                case GameState.Paused:
                    if (KeyPressed(keys, _prevKeys, Keys.P) ||
                        KeyPressed(keys, _prevKeys, Keys.Escape))
                    {
                        State = GameState.Playing;
                    }
                    break;

                case GameState.GameOver:
                case GameState.Win:
                    UpdateParticles(elapsed);
                    if (KeyPressed(keys, _prevKeys, Keys.Enter))
                    {
                        StartNewGame();
                    }
                    if (KeyPressed(keys, _prevKeys, Keys.T))
                    {
                        State = GameState.Title;
                    }
                    break;
            }

            _prevKeys = keys;
            _prevMouse = Mouse.GetState();
            base.Update(gameTime);
        }

        private MouseState _prevMouse;

        private void UpdateParticles(float elapsed)
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                p.Update(elapsed);
                if (p.Life <= 0) Particles.RemoveAt(i);
            }
        }

        /// <summary>Spawns a burst of debris when a block is destroyed.</summary>
        public void SpawnDebris(Vector2 position, Color color)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = (float)(Mathf.RandomFloat() * System.Math.PI * 2);
                float speed = 60 + Mathf.RandomFloat() * 180;
                Particles.Add(new Particle(position,
                    new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed),
                    color, 0.4f + Mathf.RandomFloat() * 0.4f));
            }
        }

        // ---- Drawing ---------------------------------------------------------

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Bg);
            SpriteBatch.Begin();

            DrawCourt();

            foreach (var b in Blocks) b.Draw(SpriteBatch, _white);
            PlayerPaddle.Draw(SpriteBatch, _white);
            TheBall.Draw(SpriteBatch, _white);
            foreach (var p in Particles) p.Draw(SpriteBatch, _white);

            DrawHud();

            switch (State)
            {
                case GameState.Title: DrawTitle(); break;
                case GameState.Serving: DrawCenterText("PRESS SPACE TO SERVE", CourtHeight / 2f + 60, Accent); break;
                case GameState.Paused: DrawOverlayPanel("PAUSED", "PRESS P TO RESUME"); break;
                case GameState.LevelClear: DrawCenterText("LEVEL CLEAR!", CourtHeight / 2f, Accent); break;
                case GameState.GameOver: DrawOverlayPanel("GAME OVER", $"SCORE {Scores.Score}   ENTER TO RETRY   T FOR TITLE"); break;
                case GameState.Win: DrawOverlayPanel("YOU WIN!", $"FINAL SCORE {Scores.Score}   ENTER TO PLAY AGAIN"); break;
            }

            if (_flashTimer > 0)
            {
                SpriteBatch.Draw(_white, new Rectangle(0, 0, CourtWidth, CourtHeight),
                    Accent * (0.5f * (_flashTimer / 0.35f)));
            }

            SpriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawCourt()
        {
            // Top / left / right walls + bottom "goal" band
            Fill(0, 0, CourtWidth, WallThickness, WallColor);                       // top
            Fill(0, 0, WallThickness, CourtHeight, WallColor);                      // left
            Fill(CourtWidth - WallThickness, 0, WallThickness, CourtHeight, WallColor); // right
            Fill(0, CourtHeight - WallThickness, CourtWidth, WallThickness, DimWhite);  // bottom (behind paddle)

            // Classic Pong dotted center line running vertically down the court
            for (int y = WallThickness + 14; y < CourtHeight - 18; y += 26)
            {
                Fill(CourtWidth / 2 - 2, y, 4, 14, DimWhite);
            }

            // Center circle hint (drawn as a ring of dots)
            for (int i = 0; i < 24; i++)
            {
                double a = i * System.Math.PI * 2 / 24;
                float x = CourtWidth / 2 + (float)System.Math.Cos(a) * 70;
                float y = CourtHeight / 2 + (float)System.Math.Sin(a) * 70;
                Fill(x - 2, y - 2, 4, 4, new Color(45, 45, 55));
            }
        }

        private void DrawHud()
        {
            string score = $"SCORE {Scores.Score}";
            string hi = $"HI {Scores.HighScore}";
            string level = $"LEVEL {CurrentLevel}/{MaxLevel}";

            SpriteBatch.DrawString(_buttonFont, score, new Vector2(WallThickness + 16, WallThickness + 10), TextColor);
            var hiSize = _buttonFont.MeasureString(hi);
            SpriteBatch.DrawString(_buttonFont, hi, new Vector2(CourtWidth - WallThickness - 16 - hiSize.X, WallThickness + 10), TextColor);

            var lvSize = _buttonFont.MeasureString(level);
            SpriteBatch.DrawString(_buttonFont, level, new Vector2(CourtWidth / 2 - lvSize.X / 2, WallThickness + 10), DimWhite);

            // Lives are drawn as little paddle pips on the right side of the HUD.
            string livesLabel = "LIVES";
            var livesSize = _buttonFont.MeasureString(livesLabel);
            SpriteBatch.DrawString(_buttonFont, livesLabel,
                new Vector2(CourtWidth - WallThickness - 150 - 12 - livesSize.X, WallThickness + 28), DimWhite);
            for (int i = 0; i < ScoreManager.MaxLives; i++)
            {
                Color c = i < Scores.Lives ? Accent : new Color(40, 40, 48);
                Fill(CourtWidth - WallThickness - 150 + i * 22, WallThickness + 30, 14, 6, c);
            }
        }

        private void DrawTitle()
        {
            DrawCenterText("PONG BREAKER", CourtHeight / 2f - 110, TextColor, 1.6f);
            DrawCenterText("BLOCK BRAKER, PONG EDITION", CourtHeight / 2f - 40, DimWhite, 0.6f);
            DrawCenterText("ARROWS OR A/D MOVE THE PADDLE", CourtHeight / 2f + 30, TextColor, 0.6f);
            DrawCenterText("SPACE LAUNCHES THE BALL - P PAUSES", CourtHeight / 2f + 60, TextColor, 0.6f);
            DrawCenterText("PRESS ENTER TO START", CourtHeight / 2f + 120, Accent, 0.8f);
        }

        private void DrawOverlayPanel(string headline, string sub)
        {
            Fill(0, 0, CourtWidth, CourtHeight, new Color(0, 0, 0) * 0.55f);
            DrawCenterText(headline, CourtHeight / 2f - 40, TextColor, 1.4f);
            DrawCenterText(sub, CourtHeight / 2f + 40, Accent, 0.6f);
        }

        private void DrawCenterText(string text, float y, Color color, float scale = 1f)
        {
            var size = _gameFont.MeasureString(text) * scale;
            SpriteBatch.DrawString(_gameFont, text,
                new Vector2(CourtWidth / 2 - size.X / 2, y - size.Y / 2), color, 0,
                Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void Fill(float x, float y, float w, float h, Color color) =>
            SpriteBatch.Draw(_white, new Rectangle((int)x, (int)y, (int)w, (int)h), color);

        public void PlayWallBounce() => Sounds.PlayWallBounce();
    }
}
