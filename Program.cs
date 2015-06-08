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
		public static int maxThreads, maxCounter, threadWait;
		public static int rows, columns, stats;
		public static long trigger = 1;
		public static Random rand;
		public static long sum = 0;
		public static int[] counters = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		public static readonly int sumCol = 0;
		public static readonly int threadCountCol = 15;
		public static CounterCheck[] counterChecks = new CounterCheck[] {
			new CounterCheck { Value = 0, LastValue = null, DecIndex = -1, IncIndex = 0, DecCol = -1, IncCol = 15, DirCol = -1 },
			new CounterCheck { Value = 1000, LastValue = 999, DecIndex = 0, IncIndex = 1, DecCol = 15, IncCol = 20, DirCol = -1 },
			new CounterCheck { Value = 2000, LastValue = 1999, DecIndex = 1, IncIndex = 2, DecCol = 20, IncCol = 25, DirCol = -1 },
			new CounterCheck { Value = 3000, LastValue = 2999, DecIndex = 2, IncIndex = 3, DecCol = 25, IncCol = 30, DirCol = -1 },
			new CounterCheck { Value = 4000, LastValue = 3999, DecIndex = 3, IncIndex = 4, DecCol = 30, IncCol = 35, DirCol = -1 },
			new CounterCheck { Value = 5000, LastValue = 4999, DecIndex = 4, IncIndex = 5, DecCol = 35, IncCol = 40, DirCol = -1 },
			new CounterCheck { Value = 6000, LastValue = 5999, DecIndex = 5, IncIndex = 6, DecCol = 40, IncCol = 45, DirCol = -1 },
			new CounterCheck { Value = 7000, LastValue = 6999, DecIndex = 6, IncIndex = 7, DecCol = 45, IncCol = 50, DirCol = -1 },
			new CounterCheck { Value = 8000, LastValue = 7999, DecIndex = 7, IncIndex = 8, DecCol = 50, IncCol = 55, DirCol = -1 },
			new CounterCheck { Value = 9000, LastValue = 8999, DecIndex = 8, IncIndex = 9, DecCol = 55, IncCol = 60, DirCol = -1 },
			new CounterCheck { Value = -8999, LastValue = -9000, DecIndex = 9, IncIndex = 8, DecCol = 60, IncCol = 55, DirCol = -1 },
			new CounterCheck { Value = -7999, LastValue = -8000, DecIndex = 8, IncIndex = 7, DecCol = 55, IncCol = 50, DirCol = -1 },
			new CounterCheck { Value = -6999, LastValue = -7000, DecIndex = 7, IncIndex = 6, DecCol = 50, IncCol = 45, DirCol = -1 },
			new CounterCheck { Value = -5999, LastValue = -6000, DecIndex = 6, IncIndex = 5, DecCol = 45, IncCol = 40, DirCol = -1 },
			new CounterCheck { Value = -4999, LastValue = -5000, DecIndex = 5, IncIndex = 4, DecCol = 40, IncCol = 35, DirCol = -1 },
			new CounterCheck { Value = -3999, LastValue = -4000, DecIndex = 4, IncIndex = 3, DecCol = 35, IncCol = 30, DirCol = -1 },
			new CounterCheck { Value = -2999, LastValue = -3000, DecIndex = 3, IncIndex = 2, DecCol = 30, IncCol = 25, DirCol = -1 },
			new CounterCheck { Value = -1999, LastValue = -2000, DecIndex = 2, IncIndex = 1, DecCol = 25, IncCol = 20, DirCol = -1 },
			new CounterCheck { Value = -999, LastValue = -1000, DecIndex = 1, IncIndex = 0, DecCol = 20, IncCol = 15, DirCol = -1 },
			new CounterCheck { Value = 0, LastValue = -1, DecIndex = 0, IncIndex = -1, DecCol = 15, IncCol = -1, DirCol = -1 }
		};

		static void Main(string[] args)
		{
			// validate input parameters
			if (args.Length == 0 || args.Length != 3)
			{
				Console.WriteLine("threads <threads> <max> <threadwait>");
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
			int size = maxThreads * 4;
			columns = Console.WindowWidth;
			rows = ((maxThreads - 1) * 4) / columns;
			stats = rows + 2;
			if (Console.WindowWidth < columns) Console.WindowWidth = columns;
			if (Console.WindowHeight < stats + 1) Console.WindowHeight = stats + 2;
			Console.Clear();
			threadFile = Path.Combine(FilePathRoot, string.Format("threads.{0,4:0000}.txt", pid));
			Console.SetCursorPosition(0, stats);
			//           1         2         3         4         5         6         7
			// 01234567890123456789012345678901234567890123456789012345678901234567890123456789
			// 99,999,999,999 9999 9999 9999 9999 9999 9999 9999 9999 9999 9999
			Console.Write("{0,14:#,##0} {1,4:###0} {2,4:###0} {3,4:###0} {4,4:###0} {5,4:###0} {6,4:###0} {7,4:###0} {8,4:###0} {9,4:###0} {10,4:###0}",
				sum, counters[0], counters[1], counters[2], counters[3], counters[4], counters[5], counters[6], counters[7], counters[8], counters[9]);
			List<int> indexPool = new List<int>();
			Random r = new Random(DateTime.Now.Millisecond);
			for (int i = 0; i < maxThreads; i++)
				indexPool.Add(i);
			while (indexPool.Count > 0)
			{
				int s = r.Next(indexPool.Count);
				int t = indexPool[s];
				indexPool.RemoveAt(s);
				ManualWorker worker = new ManualWorker(maxCounter, t, threadWait);
				Thread thread = new Thread(worker.BusyAsABee);
				threads.Add(thread);
				thread.Start();
			}
			Console.ReadLine();
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
			int delta = wait / 5;
			this.wait = wait + ((wait > 0) ? r.Next(-delta, delta) : 0);
			this.left = (t * 4) % Console.WindowWidth;
			this.top = (t * 4) / Console.WindowWidth;
			values = new List<int>();
			for (int i = 0; i <= limit; i++)
				values.Add(i);
			for (int i = -limit; i <= 0; i++)
				values.Add(i);
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
					Monitor.Enter(Program.syncScreen);
					Console.SetCursorPosition(left, top);
					Console.ForegroundColor = (value >= 0) ? ConsoleColor.Yellow : ConsoleColor.Cyan;
					Console.BackgroundColor = (value >= 0) ? ConsoleColor.Black : ConsoleColor.Black;
					Console.Write("{0,4:###0}", Math.Abs(value));
					Interlocked.Add(ref Program.sum, value);
					Console.SetCursorPosition(0, Program.stats);
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.BackgroundColor = ConsoleColor.Black;
					Console.Write("{0,14:#,##0}", Program.sum);
					Monitor.Enter(Program.syncDb);
					UpdateDatabase(t, value, Program.sum);
					Monitor.Exit(Program.syncDb);
					foreach (CounterCheck check in Program.counterChecks)
					{
						if (value == check.Value && lastValue == check.LastValue)
						{
							if (check.DecIndex != -1)
							{
								Interlocked.Decrement(ref Program.counters[(int)check.DecIndex]);
								Console.SetCursorPosition(check.DecCol, Program.stats);
								Console.Write("{0,4:###0}", Program.counters[(int)check.DecIndex]);
							}
							if (check.IncIndex != -1)
							{
								Interlocked.Increment(ref Program.counters[(int)check.IncIndex]);
								Console.SetCursorPosition(check.IncCol, Program.stats);
								Console.Write("{0,4:###0}", Program.counters[(int)check.IncIndex]);
							}
							break;
						}
					}
					Monitor.Exit(Program.syncScreen);
					lastValue = value;
					if (wait > 0)
						Thread.Sleep(wait);
				}
				Monitor.Enter(Program.syncScreen);
				Console.SetCursorPosition(left, top);
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.BackgroundColor = ConsoleColor.Black;
				Console.Write("    ");
				Monitor.Exit(Program.syncScreen);
			}
			catch
			{
			}
			finally
			{
			}
		}

		private void UpdateDatabase(int t, int value, long sum)
		{
			SqlConnectionStringBuilder b = new SqlConnectionStringBuilder();
			b.IntegratedSecurity = true;
			b.InitialCatalog = "Sandbox";
			b.DataSource = "BRLI-MOBILE-ELO\\SQLExpress";
			string message;
			string query = "";
			try
			{
				using (SqlConnection cn = new SqlConnection(b.ConnectionString))
				{
					cn.Open();
					if (value > 0)
					{
						message = string.Format("In process {0}, thread number {1}, the value is {2} and the sum is {3}", Program.pid, t, value, sum);
						query = string.Format("INSERT [Threads]([PID], [ThreadNumber], [Value], [Message]) VALUES({0}, {1}, {2}, '{3}')",
							Program.pid, t, value, message);
					}
					else
					{
						query = string.Format("DELETE [Threads] WHERE [PID] = {0} AND [ThreadNumber] = {1} AND [Value] = {2}", Program.pid, t, Math.Abs(value));
					}
					using (SqlCommand cmd = new SqlCommand(query, cn))
					{
						cmd.ExecuteNonQuery();
					}
					cn.Close();
				}
			}
			catch (System.Exception ex)
			{
				Console.SetCursorPosition(0, 20);
				Console.WriteLine(query);
				Console.WriteLine(ex.Message);
			}
		}
	}

	class CounterCheck
	{
		public int Value { get; set; }
		public int? LastValue { get; set; }
		public int DecIndex { get; set; }
		public int IncIndex { get; set; }
		public int DecCol { get; set; }
		public int IncCol { get; set; }
		public int DirCol { get; set; }
	}
}

