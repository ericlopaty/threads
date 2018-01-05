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
        public static readonly string FileName = "threads.txt";
        public static readonly DateTime StartTime = DateTime.Now;
        public static object SyncScreen = new object();
        public static object SyncFile = new object();
        public static long Total = 0;
        public static int MaxThreads, MaxCounter, ThreadWait;
        public static int Rows, Columns;
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
                MaxCounter = (int.Parse(args[1]) > 1000000) ? 1000000 : int.Parse(args[1]);
                ThreadWait = (int.Parse(args[2]) > 10000) ? 10000 : int.Parse(args[2]);
                int size = MaxThreads * 6;
                Columns = Console.WindowWidth;
                Rows = ((MaxThreads - 1) * 6) / Columns + 2;
                if (Console.WindowWidth < Columns) Console.WindowWidth = Columns;
                if (Console.WindowHeight < Rows) Console.WindowHeight = Rows;
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
        private int _threadNumber;
        private int _startDelay;
        private DateTime _startTime;
        private int _totalTime;
        private int _avgWait;
        private int _itemCount;

        static ManualWorker()
        {
        }

        public ManualWorker(int limit, int t, int wait)
        {
            Interlocked.Add(ref Program.ThreadCount, 1);
            _threadNumber = t;
            Monitor.Enter(Program.SyncScreen);
            Console.Title = $"{Program.ThreadCount:N0} - {Program.Sum:N0}";
            Monitor.Exit(Program.SyncScreen);
            var r = new Random(DateTime.Now.Millisecond);
            var delta = wait / 5;
            _wait = wait + ((wait > 0) ? r.Next(-delta, delta) : 0);
            _wait = (_wait < 0) ? 0 : _wait;
            _left = (t * 6) % Console.WindowWidth;
            _top = (t * 6) / Console.WindowWidth;
            _values = new List<int>();
            for (var i = 0; i < limit; i++)
                _values.Add(i);
            for (var i = -limit + 1; i <= 0; i++)
                _values.Add(i);
            _itemCount = _values.Count;
            Monitor.Enter(Program.SyncScreen);
            Flash(_left, _top, "######");
            Console.Title = $"{Program.ThreadCount:N0} - {Program.Sum:N0}";
            Monitor.Exit(Program.SyncScreen);
        }

        private void Log(string message)
        {
            Monitor.Enter(Program.SyncFile);
            File.AppendAllText(Program.FileName, message + "\n");
            Monitor.Exit(Program.SyncFile);
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
                _startTime = DateTime.Now;
                _startDelay = (int)_startTime.Subtract(Program.StartTime).TotalMilliseconds;
                while (_values.Count > 0)
                {
                    int value = _values[0];
                    _values.RemoveAt(0);
                    Monitor.Enter(Program.SyncScreen);
                    Flash(_left, _top, $"{Math.Abs(value),6:#####0}");
                    Interlocked.Add(ref Program.Sum, value);
                    Console.Title = $"{Program.ThreadCount:N0} - {Program.Sum:N0}";
                    Monitor.Exit(Program.SyncScreen);
                    if (_wait > 0)
                        Thread.Sleep(_wait);
                }
                _totalTime = (int)DateTime.Now.Subtract(_startTime).TotalMilliseconds;
                _avgWait = _totalTime / _itemCount;
                Monitor.Enter(Program.SyncScreen);
                Flash(_left, _top, "      ");
                Monitor.Exit(Program.SyncScreen);
                string message = string.Format(
                    "Thread {0,5:#,##0} Delay {1,8:#,##0} Items {2,6:#,##0} Wait {3,5:#,##0} Avg Wait {4,5:#,##0} Expected Total {5,8:#,##0} Actual Total {6,8:#,##0}",
                    _threadNumber, _startDelay, _itemCount, _wait, _avgWait, _itemCount * _wait, _totalTime);
                Log(message);
            }
            catch
            {
                Monitor.Enter(Program.SyncScreen);
                Flash(_left, _top, "%%%%%%");
                Monitor.Exit(Program.SyncScreen);
            }
            finally
            {
                Interlocked.Add(ref Program.ThreadCount, -1);
                Monitor.Enter(Program.SyncScreen);
                Console.Title = $"{Program.ThreadCount:N0} - {Program.Sum:N0}";
                Monitor.Exit(Program.SyncScreen);
            }
        }
    }
}

