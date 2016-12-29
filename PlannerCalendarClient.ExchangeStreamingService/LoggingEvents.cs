using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeStreamingService
{
    internal class LoggingEvents
    {
        private const ushort RangeStart = (ushort)EventIdRangeStart.ExchangeStreamingService;

        internal class ErrorEvent : ErrorEventIdBase
        {
            private ErrorEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static ErrorEvent ExceptionThrown(string name)
            {
                return new ErrorEvent(RangeStart + 901, string.Format("UnexpectedException thrown in: {0}", name));
            }

            internal static ErrorEvent StreamingManagerStartErrorUpdateMailAccounts
            {
                get { return new ErrorEvent(RangeStart + 902, "Unexpected exception when call start on the StreamingManager"); }
            }

            internal static ErrorEvent StreamingManagerStopError
            {
                get { return new ErrorEvent(RangeStart + 903, "Unexpected exception when call stop on the StreamingManager"); }
            }

            internal static ErrorEvent StreamingManagerIsRunError(string objectName)
            {
                return new ErrorEvent(RangeStart + 904, string.Format("Exception asking for the IsRunning status for the subscriber object named {0} in the StreamingManager", objectName));
            }

            internal static ErrorEvent ErrorCreateGroupSubscription(string groupName)
            {
                return new ErrorEvent(RangeStart + 905, string.Format("Exception adding an new subscriber object with the group name {0}.", groupName));
            }

            internal static ErrorEvent ErrorStreamingSubscriptionOnError(string groupName, string message)
            {
                return new ErrorEvent(RangeStart + 906, string.Format("Exception in the subscription group name \"{0}\". Exception message: {1}", groupName, message));
            }

            internal static ErrorEvent ErrorOnClosingConnection(string groupName, bool isOpen)
            {
                return new ErrorEvent(RangeStart + 907, string.Format("Exception when closing the stream connection group name \"{0}\". Current state of IsOpen: {1}", groupName, isOpen));
            }

            internal static ErrorEvent ErrorOnOpenConnection(string groupName, bool isOpen)
            {
                return new ErrorEvent(RangeStart + 908, string.Format("Exception when open the stream connection group name \"{0}\". Current state of IsOpen: {1}", groupName, isOpen));
            }

            internal static ErrorEvent ErrorStreamingSubscriptionOnDisconnect(string groupName, string message)
            {
                return new ErrorEvent(RangeStart + 909, string.Format("Exception in OnDisconnect event for the stream connection group name \"{0}\". Exception message: {1}", groupName, message));
            }

            internal static ErrorEvent ErrorSavingTheNotificationsToDb(string streamName)
            {
                return new ErrorEvent(RangeStart + 910, string.Format("Stream name: \"{0}\". Exception when writing the notification to the database!", streamName));
            }

            internal static ErrorEvent ErrorSavingTheNotifications(string streamName)
            {
                return new ErrorEvent(RangeStart + 911, string.Format("Stream name: \"{0}\". Exception in OnCalEvent method!", streamName));
            }

            internal static ErrorEvent ErrorRetrievingTheMailSubscriptionFromDb()
            {
                return new ErrorEvent(RangeStart + 912, "Exception when retrieving the mail subscription from the database!");
            }

            internal static ErrorEvent ErrorSavingTheMailSubscriptionToDb()
            {
                return new ErrorEvent(RangeStart + 913, "Exception when saving the mail subscription to the database!");
            }

            internal static ErrorEvent ErrorCheckingTheMailAccountSubscriptions()
            {
                return new ErrorEvent(RangeStart + 914, "Exception when checking the mail account subscriptions.");
            }

            internal static ErrorEvent ServiceErrorCreatingASubscriptionMailBoxNotFound(string groupName, string mailAccount, string error, int errorCode)
            {
                return new ErrorEvent(RangeStart + 915, string.Format("The stream subscription \"{0}\". Subscription Error, None existing mailbox \"{1}\". ServiceResponse error: {2} (value: {3})", groupName, mailAccount, error, errorCode));
            }

            internal static ErrorEvent ErrorUpdatingTheSubscriptionGroup(string groupName)
            {
                return new ErrorEvent(RangeStart + 918, string.Format("Exception updating the subscriber object with the group name \"{0}\"", groupName));
            }

            internal static ErrorEvent ErrorRemovingTheSubscriptionGroup(string groupName)
            {
                return new ErrorEvent(RangeStart + 919, string.Format("Exception removing the subscriber object with the group name \"{0}\"", groupName));
            }

            internal static ErrorEvent ErrorRemovingASubscriptionFromTheGroup(string groupName, string mailAccount)
            {
                return new ErrorEvent(RangeStart + 920, string.Format("The stream subscription \"{0}\". Error removing the subscription {1}!", groupName, mailAccount));
            }

            internal static ErrorEvent ServiceErrorRemovingASubscriptionFromTheGroup(string groupName, string mailAccount)
            {
                return new ErrorEvent(RangeStart + 921, string.Format("The stream subscription \"{0}\". Error removing the subscription {1}!", groupName, mailAccount));
            }

            internal static ErrorEvent ErrorAddingASubscriptionFromTheGroup(string groupName, string mailAccount)
            {
                return new ErrorEvent(RangeStart + 922, string.Format("The stream subscription \"{0}\". Error adding the subscription {1}!", groupName, mailAccount));
            }

            internal static ErrorEvent ServiceErrorAddingASubscriptionFromTheGroup(string groupName, string mailAccount)
            {
                return new ErrorEvent(RangeStart + 923, string.Format("The stream subscription \"{0}\". Error adding the subscription {1}!", groupName, mailAccount));
            }

            internal static ErrorEvent ServiceErrorGettingExchangeGroupInformation(string mailAccount)
            {
                return new ErrorEvent(RangeStart + 924, string.Format("Error getting the mail account's \"{0}\" group affinity!", mailAccount));
            }

            internal static ErrorEvent ErrorGettingExchangeGroupInformation(string mailAccount)
            {
                return new ErrorEvent(RangeStart + 925, string.Format("Error getting the mail account's \"{0}\" group affinity!", mailAccount));
            }

            internal static ErrorEvent ExchangeStreamConnectionIsEmpty
            {
                get { return new ErrorEvent(RangeStart + 926, "The notification stream couldn't be create for the named stream \"{0}\", because none of the mail accounts was able to create a notification subscription."); }
            }

            internal static ErrorEvent ErrorUpdatingTheSubscriptionGroupDb(string groupName)
            {
                return new ErrorEvent(RangeStart + 927, string.Format("Exception updating the subscriber status in the database with the group name \"{0}\"", groupName));
            }

            internal static ErrorEvent ExchangeSubscriptionGroupingError(string groupName)
            {
                return new ErrorEvent(RangeStart + 928, string.Format("The affinity group couldn't be create for the affinity group \"{0}\".", groupName));
            }

            internal static ErrorEvent ExchangeGroupInformationNotFoundError(string mailAccount, string error, string errorCode)
            {
                return new ErrorEvent(RangeStart + 929, string.Format("GroupInformation and/or ExternalEwsUrl are not found for the mail account \"{0}\". Exchange error code: \"{1}\", message: {2}.", mailAccount, error, errorCode));
            }

            internal static ErrorEvent ExchangeAutodiscoverEndpointNotFoundError(string mailAccount)
            {
                return new ErrorEvent(RangeStart + 930, string.Format("No suitable Autodiscover endpoint was found for the mail \"{0}\".", mailAccount));
            }

            internal static ErrorEvent MailAccountsFailedToBeSubscribed(string groupName, string mailAccounts)
            {
                return new ErrorEvent(RangeStart + 931, string.Format("The stream subscription group \"{0}\" failed to be setup for the following mail accounts \"{1}\"", groupName, mailAccounts));
            }


            internal static ErrorEvent TheMailAccountIsNotFoundForGroupAffinityAssigning(string mailAccount)
            {
                return new ErrorEvent(RangeStart + 932, string.Format("Error when trying to find mail account \"{0}\" for assigning the subscription.", mailAccount));
            }

            internal static ErrorEvent ErrorNoServiceUserAccountForSubscription
            {
                get { return new ErrorEvent(RangeStart + 933, "No service user account (ServiceUserCredentials) defined for the subscriptions."); }
            }

            internal static ErrorEvent ErrorNoFreeServiceUserAccountTheNewSubscriptionGroup(string groupAffinity, int serviceAccountCount, int subscriptionCount)
            {
                return new ErrorEvent(RangeStart + 934, string.Format("Too small numbers of service user account (ServiceUserCredentials) for the needed numbers of subscription groups (The subscription group affinity: {0}, Number of service accounts: {1}, Number of subscriptions: {2}).", groupAffinity, serviceAccountCount, subscriptionCount));
            }

            internal static ErrorEvent ErrorSubscriptionMissmatchInGroupname(string groupNameA, string groupNameB)
            {
                return new ErrorEvent(RangeStart + 935, string.Format("Mismatch the call parameters group name for the subscription object (\"{0}\" <> \"{1}\"", groupNameA, groupNameB));
            }

            internal static ErrorEvent StreamingManagerTimerCallbackErrorUpdateMailAccounts
            {
                get { return new ErrorEvent(RangeStart + 936, "Unexpected exception in the Timer callback in the StreamingManager"); }
            }

            internal static ErrorEvent CallbackSubscriptionConnectionMissmatchError(string groupName)
            {
                return new ErrorEvent(RangeStart + 937, string.Format("Error. Verify failed. The callback subscription connection and this instance's subscription connections object is NOT the same (group name: \"{0}\")", groupName));
            }

            internal static ErrorEvent SubscriptionConnectionAndCurrentMailSubscriptionCountMissmatchError(int currentSubscriptionsCount, int activeMailSubscriptionCount, string groupName)
            {
                return new ErrorEvent(RangeStart + 938, string.Format("Error. Verify failed. The number of subscriptions on the connections object and mail subscriptions is not the same {0} == {1}. (group name: \"{2}\")", currentSubscriptionsCount, activeMailSubscriptionCount, groupName));
            }

            internal static ErrorEvent SubscriptionConnectionAndCurrentMailSubscriptionMissmatchError(string mailAccount, string groupName)
            {
                return new ErrorEvent(RangeStart + 939, string.Format("Error. Verify failed. The mail account subscriptions don't exist on the streaming connections object: {0} (group name: \"{1}\")", mailAccount, groupName));
            }

            internal static ErrorEvent ErrorCheckConnectionToExchange(string groupName, string exceptionMessage)
            {
                return new ErrorEvent(RangeStart + 940, string.Format("Error when checking connection to Exchange for the group name: \"{0}\". Exception message: {1}", groupName, exceptionMessage));
            }

            internal static ErrorEvent ErrorReEstablishConnectionToExchange(string groupName, string exceptionMessage)
            {
                return new ErrorEvent(RangeStart + 941, string.Format("Error trying to reestablish connection to Exchange for the group name: \"{0}\". Exception message: {1}", groupName, exceptionMessage));
            }
        }

        internal class InfoEvent : InfoEventIdBase
        {
            private InfoEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static InfoEvent ServiceStart(string name, string version, string path, string releaseBuild)
            {
                return new InfoEvent(RangeStart + 101, string.Format("Service started. {0} version: {1} Executable path: \"{2}\" {3}", name, version, path, releaseBuild));
            }

            internal static InfoEvent ServiceInfo(string info)
            {
                return new InfoEvent(RangeStart + 102, string.Format("{0}", info));
            }

            internal static InfoEvent ServiceStop(int exitCode)
            {
                return new InfoEvent(RangeStart + 103, string.Format("Service stopped. Exit code {0}", exitCode));
            }

            internal static InfoEvent ConfigurationInfo(string info)
            {
                return new InfoEvent(RangeStart + 111, string.Format("{0}", info));
            }

            internal static InfoEvent SuccessfulUpdatedTheSubscriptionGroupToDb()
            {
                return new InfoEvent(RangeStart + 112, "Successfully updated the subscription groups in the database.");
            }

            internal static InfoEvent MailAccountsSuccessfullySubscribe(string groupName, string mailAccounts)
            {
                return new InfoEvent(RangeStart + 115, string.Format("The stream subscription group \"{0}\" has been successful setup for the following mail accounts: {1}", groupName, mailAccounts));
            }
        }

        internal class WarningEvent : WarningEventIdBase
        {
            private WarningEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static WarningEvent ErrorNoActiveSubscriptionMailAccountsInDb()
            {
                return new WarningEvent(RangeStart + 200, "No active subscriber mail accounts in database!"); 
            }

            internal static WarningEvent NoSuccessfulNotificationSubscriptionCreated()
            {
                return new WarningEvent(RangeStart + 201, "No successful notification subscription created!"); 
            }

            internal static WarningEvent SubscriptionAllreadyRunning(string groupName)
            {
                return new WarningEvent(RangeStart + 203, string.Format("The stream subscription is already running for the group \"{0}\". The call is ignored.", groupName));
            }

            internal static WarningEvent SubscriptionAlreadyStopped(string groupName)
            {
                return new WarningEvent(RangeStart + 204, string.Format("The stream subscription has already been stopped for the group \"{0}\". The call is ignored.", groupName));
            }

            internal static WarningEvent UnexpectedNotificationObjectType(string groupName, string objectType)
            {
                return new WarningEvent(RangeStart + 205, string.Format("Unexpected notification object type \"{1}\". Received in the group \"{0}\".", groupName, objectType));
            }

            internal static WarningEvent DisposingError()
            {
                return new WarningEvent(RangeStart + 206, "Exception when disposing an object.");
            }

            internal static WarningEvent ServiceErrorCreatingASubscriptionFolderNotFound(string groupName, string mailAccount, string error, int errorCode)
            {
                return new WarningEvent(RangeStart + 207, string.Format("The stream subscription \"{0}\". Subscription Error - the calendar folder could not be opened for the mailbox \"{1}\". ServiceResponse error: {2} (value: {3})", groupName, mailAccount, error, errorCode));
            }

            internal static WarningEvent ServiceErrorCreatingASubscription(string groupName, string mailAccount, string error, int errorCode)
            {
                return new WarningEvent(RangeStart + 208, string.Format("The stream subscription \"{0}\". Error adding the subscription {1}! ServiceResponse error: {2} (value: {3})", groupName, mailAccount, error, errorCode));
            }

            internal static WarningEvent DestroyStreamingConnectionWarning(string groupName, string errorPlace)
            {
                return new WarningEvent(RangeStart + 208, string.Format("{1} occurre when destroying the subscription connection for the subscription group name \"{0}\"", groupName, errorPlace));
            }
        }


        /// <summary>
        /// The debug event (Verbose)
        /// </summary>
        internal class DebugEvent : DebugEventIdBase
        {
            private DebugEvent(ushort eventId, string message)
                : base(eventId, message)
            { }

            internal static DebugEvent General(string info)
            {
                return new DebugEvent(RangeStart + 501, info);
            }

            internal static DebugEvent StartUpdatingMailAccounts()
            {
                return new DebugEvent(RangeStart + 504, "Checker the mails subscriptions are up to dated."); 
            }

            internal static DebugEvent FinishCheckingTheMailsSubscriptions()
            {
                return new DebugEvent(RangeStart + 505, "Finish checking the mails subscriptions.");
            }

            internal static DebugEvent NoChangeInTheMailsSubscriptions()
            {
                return new DebugEvent(RangeStart + 506, "No change in the mails subscriptions."); 
            }

            internal static DebugEvent AddSubscriptionGroup(string groupName, int mailAccountCount)
            {
                return new DebugEvent(RangeStart + 507, string.Format("Adding the subscription group \"{0}\" to the subscriptions with {1} mail accounts.", groupName, mailAccountCount));
            }

            internal static DebugEvent UpdateSubscriptionGroup(string groupName, int mailAccountCount)
            {
                return new DebugEvent(RangeStart + 507, string.Format("Update the subscription group \"{0}\"'s subscription with {1} mail accounts.", groupName, mailAccountCount));
            }

            internal static DebugEvent RemoveSubscriptionGroup(string groupName, int mailAccountCount)
            {
                return new DebugEvent(RangeStart + 507, string.Format("Remove the subscription group \"{0}\"'s subscription with {1} mail accounts.", groupName, mailAccountCount));
            }

            internal static DebugEvent DoAutodiscoverMailAccountWithParams(int attempt, string emailAddress, string autoDiscoverServiceUrl, bool scpLookupEnabled)
            {
                return new DebugEvent(RangeStart + 508, string.Format("{0}. Attemp to autodiscover the mail account \"{1}\" with the url \"{2}\" and EnableScpLookup set to {3}.", attempt, emailAddress, autoDiscoverServiceUrl, scpLookupEnabled));
            }

            internal static DebugEvent AutodiscoverMailAccountUseTheUrl(string mailAddress, string url)
            {
                return new DebugEvent(RangeStart + 509, string.Format("Autodiscover for the mailaddress \"{0}\" with the url \"{1}\"", mailAddress, url));
            }

            internal static DebugEvent AutodiscoverMailAccountSuccessed(string emailAddress)
            {
                return new DebugEvent(RangeStart + 510, string.Format("Autodiscover for the \"{0}\" successed.",emailAddress)); 
            }

            internal static DebugEvent AutodiscoverMailAccountFailed(string emailAddress, string errCode, string errMsg)
            {
                return new DebugEvent(RangeStart + 511, string.Format("Autodiscover failed for the \"{0}\" failed with the error code \"{1}\" and error message: \"{2}\"",emailAddress,errCode,errMsg)); 
            }

            internal static DebugEvent AutodiscoverMailAccountFailedWithEndpointNotFound(string emailAddress)
            {
                return new DebugEvent(RangeStart + 512, string.Format("Autodiscover failed for the \"{0}\" with Endpoint not found.",emailAddress)); 
            }

            internal static DebugEvent MailAccountGroupAffinityFound
            {
                get { return new DebugEvent(RangeStart + 513, "Found the group affinity for \"{0}\" to be \"{1}\" and url to be \"{2}\"."); }
            }

            internal static DebugEvent MailAccountGroupAffinityNotFound
            {
                get { return new DebugEvent(RangeStart + 514, "The group affinity is not Found for \"{0}\". Error code: \"{1}\", Error message: \"{2}\"."); }
            }

            internal static DebugEvent CreateStreamingSubscriber
            {
                get { return new DebugEvent(RangeStart + 515, "Created the StreamingSubscriber for subscription group object \"{0}\""); }
            }

            internal static DebugEvent CreateStreamingManager()
            {
                return new DebugEvent(RangeStart + 516, "Create the StreamingManager");
            }

            internal static DebugEvent SaveEventNotifications
            {
                get { return new DebugEvent(RangeStart + 517, "Stored the {1}. received event/notifications received at {2} on the stream group: \"{0}\". "); }
            }

            internal static DebugEvent AppointmentInfoDump
            {
                get { return new DebugEvent(RangeStart + 518, "stream group \"{0}\": {1}. "); }
            }

            internal static DebugEvent AutodiscoverNoEndpointFound
            {
                get { return new DebugEvent(RangeStart + 519, "No suitable Autodiscover endpoint was found for the mail \"{0}\"."); }
            }

            internal static DebugEvent CreateGroupAffinitySolver(string info)
            {
                return new DebugEvent(RangeStart + 520, string.Format("Create GroupAffinitySolver with the parameter \"{0}\".", info)); 
            }

            internal static DebugEvent CreateGroupAffinitySolverLogin(string serviceEMainAccout, string password)
            {
                return new DebugEvent(RangeStart + 521, string.Format("GroupAffinitySolver login credentials ServerUserEmailAccount: \"{0}\", Password: \"{1}\".", serviceEMainAccout, password)); 
            }

            internal static DebugEvent CreateGroupAffinitySolverUserDefaultCredential(string domain, string userName)
            {
                return new DebugEvent(RangeStart + 522,string.Format("GroupAffinitySolver login use default credentials - current user: {0}\\{1}", domain, userName));
            }

            internal static DebugEvent OnDisconnectEvent(string groupName)
            {
                return new DebugEvent(RangeStart + 523, string.Format("The connection to the subscription is disconnected (OnDisconnect call). Autoreconnect the subscription stream \"{0}\"",groupName));
            }

            internal static DebugEvent ReEstablishConnectionToExchange(string groupName)
            {
                return new DebugEvent(RangeStart + 524, string.Format("Try to reestablish the subscription connection to Exchange for the subscription group  \"{0}\"", groupName));
            }

            internal static DebugEvent CheckConnectionToExchange(string groupName)
            {
                return new DebugEvent(RangeStart + 524, string.Format("Check connection to exchange for the subscription group  \"{0}\"", groupName));
            }

            internal static DebugEvent IsExchangeConnectionBroken(string groupName, string info)
            {
                return new DebugEvent(RangeStart + 524, string.Format("IsExchangeConnectionBroken (subscription group  \"{0}\") is true because of: {1}", groupName, info));
            }
        }
    }
}