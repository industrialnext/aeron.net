/*
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

using System.Threading;

namespace Adaptive.Agrona.Concurrent
{
    /// <summary>
    /// Busy spin strategy targeted at lowest possible latency. This strategy will monopolise a thread to achieve the lowest
    /// possible latency. Useful for creating bubbles in the execution pipeline of tight busy spin loops with no other logic 
    /// than status checks on progress.
    /// </summary>
    public sealed class BusySpinIdleStrategy : IIdleStrategy
    {
        /// <summary>
        /// <b>Note</b>: this implementation will result in no safepoint poll once inlined.
        /// </summary>
        public void Idle(int workCount)
        {
            if (workCount > 0)
            {
                return;
            }

            Thread.SpinWait(0);
        }

        public void Idle()
        {
            Thread.SpinWait(0);
        }

        public void Reset()
        {
        }
    }
}