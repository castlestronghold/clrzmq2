﻿/*

    Copyright (c) 2011 Michael Compton <michael.compton@littleedge.co.uk>

    This file is part of clrzmq2.

    clrzmq2 is free software; you can redistribute it and/or modify it under
    the terms of the Lesser GNU General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    clrzmq2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    Lesser GNU General Public License for more details.

    You should have received a copy of the Lesser GNU General Public License
    along with this program. If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using System.Threading;

namespace ZMQ.ZMQDevice {
	using System.Diagnostics;
	using ZMQ.Counters;

	public abstract class Device : IDisposable {
	    //private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Device));

        private const long PollingIntervalUsec = 750000;

        protected volatile bool _run;
        protected Socket _frontend;
        protected Socket _backend;

        private readonly Thread _runningThread;
        private readonly ManualResetEvent _doneEvent;

        private bool _isRunning;

        /// <summary>
        /// Create Device
        /// </summary>
        /// <param name="frontend">Fontend Socket</param>
        /// <param name="backend">Backend Socket</param>
        protected Device(Socket frontend, Socket backend) {
            _backend = backend;
            _frontend = frontend;
            _isRunning = false;
            _run = false;
	        _runningThread = new Thread(RunningLoop) { IsBackground = true };
            _frontend.PollInHandler += FrontendHandler;
            _backend.PollInHandler += BackendHandler;
            _doneEvent = new ManualResetEvent(false);
        }

        ~Device() {
            Dispose(false);
        }

        public ManualResetEvent DoneEvent { get { return _doneEvent; } }

        public bool IsRunning {
            get { return _isRunning; }
            set { _isRunning = value; }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
			//logger.Debug("Disposing Device...");

            if (_isRunning) {
                Stop();
	            _runningThread.Join((int)PollingIntervalUsec * 2 / 1000);

				//logger.Debug("Running thread joined...");
            }

            _frontend.Dispose();
            _backend.Dispose();

			//logger.Debug("Deviced disposed.");
        }

        protected abstract void FrontendHandler(Socket socket, IOMultiPlex revents);
        protected abstract void BackendHandler(Socket socket, IOMultiPlex revents);

        /// <summary>
        /// Start Device
        /// </summary>
        public virtual void Start() {
            _doneEvent.Reset();
            _run = true;
            _runningThread.Start();
            _isRunning = true;
        }

        /// <summary>
        /// Stop Device
        /// </summary>
        public virtual void Stop() {
            _run = false;
        }

        protected virtual void RunningLoop() {
	        try
	        {
		        var skts = new[] { _frontend, _backend };

		        while (_run) {
			        var poller = Context.Poller(skts, PollingIntervalUsec);

					//if (logger.IsDebugEnabled)
					//	logger.Debug("RunningLoop Context Polling Result: " + poller);
		        }

		        IsRunning = false;
		        _doneEvent.Set();
	        }
	        catch (Exception e)
	        {
				//logger.Fatal("Error on RunningLoop", e);
	        }
        }
    }

    /// <summary>
    /// Standard Queue Device
    /// </summary>
    public class Queue : Device
    {
		private static readonly PerformanceCounter backendCounter = PerfCounterRegistry.Get(PerfCounters.NumberOfCallForwardedToBackend);
		private static readonly PerformanceCounter frontendCounter = PerfCounterRegistry.Get(PerfCounters.NumberOfCallForwardedToFrontend);

        public Queue(Context context, string frontendAddr, string backendAddr)
            : base(context.Socket(SocketType.XREP), context.Socket(SocketType.XREQ)) {
            _frontend.Bind(frontendAddr);
            _backend.Connect(backendAddr);
        }

        [Obsolete("Use the constructor that accepts a Context. Will be removed in 3.x.")]
        public Queue(string frontendAddr, string backendAddr)
            : base(new Socket(SocketType.XREP), new Socket(SocketType.XREQ)) {
            _frontend.Bind(frontendAddr);
            _backend.Bind(backendAddr);          
        }

        protected override void FrontendHandler(Socket socket, IOMultiPlex revents) {
            socket.Forward(_backend);
			backendCounter.Increment();
        }

        protected override void BackendHandler(Socket socket, IOMultiPlex revents) {
            socket.Forward(_frontend);
			frontendCounter.Increment();
		}
    }

    public class Forwarder : Device {
        public Forwarder(Context context, string frontendAddr, string backendAddr, MessageProcessor msgProc)
            : base(context.Socket(SocketType.SUB), context.Socket(SocketType.PUB)) {
            _frontend.Bind(frontendAddr);
            _backend.Connect(backendAddr);
        }

        [Obsolete("Use the constructor that accepts a Context. Will be removed in 3.x.")]
        public Forwarder(string frontendAddr, string backendAddr, MessageProcessor msgProc)
            : base(new Socket(SocketType.SUB), new Socket(SocketType.PUB)) {
            _frontend.Connect(frontendAddr);
            _backend.Bind(backendAddr);
        }

        protected override void FrontendHandler(Socket socket, IOMultiPlex revents) {
            socket.Forward(_backend);
        }

        protected override void BackendHandler(Socket socket, IOMultiPlex revents) {
            throw new NotImplementedException();
        }
    }

    public class Streamer : Device {
        public Streamer(Context context, string frontendAddr, string backendAddr, MessageProcessor msgProc)
            : base(context.Socket(SocketType.PULL), context.Socket(SocketType.PUSH)) {
            _frontend.Bind(frontendAddr);
            _backend.Connect(backendAddr);
        }

        [Obsolete("Use the constructor that accepts a Context. Will be removed in 3.x.")]
        public Streamer(string frontendAddr, string backendAddr, MessageProcessor msgProc)
            : base(new Socket(SocketType.PULL), new Socket(SocketType.PUSH)) {
            _frontend.Bind(frontendAddr);
            _backend.Connect(backendAddr);
        }

        protected override void FrontendHandler(Socket socket, IOMultiPlex revents) {
            throw new NotImplementedException();
        }

        protected override void BackendHandler(Socket socket, IOMultiPlex revents) {
            socket.Forward(_frontend); 
        }
    }

    public delegate void MessageProcessor(byte[] identity, Queue<byte[]> msgParts);

    public class AsyncReturn : Device {
        private readonly MessageProcessor _messageProcessor;

        public AsyncReturn(Context context, string frontendAddr, string backendAddr, MessageProcessor msgProc)
            : base(context.Socket(SocketType.XREP), context.Socket(SocketType.PULL)) {
            _messageProcessor = msgProc;
            _frontend.Bind(frontendAddr);
            _backend.Bind(backendAddr);
        }

        [Obsolete("Use the constructor that accepts a Context. Will be removed in 3.x.")]
        public AsyncReturn(string frontendAddr, string backendAddr, MessageProcessor msgProc)
            : base(new Socket(SocketType.XREP), new Socket(SocketType.PULL)) {
            _messageProcessor = msgProc;
            _frontend.Bind(frontendAddr);
            _backend.Bind(backendAddr);
        }

        protected override void FrontendHandler(Socket socket, IOMultiPlex revents) {
            Queue<byte[]> msgs = socket.RecvAll();
            _messageProcessor(msgs.Dequeue(), msgs);
        }

        protected override void BackendHandler(Socket socket, IOMultiPlex revents) {
            socket.Forward(_frontend);            
        }
    }
}
