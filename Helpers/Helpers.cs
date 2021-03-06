using System;
//using System.Runtime.CompilerServices;
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
}