using Seq.Forwarder.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Seq.Forwarder.Storage
{
    public class OtelLogRecord
    {
        [JsonPropertyName("timeUnixNano")]
        public string Timestamp { get; private set; }

        [JsonPropertyName("observedTimeUnixNano")]
        public string TimestampObserved { get; private set; }

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

        [JsonPropertyName("attributes")]
        public Dictionary<string, string?>? Attributes { get; private set; }

        public OtelLogRecord(byte[] entry)
        {
            if(entry == null || entry.Length == 0)
                throw new ArgumentException("JSON data cannot be null or empty", nameof(entry));

            // Convert byte array to string
            string jsonString = Encoding.UTF8.GetString(entry);


            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                var root = doc.RootElement;

                // Extract Timestamp
                if (root.TryGetProperty("Timestamp", out JsonElement timestampElement))
                {
                    string? timestampStr = timestampElement.GetString();
                    if (DateTimeOffset.TryParse(timestampStr, out DateTimeOffset parsedTimestamp))
                    {
                        Timestamp = parsedTimestamp.ToUnixTimeNanoseconds().ToString();
                    }
                    else
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeNanoseconds().ToString();
                    }
                }
                else
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeNanoseconds().ToString();
                }

                // Set the observed timestamp
                TimestampObserved = DateTimeOffset.UtcNow.ToUnixTimeNanoseconds().ToString();

                // Extract Level (Handle missing field by using nullable string)
                string? level = null;
                if (root.TryGetProperty("Level", out JsonElement levelElement))
                {
                    level = levelElement.GetString();
                }
                (SeverityNumber, SeverityText) = level != null ? LogLevelConverter.ConvertToOpenTelemetry(level) : (12, "INFO");

                // Extract Properties (if they exist)
                Dictionary<string, string?> properties = new();
                if (root.TryGetProperty("Properties", out JsonElement propertiesElement))
                {
                    foreach (JsonProperty prop in propertiesElement.EnumerateObject())
                    {
                        properties[prop.Name] = prop.Value.ToString();
                    }
                }

                // Extract MessageTemplate and replace placeholders with properties
                if (root.TryGetProperty("MessageTemplate", out JsonElement messageTemplateElement))
                {
                    string? messageTemplate = messageTemplateElement.GetString();

                    if (messageTemplate != null)
                    {
                        foreach (var property in properties)
                        {
                            messageTemplate = messageTemplate.Replace($"{{{property.Key}}}", property.Value);
                        }
                    }

                    Body = new Dictionary<string, string?> { { "stringValue", messageTemplate } };
                }
                else
                {
                    Body = new Dictionary<string, string?> { { "stringValue", null } };
                }

                // Extract additional attributes, including exceptions
                Attributes = new Dictionary<string, string?>();
                if (root.TryGetProperty("Exception", out JsonElement exceptionElement))
                {
                    if (exceptionElement.ValueKind == JsonValueKind.String)
                    {
                        // Exception is a string, parse it
                        string exceptionString = exceptionElement.GetString() ?? string.Empty;

                        // Extract exception message
                        int firstLineEndIndex = exceptionString.IndexOf("\r\n");
                        if (firstLineEndIndex > 0)
                        {
                            Attributes["exception.message"] = exceptionString.Substring(0, firstLineEndIndex);
                        }
                        else
                        {
                            Attributes["exception.message"] = exceptionString;
                        }

                        // Extract exception type
                        int typeEndIndex = exceptionString.IndexOf(':');
                        if (typeEndIndex > 0)
                        {
                            Attributes["exception.type"] = exceptionString.Substring(0, typeEndIndex).Trim();
                        }

                        // Extract stack trace
                        int stackTraceStartIndex = exceptionString.IndexOf("   at ");
                        if (stackTraceStartIndex >= 0)
                        {
                            Attributes["exception.stacktrace"] = exceptionString.Substring(stackTraceStartIndex).Trim();
                        }
                    }
                }

                TraceId = string.Empty;
                SpanId = string.Empty;
            }
        }
    }
}
