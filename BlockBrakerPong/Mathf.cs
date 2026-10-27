using System;

namespace BlockBrakerPong
{
    /// <summary>Small math helpers so game code stays readable.</summary>
    public static class Mathf
    {
        private static readonly Random _rng = new Random();

        public static float Clamp(float value, float min, float max) =>
            value < min ? min : (value > max ? max : value);

        public static int Clamp(int value, int min, int max) =>
            value < min ? min : (value > max ? max : value);

        /// <summary>Random float in [0,1).</summary>
        public static float RandomFloat() => (float)_rng.NextDouble();

        /// <summary>Random float in [min,max).</summary>
        public static float RandomRange(float min, float max) =>
            min + (float)_rng.NextDouble() * (max - min);
    }
}
