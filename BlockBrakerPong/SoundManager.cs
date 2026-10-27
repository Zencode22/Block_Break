using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace BlockBrakerPong
{
    /// <summary>
    /// Loads and plays all the retro sound effects: Pong-style blips for
    /// walls and paddles, a crunch for breaking blocks, and jingles for
    /// level clear / game over.
    /// </summary>
    public class SoundManager
    {
        private readonly SoundEffect _ping1;
        private readonly SoundEffect _ping2;
        private readonly SoundEffect _paddleHit;
        private readonly SoundEffect _blockBreak;
        private readonly SoundEffect _wallBounce;
        private readonly SoundEffect _gameOver;
        private readonly SoundEffect _levelClear;

        public SoundManager(ContentContentManager content)
        {
            _ping1 = content.Load<SoundEffect>("ping1");
            _ping2 = content.Load<SoundEffect>("ping2");
            _paddleHit = content.Load<SoundEffect>("paddleHit");
            _blockBreak = content.Load<SoundEffect>("blockBreak");
            _wallBounce = content.Load<SoundEffect>("wallBounce");
            _gameOver = content.Load<SoundEffect>("gameOver");
            _levelClear = content.Load<SoundEffect>("levelClear");
        }

        /// <summary>Classic alternating Pong "ping" blip.</summary>
        public void PlayPing() => _ping1.Play();

        /// <summary>The lower half of the classic ping/pong pair.</summary>
        public void PlayPong() => _ping2.Play();

        public void PlayPaddleHit() => _paddleHit.Play();

        public void PlayBlockBreak() => _blockBreak.Play();

        public void PlayWallBounce() => _wallBounce.Play();

        public void PlayGameOverJingle() => _gameOver.Play();

        public void PlayLevelClear() => _levelClear.Play();
    }
}
