using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlannerCalendarClient.Logging;
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.ExchangeServiceCreator
{
    public static class ExchangeFolderUtils
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly static EWS.PropertySet FolderPropertiesToRetrieve = new EWS.PropertySet(
                EWS.FolderSchema.Id,
                EWS.FolderSchema.WellKnownFolderName
                );

        public static EWS.FolderId GetMailAccountsCalendarFolderId(EWS.ExchangeService exchangeService, string mailbox, bool useImpersonation)
        {
            EWS.Folder folder = GetMailAccountsCalendarFolder(exchangeService, mailbox, useImpersonation);
            return folder.Id;
        }

        public static EWS.CalendarFolder GetMailAccountsCalendarFolder(EWS.ExchangeService exchangeService, string mailbox, bool useImpersonation)
        {
            if (string.IsNullOrWhiteSpace(mailbox)) throw new ArgumentNullException("mailbox");

            var folderId = new EWS.FolderId(EWS.WellKnownFolderName.Calendar, new EWS.Mailbox(mailbox));

            var folder = ExchangeServerUtils.CallImpersonated<EWS.CalendarFolder>(
                exchangeService, 
                mailbox,
                useImpersonation,
                (exchService) => EWS.CalendarFolder.Bind(exchService, folderId, FolderPropertiesToRetrieve),
                "CalendarFolder.Bind (1)");

            return folder;
        }
    }
}
