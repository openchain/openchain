﻿// Copyright 2015 Coinprism, Inc.
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

using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace Openchain.Server.Models
{
    public class DateLogger : ILogger
    {
        private readonly ILogger logger;

        public DateLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this.logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return this.logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string date = DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture);

            this.logger.Log(
                logLevel,
                eventId,
                state,
                exception,
                (logState, ex) => $"[{date}] {formatter(logState, ex)}");
        }
    }
}
