using Microsoft.Exchange.WebServices.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlannerCalendarClient.EventProcessorService;
using System;
using System.Collections.Generic;

namespace PlannerCalendarClient.UnitTest.EventProcessorService
{
    [TestClass]
    public class TestExchangeAppointmentProvider
    {
        [TestMethod]
        public void IsAppointmentInDeletedItemsFolder()
        {
            // Arrange
            var wellKnownFolderNames = typeof(WellKnownFolderName).GetEnumNames();
            var deletedItemsFolderNames = new HashSet<WellKnownFolderName>
            {
                WellKnownFolderName.DeletedItems,
                WellKnownFolderName.ArchiveDeletedItems,
                WellKnownFolderName.ArchiveRecoverableItemsDeletions,
                WellKnownFolderName.RecoverableItemsDeletions
            };

            // Act
            foreach (var folderName in wellKnownFolderNames)
            {
                var wellKnownFolder = (WellKnownFolderName)Enum.Parse(typeof(WellKnownFolderName), folderName);
                var actual = ExchangeGateway.IsAppointmentInDeletedItemsFolder(wellKnownFolder);

                // Assert
                if (deletedItemsFolderNames.Contains(wellKnownFolder))
                    Assert.IsTrue(actual, "The " + folderName + " folder is a deleted items folder");
                else
                    Assert.IsFalse(actual, "The " + folderName + " folder is not a deleted items folder");
            }
        }
    }
}
