using System.Linq;

namespace Seq.Forwarder.SerilogJsonConverter
{
    static class JsonFields
    {
        // Constants representing normal JSON field names
        public const string Timestamp = "Timestamp";
        public const string MessageTemplate = "MessageTemplate";
        public const string Level = "Level";
        public const string Exception = "Exception";
        public const string Renderings = "Renderings";
        public const string EventId = "EventId";
        public const string Message = "Message";
        public const string TraceId = "TraceId";
        public const string SpanId = "SpanId";

        // Array of all recognized JSON field names
        public static readonly string[] All =
        {
            Timestamp, MessageTemplate, Level, Exception, Renderings, EventId, Message, TraceId, SpanId
        };

        // Method to check if a field name is unrecognized
        public static bool IsUnrecognized(string name)
        {
            return !All.Contains(name);
        }
    }
}
