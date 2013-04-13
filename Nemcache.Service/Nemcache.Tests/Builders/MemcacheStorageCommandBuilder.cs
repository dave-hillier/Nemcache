﻿using System.Linq;
using System.Text;

namespace Nemcache.Tests.Builders
{
    class MemcacheStorageCommandBuilder
    {
        private readonly string _command;

        protected readonly string _key;
        protected byte[] _data;
        protected short _flags;
        protected int _time;
        protected bool _noReply;

        public MemcacheStorageCommandBuilder(string command, string key, byte[] data)
        {
            _command = command;
            _key = key;
            _data = data;
        }

        public MemcacheStorageCommandBuilder(string command, string key, string data) :
            this(command, key, Encoding.ASCII.GetBytes(data))
        {
        }

        public MemcacheStorageCommandBuilder WithFlags(short flag)
        {
            _flags = flag;
            return this;
        }

        public MemcacheStorageCommandBuilder WithExpiry(int time)
        {
            _time = time;
            return this;
        }

        public MemcacheStorageCommandBuilder NoReply()
        {
            _noReply = true;
            return this;
        }

        public MemcacheStorageCommandBuilder Data(string value)
        {
            _data = Encoding.ASCII.GetBytes(value);
            return this;
        }

        public virtual byte[] ToRequest()
        {
            var format = string.Format("{0} {1} {2} {3} {4}{5}\r\n",
                                       _command, _key, _flags, _time, _data.Length, _noReply ? " noreply" : "");
            var start = Encoding.ASCII.GetBytes(format);
            var end = Encoding.ASCII.GetBytes("\r\n");
            return start.Concat(_data).Concat(end).ToArray();
        }
    }
}