using ReLogic.Threading;
using System;
using System.Threading;

namespace Zenith.Core.Features.Threading;

public static class FasterParallel
{
    public static void For(int fromInclusive, int toExclusive, ParallelForAction callback, object? context = null)
    {
        int rangeLength = toExclusive - fromInclusive;

        if (rangeLength == 0)
        {
            return;
        }

        int initialCount = Math.Min(Math.Max(1, Environment.ProcessorCount - 1), rangeLength);
        int rangeLengthPerTask = rangeLength / initialCount;
        int remainder = rangeLength % initialCount;
        CountdownEvent countdownEvent = new(initialCount);
        int currentRangeStart = toExclusive;

        for (int i = initialCount - 1; i >= 0; --i)
        {
            int rangeLengthForTask = rangeLengthPerTask;

            if (i < remainder)
            {
                rangeLengthForTask++;
            }

            currentRangeStart -= rangeLengthForTask;
            int rangeStart = currentRangeStart;
            int rangeEnd = rangeStart + rangeLengthForTask;
            RangeTask rangeTask = new(callback, rangeStart, rangeEnd, context, countdownEvent);

            if (i < 1)
            {
                InvokeTask(rangeTask);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(InvokeTask, rangeTask);
            }
        }

        countdownEvent.Wait();
    }

    private static void InvokeTask(object? context) => (context as RangeTask)?.Invoke();

    private class RangeTask
    {
        private readonly ParallelForAction action;
        private readonly int fromInclusive;
        private readonly int toExclusive;
        private readonly object? context;
        private readonly CountdownEvent countdownEvent;

        public RangeTask(ParallelForAction action, int fromInclusive, int toExclusive, object? context, CountdownEvent countdownEvent)
        {
            this.action = action;
            this.fromInclusive = fromInclusive;
            this.toExclusive = toExclusive;
            this.context = context;
            this.countdownEvent = countdownEvent;
        }

        public void Invoke()
        {
            try
            {
                if (fromInclusive == toExclusive)
                {
                    return;
                }

                action(fromInclusive, toExclusive, context);
            }
            finally
            {
                countdownEvent.Signal();
            }
        }
    }
}