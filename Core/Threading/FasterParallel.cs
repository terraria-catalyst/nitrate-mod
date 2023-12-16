using ReLogic.Threading;
using System;
using System.Threading;

namespace Nitrate.Core.Threading;

/// <summary>
///     A faster reimplementation of <see cref="FastParallel"/>.
/// </summary>
/// <seealso cref="FastParallel"/>
[ApiReleaseCandidate("1.0.0")]
internal static class FasterParallel
{
    /// <summary>
    ///     A faster reimplementation of <see cref="FastParallel.For"/>.
    /// </summary>
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
        private readonly ParallelForAction _action;
        private readonly int _fromInclusive;
        private readonly int _toExclusive;
        private readonly object? _context;
        private readonly CountdownEvent _countdownEvent;

        public RangeTask(ParallelForAction action, int fromInclusive, int toExclusive, object? context, CountdownEvent countdownEvent)
        {
            _action = action;
            _fromInclusive = fromInclusive;
            _toExclusive = toExclusive;
            _context = context;
            _countdownEvent = countdownEvent;
        }

        public void Invoke()
        {
            try
            {
                if (_fromInclusive == _toExclusive)
                {
                    return;
                }

                _action(_fromInclusive, _toExclusive, _context);
            }
            finally
            {
                _countdownEvent.Signal();
            }
        }
    }
}