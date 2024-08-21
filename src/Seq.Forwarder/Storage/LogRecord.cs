using Seq.Forwarder.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Seq.Forwarder.Storage
{
    public class LogRecord
    {
        [JsonPropertyName("timeUnixNano")]
        public string Timestamp { get; private set; }

        [JsonPropertyName("observedTimeUnixNano")]
        public string TimestampObserved { get; private set; }

        [JsonIgnore]
        public DateTime ParsedTimestamp { get; private set; }

        [JsonPropertyName("severityText")]
        public string SeverityText { get; private set; }

        [JsonIgnore]
        public int SeverityNumber { get; private set; }

        [JsonPropertyName("body")]
        public Dictionary<string, string?> Body { get; private set; }

        [JsonPropertyName("traceId")]
        public string TraceId { get; private set; }

        [JsonPropertyName("spanId")]
        public string SpanId { get; private set; }

        public LogRecord(byte[] entry)
        {
            string? timestampStr = ExtractJsonValue(entry, "Timestamp");
            if (DateTimeOffset.TryParse(timestampStr, out DateTimeOffset parsedTimestamp))
            {
                Timestamp = ((DateTimeOffset)parsedTimestamp).ToUnixTimeNanoseconds().ToString();
            }
            else
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeNanoseconds().ToString();
            }
            TimestampObserved = DateTimeOffset.UtcNow.ToUnixTimeNanoseconds().ToString();
            string? level = ExtractJsonValue(entry, "Level");
            (SeverityNumber, SeverityText) = level != null ? LogLevelConverter.ConvertToOpenTelemetry(level) : (12, "INFO");

            string? messageTemplate = ExtractJsonValue(entry, "MessageTemplate");
            Body = new Dictionary<string, string?> { { "stringValue", messageTemplate } };

            TraceId = "";
            SpanId = "EEE19B7EC3C1B174";
        }

        private string? ExtractJsonValue(byte[] byteArray, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes($"\"{key}\"");

            int keyIndex = SearchBytePattern(byteArray, keyBytes);
            if (keyIndex == -1) return null;

            int valueStartIndex = keyIndex + keyBytes.Length;
            while (valueStartIndex < byteArray.Length && (byteArray[valueStartIndex] == (byte)':'))
            {
                valueStartIndex++;
            }

            while (valueStartIndex < byteArray.Length && (byteArray[valueStartIndex] == (byte)' ' || byteArray[valueStartIndex] == (byte)'"'))
            {
                valueStartIndex++;
            }

            int valueEndIndex = valueStartIndex;
            while (valueEndIndex < byteArray.Length && byteArray[valueEndIndex] != (byte)',' && byteArray[valueEndIndex] != (byte)'}' && byteArray[valueEndIndex] != (byte)'"')
            {
                valueEndIndex++;
            }

            return Encoding.UTF8.GetString(byteArray, valueStartIndex, valueEndIndex - valueStartIndex);
        }

        private int SearchBytePattern(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
