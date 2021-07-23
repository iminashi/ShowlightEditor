using System;

namespace ShowLightGenerator
{
    internal static class Randomizer
    {
        private static readonly Random randomizer = new Random();

        internal static int Next(int minValue, int maxValue) =>
            randomizer.Next(minValue, maxValue);
    }
}
