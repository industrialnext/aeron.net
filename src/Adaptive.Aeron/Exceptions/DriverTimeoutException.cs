﻿/*
 * Copyright 2014 - 2017 Adaptive Financial Consulting Ltd
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0S
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Adaptive.Aeron.Exceptions
{
    /// <summary>
    /// A timeout has occurred while waiting on the media driver responding to an operation.
    /// </summary>
    public class DriverTimeoutException : AeronTimeoutException
    {
        public DriverTimeoutException(string message) : base(message, Category.FATAL)
        {
        }
    }
}