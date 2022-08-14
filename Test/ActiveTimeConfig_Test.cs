using Microsoft.VisualStudio.TestTools.UnitTesting;
using PagerBuddy.Models;

namespace PagerBuddy.UnitTests.Models {
    [TestClass]
    public class ActiveTimeConfig_Test {
        [TestMethod]
        [DataRow(DayOfWeek.Sunday, false)]
        [DataRow(DayOfWeek.Monday, false)]
        [DataRow(DayOfWeek.Saturday, true)]
        public void setDay_ChangesActiveDayList(DayOfWeek day, bool active) {
            //Arrange
            ActiveTimeConfig config = new ActiveTimeConfig();

            //Act
            config.setDay(day, active);
            bool daySet = config.checkDay(day);

            //Assert
            Assert.AreEqual(active, daySet);
        }

        [TestMethod]
        [DynamicData(nameof(GetTimeTestCases), DynamicDataSourceType.Method)]
        public void isActiveTime_ReturnsCorrectly(TimeSpan startTime, TimeSpan endTime, DateTime testTime, bool reference) {
            //Arrange
            ActiveTimeConfig config = new ActiveTimeConfig(); //Defaults to all active days
            config.activeStartTime = startTime;
            config.activeStopTime = endTime;

            //Act
            bool isActive = config.isActiveTime(testTime);

            //Assert
            Assert.AreEqual(reference, isActive);
        }

        private static IEnumerable<object[]> GetTimeTestCases() {
            return new List<object[]>() {
                new object[]{ new TimeSpan(12, 0, 0), new TimeSpan(12, 1, 0), new DateTime(2000, 1, 1, 12, 0, 30), true },
                new object[]{ new TimeSpan(12, 0, 0), new TimeSpan(12, 1, 0), new DateTime(2000, 1, 1, 12, 1, 1), false },
                new object[]{ new TimeSpan(23, 59, 0), new TimeSpan(0, 0, 0), new DateTime(2000, 1, 1, 23, 59, 30), true }

            };
        }

    }
}