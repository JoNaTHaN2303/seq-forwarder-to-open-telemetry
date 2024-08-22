// Copyright Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Newtonsoft.Json;
using Seq.Forwarder.Cryptography;

// ReSharper disable UnusedMember.Global, AutoPropertyCanBeMadeGetOnly.Global

namespace Seq.Forwarder.Config
{
    public class SeqForwarderOutputConfig
    {
        //public string ServerUrl2 { get; set; } = "http://localhost:5341";

        //ToDo make sure this can be dynamic, 
        //public string ServerUrl { get; set; } = "http://localhost:4318";

        private string _serverUrl = "http://localhost:5341"; // Default to Seq
        public string ServerUrl
        {
            get => _serverUrl;
            private set => _serverUrl = value;
        }

        public ulong EventBodyLimitBytes { get; set; } = 256 * 1024;
        public ulong RawPayloadLimitBytes { get; set; } = 10 * 1024 * 1024;
        public uint? PooledConnectionLifetimeMilliseconds { get; set; } = null;

        const string ProtectedDataPrefix = "pd.";

        public string? ApiKey { get; set; }

        public string? GetApiKey(IStringDataProtector dataProtector)
        {
            if (string.IsNullOrWhiteSpace(ApiKey))
                return null;

            if (!ApiKey.StartsWith(ProtectedDataPrefix))
                return ApiKey;

            return dataProtector.Unprotect(ApiKey.Substring(ProtectedDataPrefix.Length));
        }

        public void SetApiKey(string? apiKey, IStringDataProtector dataProtector)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ApiKey = null;
                return;
            }

            ApiKey = $"{ProtectedDataPrefix}{dataProtector.Protect(apiKey)}";
        }

        // New property to set the logging backend and adjust ServerUrl dynamically
        private string _loggingBackend = "Seq"; // Default to Seq

        public string LoggingBackend
        {
            get => _loggingBackend;
            set
            {
                _loggingBackend = value;
                switch (_loggingBackend)
                {
                    case "OTel":
                        ServerUrl = "http://localhost:4318"; // Default OTel server URL
                        break;
                    case "Seq":
                        ServerUrl = "http://localhost:5341"; // Default Seq server URL
                        break;
                    default:
                        throw new ArgumentException("Invalid logging backend. Use either 'Seq' or 'OTel'.");
                }
            }
        }
    }
}
