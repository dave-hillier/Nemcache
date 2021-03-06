﻿using System;
using System.IO;

namespace Nemcache.Tests.IO
{
    internal class TestStream : Stream
    {
        public bool HasCalledFlush { get; private set; }
        public bool HasCalledClose { get; private set; }

        public override bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override long Length
        {
            get { return 0; }
        }

        public override long Position { get; set; }

        public override void Close()
        {
            base.Close();
            HasCalledClose = true;
        }

        public override void Flush()
        {
            HasCalledFlush = true;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}