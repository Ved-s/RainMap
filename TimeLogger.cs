using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RainMap
{
    public class TimeLogger<T> where T : notnull
    {
        public ReadOnlyDictionary<T, TimeSpan> Times { get; }

        Dictionary<T, TimeSpan> Timespans = new();
        T? CurrentTime;
        bool AddTime;
        Stopwatch Stopwatch = new();

        public TimeLogger()
        {
            Times = new(Timespans);
        }

        public void ResetWatches()
        {
            foreach (T t in Timespans.Keys)
                Timespans[t] = TimeSpan.Zero;
        }

        public void StartWatch(T id, bool add = false)
        {
            FinishWatch();

            AddTime = add;
            CurrentTime = id;
            Stopwatch.Restart();
        }

        public void FinishWatch()
        {
            if (CurrentTime is null || !Stopwatch.IsRunning)
                return;

            Stopwatch.Stop();
            if (AddTime && Timespans.ContainsKey(CurrentTime))
                Timespans[CurrentTime] += Stopwatch.Elapsed;
            else
                Timespans[CurrentTime] = Stopwatch.Elapsed;

            CurrentTime = default;
            AddTime = false;
        }
    }
}
