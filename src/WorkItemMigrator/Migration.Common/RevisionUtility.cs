using System;

namespace Migration.Common
{
    public static class RevisionUtility
    {
        private static TimeSpan _deltaTime = TimeSpan.FromMilliseconds(50);

        public static DateTime NextValidDeltaRev(DateTime current, DateTime? next = null)
        {
            if (next == null || current + _deltaTime < next)
                return current + _deltaTime;

            TimeSpan diff = next.Value - current;
            var middle = new TimeSpan(diff.Ticks / 2);
            return current + middle;
        }
    }
}