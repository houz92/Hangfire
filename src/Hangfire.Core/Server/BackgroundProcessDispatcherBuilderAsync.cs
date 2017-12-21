﻿// This file is part of Hangfire.
// Copyright © 2017 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Processing;

namespace Hangfire.Server
{
    public sealed class BackgroundProcessDispatcherBuilderAsync : IBackgroundProcessDispatcherBuilder
    {
        private readonly int _maxConcurrency;
        private readonly bool _ownsScheduler;
        private readonly Func<TaskScheduler> _taskScheduler;
        private readonly IBackgroundProcessAsync _process;

        public BackgroundProcessDispatcherBuilderAsync(
            [NotNull] IBackgroundProcessAsync process,
            [NotNull] Func<TaskScheduler> taskScheduler,
            int maxConcurrency,
            bool ownsScheduler)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            if (taskScheduler == null) throw new ArgumentNullException(nameof(taskScheduler));
            if (maxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

            _process = process;
            _taskScheduler = taskScheduler;
            _maxConcurrency = maxConcurrency;
            _ownsScheduler = ownsScheduler;
        }

        public IBackgroundDispatcher Create(BackgroundProcessContext context, BackgroundProcessingServerOptions options)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new BackgroundDispatcherAsync(
                new BackgroundExecution(context.CancellationToken, context.AbortToken, new BackgroundExecutionOptions
                {
                    Name = _process.GetType().Name,
                    RetryDelay = BackgroundExecutionOptions.GetBackOffMultiplier
                }),
                ExecuteProcess,
                Tuple.Create(_process, context),
                _taskScheduler(),
                _maxConcurrency,
                _ownsScheduler);
        }

        public override string ToString()
        {
            return _process.GetType().Name;
        }

        private static Task ExecuteProcess(object state)
        {
            var context = (Tuple<IBackgroundProcessAsync, BackgroundProcessContext>)state;
            return context.Item1.ExecuteAsync(context.Item2);
        }
    }
}
