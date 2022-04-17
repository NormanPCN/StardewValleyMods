using System;
using System.Runtime.CompilerServices;

namespace NormanPCN.Utils
{
    public class RandomNumbers
    {
        public const uint XorShiftWow = 0;
        public const uint XorShiftPlus = 1;
        public const uint NR_Ranq1 = 2;
        public const uint NR_Ran = 3;
        public const uint DefaultRNG = XorShiftWow;

        private uint genType;

        // state variables
        private uint xorw_v;
        private uint xorw_w;
        private uint xorw_z;
        private uint xorw_y;
        private uint xorw_x;
        private uint incr;
        private ulong ran_u;
        private ulong ran_v;
        private ulong ran_w;
        private ulong xorp_0;
        private ulong xorp_1;

        private const double uintToDouble = 2.32830643653869629E-10;// 1.0 / 2*32
        private const double ulongToDouble = 5.42101086242752217E-20;// 1.0 / 2**64

        //private delegate ulong RandomNumberFunc();
        //private RandomNumberFunc randFunc;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint XorShift32(uint seed)
        {
            seed ^= seed << 13;
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

            seed += Environment.CurrentManagedThreadId;
            seed += Environment.ProcessId;
            seed += Environment.TickCount64;
            //seed += System.Threading.Thread.CurrentThread.ManagedThreadId;
            //seed += System.Diagnostics.Process.GetCurrentProcess().Id;


            return (uint)XorShift64((ulong)seed);
        }

        public RandomNumbers() : this(GetRandomSeed(), DefaultRNG)
        {
        }

        public RandomNumbers(uint seed, uint genType = DefaultRNG)
        {
            this.genType = genType;

            if ((genType < XorShiftWow) || (genType > NR_Ran))
                throw new ArgumentOutOfRangeException(nameof(genType));

            //switch (genType)
            //{
            //    case XorShiftWow:
            //        this.randFunc = xorwow;
            //        break;
            //    case XorShiftPlus:
            //        this.randFunc = xorp;
            //        break;
            //    case NR_Ranq1:
            //        this.randFunc = Ranq1;
            //        break;
            //    case NR_Ran:
            //        this.randFunc = Ran;
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException(nameof(genType));
            //}

            Reseed(seed);
        }

        // need both flags for best performance in resulting code

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private uint xorwow()
        {
            uint t = xorw_x;
            uint s = xorw_v;

            xorw_x = xorw_y;
            xorw_y = xorw_z;
            xorw_z = xorw_w;
            xorw_w = s;

            t ^= t >> 2;
            t ^= t << 1;
            t ^= s ^ (s << 4);
            xorw_v = t;
            incr += 362437;
            return t + incr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private ulong xorp()
        {
            ulong t = xorp_0;
            ulong s = xorp_1;

            xorp_0 = s;
            t ^= t << 23;
            t ^= t >> 18;
            t ^= s ^ (s >> 5);
            xorp_1 = t;

            return t + s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private ulong Ranq1()
        {
            // a so called XorShift64* algorithm
            ulong v = ran_v;
            v ^= v >> 21;
            v ^= v << 35;
            v ^= v >> 4;
            ran_v = v;
            return v * 2685821657736338717;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private ulong Ran()
        {
            ulong u = ran_u;
            ulong v = ran_v;
            ulong w = ran_w;

            u = u * 2862933555777941757 + 7046029254386353087;
            ran_u = u;
            v ^= v >> 17;
            v ^= v << 31;
            v ^= v >> 8;
            ran_v = v;
            w = 4294957665U * (w & 0xffffffff) + (w >> 32);
            ran_w = w;
            ulong x = u ^ (u << 21);
            x ^= x >> 35;
            x ^= x << 4;
            return (x + v) ^ w;
        }

        public void Reseed(uint seed)
        {
            switch (genType)
            {
                case XorShiftWow:
                    incr = 6615241;//could be anything. xorwow uses this value.
                    seed = XorShift32(seed);
                    xorw_x = seed;
                    seed = XorShift32(seed);
                    xorw_y = seed;
                    seed = XorShift32(seed);
                    xorw_z = seed;
                    seed = XorShift32(seed);
                    xorw_w = seed;
                    seed = XorShift32(seed);
                    xorw_v = seed;
                    return;
                case XorShiftPlus:
                    xorp_0 = XorShift64((ulong)seed ^ 4101842887655102017);
                    xorp_1 = XorShift64(xorp_0);
                    return;
                case NR_Ranq1:
                    ran_v = (ulong)(seed) ^ 4101842887655102017;
                    ran_v = Ranq1();
                    return;
                case NR_Ran:
                    ran_v = 4101842887655102017;
                    ran_w = 1;
                    ran_u = (ulong)seed ^ ran_v;
                    Ran();
                    ran_v = ran_u;
                    Ran();
                    ran_w = ran_v;
                    Ran();
                    return;
                default:
                    throw new InvalidOperationException("genType invalid");
            }

        }

        public double NextDouble()
        {
            switch (genType)
            {
                case XorShiftWow:
                    return (double)xorwow() * uintToDouble;
                case XorShiftPlus:
                    return (double)xorp() * ulongToDouble;
                case NR_Ranq1:
                    return (double)Ranq1() * ulongToDouble;
                case NR_Ran:
                    return (double)Ran() * ulongToDouble;
                default:
                    throw new InvalidOperationException("genType invalid");
            }
        }

        public int Next()
        {
            switch (genType)
            {
                // return signed int positive range. take middle bits of long results.
                case XorShiftWow:
                    return (int)(xorwow() & 0x7fffffff);
                case XorShiftPlus:
                    return (int)(xorp() >> 8 & 0x7fffffff);
                case NR_Ranq1:
                    return (int)(Ranq1() >> 8 & 0x7fffffff);
                case NR_Ran:
                    return (int)(Ran() >> 8 & 0x7fffffff);
                default:
                    throw new InvalidOperationException("genType invalid");
            }
            
        }

        public int Next(int maxValue)
        {
            if (maxValue > 0)
            {
                // double only has 52 explicit bits in mantissa. thus low bits of a long are unused.
                switch (genType)
                {
                    case XorShiftWow:
                        return (int)(((ulong)xorwow() * (ulong)maxValue) >> 32);
                    case XorShiftPlus:
                        return (int)((double)xorp() * ulongToDouble * (double)maxValue);
                    case NR_Ranq1:
                        //return (int)(Ranq1() % (ulong)maxValue);
                        return (int)((double)Ranq1() * ulongToDouble * (double)maxValue);
                    case NR_Ran:
                        return (int)((double)Ran() * ulongToDouble * (double)maxValue);
                    default:
                        throw new InvalidOperationException("genType invalid");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue < maxValue)
            {
                long range = (long)maxValue - (long)minValue;
                if (range <= (long)Int32.MaxValue)
                {
                    // double only has 52 explicit bits in mantissa. thus low bits of a long are unused.
                    switch (genType)
                    {
                        case XorShiftWow:
                            return (int)(((ulong)xorwow() * (ulong)range) >> 32) + minValue;
                        case XorShiftPlus:
                            return (int)((double)xorp() * ulongToDouble * (double)range) + minValue;
                        case NR_Ranq1:
                            return (int)((double)Ranq1() * ulongToDouble * (double)range) + minValue;
                        case NR_Ran:
                            return (int)((double)Ran() * ulongToDouble * (double)range) + minValue;
                        default:
                            throw new InvalidOperationException("genType invalid");
                    }
                }
                else
                {
                    throw new ArgumentException("range too large");
                }
            }
            else
            {
                throw new ArgumentException("minValue >= maxValue");
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
                    case NR_Ranq1:
                        d = Ranq1();
                        break;
                    case NR_Ran:
                        d = Ran();
                        break;
                    default:
                        throw new InvalidOperationException("genType invalid");
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
