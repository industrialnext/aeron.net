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

namespace Adaptive.Agrona
{
    public enum ByteOrder
    {
        /// <summary>
        /// Constant denoting big-endian byte order.  In this order, the bytes of a
        /// multibyte value are ordered from most significant to least significant.
        /// </summary>
        BigEndian,

        /// <summary>
        /// Constant denoting little-endian byte order.  In this order, the bytes of
        /// a multibyte value are ordered from least significant to most
        /// significant.
        /// </summary>
        LittleEndian
    }
}