namespace BlockBrakerPong
{
    /// <summary>
    /// Tracks score, high score and remaining lives. Blocks are worth more as
    /// they get tougher (multi-hit inner rows), rewarding deeper clears.
    /// </summary>
    public class ScoreManager
    {
        public const int MaxLives = 3;

        public int Score { get; private set; }
        public int HighScore { get; set; }
        public int Lives { get; private set; } = MaxLives;

        /// <summary>Points for destroying a block, scaled by its original toughness.</summary>
        public void AddForBlock(int maxHp)
        {
            Score += 10 * maxHp;
            if (Score > HighScore) HighScore = Score;
        }

        /// <summary>Bonus points for clearing a whole level without missing.</summary>
        public void AddLevelBonus(bool flawless)
        {
            int bonus = flawless ? 250 : 100;
            Score += bonus;
            if (Score > HighScore) HighScore = Score;
        }

        /// <summary>Big bonus for beating the final level.</summary>
        public void AddBonusForWin()
        {
            Score += 1000;
            if (Score > HighScore) HighScore = Score;
        }

        /// <summary>Consumes one life. Returns true when the game is over.</summary>
        public bool LoseLife()
        {
            Lives--;
            return Lives <= 0;
        }

        public void Reset()
        {
            Score = 0;
            Lives = MaxLives;
        }
    }
}
