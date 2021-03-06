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

namespace Adaptive.Agrona.Concurrent.Errors
{
    /// <summary>
    /// Callback handler for consuming errors encountered in a <see cref="DistinctErrorLog"/>
    /// </summary>
    /// <param name="observationCount"> the number of times this distinct exception has been recorded.</param>
    /// <param name="firstObservationTimestamp"> time the first observation was recorded.</param>
    /// <param name="lastObservationTimestamp"> time the last observation was recorded.</param>
    /// <param name="encodedException"> String encoding of the exception and stack trace in UTF-8 format.</param>
    public delegate void ErrorConsumer(int observationCount, long firstObservationTimestamp, long lastObservationTimestamp, string encodedException);
}