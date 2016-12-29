using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.UnitTest.Utility
{
    [TestClass]
    public class TestCalculating
    {
        [TestMethod]
        public void CalculateOverMidnight()
        {
            var testList = new[] {TimeSpan.Parse("01:00:00")};
            var now = TimeSpan.Parse("02:00:00");
            var calc = now.CalculateIntervalToNextEvent(testList);
            Assert.AreEqual(TimeSpan.Parse("23:00:00"), calc);
        }

        [TestMethod]
        public void CalculateBetweenTwo()
        {
            var testList = new[] { TimeSpan.Parse("01:00:00"), TimeSpan.Parse("03:00:00") };
            var now = TimeSpan.Parse("02:00:00");
            var calc = now.CalculateIntervalToNextEvent(testList);
            Assert.AreEqual(TimeSpan.Parse("01:00:00"), calc);
        }

        [TestMethod]
        public void CalculateSameFoundEqualsZero()
        {
            var testList = new[] { TimeSpan.Parse("01:00:00"), TimeSpan.Parse("03:00:00") };
            var now = TimeSpan.Parse("03:00:00");
            var calc = now.CalculateIntervalToNextEvent(testList);
            Assert.AreEqual(TimeSpan.Parse("00:00:00"), calc);
        }

        [TestMethod]
        public void CalculateSimpleSetup()
        {
            var testList = new[] { TimeSpan.Parse("01:00:00"), TimeSpan.Parse("03:00:00"), TimeSpan.Parse("23:00:00") };
            var now = TimeSpan.Parse("05:00:00");
            var calc = now.CalculateIntervalToNextEvent(testList);
            Assert.AreEqual(TimeSpan.Parse("18:00:00"), calc);
        }
    }
}
