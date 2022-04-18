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

using System;

namespace Adaptive.Agrona
{
    /// <summary>
    /// Callback to notify of an error that has occurred when processing an operation or event.
    /// 
    /// This method is assumed non-throwing, so rethrowing the exception or triggering further exceptions would be a bug.
    /// 
    /// <param name="exception"> exception that occurred while processing an operation or event.</param>
    /// </summary>
    public delegate void ErrorHandler(Exception exception);
}