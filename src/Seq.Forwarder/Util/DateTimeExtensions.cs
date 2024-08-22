using System;

namespace Seq.Forwarder.Util
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeNanoseconds(this DateTimeOffset dateTimeOffset)
        {
            // Get Unix time in seconds
            long unixTimeSeconds = dateTimeOffset.ToUnixTimeSeconds();

            // Convert to nanoseconds
            long unixTimeNanoseconds = unixTimeSeconds * 1_000_000_000;

            // Add the fractional seconds as nanoseconds
            long additionalNanoseconds = dateTimeOffset.Millisecond * 1_000_000 +
                                         (dateTimeOffset.Microsecond * 1_000);

            return unixTimeNanoseconds + additionalNanoseconds;
        }
    }

}
