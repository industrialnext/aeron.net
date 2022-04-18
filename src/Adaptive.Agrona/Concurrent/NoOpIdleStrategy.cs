﻿/*
 * Copyright 2014 - 2017 Adaptive Financial Consulting Ltd
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Adaptive.Agrona.Concurrent
{
    /// <summary>
    /// Low-latency idle strategy to be employed in loops that do significant work on each iteration such that any
    /// work in the idle strategy would be wasteful.
    /// </summary>
    public sealed class NoOpIdleStrategy : IIdleStrategy
    {
        /// <summary>
        /// <b>Note</b>: this implementation will result in no safepoint poll once inlined.
        /// </summary>
        /// <seealso cref="IIdleStrategy" />
        public void Idle(int workCount)
        {
        }

        /// <summary>
        /// <b>Note</b>: this implementation will result in no safepoint poll once inlined.
        /// </summary>
        /// <seealso cref="IIdleStrategy" />
        public void Idle()
        {
        }

        public void Reset()
        {
        }
    }
}