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

namespace Adaptive.Agrona.Concurrent
{
    /// <summary>
    /// An Agent is scheduled to do work on a thread on a duty cycle. Each Agent should have a defined role in a system.
    /// 
    /// <see cref="OnStart"/>, <see cref="DoWork"/>, and <see cref="OnClose"/> will all be called by the same thread and in a
    /// threadsafe manner.
    /// 
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// To be overridden by Agents that need to do resource init on start.
        /// 
        /// This method will be called by the agent thread. It will only be called once.
        /// 
        /// <b>Note:</b> Implementations of this method must be idempotent.
        /// 
        /// In Java this is optional to implement (default method) C# doesn't have the same construct for interfaces.
        /// </summary>
        void OnStart();

        /// <summary>
        /// An agent should implement this method to do its work.
        /// 
        /// The return value is used for implementing a backoff strategy that can be employed when no work is
        /// currently available for the agent to process.
        /// 
        /// If the Agent should terminate and close then a <see cref="AgentTerminationException"/> can be thrown.
        /// </summary>
        /// <returns> 0 to indicate no work was currently available, a positive value otherwise. </returns>
        /// <exception cref="Exception"> if an error has occurred </exception>
        int DoWork();

        /// <summary>
        /// To be overridden by Agents that need to do resource cleanup on close.
        /// 
        /// This method will be called after the agent thread has terminated. It will only be called once by a single thread.
        /// 
        /// <b>Note:</b> Implementations of this method must be idempotent.
        /// </summary>
        void OnClose(); // default to do nothing unless you want to handle the notification.

        /// <summary>
        /// Get the name of this agent's role.
        /// </summary>
        /// <returns> the name of this agent's role. </returns>
        string RoleName();
    }
}