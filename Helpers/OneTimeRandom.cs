using System;
using System.Runtime.CompilerServices;

namespace NormanPCN.Utils
{
    public static class OneTimeRandom
    {
        //numerical recipes
        //Every once in a while, you want a random sequence H(i) whose values you can visit or revisit in any order of i’s.
        //That is to say, you want a random hash of the integers i, one that passes serious tests for randomness, even for very ordered sequences of i’s.

        //this class provides 32 and 64-bit integer calculations for a randomized result.
        //Using the 64-bit ulong seed methods uses the 64-bit calculations.
        //Using the 32-bit uint seed methods uses the 32-bit calculations.

        private const double uintToDouble = 2.32830643653869629E-10;// 1.0 / 2*32
        private const double ulongToDouble = 5.42101086242752217E-20;// 1.0 / 2**64

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static uint RanHash32(uint seed)
        {
            // different generators feeding into the next.
            // LCG->XorShift->MLCG->XorShift

            // 32-bit using numerical recipes rec values for sub functions
            uint v = (seed * 1372383749) + 1289706101; // I1
            v ^= v << 13; // G1
            v ^= v >> 17;
            v ^= v << 5;
            v *= 1597334677; //J1
            v ^= v >> 9; //G3
            v ^= v << 17;
            v ^= v >> 6;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static ulong RanHash64(ulong seed)
        {
            // different generators feeding into the next.
            // LCG->XorShift->MLCG->XorShift

            // verbatum from Numerical recipes Ranhash.
            ulong v = (seed * 3935559000370003845) + 2691343689449507681; //C1
            v ^= v >> 21; //A7
            v ^= v << 37;
            v ^= v >> 4;
            v *= 4768777513237032717; //D3
            v ^= v << 20; //A2
            v ^= v >> 41;
            v ^= v << 5;
            return v;
        }

        public static double RndDouble(uint seed)
        {
            return RanHash32(seed) * uintToDouble;
        }

        public static double RndDouble(ulong seed)
        {
            return RanHash64(seed) * ulongToDouble;
        }

        public static int Rnd(uint seed, int range)
        {
            if (range >= 0)
            {
                uint v = RanHash32(seed);
                if (range == 0)
                    return (int)(v & 0x7fffffff);// return the positive 32-bit signed integer range
                else
                    return (int)(((ulong)v * (ulong)range) >> 32);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(range), "range < 0");
            }
        }

        public static int Rnd(ulong seed, int range)
        {
            if (range >= 0)
            {
                ulong v = RanHash64(seed);
                if (range == 0)
                    return (int)(v & 0x7fffffff);// return the positive 32-bit signed integer range
                else
                    return (int)((double)v * ulongToDouble * (double)range);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(range), "range < 0");
            }
        }

        public static int Rnd(uint seed, int minValue, int maxValue)
        {
            if (minValue < maxValue)
            {
                long range = (long)maxValue - (long)minValue;
                if (range <= (long)Int32.MaxValue)
                {
                    return (int)(((ulong)RanHash32(seed) * (ulong)range) >> 32) + minValue;
                }
                else
                {
                    throw new ArgumentException("range too large");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), "minValue >= maxValue");
            }
        }
        public static int Rnd(ulong seed, int minValue, int maxValue)
        {
            if (minValue < maxValue)
            {
                long range = (long)maxValue - (long)minValue;
                if (range <= (long)Int32.MaxValue)
                {
                    return (int)((double)RanHash64(seed) * ulongToDouble * (double)range) + minValue;
                }
                else
                {
                    throw new ArgumentException("range too large");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), "minValue >= maxValue");
            }
        }
    }
}
