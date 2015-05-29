using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

namespace threads
{

	// 01234567890123456789012345678901234567890123456789012345678901234567890123456789
	// 99,999,999,999 ###0 ###0-###0-###0-###0-###0-###0-###0-###0-###0-###0

	class Program
	{
		public static readonly string FilePathRoot = "c:\\temp\\threads";
		static List<Thread> threads = new List<Thread>();
		public static string threadFile = "";
		public static int pid;
		public static object syncScreen = new object();
		public static object syncFile = new object();
		public static object syncDb = new object();
		public static long total = 0;
		public static int maxThreads, maxCounter, threadWait, createWait;
		public static int rows, columns, stats;
		public static long trigger = 1;
		public static Random rand;
		public static long sum = 0;
		public static int[] displayCounters = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public static readonly int sumCol = 0;
		public static readonly int threadCountCol = 15;
		public static CounterCheck[] counterChecks = new CounterCheck[] {
			new CounterCheck { Value = 0, LastValue = null, DecIndex = null, IncIndex = 0, Direction = ' ', DecCol = -1, IncCol = 20, DirCol = -1 },
			new CounterCheck { Value = 1000, LastValue = 999, DecIndex = 0, IncIndex = 1, Direction = '>', DecCol = 20, IncCol = 25, DirCol = -1 },
			new CounterCheck { Value = 2000, LastValue = 1999, DecIndex = 1, IncIndex = 2, Direction = '>', DecCol = 25, IncCol = 30, DirCol = -1 },
			new CounterCheck { Value = 3000, LastValue = 2999, DecIndex = 2, IncIndex = 3, Direction = '>', DecCol = 30, IncCol = 35, DirCol = -1 },
			new CounterCheck { Value = 4000, LastValue = 3999, DecIndex = 3, IncIndex = 4, Direction = '>', DecCol = 35, IncCol = 40, DirCol = -1 },
			new CounterCheck { Value = 5000, LastValue = 4999, DecIndex = 4, IncIndex = 5, Direction = '>', DecCol = 40, IncCol = 45, DirCol = -1 },
			new CounterCheck { Value = 6000, LastValue = 5999, DecIndex = 5, IncIndex = 6, Direction = '>', DecCol = 45, IncCol = 50, DirCol = -1 },
			new CounterCheck { Value = 7000, LastValue = 6999, DecIndex = 6, IncIndex = 7, Direction = '>', DecCol = 50, IncCol = 55, DirCol = -1 },
			new CounterCheck { Value = 8000, LastValue = 7999, DecIndex = 7, IncIndex = 8, Direction = '>', DecCol = 55, IncCol = 60, DirCol = -1 },
			new CounterCheck { Value = 9000, LastValue = 8999, DecIndex = 8, IncIndex = 9, Direction = '>', DecCol = 60, IncCol = 65, DirCol = -1 },
			new CounterCheck { Value = -8999, LastValue = -9000, DecIndex = 9, IncIndex = 8, Direction = '<', DecCol = 65, IncCol = 60, DirCol = -1 },
			new CounterCheck { Value = -7999, LastValue = -8000, DecIndex = 8, IncIndex = 7, Direction = '<', DecCol = 60, IncCol = 55, DirCol = -1 },
			new CounterCheck { Value = -6999, LastValue = -7000, DecIndex = 7, IncIndex = 6, Direction = '<', DecCol = 55, IncCol = 50, DirCol = -1 },
			new CounterCheck { Value = -5999, LastValue = -6000, DecIndex = 6, IncIndex = 5, Direction = '<', DecCol = 50, IncCol = 45, DirCol = -1 },
			new CounterCheck { Value = -4999, LastValue = -5000, DecIndex = 5, IncIndex = 4, Direction = '<', DecCol = 45, IncCol = 40, DirCol = -1 },
			new CounterCheck { Value = -3999, LastValue = -4000, DecIndex = 4, IncIndex = 3, Direction = '<', DecCol = 40, IncCol = 35, DirCol = -1 },
			new CounterCheck { Value = -2999, LastValue = -3000, DecIndex = 3, IncIndex = 2, Direction = '<', DecCol = 35, IncCol = 30, DirCol = -1 },
			new CounterCheck { Value = -1999, LastValue = -2000, DecIndex = 2, IncIndex = 1, Direction = '<', DecCol = 30, IncCol = 25, DirCol = -1 },
			new CounterCheck { Value = -999, LastValue = -1000, DecIndex = 1, IncIndex = 0, Direction = '<', DecCol = 25, IncCol = 20, DirCol = -1 },
			new CounterCheck { Value = 0, LastValue = -1, DecIndex = 0, IncIndex = null, Direction = ' ', DecCol = 20, IncCol = -1, DirCol = -1 }
		};

		static void Main(string[] args)
		{
			// validate input parameters
			if (args.Length == 0 || args.Length != 4)
			{
				Console.WriteLine("threads <threads> <max> <threadwait> <createwait>");
				return;
			}
			// create or clear file area
			if (!Directory.Exists(FilePathRoot))
			{
				Directory.CreateDirectory(FilePathRoot);
			}
			else
			{
				DirectoryInfo di = new DirectoryInfo(FilePathRoot);
				foreach (FileInfo file in di.GetFiles())
					file.Delete();
			}
			pid = Process.GetCurrentProcess().Id;
			rand = new Random(DateTime.Now.Day);
			maxThreads = (int.Parse(args[0]) > 1000) ? 1000 : int.Parse(args[0]);
			maxCounter = (int.Parse(args[1]) > 9999) ? 9999 : int.Parse(args[1]);
			threadWait = (int.Parse(args[2]) > 10000) ? 10000 : int.Parse(args[2]);
			createWait = (int.Parse(args[3]) > 10000) ? 10000 : int.Parse(args[3]);
			int size = maxThreads * 4;
			columns = Console.WindowWidth;
			rows = ((maxThreads - 1) * 4) / columns;
			stats = rows + 2;
			if (Console.WindowWidth < columns) Console.WindowWidth = columns;
			if (Console.WindowHeight < stats + 1) Console.WindowHeight = stats + 2;
			Console.Clear();
			threadFile = Path.Combine(FilePathRoot, string.Format("threads.{0,4:0000}.txt", pid));
			Console.SetCursorPosition(0, stats);
			Console.Write("{0,14:#,##0}      {1,4:###0} {2,4:###0} {3,4:###0} {4,4:###0} {5,4:###0} {6,4:###0} {7,4:###0} {8,4:###0} {9,4:###0} {10,4:###0}",
				sum, displayCounters[0], displayCounters[1], displayCounters[2], displayCounters[3], displayCounters[4],
				displayCounters[5], displayCounters[6], displayCounters[7], displayCounters[8], displayCounters[9]);
			Thread starter = new Thread(LoadManualThreads);
			starter.Start();
			Console.ReadLine();
		}

		private static void LoadManualThreads()
		{
			List<int> indexPool = new List<int>();
			Random r = new Random(DateTime.Now.Millisecond);
			for (int i = 0; i < maxThreads; i++)
				indexPool.Add(i);
			while (indexPool.Count > 0)
			{
				int s = 0;	// r.Next(indexPool.Count);
				int t = indexPool[s];
				indexPool.RemoveAt(s);
				ManualWorker worker = new ManualWorker(maxCounter, t, threadWait);
				Thread thread = new Thread(worker.BusyAsABee);
				threads.Add(thread);
				thread.Start();
				if (createWait > 0)
					Thread.Sleep(createWait);
			}
		}
	}

	class ManualWorker
	{
		private int t;
		private int wait;
		private int left;
		private int top;
		private int limit;
		private List<int> values;

		static ManualWorker()
		{
		}

		public ManualWorker(int limit, int t, int wait)
		{
			this.limit = limit;
			this.t = t;
			Random r = new Random(DateTime.Now.Millisecond);
			int delta = wait / 10;
			this.wait = wait + ((wait > 0) ? r.Next(-delta, delta) : 0);
			this.left = (t * 4) % Console.WindowWidth;
			this.top = (t * 4) / Console.WindowWidth;
			values = new List<int>();
			for (int i = 0; i <= limit; i++)
				values.Add(i);
			for (int i = -limit; i <= 0; i++)
				values.Add(i);
		}

		private void ConsoleWrite(int left, int top, string format, long value)
		{
			Monitor.Enter(Program.syncScreen);
			Console.SetCursorPosition(left, top);
			Console.Write(format, value);
			Monitor.Exit(Program.syncScreen);
		}

		public void BusyAsABee()
		{
			try
			{
				int? lastValue = null;
				while (values.Count > 0)
				{
					int value = values[0];
					values.RemoveAt(0);
					//				ConsoleColor.Black;
					//    0-0999	ConsoleColor.Gray;
					// 1000-1999	ConsoleColor.DarkYellow;
					// 2000-2999	ConsoleColor.DarkRed;
					// 3000-3999	ConsoleColor.DarkGreen;
					// 4000-4999	ConsoleColor.DarkCyan;
					// 5000-5999	ConsoleColor.Yellow;
					// 6000-6999	ConsoleColor.Green;
					// 7000-7999	ConsoleColor.Cyan;
					// 8000-8999	ConsoleColor.Magenta;
					// 9000-9999	ConsoleColor.Red;
					//				ConsoleColor.Blue;
					//				ConsoleColor.DarkBlue;
					//				ConsoleColor.DarkGray;
					//				ConsoleColor.DarkMagenta;
					//				ConsoleColor.White;
					ConsoleWrite(left, top, "{0,4:###0}", Math.Abs(value));
					Interlocked.Add(ref Program.sum, value);
					ConsoleWrite(1, Program.stats, "{0,14:#,##0}", Program.sum);
					foreach (CounterCheck check in Program.counterChecks)
					{
						if (value == check.Value && lastValue == check.LastValue)
						{
							if (check.DecIndex != null)
							{
								Interlocked.Decrement(ref Program.displayCounters[(int)check.DecIndex]);
								ConsoleWrite(check.DecCol, Program.stats, "{0,4:###0}", Program.displayCounters[(int)check.DecIndex]);
							}
							if (check.IncIndex != null)
							{
								Interlocked.Increment(ref Program.displayCounters[(int)check.IncIndex]);
								ConsoleWrite(check.IncCol, Program.stats, "{0,4:###0}", Program.displayCounters[(int)check.IncIndex]);
							}
							break;
						}
					}
					lastValue = value;
					if (wait > 0)
						Thread.Sleep(wait);
				}
				ConsoleWrite(left, top, "{0,4:####}", 0);
			}
			catch
			{
			}
			finally
			{
			}
		}
	}

	class CounterCheck
	{
		public int Value { get; set; }
		public int? LastValue { get; set; }
		public int? DecIndex { get; set; }
		public int? IncIndex { get; set; }
		public char Direction { get; set; }
		public int DecCol { get; set; }
		public int IncCol { get; set; }
		public int DirCol { get; set; }
	}
}

