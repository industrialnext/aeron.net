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
using Adaptive.Aeron.Exceptions;
using Adaptive.Aeron.LogBuffer;
using Adaptive.Aeron.Protocol;
using Adaptive.Agrona;
using Adaptive.Agrona.Concurrent;
using FakeItEasy;
using NUnit.Framework;

namespace Adaptive.Aeron.Tests.LogBuffer
{
    [TestFixture]
    public class TermAppenderTest
    {
        private const int TermBufferLength = LogBufferDescriptor.TERM_MIN_LENGTH;
        private static readonly int MetaDataBufferLength = LogBufferDescriptor.LOG_META_DATA_LENGTH;
        private const int MaxFrameLength = 1024;
        private const int MaxPayloadLength = MaxFrameLength - DataHeaderFlyweight.HEADER_LENGTH;
        private const int PartionIndex = 0;
        private static readonly int TermTailCounterOffset = LogBufferDescriptor.TERM_TAIL_COUNTERS_OFFSET + PartionIndex * BitUtil.SIZE_OF_LONG;
        private static UnsafeBuffer _defaultHeader;
        private const int TermID = 7;
        private const long RV = 7777;

        private static ReservedValueSupplier RVS = (termBuffer, termOffset, frameLength) => RV;

        private UnsafeBuffer _termBuffer;
        private UnsafeBuffer _logMetaDataBuffer;
        private HeaderWriter _headerWriter;

        private TermAppender _termAppender;

        [SetUp]
        public void SetUp()
        {
            _defaultHeader = new UnsafeBuffer(new byte[DataHeaderFlyweight.HEADER_LENGTH]);

            var buffer = new UnsafeBuffer(new byte[TermBufferLength]);

            _termBuffer = A.Fake<UnsafeBuffer>(x => x.Wrapping(buffer));
            _logMetaDataBuffer = A.Fake<UnsafeBuffer>(x => x.Wrapping(new UnsafeBuffer(new byte[MetaDataBufferLength])));
            _headerWriter = A.Fake<HeaderWriter>(x => x.Wrapping(new HeaderWriter(DataHeaderFlyweight.CreateDefaultHeader(0, 0, TermID))));

            A.CallTo(() => _termBuffer.Capacity).Returns(TermBufferLength);
            A.CallTo(() => _termBuffer.BufferPointer).Returns(buffer.BufferPointer);
            A.CallTo(() => _logMetaDataBuffer.Capacity).Returns(MetaDataBufferLength);

            _termAppender = new TermAppender(_termBuffer, _logMetaDataBuffer, PartionIndex);
        }

        [Test]
        public void ShouldAppendFrameToEmptyLog()
        {
            int headerLength = _defaultHeader.Capacity;
            UnsafeBuffer buffer = new UnsafeBuffer(new byte[128]);
            const int msgLength = 20;
            int frameLength = msgLength + headerLength;
            int alignedFrameLength = BitUtil.Align(frameLength, FrameDescriptor.FRAME_ALIGNMENT);
            const int tail = 0;

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tail));

            Assert.AreEqual(alignedFrameLength, _termAppender.AppendUnfragmentedMessage(_headerWriter, buffer, 0, msgLength, RVS, TermID));

            Assert.AreEqual(LogBufferDescriptor.PackTail(TermID, tail + alignedFrameLength), LogBufferDescriptor.RawTailVolatile(_logMetaDataBuffer, PartionIndex));

            A.CallTo(() => _headerWriter.Write(_termBuffer, tail, frameLength, TermID)).MustHaveHappened()
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength, buffer, 0, msgLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail, frameLength)).MustHaveHappened());
        }

        [Test]
        public void ShouldAppendFrameTwiceToLog()
        {
            int headerLength = _defaultHeader.Capacity;
            UnsafeBuffer buffer = new UnsafeBuffer(new byte[128]);
            const int msgLength = 20;
            int frameLength = msgLength + headerLength;
            int alignedFrameLength = BitUtil.Align(frameLength, FrameDescriptor.FRAME_ALIGNMENT);
            int tail = 0;

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tail));

            Assert.AreEqual(alignedFrameLength, _termAppender.AppendUnfragmentedMessage(_headerWriter, buffer, 0, msgLength, RVS, TermID));
            Assert.AreEqual(alignedFrameLength * 2, _termAppender.AppendUnfragmentedMessage(_headerWriter, buffer, 0, msgLength, RVS, TermID));

            Assert.AreEqual(LogBufferDescriptor.PackTail(TermID, tail + alignedFrameLength * 2), LogBufferDescriptor.RawTailVolatile(_logMetaDataBuffer, PartionIndex));

            A.CallTo(() => _headerWriter.Write(_termBuffer, tail, frameLength, TermID)).MustHaveHappened()
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength, buffer, 0, msgLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail, frameLength)).MustHaveHappened())
                .Then(A.CallTo(() => _headerWriter.Write(_termBuffer, alignedFrameLength, frameLength, TermID)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutBytes(alignedFrameLength + headerLength, buffer, 0, msgLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(alignedFrameLength + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(alignedFrameLength, frameLength)).MustHaveHappened());
        }

        [Test]
        public void ShouldPadLogWhenAppendingWithInsufficientRemainingCapacity()
        {
            const int msgLength = 120;
            int headerLength = _defaultHeader.Capacity;
            int requiredFrameSize = BitUtil.Align(headerLength + msgLength, FrameDescriptor.FRAME_ALIGNMENT);
            int tailValue = TermBufferLength - BitUtil.Align(msgLength, FrameDescriptor.FRAME_ALIGNMENT);
            UnsafeBuffer buffer = new UnsafeBuffer(new byte[128]);
            int frameLength = TermBufferLength - tailValue;

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tailValue));

            Assert.AreEqual(TermAppender.FAILED, _termAppender.AppendUnfragmentedMessage(_headerWriter, buffer, 0, msgLength, RVS, TermID));

            Assert.AreEqual(LogBufferDescriptor.PackTail(TermID, tailValue + requiredFrameSize), LogBufferDescriptor.RawTailVolatile(_logMetaDataBuffer, PartionIndex));

            A.CallTo(() => _headerWriter.Write(_termBuffer, tailValue, frameLength, TermID)).MustHaveHappened()
                .Then(A.CallTo(() => _termBuffer.PutShort(FrameDescriptor.TypeOffset(tailValue), FrameDescriptor.PADDING_FRAME_TYPE)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tailValue, frameLength)).MustHaveHappened());
        }

        [Test]
        public void ShouldFragmentMessageOverTwoFrames()
        {
            int msgLength = MaxPayloadLength + 1;
            int headerLength = _defaultHeader.Capacity;
            int frameLength = headerLength + 1;
            int requiredCapacity = BitUtil.Align(headerLength + 1, FrameDescriptor.FRAME_ALIGNMENT) + MaxFrameLength;
            UnsafeBuffer buffer = new UnsafeBuffer(new byte[msgLength]);
            int tail = 0;

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tail));

            Assert.AreEqual(requiredCapacity, _termAppender.AppendFragmentedMessage(_headerWriter, buffer, 0, msgLength, MaxPayloadLength, RVS, TermID));

            Assert.AreEqual(LogBufferDescriptor.PackTail(TermID, tail + requiredCapacity), LogBufferDescriptor.RawTailVolatile(_logMetaDataBuffer, PartionIndex));

            A.CallTo(() => _headerWriter.Write(_termBuffer, tail, MaxFrameLength, TermID)).MustHaveHappened()
                .Then(A.CallTo(() => _termBuffer.PutBytes(tail + headerLength, buffer, 0, MaxPayloadLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutByte(FrameDescriptor.FlagsOffset(tail), FrameDescriptor.BEGIN_FRAG_FLAG)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail, MaxFrameLength)).MustHaveHappened())
                .Then(A.CallTo(() => _headerWriter.Write(_termBuffer, MaxFrameLength, frameLength, TermID)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutBytes(MaxFrameLength + headerLength, buffer, MaxPayloadLength, 1)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutByte(FrameDescriptor.FlagsOffset(MaxFrameLength), FrameDescriptor.END_FRAG_FLAG)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(MaxFrameLength + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(MaxFrameLength, frameLength)).MustHaveHappened());
        }

        [Test]
        public void ShouldClaimRegionForZeroCopyEncoding()
        {
            int headerLength = _defaultHeader.Capacity;
            const int msgLength = 20;
            int frameLength = msgLength + headerLength;
            int alignedFrameLength = BitUtil.Align(frameLength, FrameDescriptor.FRAME_ALIGNMENT);
            const int tail = 0;
            BufferClaim bufferClaim = new BufferClaim();

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tail));

            Assert.AreEqual(alignedFrameLength, _termAppender.Claim(_headerWriter, msgLength, bufferClaim, TermID));

            Assert.AreEqual(tail + headerLength, bufferClaim.Offset);
            Assert.AreEqual(msgLength, bufferClaim.Length);

            Assert.AreEqual(LogBufferDescriptor.PackTail(TermID, tail + alignedFrameLength), LogBufferDescriptor.RawTailVolatile(_logMetaDataBuffer, PartionIndex));
            
            // Map flyweight or encode to buffer directly then call commit() when done
            bufferClaim.Commit();

            A.CallTo(() => _headerWriter.Write(_termBuffer, tail, frameLength, TermID)).MustHaveHappened();
        }

        [Test]
        public void ShouldAppendUnfragmentedFromVectorsToEmptyLog()
        {
            var headerLength = _defaultHeader.Capacity;
            var bufferOne = new UnsafeBuffer(new byte[64]);
            var bufferTwo = new UnsafeBuffer(new byte[256]);
            bufferOne.SetMemory(0, bufferOne.Capacity, (byte) '1');
            bufferTwo.SetMemory(0, bufferTwo.Capacity, (byte) '2');
            var msgLength = bufferOne.Capacity + 200;
            var framgeLength = msgLength + headerLength;
            var alignedFrameLength = BitUtil.Align(framgeLength, FrameDescriptor.FRAME_ALIGNMENT);
            var tail = 0;

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tail));

            var vectors = new[]
            {
                new DirectBufferVector(bufferOne, 0, bufferOne.Capacity),
                new DirectBufferVector(bufferTwo, 0, 200)
            };

            Assert.AreEqual(alignedFrameLength, _termAppender.AppendUnfragmentedMessage(_headerWriter, vectors, msgLength, RVS, TermID));

            Assert.AreEqual(LogBufferDescriptor.PackTail(TermID, tail + alignedFrameLength), LogBufferDescriptor.RawTailVolatile(_logMetaDataBuffer, PartionIndex));
            
            A.CallTo(() => _headerWriter.Write(_termBuffer, tail, framgeLength, TermID)).MustHaveHappened()
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength, bufferOne, 0, bufferOne.Capacity)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength + bufferOne.Capacity, bufferTwo, 0, 200)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail, framgeLength)).MustHaveHappened());
        }

        [Test]
        public void ShouldAppendFragmentedFromVectorsToEmptyLog()
        {
            var mtu = 2048;
            var headerLength = _defaultHeader.Capacity;
            var maxPayloadLength = mtu - headerLength;
            var bufferOneLength = 64;
            var bufferTwoLength = 3000;
            var bufferOne = new UnsafeBuffer(new byte[bufferOneLength]);
            var bufferTwo = new UnsafeBuffer(new byte[bufferTwoLength]);
            bufferOne.SetMemory(0, bufferOne.Capacity, (byte) '1');
            bufferTwo.SetMemory(0, bufferTwo.Capacity, (byte) '2');
            var msgLength = bufferOneLength + bufferTwoLength;
            var tail = 0;
            var frameOneLength = mtu;
            var frameTwoLength = (msgLength - (mtu - headerLength)) + headerLength;
            var resultingOffset = frameOneLength + BitUtil.Align(frameTwoLength, FrameDescriptor.FRAME_ALIGNMENT);

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tail));

            var vectors = new[]
            {
                new DirectBufferVector(bufferOne, 0, bufferOneLength),
                new DirectBufferVector(bufferTwo, 0, bufferTwoLength)
            };

            Assert.AreEqual(resultingOffset, _termAppender.AppendFragmentedMessage(_headerWriter, vectors, msgLength, maxPayloadLength, RVS, TermID));

            var tail2 = tail + frameOneLength;
            var bufferTwoOffset = maxPayloadLength - bufferOneLength;
            var fragmentTwoPayloadLength = bufferTwoLength - (maxPayloadLength - bufferOneLength);

            A.CallTo(() => _headerWriter.Write(_termBuffer, tail, frameOneLength, TermID)).MustHaveHappened()
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength, bufferOne, 0, bufferOneLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength + bufferOneLength, bufferTwo, 0, maxPayloadLength - bufferOneLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail, frameOneLength)).MustHaveHappened())
                .Then(A.CallTo(() => _headerWriter.Write(_termBuffer, tail2, frameTwoLength, TermID)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutBytes(tail2 + headerLength, bufferTwo, bufferTwoOffset, fragmentTwoPayloadLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail2 + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail2, frameTwoLength)).MustHaveHappened());
        }

        [Test]
        public void ShouldAppendFragmentedFromVectorsWithNonZeroOffsetToEmptyLog()
        {
            var mtu = 2048;
            var headerLength = _defaultHeader.Capacity;
            var maxPayloadLength = mtu - headerLength;
            var bufferOneLength = 64;
            var offset = 15;
            var bufferTwoTotalLength = 3000;
            var bufferTwoLength = bufferTwoTotalLength - offset;
            var bufferOne = new UnsafeBuffer(new byte[bufferOneLength]);
            var bufferTwo = new UnsafeBuffer(new byte[bufferTwoTotalLength]);
            bufferOne.SetMemory(0, bufferOne.Capacity, (byte) '1');
            bufferTwo.SetMemory(0, bufferTwo.Capacity, (byte) '2');
            var msgLength = bufferOneLength + bufferTwoLength;
            var tail = 0;
            var frameOneLength = mtu;
            var frameTwoLength = (msgLength - (mtu - headerLength)) + headerLength;
            var resultingOffset = frameOneLength + BitUtil.Align(frameTwoLength, FrameDescriptor.FRAME_ALIGNMENT);

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID, tail));

            var vectors = new[]
            {
                new DirectBufferVector(bufferOne, 0, bufferOneLength),
                new DirectBufferVector(bufferTwo, offset, bufferTwoLength)
            };

            Assert.AreEqual(resultingOffset, _termAppender.AppendFragmentedMessage(_headerWriter, vectors, msgLength, maxPayloadLength, RVS, TermID));

            var tail2 = tail + frameOneLength;
            var bufferTwoOffset = maxPayloadLength - bufferOneLength + offset;
            var fragmentTwoPayloadLength = bufferTwoLength - (maxPayloadLength - bufferOneLength);

            A.CallTo(() => _headerWriter.Write(_termBuffer, tail, frameOneLength, TermID)).MustHaveHappened()
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength, bufferOne, 0, bufferOneLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutBytes(headerLength + bufferOneLength, bufferTwo, offset, maxPayloadLength - bufferOneLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail, frameOneLength)).MustHaveHappened())
                .Then(A.CallTo(() => _headerWriter.Write(_termBuffer, tail2, frameTwoLength, TermID)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutBytes(tail2 + headerLength, bufferTwo, bufferTwoOffset, fragmentTwoPayloadLength)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutLong(tail2 + DataHeaderFlyweight.RESERVED_VALUE_OFFSET, RV, ByteOrder.LittleEndian)).MustHaveHappened())
                .Then(A.CallTo(() => _termBuffer.PutIntOrdered(tail2, frameTwoLength)).MustHaveHappened());
        }

        [Test]
        public void ShouldDetectInvalidTerm()
        {
            var length = 128;
            var srcOffset = 0;

            var buffer = new UnsafeBuffer(new byte[length]);

            _logMetaDataBuffer.PutLong(TermTailCounterOffset, LogBufferDescriptor.PackTail(TermID + 1, 0));

            Assert.Throws(typeof(AeronException),
                () => _termAppender.AppendUnfragmentedMessage(_headerWriter, buffer, srcOffset, length, RVS, TermID));
        }
    }
}