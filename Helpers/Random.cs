using System;
using System.Runtime.CompilerServices;

namespace NormanPCN.Utils
{
    public class RandomNumbers
    {
        public const int XorShiftWow = 1;
        public const int XorShiftPlus = 2;
        public const int Ranq1 = 3;

        private int genType;
        private uint[] state32 = new uint[5];
        private ulong[] state64 = new ulong[2];
        private uint incr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint XorShift32(uint seed)
        {
            seed ^= seed << 13; // G1, numerical recipes
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return seed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong XorShift64(ulong seed)
        {
            seed ^= seed << 13;
            seed ^= seed >> 7;
            seed ^= seed << 17;
            return seed;
        }

        public static uint GetRandomSeed()
        {
            long seed = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            //DateTime epoc = new DateTime(2000, 1, 1);
            //long seed = (ulong)(DateTime.UtcNow - epoc).TotalSeconds;

            return XorShift32((uint)seed);
        }

        public RandomNumbers() : this(GetRandomSeed(), XorShiftWow)
        {
        }

        public RandomNumbers(uint seed, int genType = XorShiftWow)
        {
            this.genType = genType;
            Reseed(seed);
        }

        public void Reseed(uint seed)
        {
            switch (genType)
            {
                case XorShiftWow:
                    incr = 6615241;//could be anything. xorwow uses this value.
                    for (int i = 0; i < 5; i++)
                    {
                        seed = XorShift32(seed);
                        state32[i] = seed;
                    }
                    return;
                case XorShiftPlus:
                    state64[0] = XorShift64(seed);
                    state64[1] = XorShift64(state64[0]);
                    return;
                case Ranq1:
                    state64[0] = (ulong)(seed) ^ 4101842887655102017;
                    return;
                default:
                    throw new InvalidOperationException();
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint xorwow()
        {
            uint t = state32[4];
            uint s = state32[0];

            state32[4] = state32[3];
            state32[3] = state32[2];
            state32[2] = state32[1];
            state32[1] = s;

            t ^= t >> 2;
            t ^= t << 1;
            t ^= s ^ (s << 4);
            state32[0] = t;
            incr += 362437;
            return t + incr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong xorp()
        {
            ulong t = state64[0];
            ulong s = state64[1];

            state64[0] = s;
            t ^= t << 23;
            t ^= t >> 18;
            t ^= s ^ (s >> 5);
            state64[1] = t;

            return t + s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong Ran_q1()
        {
            ulong v = state64[0];
            v ^= v >> 21;
            v ^= v << 35;
            v ^= v >> 4;
            state64[0] = v;
            return v * 2685821657736338717;
        }

        public double NextDouble()
        {
            switch (genType)
            {
                case XorShiftWow:
                    return (double)xorwow() * 2.32830643653869629E-10;
                case XorShiftPlus:
                    return (double)xorp() * 5.42101086242752217E-20;
                case Ranq1:
                    return (double)Ran_q1() * 5.42101086242752217E-20;
                default:
                    throw new InvalidOperationException();
            }
        }

        public int Next()
        {
            switch (genType)
            {
                case XorShiftWow:
                    return (int)(xorwow() & 0x7fffffff);
                case XorShiftPlus:
                    return (int)(xorp() & 0x7fffffff);
                case Ranq1:
                    return (int)(Ran_q1() & 0x7fffffff);
                default:
                    throw new InvalidOperationException();
            }
            
        }

        public int Next(int maxValue)
        {
            if (maxValue > 0)
            {
                switch (genType)
                {
                    case XorShiftWow:
                        return (int)(xorwow() % maxValue);
                    case XorShiftPlus:
                        return (int)(xorp() % (ulong)maxValue);
                    case Ranq1:
                        return (int)(Ran_q1() % (ulong)maxValue);
                    default:
                        throw new InvalidOperationException();
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), $"maxValue <= 0 in {nameof(Next)}");
            }
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue < maxValue)
            {
                long range = (long)maxValue - (long)minValue;
                if (range <= (long)Int32.MaxValue)
                {
                    switch (genType)
                    {
                        case XorShiftWow:
                            return (int)(xorwow() % (uint)range) + minValue;
                        case XorShiftPlus:
                            return (int)(xorp() % (ulong)range) + minValue;
                        case Ranq1:
                            return (int)(Ran_q1() % (ulong)range) + minValue;
                        default:
                            throw new InvalidOperationException();
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(minValue), $"range too large in {nameof(Next)}");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(minValue), $"minValue >= maxValue in {nameof(Next)}");
            }
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            int i = 0;
            int l = buffer.Length;
            while (i < l)
            {
                ulong d;
                int b = 8;
                switch (genType)
                {
                    case XorShiftWow:
                        d = xorwow();
                        b = 4;
                        break;
                    case XorShiftPlus:
                        d = xorp();
                        break;
                    case Ranq1:
                        d = Ran_q1();
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                for (int j = 0; j < b; i++)
                {
                    if (i < l)
                    {
                        buffer[i] = (byte)(d & 0xff);
                        d >>= 8;
                        i++;
                    }
                }
            }
        }
    }
}
