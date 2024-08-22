using Serilog.Events;
using System;

namespace Seq.Forwarder.Util
{
    public class LogLevelConverter
    {
        public static (int severityNumber, string severityText) ConvertToOpenTelemetry(string serilogLevel)
        {
            switch (serilogLevel.ToLower())
            {
                case "verbose":
                    return (4, "TRACE");
                case "debug":
                    return (8, "DEBUG");
                case "information":
                    return (12, "INFO");
                case "warning":
                    return (16, "WARN");
                case "error":
                    return (20, "ERROR");
                case "fatal":
                    return (24, "FATAL");
                default:
                    throw new ArgumentOutOfRangeException(nameof(serilogLevel), serilogLevel, "Unknown Serilog level");
            }
        }
    }


}
