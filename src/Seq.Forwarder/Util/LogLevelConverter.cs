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
                    return (5, "TRACE");
                case "debug":
                    return (9, "DEBUG");
                case "information":
                    return (13, "INFO");
                case "warning":
                    return (17, "WARN");
                case "error":
                    return (21, "ERROR");
                case "fatal":
                    return (23, "FATAL");
                default:
                    throw new ArgumentOutOfRangeException(nameof(serilogLevel), serilogLevel, "Unknown Serilog level");
            }
        }
    }


}
