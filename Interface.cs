using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AuthorMigrationCache
{
    public static class Interface
    {
        private static Timer _timer;
        private static byte _currentValue = 0;
        private static int _completedArticles = 0;
        private static int _totalArticles;
        private static readonly char[] _WHEEL = new char[] { '/', '-', '\\' };

        public static void Start(int totalArticles)
        {
            _totalArticles = totalArticles;

            _timer = new Timer()
            {
                Interval = 500,
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += Timer_Elapsed;
        }

        public static void Stop()
        {
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Enabled = false;
        }

        public static void IncrementCompleted()
        {
            ++_completedArticles;
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.Clear();

            if (_currentValue == 2)
                _currentValue = 0;
            else
                ++_currentValue;

            double percentComplete = _completedArticles * 100.0d / _totalArticles;

            Console.WriteLine($"{_WHEEL[_currentValue]} Working {_WHEEL[_currentValue]}");
            Console.WriteLine(percentComplete.ToString("N2") + "% complete");
        }
    }
}
