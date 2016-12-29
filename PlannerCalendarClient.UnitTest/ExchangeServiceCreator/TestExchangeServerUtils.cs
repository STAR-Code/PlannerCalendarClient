using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.WebServices.Data;
using PlannerCalendarClient.ExchangeServiceCreator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PlannerCalendarClient.UnitTest.ExchangeServiceCreator
{
    [TestClass()]
    public class TestExchangeServerUtils
    {
        #region Stubs

        class TestClass
        {
            public string Arg1 { get; set; }
            public DateTime Start { get; private set; }
            public DateTime End { get; private set; }
            public TimeSpan Duration { get { return End - Start; } }

            public int NoOfCalls { get; private set; }
            public int FailNoOfCalls { get; set; }
            public ServerBusyException ThrownException { get; private set; }

            public TestClass(int failNoOfCalls)
            {
                FailNoOfCalls = failNoOfCalls;
            }

            public void CallVoid()
            {
                VerifyCall();
            }


            public void CallVoidWithArg(string arg1)
            {
                VerifyCall();
            }

            public T CallFuncWithArg<T>(T arg1)
            {
                VerifyCall();

                return arg1;
            }

            private void VerifyCall()
            {
                if (NoOfCalls == 0)
                {
                    Start = DateTime.Now;
                }

                NoOfCalls++;

                if (NoOfCalls <= FailNoOfCalls)
                {
                    ThrownException = new ServerBusyException(new ServiceResponse(ServiceError.ErrorServerBusy, "UnitTest: ServerBusy"));
                    throw ThrownException;
                }

                End = DateTime.Now;
                ThrownException = null;
            }
        }

        #endregion Stubs

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccess1Time()
        {
            int noOfCalls = 1;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 0, 100);
            var t = new TestClass(noOfCalls - 1);

            ExchangeServerUtils.ServerBusyRetry(t.CallVoid, "TestClass.CallVoid");

            Assert.AreEqual(t.NoOfCalls, noOfCalls);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccess1TimeWithArg()
        {
            int noOfCalls = 1;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 0, 100);
            var t = new TestClass(noOfCalls - 1);

            ExchangeServerUtils.ServerBusyRetry(() => t.CallVoidWithArg("Test"), "TestClass.CallVoidWithArg");

            Assert.AreEqual(t.NoOfCalls, noOfCalls);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccess1TimeWithArgAndReturnValue()
        {
            int noOfCalls = 1;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 0, 100);
            var t = new TestClass(noOfCalls - 1);
            string arg = "Test";
            var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), "TestClass.CallFuncWithArg");

            Assert.AreEqual(t.NoOfCalls, noOfCalls);
            Assert.AreEqual(result, arg);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccess2TimeWithArgAndReturnValue()
        {
            int noOfCalls = 2;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 0, 250);
            var t = new TestClass(noOfCalls - 1);
            string arg = "Test";
            var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), "TestClass.CallFuncWithArg");

            Assert.AreEqual(t.NoOfCalls, noOfCalls);
            Assert.AreEqual(result, arg);
            // this call has a wait of 200ms
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccess3TimeWithArgAndReturnValue()
        {
            int noOfCalls = 3;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 0, 650);
            var t = new TestClass(noOfCalls-1);
            string arg = "Test";

            var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), "TestClass.CallFuncWithArg");

            Assert.AreEqual(t.NoOfCalls, noOfCalls);
            Assert.AreEqual(result, arg);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccess4TimeWithArgAndReturnValue()
        {
            int noOfCalls = 4;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 1, 850);
            var t = new TestClass(noOfCalls - 1);
            string arg = "Test";

            var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), "TestClass.CallFuncWithArg");

            Assert.AreEqual(t.NoOfCalls, noOfCalls);
            Assert.AreEqual(result, arg);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }


        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccessDefaultNoOfTimeMinus1()
        {
            int defaultMaxFailCount = ExchangeServerUtils._DefaultBusyRetryCount;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 1, 850);
            var t = new TestClass(defaultMaxFailCount - 1);

            ExchangeServerUtils.ServerBusyRetry(t.CallVoid, "TestClass.CallVoid");

            Assert.AreEqual(t.NoOfCalls, defaultMaxFailCount);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccessDefaultNoOfTimeMinus1WithArg()
        {
            int defaultMaxFailCount = ExchangeServerUtils._DefaultBusyRetryCount;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 1, 850);
            var t = new TestClass(defaultMaxFailCount - 1);

            ExchangeServerUtils.ServerBusyRetry(() => t.CallVoidWithArg("Test"), "TestClass.CallVoidWithArg");

            Assert.AreEqual(t.NoOfCalls, defaultMaxFailCount);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallSuccessDefaultNoOfTimeMinus1WithArgAndReturnValue()
        {
            int defaultMaxFailCount = ExchangeServerUtils._DefaultBusyRetryCount;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 1, 850);
            var t = new TestClass(defaultMaxFailCount - 1);
            string arg = "Test";

            var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), "TestClass.CallFuncWithArg");

            Assert.AreEqual(t.NoOfCalls, defaultMaxFailCount);
            Assert.AreEqual(result, arg);
            Assert.IsTrue(t.ThrownException == null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallFailedDefaultNoOfTime()
        {
            int defaultMaxFailCount = ExchangeServerUtils._DefaultBusyRetryCount;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 1, 850);
            var t = new TestClass(defaultMaxFailCount);

            try
            {
                ExchangeServerUtils.ServerBusyRetry(t.CallVoid, "TestClass.CallFuncWithArg");
            }
            catch (ServerBusyException)
            {
            }

            Assert.AreEqual(defaultMaxFailCount, t.NoOfCalls);
            Assert.IsTrue(t.ThrownException != null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallFailedDefaultNoOfTimeWithArg()
        {
            int defaultMaxFailCount = ExchangeServerUtils._DefaultBusyRetryCount;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 1, 850);
            var t = new TestClass(defaultMaxFailCount);

            try
            {
                ExchangeServerUtils.ServerBusyRetry(() => t.CallVoidWithArg("Test"), "TestClass.CallVoidWithArg");
            }
            catch (ServerBusyException)
            {
            }

            Assert.AreEqual(defaultMaxFailCount, t.NoOfCalls);
            Assert.IsTrue(t.ThrownException != null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        [TestMethod()]
        public void ServerBusyRetryTest_CallFailedDefaultNoOfTimeArgAndReturnValue()
        {
            int defaultMaxFailCount = ExchangeServerUtils._DefaultBusyRetryCount;
            TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 1, 850);
            var t = new TestClass(defaultMaxFailCount);
            string arg = "Test";
            try
            {
                var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), "TestClass.CallFuncWithArg");
            }
            catch (ServerBusyException)
            {
            }

            Assert.AreEqual(defaultMaxFailCount, t.NoOfCalls);
            Assert.IsTrue(t.ThrownException != null);
            Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        }

        // This test spoil the ServerBusyRetry by gette all test run later fail with a ShutdownInProgressException
        //[TestMethod()]
        //public void ServerBusyRetryTest_ForceCancelWait()
        //{
        //    int defaultMaxFailCount = ExchangeServerUtils._DefaultBusyRetryCount;
        //    TimeSpan expectedWaitTime = new TimeSpan(0, 0, 0, 0, 50);
        //    var t = new TestClass(defaultMaxFailCount);
        //    string arg = "Test";
        //    bool bShutdownInProgressException = false;
        //    // When this method is called all call to the ServerBusyRetry will fail with ShutdownInProgressException
        //    ExchangeServerUtils.ForceCancelWait();

        //    try
        //    {
        //        var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), "TestClass.CallFuncWithArg");
        //    }
        //    catch (ShutdownInProgressException ex)
        //    {
        //        bShutdownInProgressException = true;
        //    }

        //    Assert.IsTrue(bShutdownInProgressException);
        //    Assert.AreEqual(0, t.NoOfCalls);
        //    Assert.IsTrue(t.ThrownException == null);
        //    Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        //}

        // this test works but take to long time to run.
        //[TestMethod()]
        //public void ServerBusyRetryTest_CallSuccess8NoOfTimeWithArgAndReturnValue()
        //{
        //    int defaultMaxFailCount = 8;
        //    TimeSpan expectedWaitTime = new TimeSpan(0, 0, 4, 30, 0);
        //    var t = new TestClass(defaultMaxFailCount - 1);
        //    string arg = "Test";

        //    var result = ExchangeServerUtils.ServerBusyRetry(() => t.CallFuncWithArg(arg), 8, "TestClass.CallFuncWithArg");

        //    Assert.AreEqual(t.NoOfCalls, defaultMaxFailCount);
        //    Assert.AreEqual(result, arg);
        //    Assert.IsTrue(t.ThrownException == null);
        //    Assert.IsTrue(t.Duration < expectedWaitTime, "The wait time exceed the {0} sec.: {1}", expectedWaitTime, t.Duration.ToString());
        //}
    }
}
