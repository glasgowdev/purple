﻿using System;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Purple.Bitcoin.Signals;
using Purple.Bitcoin.Tests.Logging;

namespace Purple.Bitcoin.Tests.Signals
{
    public class SignalObserverTest : LogsTestBase
    {
        private SignalObserver<Block> observer;

        public SignalObserverTest()
        {
            this.observer = new TestBlockSignalObserver();
        }

        // the log was removed from the observer
        //[Fact]
        public void SignalObserverLogsSignalOnError()
        {
            var exception = new InvalidOperationException("This should not have occurred!");

            this.observer.OnError(exception);

            this.AssertLog(this.FullNodeLogger, LogLevel.Error, exception.ToString());
        }

        private class TestBlockSignalObserver : SignalObserver<Block>
        {
            public TestBlockSignalObserver()
            {
            }

            protected override void OnNextCore(Block value)
            {
            }
        }
    }
}
