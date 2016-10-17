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
        //public static readonly string FilePathRoot = "c:\\temp\\threads";
        static readonly List<Thread> Threads = new List<Thread>();
        public static object SyncScreen = new object();
        public static long Total = 0;
        public static int MaxThreads, MaxCounter, ThreadWait;
        public static int Rows, Columns, Stats;
        public static Random Rand;
        public static long Sum = 0;
        public static int ThreadCount = 0;

        static void Main(string[] args)
        {
            try
            {
                Console.CursorVisible = false;
                // validate input parameters
                if (args.Length == 0 || args.Length != 3)
                {
                    Console.WriteLine("threads <threads> <max> <threadwait>");
                    return;
                }
                Rand = new Random(DateTime.Now.Day);
                MaxThreads = (int.Parse(args[0]) > 1000) ? 1000 : int.Parse(args[0]);
                MaxCounter = (int.Parse(args[1]) > 10000) ? 10000 : int.Parse(args[1]);
                ThreadWait = (int.Parse(args[2]) > 10000) ? 10000 : int.Parse(args[2]);
                int size = MaxThreads * 4;
                Columns = Console.WindowWidth;
                Rows = ((MaxThreads - 1) * 4) / Columns;
                Stats = Rows + 1;
                if (Console.WindowWidth < Columns) Console.WindowWidth = Columns;
                if (Console.WindowHeight < Stats + 1) Console.WindowHeight = Stats + 2;
                Console.Clear();
                var indexPool = new List<int>();
                var r = new Random(DateTime.Now.Millisecond);
                for (var i = 0; i < MaxThreads; i++)
                    indexPool.Add(i);
                while (indexPool.Count > 0)
                {
                    var s = r.Next(indexPool.Count);
                    var t = indexPool[s];
                    indexPool.RemoveAt(s);
                    var worker = new ManualWorker(MaxCounter, t, ThreadWait);
                    var thread = new Thread(worker.BusyAsABee);
                    Threads.Add(thread);
                    thread.Start();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Console.CursorVisible = true;
            }
            Console.ReadLine();
        }
    }

    class ManualWorker
    {
        private readonly int _wait;
        private readonly int _left;
        private readonly int _top;
        private readonly List<int> _values;

        static ManualWorker()
        {
        }

        public ManualWorker(int limit, int t, int wait)
        {
            Interlocked.Add(ref Program.ThreadCount, 1);
            Monitor.Enter(Program.SyncScreen);
            Console.Title = string.Format("{0} - {1:#,##0}", Program.ThreadCount, Program.Sum);
            Monitor.Exit(Program.SyncScreen);
            var r = new Random(DateTime.Now.Millisecond);
            var delta = wait / 5;
            _wait = wait + ((wait > 0) ? r.Next(-delta, delta) : 0);
            _wait = (_wait < 0) ? 0 : _wait;
            _left = (t * 4) % Console.WindowWidth;
            _top = (t * 4) / Console.WindowWidth;
            _values = new List<int>();
            for (var i = 0; i < limit; i++)
                _values.Add(i);
            for (var i = -limit + 1; i <= 0; i++)
                _values.Add(i);
            Monitor.Enter(Program.SyncScreen);
            Flash(_left, _top, "####");
            Console.Title = string.Format("{0} - {1:#,##0}", Program.ThreadCount, Program.Sum);
            Monitor.Exit(Program.SyncScreen);
        }

        private void Flash(int left, int top, string value)
        {
            Console.SetCursorPosition(left, top);
            Console.Write(value);
        }

        public void BusyAsABee()
        {
            try
            {
                while (_values.Count > 0)
                {
                    int value = _values[0];
                    _values.RemoveAt(0);
                    Monitor.Enter(Program.SyncScreen);
                    Flash(_left, _top, string.Format("{0,4:###0}", Math.Abs(value)));
                    Interlocked.Add(ref Program.Sum, value);
                    Console.Title = string.Format("{0} - {1:#,##0}", Program.ThreadCount, Program.Sum);
                    Monitor.Exit(Program.SyncScreen);
                    if (_wait > 0)
                        Thread.Sleep(_wait);
                }
                Monitor.Enter(Program.SyncScreen);
                Flash(_left, _top, "----");
                Monitor.Exit(Program.SyncScreen);
            }
            catch
            {
                Monitor.Enter(Program.SyncScreen);
                Flash(_left, _top, "%%%%");
                Monitor.Exit(Program.SyncScreen);
            }
            finally
            {
                Interlocked.Add(ref Program.ThreadCount, -1);
                Monitor.Enter(Program.SyncScreen);
                Console.Title = string.Format("{0} - {1:#,##0}", Program.ThreadCount, Program.Sum);
                Monitor.Exit(Program.SyncScreen);
            }
        }
    }
}

