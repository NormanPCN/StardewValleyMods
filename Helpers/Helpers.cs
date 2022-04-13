using System;
using StardewModdingAPI;

namespace Helpers
{
	public class Logger
	{
		private readonly IMonitor Monitor;
		private readonly string Prefix;
		private readonly LogLevel Level;

		public Logger(IMonitor monitor, LogLevel level = LogLevel.Trace, string prefix = "")
		{
			this.Monitor = monitor;
			this.Prefix = prefix;
			if (this.Prefix != "")
				this.Prefix = this.Prefix + " ";
			this.Level = level;
		}

		private void LogIt(string logMessage, LogLevel logLevel)
		{
			Monitor.Log(Prefix + logMessage, logLevel);
		}

		public void LogOnce(string logMessage, LogLevel logLevel = LogLevel.Info)
		{
			Monitor.LogOnce(Prefix + logMessage, logLevel);
		}

		public void Log(string logMessage)
		{
			this.LogIt(logMessage, Level);
		}

		public void Info(string logMessage)
		{
			this.LogIt(logMessage, LogLevel.Info);
		}

		public void Debug(string logMessage)
		{
			this.LogIt(logMessage, LogLevel.Debug);
		}

		public void Trace(string logMessage)
		{
			this.LogIt(logMessage, LogLevel.Trace);
		}

		public void Warn(string logMessage)
		{
			this.LogIt(logMessage, LogLevel.Warn);
		}

		public void Error(string logMessage)
		{
			this.LogIt(logMessage, LogLevel.Error);
		}

		public void Exception(string logMessage, Exception e)
		{
			Monitor.Log($"{Prefix}{logMessage}\n    Exception: {e.Message}", LogLevel.Error);
			Monitor.Log($"    Full exception data: \n{e.Data}", LogLevel.Error);
		}
	}

	public static class OneTimeRandom
    {
		//numerical recipes
		//Every once in a while, you want a random sequence H(i) whose values you can visit or revisit in any order of i’s.
		//That is to say, you want a random hash of the integers i, one that passes serious tests for randomness, even for very ordered sequences of i’s.
		
		private static uint RanHash(uint seed)
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

		private static ulong RanHash(ulong seed)
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

		public static double Rnd(uint seed)
        {
			return RanHash(seed) * 2.32830643653869629E-10;
		}

		public static double Rnd(ulong seed)
		{
			return RanHash(seed) * 5.42101086242752217E-20;
		}

		public static int Rnd(uint seed, int range)
		{
			if (range >= 0)
			{
				uint v = RanHash(seed);
				if (range == 0)
					return (int)(v & 0x7fffffff);
				else
					return (int)(v % range);
			}
			else
            {
				throw new ArgumentOutOfRangeException(nameof(range), $"range < 0 in {nameof(Rnd)}");
			}
		}

		public static int Rnd(ulong seed, int range)
		{
			if (range >= 0)
			{
				ulong v = RanHash(seed);
				if (range == 0)
					return (int)(v & 0x7fffffff);// return the positive 32-bit signed integer range
				else
					return (int)(v % (ulong)range);
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(range), $"range < 0 in {nameof(Rnd)}");
			}
		}

		public static int Rnd(uint seed, int minValue, int maxValue)
		{
			if (minValue < maxValue)
			{
				long range = (long)maxValue - (long)minValue;
				if (range <= (long)Int32.MaxValue)
				{
					return (int)(RanHash(seed) % (uint)range) + minValue;
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(minValue), $"range too large in {nameof(Rnd)}");
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(minValue), $"minValue >= maxValue in {nameof(Rnd)}");
			}
		}
		public static int Rnd(ulong seed, int minValue, int maxValue)
		{
			if (minValue < maxValue)
			{
				long range = (long)maxValue - (long)minValue;
				if (range <= (long)Int32.MaxValue)
				{
					return (int)(RanHash(seed) % (ulong)range) + minValue;
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(minValue), $"range too large in {nameof(Rnd)}");
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(minValue), $"minValue >= maxValue in {nameof(Rnd)}");
			}
		}
	}
}