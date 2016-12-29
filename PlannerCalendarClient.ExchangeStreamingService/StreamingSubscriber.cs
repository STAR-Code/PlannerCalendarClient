using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using PlannerCalendarClient.ExchangeStreamingService.Affinity;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ExchangeServiceCreator;
using EWS = Microsoft.Exchange.WebServices.Data;

namespace PlannerCalendarClient.ExchangeStreamingService
{
    /// <summary>
    /// 
    /// </summary>
    class StreamingSubscriber : IDisposable
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        #region defined the locale SubscriptionInfo class

        /// <summary>
        /// The information about a single mail account subscription
        /// </summary>
        class SubscriptionInfo
        {
            private string _errorMessage;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="mailAccount"></param>
            public SubscriptionInfo(string mailAccount)
            {
                MailAccount = mailAccount;
                SubscriptionId = null;
            }

            /// <summary>
            /// The mail account
            /// </summary>
            public string MailAccount { get; private set; }
            /// <summary>
            /// The subscription id
            /// </summary>
            public string SubscriptionId { get; set; }
            /// <summary>
            /// Set to true for removing the mail account from the subscription.
            /// </summary>
            public bool Removed { get; set; }

            #region Error properties

            public string ErrorCode { get; private set; }

            public string ErrorMessage
            {
                get
                {
                    if (Removed)
                    {
                        return "Removed" + (string.IsNullOrEmpty(_errorMessage) ? ": " + _errorMessage : "");
                    }
                    else
                    {
                        return _errorMessage;
                    }
                }
            }

            public DateTime? ErrorDate { get; private set; }

            public void ClearError()
            {
                ErrorCode = null;
                _errorMessage = null;
                // Remember the last update time for the status.
                ErrorDate = DateTime.Now;
            }

            internal void SetError(string errorCode, string errorMessage)
            {
                ErrorCode = errorCode;
                _errorMessage = errorMessage;
                ErrorDate = DateTime.Now;
            }

            public bool IsInError()
            {
                return ErrorCode != null;
            }

            #endregion Error properties
        }

        /// <summary>
        /// The list of mail account subscriptions in this subscription.
        /// </summary>
        class SubscriptionInfoList : IEnumerable<SubscriptionInfo>
        {
            readonly List<SubscriptionInfo> _list = new List<SubscriptionInfo>();

            public void Add(SubscriptionInfo item)
            {
                if (_list.SingleOrDefault(m => m.MailAccount.Equals(item.MailAccount, StringComparison.CurrentCultureIgnoreCase)) == null)
                {
                    _list.Add(item);
                }
                else
                {
                    throw new Exception("Program error!");
                }
            }

            public int Count
            {
                get { return _list.Count; }
            }

            public SubscriptionInfo Lookup(string mailAccount)
            {
                return _list.Find(i => i.MailAccount.Equals(mailAccount, StringComparison.InvariantCultureIgnoreCase));

            }

            public String[] ActiveMailSubscription()
            {
                return _list.Where(i => i.SubscriptionId != null).Select(i => i.MailAccount).ToArray();
            }

            public String[] InactiveMailSubscription()
            {
                return _list.Where(i => i.SubscriptionId == null).Select(i => i.MailAccount).ToArray();
            }

            /// <summary>
            /// Delete all items with the property Removed set to true.
            /// This cleanup the list of SubscriptionInfo to only contain the mail 
            /// that should subscribe to notifications.
            /// </summary>
            public void DeleteRemovedSubscription()
            {
                var removed = _list.Where(s => s.Removed).ToList();

                removed.ForEach(r => _list.Remove(r));
            }

            public IEnumerator<SubscriptionInfo> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _list.GetEnumerator();
            }

        }

        #endregion

        /// <summary>
        /// The mail group name for the subscription
        /// </summary>
        private readonly string _groupName;
        /// <summary>
        /// The factory to create the db context for persist the data.
        /// </summary>
        private readonly IClientDbEntitiesFactory _dbContextFactory;

        /// <summary>
        /// The event type that the mail subscription are setup for.
        /// </summary>
        private readonly EWS.EventType[] _notificationEventTypes = { EWS.EventType.FreeBusyChanged };
       
        /// <summary>
        /// The exchange service object
        /// </summary>
        private readonly EWS.ExchangeService _exchangeService;
        /// <summary>
        /// The streaming subscription connection
        /// </summary>
        private EWS.StreamingSubscriptionConnection _subscriptionConnection;
        /// <summary>
        /// The list of successful mail account subscriptions
        /// </summary>
        private readonly SubscriptionInfoList _currentMailsSubscriptions;
        /// <summary>
        /// True when this subscription is running (start has been called)
        /// </summary>
        private volatile bool _running;
        /// <summary>
        /// set to true when the object has been disposed.
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// Count the number of callback events this instance receive.
        /// </summary>
        private int _eventCount = 0;
        /// <summary>
        /// Count the number of notifications this instance receive.
        /// </summary>
        private int _notificationCount = 0;
        /// <summary>
        /// Use impersonation when the call fail with a access problem.
        /// </summary>
        private readonly bool _useImpersonation;
        /// <summary>
        /// The service mail account use in the call 
        /// </summary>
        private readonly string _serviceUserEMailAccount;
        /// <summary>
        /// 
        /// </summary>
        private readonly int _connectionTimeoutInMinuts;
        /// <summary>
        /// The timestamp for the latest call of the OnDisconnection where the "Open" is called. 
        /// Use for dead connection detection.
        /// </summary>
        private DateTime _lastReopenSubscriptionConnection;

        /// <summary>
        /// This object is use to control the access to this object from multiply thread.
        /// All public and OnXxxx notification methods are surrounded by the lock(_lock) statement 
        /// so that  the instance only run in a single thread at the time.
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContextFactory"></param>
        /// <param name="exchangeStreamingConfig"></param>
        /// <param name="mailSubscriptionGroup"></param>
        public StreamingSubscriber(
            IClientDbEntitiesFactory dbContextFactory,
            IExchangeStreamingConfig exchangeStreamingConfig,
            SubscriptionGroupDictionary.SubscriptionGroup mailSubscriptionGroup)
        {
            if (dbContextFactory == null) throw new ArgumentNullException("dbContextFactory");
            if (exchangeStreamingConfig == null) throw new ArgumentNullException("exchangeStreamingConfig");
            if (mailSubscriptionGroup == null) throw new ArgumentNullException("mailSubscriptionGroup");
            if (!mailSubscriptionGroup.Mails.Any())
                throw new ArgumentException("The list of mail account is empty in the Subscription Group.","mailSubscriptionGroup");

            _dbContextFactory = dbContextFactory;
            _groupName = mailSubscriptionGroup.GroupName;
            _useImpersonation = exchangeStreamingConfig.UseImpersonation;
            _serviceUserEMailAccount = exchangeStreamingConfig.ServerUserEmailAccount;

            _currentMailsSubscriptions = new SubscriptionInfoList(); 
            mailSubscriptionGroup.Mails.ForEach(m => _currentMailsSubscriptions.Add(new SubscriptionInfo(m)));

            _exchangeService = CreateExchangeService(exchangeStreamingConfig, mailSubscriptionGroup);
            _connectionTimeoutInMinuts = exchangeStreamingConfig.ConnectionTimeout;
            _subscriptionConnection = CreateStreamingConnection(_exchangeService, _connectionTimeoutInMinuts);

            Logger.LogDebug(LoggingEvents.DebugEvent.CreateStreamingSubscriber, _groupName);
        }

        public string Name
        {
            get { return _groupName; }
        }

        public void Start()
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.General("StreamSubscription.Start Called for the subscription group object \"{0}\"".SafeFormat(_groupName)));

            lock (_lock)
            {
                if (!_running)
                {
                    RefreshLiveSubscription(_exchangeService, _subscriptionConnection, _currentMailsSubscriptions);
                }
                else
                {
                    Logger.LogWarning(LoggingEvents.WarningEvent.SubscriptionAllreadyRunning(_groupName));
                }
            }
        }

        public void Stop()
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.General("StreamSubscription.Stop Called for the subscription group object \"{0}\"".SafeFormat(_groupName)));

            lock (_lock)
            {
                if (_running)
                {
                    CloseConnection(_subscriptionConnection);
                }
                else
                {
                    Logger.LogWarning(LoggingEvents.WarningEvent.SubscriptionAlreadyStopped(_groupName));
                }
            }
        }

        public bool IsRunning()
        {
            return _running;
        }

        public int MailSubscriptionCount()
        {
            lock (_lock)
            {
                return _currentMailsSubscriptions.Count();
            }
        }

        /// <summary>
        /// Update the subscriptions by comparing the updated subscription group with the instance's current subscription items.
        /// Add and remove new mail account so that the current subscription group match the updated subscription group. 
        /// </summary>
        /// <param name="updatedMailSubscriptionGroup">The update mail subscription group</param>
        public void UpdateSubscriptions(SubscriptionGroupDictionary.SubscriptionGroup updatedMailSubscriptionGroup)
        {
            lock (_lock)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General(string.Format("Ext-UpdateSubscriptions in - Group {0}: Connection IsOpen {1}", _groupName, _subscriptionConnection.IsOpen)));

                // Verify that the SubscriptionGroup match this stream.
                if (updatedMailSubscriptionGroup.GroupName == _groupName)
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.General("UpdateSubscriptions: Ext-Update the subscription group object \"{0}\"".SafeFormat(_groupName)));

                    var existingMailSubscription = _currentMailsSubscriptions.Select(i => i.MailAccount).ToList();
                    var toBeRemoved = existingMailSubscription.Except(updatedMailSubscriptionGroup.Mails);
                    var toBeAdded = updatedMailSubscriptionGroup.Mails.Except(existingMailSubscription);
                    var unChange = existingMailSubscription.Except(toBeRemoved);

                    foreach (var mailAccount in unChange)
                    {
                        Logger.LogDebug(LoggingEvents.DebugEvent.General("Ext-Update: Unchanged mail account \"{0}\" for subscription group object \"{1}\"".SafeFormat(mailAccount, _groupName)));
                    }

                    foreach (var mailAccount in toBeRemoved)
                    {
                        var mailSubscription = _currentMailsSubscriptions.Lookup(mailAccount);
                        mailSubscription.Removed = true;
                        Logger.LogDebug(LoggingEvents.DebugEvent.General("Ext-Update: Remove mail account \"{0}\" for subscription group object \"{1}\"".SafeFormat(mailAccount, _groupName)));
                    }

                    foreach (var mailAccount in toBeAdded)
                    {
                        var subInfo = new SubscriptionInfo(mailAccount);
                        _currentMailsSubscriptions.Add(subInfo);
                        Logger.LogDebug(LoggingEvents.DebugEvent.General("Ext-Update: Added mail account \"{0}\" for subscription group object \"{1}\"".SafeFormat(mailAccount, _groupName)));
                    }
                }
                else
                {
                    // throw and exception that the updated group don't match the stream. This is a programming error!
                    throw new ExchangeSubscriptionGroupException(LoggingEvents.ErrorEvent.ErrorSubscriptionMissmatchInGroupname(updatedMailSubscriptionGroup.GroupName, _groupName));
                }

                CheckConnectionToExchange();
                Logger.LogDebug(LoggingEvents.DebugEvent.General(string.Format("Ext-UpdateSubscriptions out - Group {0}: Connection IsOpen {1}", _groupName, _subscriptionConnection.IsOpen)));
            }
        }


        #region checking connection to exchange

        private void CheckConnectionToExchange()
        {
            try
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.CheckConnectionToExchange(_groupName));
                if (IsExchangeConnectionBroken())
                {
                    ReEstablishConnectionToExchange();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorCheckConnectionToExchange(_groupName, ex.Message));
            }
        }

        /// <summary>
        /// This method return true if the connection is broken/closed
        /// </summary>
        /// <returns></returns>
        private bool IsExchangeConnectionBroken()
        {
            try
            {
                if (_subscriptionConnection.IsOpen == false)
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.IsExchangeConnectionBroken(_groupName, "IsOpen is false"));
                    return true;
                }

                // The last open time is greater than the expected, so the connection must be broken.
                var now = DateTime.Now;
                var expectedReopen = _lastReopenSubscriptionConnection.AddMinutes(_connectionTimeoutInMinuts + 2);
                if (now > expectedReopen)
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.IsExchangeConnectionBroken(_groupName, string.Format("Now {0} > last reopen {1}", now, expectedReopen)));
                    return true;
                }
            }
            catch (ObjectDisposedException ex)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.IsExchangeConnectionBroken(_groupName, string.Format("Exception in IsExchangeConnectionBroken: {0} > last reopen {1}", ex.ToString())));
                return true;
            }

            return false;
        }

        private void ReEstablishConnectionToExchange()
        {
            try
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.ReEstablishConnectionToExchange(_groupName));
                //if (IsSubscriptionConnectionOpen)
                //{
                    // Destroy the streaming connection 
                    DestroyStreamingConnection(ref _subscriptionConnection);

                    // Remove the old subscription id from the Subscription info list
                    foreach (var info in _currentMailsSubscriptions)
                    {
                        info.ClearError();
                        info.SubscriptionId = null;
                    }
                    // Create a new subscription stream.
                    _subscriptionConnection = CreateStreamingConnection(_exchangeService, _connectionTimeoutInMinuts);
                //}

                // Refresh the subscriptions
                RefreshLiveSubscription(_exchangeService, _subscriptionConnection, _currentMailsSubscriptions);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorReEstablishConnectionToExchange(_groupName, ex.Message));
            }
        }

        #endregion


        /// <summary>
        /// Refresh/Update the exchange/ews subscription connections with the current status of the mail subscription items.
        /// </summary>
        private void RefreshLiveSubscription(EWS.ExchangeService exchangeService, EWS.StreamingSubscriptionConnection subscriptionConnection, SubscriptionInfoList currentMailsSubscriptions)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.General("RefreshLiveSubscription (enter): Refresh the subscription group \"{0}\"'s subscription. SubscriptionConnection open state: {1}".SafeFormat(_groupName, _subscriptionConnection.IsOpen)));

            try
            {
                UpdateLiveSubscriptions(exchangeService, subscriptionConnection, currentMailsSubscriptions);
            }
            catch (Exception ex)
            {
                var error = LoggingEvents.ErrorEvent.ErrorUpdatingTheSubscriptionGroup(_groupName);
                var exExch = ex as ExchangeBaseException;
                string errorNo = exExch != null
                    ? exExch.Event.ToString()
                    : error.EventId.ToString(CultureInfo.InvariantCulture);

                foreach (var mailSubscrip in currentMailsSubscriptions)
                {
                    if (mailSubscrip.IsInError())
                    {
                        mailSubscrip.SetError(errorNo, ex.Message);
                    }
                }

                Logger.LogError(ex, error);
            }

            try
            {
                SaveMailAccountSubscriptionStatus(currentMailsSubscriptions);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorUpdatingTheSubscriptionGroupDb(_groupName));
            }

            OpenConnection(subscriptionConnection);

            Logger.LogDebug(LoggingEvents.DebugEvent.General("RefreshLiveSubscription (Leave):  subscriptionConnection open state: {0}".SafeFormat(_subscriptionConnection.IsOpen)));
        }

        /// <summary>
        /// Create the exchange server instance with the right credentials.
        /// Note: The credentials for the subscription come from the SubscriptionGroup properties 
        /// and not from the values in the application config file.
        /// </summary>
        /// <param name="exchangeStreamingConfig"></param>
        /// <param name="mailSubscriptionGroup"></param>
        /// <returns></returns>
        private EWS.ExchangeService CreateExchangeService(
            IExchangeStreamingConfig exchangeStreamingConfig, 
            SubscriptionGroupDictionary.SubscriptionGroup mailSubscriptionGroup)
        {
            // Setup the exchange server user's credentials for the stream connection
            ExchangeServiceCreator.Config.ExchangeConnectionConfig groupExchangeConfig;

            if (string.IsNullOrWhiteSpace(mailSubscriptionGroup.ServiceUser))
            {
                // Use default credentials.
                groupExchangeConfig = new ExchangeServiceCreator.Config.ExchangeConnectionConfig(
                    exchangeStreamingConfig.AutodiscoverUrl,
                    exchangeStreamingConfig.EnableScpLookup,
                    exchangeStreamingConfig.UseImpersonation,
                    exchangeStreamingConfig.Version,
                    exchangeStreamingConfig.EwsApiTraceFlags);
            }
            else
            {
                // Use the explicit user and password.
                groupExchangeConfig = new ExchangeServiceCreator.Config.ExchangeConnectionConfig(
                    mailSubscriptionGroup.ServiceUser,
                    mailSubscriptionGroup.ServicePassword,
                    exchangeStreamingConfig.AutodiscoverUrl,
                    exchangeStreamingConfig.EnableScpLookup,
                    exchangeStreamingConfig.UseImpersonation,
                    exchangeStreamingConfig.Version,
                    exchangeStreamingConfig.EwsApiTraceFlags);
            }

            return CreateExchangeServiceConnection.ConnectToService(groupExchangeConfig, mailSubscriptionGroup.Mails.First());
        }

        /// <summary>
        /// Create the exchange streaming subscription for calendar events
        /// </summary>
        /// <param name="exchangeService"></param>
        /// <param name="connectionTimeout"></param>
        /// <returns></returns>
        private EWS.StreamingSubscriptionConnection CreateStreamingConnection(EWS.ExchangeService exchangeService, int connectionTimeout)
        {
            //var eventList = new EventType[] { EventType.FreeBusyChanged, EventType.Modified, EventType.Created, EventType.Deleted };
            Logger.LogDebug(LoggingEvents.DebugEvent.General("Created the following subscription group {0} with notification types: {1}".SafeFormat(_groupName, _notificationEventTypes.Aggregate("", (x, s) => s + "," + x))));

            // This event types are not used for calendar event items: EventType.Moved, EventType.Copied
            var connection = new EWS.StreamingSubscriptionConnection(exchangeService, connectionTimeout);

            // Delegate event handlers. 
            connection.OnNotificationEvent += OnCalEvent;
            connection.OnSubscriptionError += OnError;
            connection.OnDisconnect += OnDisconnect;
            
            return connection;
        }

        /// <summary>
        /// Destroy the exchange streaming connection object
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private void DestroyStreamingConnection(ref EWS.StreamingSubscriptionConnection connection)
        {
            //var eventList = new EventType[] { EventType.FreeBusyChanged, EventType.Modified, EventType.Created, EventType.Deleted };
            Logger.LogDebug(LoggingEvents.DebugEvent.General("Destroy the following subscription: {0}".SafeFormat(_groupName)));

            try
            {
                if (connection != null)
                {
                    // Delegate event handlers. 
                    connection.OnNotificationEvent -= OnCalEvent;
                    connection.OnSubscriptionError -= OnError;
                    connection.OnDisconnect -= OnDisconnect;

                    try
                    {
                        CloseConnection(connection);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex,
                            LoggingEvents.WarningEvent.DestroyStreamingConnectionWarning(_groupName, "Closing error"));
                    }

                    try
                    {
                        connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex,
                            LoggingEvents.WarningEvent.DestroyStreamingConnectionWarning(_groupName, "Disposing error"));
                    }
                }
            }
            finally
            {
                connection = null;
            }
        }

        #region ews events
        /// <summary>
        /// The callback method that receive the streaming notification from exchange/ews.
        /// </summary>
        /// <param name="sender">The StreamingSubscriptionConnection object</param>
        /// <param name="args">The notification object</param>
        private void OnCalEvent(object sender, EWS.NotificationEventArgs args)
        {
            try
            {
                lock (_lock)
                {
                    DateTime receiveTime = DateTime.Now;

                    _eventCount++;

                    Logger.LogDebug(
                        LoggingEvents.DebugEvent.General(
                            "Received the {2}. event with {1} notifications since the program start on the subscription stream name: \"{0}\" ."
                                .SafeFormat(_groupName, args.Events.Count(), _eventCount)));

                    var items = new List<EWS.ItemEvent>();

                    foreach (var notification in args.Events)
                    {
                        var itemEvent = notification as EWS.ItemEvent;

                        if (itemEvent != null)
                        {
                            _notificationCount++;

                            DumpNotificationAsAppointment(itemEvent);

                            items.Add(itemEvent);
                        }
                        else
                        {
                            Logger.LogWarning(LoggingEvents.WarningEvent.UnexpectedNotificationObjectType(_groupName,
                                notification != null ? notification.GetType().Name : "(null)"));
                        }
                    }

                    try
                    {
                        SaveNotifications(items, receiveTime);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorSavingTheNotificationsToDb(_groupName));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.ErrorSavingTheNotifications(_groupName));
            }
        }

        /// <summary>
        /// Notification from exchange/Ews on the disconnection. Do reconnect to the service. 
        /// </summary>
        /// <param name="sender">The StreamingSubscriptionConnection object</param>
        /// <param name="args">The notification object</param>
        private void OnDisconnect(object sender, EWS.SubscriptionErrorEventArgs args)
        {
            if (!_running) return;

            lock (_lock)
            {

                #region Validate of arguments

                // Cast the sender as a StreamingSubscriptionConnection object.           
                var callbackSubscriptionConnection = (EWS.StreamingSubscriptionConnection) sender;
                if (!_subscriptionConnection.Equals(callbackSubscriptionConnection))
                {
                    Logger.LogError(LoggingEvents.ErrorEvent.CallbackSubscriptionConnectionMissmatchError(_groupName));
                }

                #endregion Validate of arguments

                try
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.OnDisconnectEvent(_groupName));
                    RefreshLiveSubscription(_exchangeService, _subscriptionConnection, _currentMailsSubscriptions);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                        LoggingEvents.ErrorEvent.ErrorStreamingSubscriptionOnDisconnect(_groupName, ex.Message));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">The StreamingSubscriptionConnection object</param>
        /// <param name="args">The notification object with the exception</param>
        private void OnError(object sender, EWS.SubscriptionErrorEventArgs args)
        {
            // Handle error conditions. 

            //lock (_lock) Lock is not needed here because this method just lock the error and do not access any local properties.
            {

                var exService = args.Exception as EWS.ServiceResponseException;
                string errMsg;

                if (exService != null)
                {
                    errMsg = SafeStringFormat.SafeFormat("OnError: {0} - {1} (exception class: {2}", exService.ErrorCode,
                        exService.Message, exService.GetType().Name);
                }
                else
                {
                    Exception e = args.Exception;
                    if (e != null)
                    {
                        errMsg = SafeStringFormat.SafeFormat("OnError: {0} (exception class: {1}", e.Message,
                            e.GetType().Name);
                    }
                    else
                    {
                        errMsg = "OnError: Exception object null)";
                    }
                }

                Logger.LogError(args.Exception,
                    LoggingEvents.ErrorEvent.ErrorStreamingSubscriptionOnError(_groupName, errMsg));
            }
        }

        #endregion ews events

        /// <summary>
        /// Take all failed mail subscription in the current mail subscription group and try to create a subscription for them. 
        /// </summary>
        /// <param name="exchangeService"></param>
        /// <param name="subscriptionConnection"></param>
        /// <param name="currentMailsSubscriptions"></param>
        private void UpdateLiveSubscriptions(
            EWS.ExchangeService exchangeService, 
            EWS.StreamingSubscriptionConnection subscriptionConnection,
            SubscriptionInfoList currentMailsSubscriptions)
        {
            Logger.LogDebug(LoggingEvents.DebugEvent.General("UpdateLiveSubscriptions: Live Update Stream subscription group \"{0}\".".SafeFormat(_groupName)));

            // Remove subscription
            var removeSubscriptions = currentMailsSubscriptions.Where(i => i.SubscriptionId != null && i.Removed);
            foreach (var subInfo in removeSubscriptions)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("  Remove subscription from EWS connection: {0}  \"{1}\".".SafeFormat(subInfo.MailAccount,_groupName)));

                try
                {
                    var subObj = subscriptionConnection.CurrentSubscriptions.FirstOrDefault(s => s.Id == subInfo.SubscriptionId);
                    if (subObj != null)
                    {
                        subscriptionConnection.RemoveSubscription(subObj);
                        subInfo.SubscriptionId = null;
                        subInfo.ClearError();
                    }
                }
                catch (EWS.ServiceResponseException ex)
                {
                    subInfo.SetError(ex.ErrorCode.ToString(), ex.Message);
                    LogServiceResponseException(LoggingEvents.ErrorEvent.ServiceErrorRemovingASubscriptionFromTheGroup(_groupName, subInfo.MailAccount), ex, subInfo.MailAccount);
                }
                catch (Exception ex)
                {
                    subInfo.SetError(ex.GetType().Name, ex.Message);
                    LogServiceResponseException(LoggingEvents.ErrorEvent.ErrorRemovingASubscriptionFromTheGroup(_groupName, subInfo.MailAccount), ex, subInfo.MailAccount);
                }
            }

            // Add subscription
            var inactiveSubscription = currentMailsSubscriptions.Where(i => i.SubscriptionId == null && !i.Removed);
            foreach (var subInfo in inactiveSubscription)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("  Add subscription from EWS connection: {0}  \"{1}\".".SafeFormat(subInfo.MailAccount, _groupName)));

                try
                {
                    // Incase of an exception the don't get the SubscriptionId set!
                    subInfo.SubscriptionId = CreateStreamingNotification(exchangeService, subscriptionConnection, subInfo.MailAccount, _notificationEventTypes);
                    subInfo.ClearError();
                }
                catch (EWS.ServiceResponseException ex)
                {
                    subInfo.SetError(ex.ErrorCode.ToString(), ex.Message);
                    LogServiceResponseException(LoggingEvents.ErrorEvent.ServiceErrorAddingASubscriptionFromTheGroup(_groupName, subInfo.MailAccount), ex, subInfo.MailAccount);
                }
                catch (Exception ex)
                {
                    subInfo.SetError(ex.GetType().Name, ex.Message);
                    LogServiceResponseException(LoggingEvents.ErrorEvent.ErrorAddingASubscriptionFromTheGroup(_groupName, subInfo.MailAccount), ex, subInfo.MailAccount);
                }
            }

            // Check that the subscription exist on the connection object. If not set the SubscriptionId to null.
            VerifyConnectionStates(subscriptionConnection, currentMailsSubscriptions);
        }

        /// <summary>
        /// Save the mail accounts current status to the three 3 error fields.
        /// In case of no error all three fields are set to null.
        /// </summary>
        /// <param name="currentMailsSubscriptions"></param>
        private void SaveMailAccountSubscriptionStatus(SubscriptionInfoList currentMailsSubscriptions)
        {
            if (currentMailsSubscriptions.Any())
            {
                using (var dbContext = _dbContextFactory.CreateClientDbEntities())
                {
                    foreach (var mailSubscription in currentMailsSubscriptions)
                    {
                        var plannerResource = dbContext.PlannerResources.Single(r => r.MailAddress.Equals(mailSubscription.MailAccount, StringComparison.CurrentCultureIgnoreCase));
                        
                        plannerResource.ErrorCode = mailSubscription.ErrorCode;
                        plannerResource.ErrorDescription = mailSubscription.ErrorMessage;
                        plannerResource.ErrorDate = mailSubscription.ErrorDate;
                    }

                    dbContext.SaveChangesToDb();
                }

                currentMailsSubscriptions.DeleteRemovedSubscription();;
            }

            LogSubscriptionStatus(currentMailsSubscriptions);
        }

        // Note: the ServiceResponseException has a error code property of the enum type ServiceError.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorEvent"></param>
        /// <param name="ex"></param>
        /// <param name="mailAccount"></param>
        private void LogServiceResponseException(LoggingEvents.ErrorEvent errorEvent, Exception ex, string mailAccount)
        {
            if (ex is EWS.ServiceResponseException)
            {
                var responseException = ex as EWS.ServiceResponseException;

                if (responseException.ErrorCode == EWS.ServiceError.ErrorNonExistentMailbox)
                {
                    Logger.LogError(ex, LoggingEvents.ErrorEvent.ServiceErrorCreatingASubscriptionMailBoxNotFound(_groupName, mailAccount, responseException.ErrorCode.ToString(), (int)responseException.ErrorCode));
                }
                else if (responseException.ErrorCode == EWS.ServiceError.ErrorFolderNotFound)
                {
                    Logger.LogError(ex, LoggingEvents.WarningEvent.ServiceErrorCreatingASubscriptionFolderNotFound(_groupName, mailAccount, responseException.ErrorCode.ToString(), (int)responseException.ErrorCode));
                }
                else
                {
                    Logger.LogError(ex, LoggingEvents.WarningEvent.ServiceErrorCreatingASubscription(_groupName, mailAccount, responseException.ErrorCode.ToString(), (int)responseException.ErrorCode));
                }
            }
            else
            {
                Logger.LogError(ex, errorEvent, _groupName, mailAccount);
            }
        }

        private string CreateStreamingNotification(EWS.ExchangeService exchangeService, EWS.StreamingSubscriptionConnection connection, string emailAccount, EWS.EventType[] eventList)
        {
            var subscriptionFolderId = ExchangeFolderUtils.GetMailAccountsCalendarFolderId(exchangeService, emailAccount, _useImpersonation);

            // Subscribe to streaming notifications on the Inbox Calendar 
            // var streamingsubscription = exchangeService.SubscribeToStreamingNotifications(new[] { subscriptionFolderId }, eventList);

            var streamingsubscription = ExchangeServerUtils.CallImpersonated(
                exchangeService, 
                emailAccount, 
                _useImpersonation,
                exchService => exchService.SubscribeToStreamingNotifications(new[] {subscriptionFolderId}, eventList),
                "Exchange.SubscribeToStreamingNotifications");

            connection.AddSubscription(streamingsubscription);

            return streamingsubscription.Id;
        }

        /// <summary>
        /// Save a received notification to the database.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="receiveTime"></param>
        private void SaveNotifications(List<EWS.ItemEvent> items, DateTime receiveTime)
        {
            var dbItems = items.ConvertAll(r => MapItemEvent2Notification(r, receiveTime));

            using (var dbContext = _dbContextFactory.CreateClientDbEntities())
            {
                dbContext.Notifications.AddRange(dbItems);

                dbContext.SaveChangesToDb();
            }

            Logger.LogDebug(LoggingEvents.DebugEvent.SaveEventNotifications, _groupName, items.Count(), receiveTime);
        }

        /// <summary>
        /// Convert a Exchange notification object to a notification object that can be persisted
        /// </summary>
        /// <param name="itemEvent"></param>
        /// <param name="receiveTime"></param>
        /// <returns></returns>
        private Notification MapItemEvent2Notification(EWS.ItemEvent itemEvent, DateTime receiveTime)
        {
            return new Notification()
            {
                EwsId = itemEvent.ItemId.UniqueId,
                EwsTimestamp = itemEvent.TimeStamp,
                ReceiveTime = receiveTime
            };
        }

        private void OpenConnection(EWS.StreamingSubscriptionConnection connection)
        {
            try
            {
                if (connection.CurrentSubscriptions.Any())
                {
                    bool isOpen = _subscriptionConnection.IsOpen;
                
                    if (isOpen == false)
                    {
                        ExchangeServerUtils.ServerBusyRetry(connection.Open, 10, "StreamingSubscriptionConnection.Open ({0})".SafeFormat(_groupName));

                        _lastReopenSubscriptionConnection = DateTime.Now;
                        _running = true;
                    }

                    Logger.LogDebug(LoggingEvents.DebugEvent.General("Stream subscription group \"{0}\" Call Open Connection {1} => {2}.".SafeFormat(_groupName, isOpen, _subscriptionConnection.IsOpen)));
                }
            }
            catch (Exception ex)
            {
                throw new ExchangeStreamConnectionException(LoggingEvents.ErrorEvent.ErrorOnOpenConnection(_groupName, _subscriptionConnection.IsOpen), ex, _groupName, _subscriptionConnection.IsOpen);
            }
        }

        private void CloseConnection(EWS.StreamingSubscriptionConnection connection)
        {
            try
            {
                _running = false;

                bool isOpen = _subscriptionConnection.IsOpen;

                if (isOpen == true)
                {
                    // Close the connection
                    ExchangeServerUtils.ServerBusyRetry(connection.Close, "StreamingSubscriptionConnection.Close ({0})".SafeFormat(_groupName));
                }

                Logger.LogDebug(LoggingEvents.DebugEvent.General("Stream subscription group \"{0}\" Call Close Connection {1} => {2}.".SafeFormat(_groupName, isOpen, _subscriptionConnection.IsOpen)));
            }
            catch (Exception ex)
            {
                throw new ExchangeStreamConnectionException(LoggingEvents.ErrorEvent.ErrorOnClosingConnection(_groupName, _subscriptionConnection.IsOpen), ex, _groupName, _subscriptionConnection.IsOpen);
            }
        }

        /// <summary>
        //  Log the status for the active and inactive mail subscriptions
        /// </summary>
        private void LogSubscriptionStatus(SubscriptionInfoList currentMailsSubscriptions)
        {
            var activeMailSubscriptions = currentMailsSubscriptions.ActiveMailSubscription();
            Logger.LogInfo(LoggingEvents.InfoEvent.MailAccountsSuccessfullySubscribe(_groupName, (activeMailSubscriptions.Any()) ? string.Join(", ", activeMailSubscriptions) : "(None)"));
            var failedMailAccounts = currentMailsSubscriptions.InactiveMailSubscription();
            if (failedMailAccounts.Any())
            {
                Logger.LogError(LoggingEvents.ErrorEvent.MailAccountsFailedToBeSubscribed(_groupName, string.Join(", ", failedMailAccounts)));
            }
        }

        /// <summary>
        /// Verify that the exchange subscription connection obejct and the current mail subscription match.
        /// </summary>
        /// <param name="subscriptionConnection"></param>
        /// <param name="currentMailsSubscriptions"></param>
        private void VerifyConnectionStates(EWS.StreamingSubscriptionConnection subscriptionConnection, SubscriptionInfoList currentMailsSubscriptions)
        {
            if (subscriptionConnection.CurrentSubscriptions != null)
            {
                if(subscriptionConnection.CurrentSubscriptions.Count() != currentMailsSubscriptions.ActiveMailSubscription().Count())
                {
                    Logger.LogError(LoggingEvents.ErrorEvent.SubscriptionConnectionAndCurrentMailSubscriptionCountMissmatchError(subscriptionConnection.CurrentSubscriptions.Count(), currentMailsSubscriptions.ActiveMailSubscription().Count(), _groupName));
                }

                int counter = 0;
                foreach (var mailSubscrip in currentMailsSubscriptions)
                {
                    counter++;

                    if (mailSubscrip.SubscriptionId != null)
                    {
                        var exist = subscriptionConnection.CurrentSubscriptions.FirstOrDefault(r => mailSubscrip.SubscriptionId == r.Id);
                        if (exist == null)
                        {
                            if (!mailSubscrip.IsInError())
                            {
                                mailSubscrip.SetError("XX-DisconnectMailAccount", "Error The subscription do no longer exist in the Subscription connection object.");
                            }

                            Logger.LogDebug(LoggingEvents.DebugEvent.General("Verify: Subscription {0} (NOT Found). Mail: {1}, Removed: {2} Subscription Id: {3}  ErrorText: {4} (subscription group \"{5}\")".SafeFormat(counter, mailSubscrip.MailAccount, mailSubscrip.Removed, mailSubscrip.SubscriptionId, mailSubscrip.ErrorMessage, _groupName)));

                            mailSubscrip.SubscriptionId = null;
                        }
                        else
                        {
                            Logger.LogDebug(LoggingEvents.DebugEvent.General("Verify: Subscription {0} (Found    ). Mail: {1}, Removed: {2} Subscription Id: {3} ErrorText: {4} (subscription group \"{5}\")".SafeFormat(counter, mailSubscrip.MailAccount, mailSubscrip.Removed, mailSubscrip.SubscriptionId, mailSubscrip.ErrorMessage, _groupName)));
                        }
                    }
                    else
                    {
                        Logger.LogDebug(LoggingEvents.DebugEvent.General("Verify: Subscription {0} (No SubId ). Mail: {1}, Removed: {2} Subscription Id: {3} ErrorText: {4} (subscription group \"{5}\")".SafeFormat(counter, mailSubscrip.MailAccount, mailSubscrip.Removed, mailSubscrip.SubscriptionId, mailSubscrip.ErrorMessage, _groupName)));
                    }
                }
            }
            else
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.General("Verify failed. The instance's Connections object is null for the streaming subscription group: \"{0}\"".SafeFormat(_groupName)));
            }
        }

        [Conditional("DEBUG")]
        private void DumpNotificationAsAppointment(EWS.ItemEvent notification)
        {
            // The NotificationEvent for an email message is an ItemEvent. 
            Logger.LogDebug(LoggingEvents.DebugEvent.General("StreamSubscription group name: \"{0}\". Event/notification count: [{1}/{2}] ItemId: {3}, Timestamp {4}.".SafeFormat(_groupName, _eventCount, _notificationCount, notification.ItemId.UniqueId, notification.TimeStamp)));

            try
            {
                var aiRequest = new EWS.AlternateId
                {
                    UniqueId = notification.ItemId.UniqueId,
                    Mailbox = _serviceUserEMailAccount,
                    Format = EWS.IdFormat.EwsId
                };

                // No impersonation call is use here.
                var response = (EWS.AlternateId)_exchangeService.ConvertId(aiRequest, EWS.IdFormat.StoreId);
                string mailbox =  response.Mailbox;

                //var calView = new CalendarView(startDate, endDate);
                var propSet = new EWS.PropertySet();
                //propSet.AddRange(PropertySet.FirstClassProperties);
                propSet.AddRange(new EWS.PropertyDefinitionBase[]
                {
                    EWS.ItemSchema.Id,
                    EWS.ItemSchema.Subject,
                    EWS.AppointmentSchema.Start,
                    EWS.AppointmentSchema.End,
                    EWS.AppointmentSchema.Duration,
                    EWS.AppointmentSchema.ICalDateTimeStamp,
                    EWS.AppointmentSchema.ICalRecurrenceId,
                    EWS.AppointmentSchema.ICalUid,
                    EWS.AppointmentSchema.Recurrence,
                    EWS.AppointmentSchema.FirstOccurrence,
                    EWS.AppointmentSchema.AppointmentType,
                    EWS.AppointmentSchema.LegacyFreeBusyStatus,
                    EWS.ItemSchema.DateTimeCreated
                });

                EWS.ItemId id = new EWS.ItemId(notification.ItemId.UniqueId);

                EWS.Appointment appoint = ExchangeServerUtils.CallImpersonated(
                    _exchangeService,
                    mailbox,
                    _useImpersonation,
                    exchangeService => EWS.Appointment.Bind(exchangeService, notification.ItemId, propSet),
                    "Appointment.Bind");

                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, "     -----------------------------------------------------------------------------");
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, SafeStringFormat.SafeFormat("     Id \"{0}\"  DateTimeStamp: {1}", appoint.Id.UniqueId, appoint.DateTimeCreated));
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, SafeStringFormat.SafeFormat("     Count: [{0}/{1}] Mailbox: {2}, Starttime: {3} {4} \"{5}\"", _eventCount, _notificationCount, mailbox, appoint.Start, appoint.Duration, appoint.Subject));
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, SafeStringFormat.SafeFormat("     ICalUid \"{0}\", ICalRecurrenceId: {1} DateTimeStamp: {2}", appoint.ICalUid, appoint.ICalRecurrenceId, appoint.ICalDateTimeStamp));
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, SafeStringFormat.SafeFormat("     AppointmentType: {0},  Free/busy: {1}", appoint.AppointmentType, appoint.LegacyFreeBusyStatus));
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, "     -----------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, "     -----------------------------------------------------------------------------");
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, SafeStringFormat.SafeFormat("Exception retrieving the appointment. {0}", ex));
                Logger.LogDebug(LoggingEvents.DebugEvent.AppointmentInfoDump, _groupName, "     -----------------------------------------------------------------------------");
            }
        }

        /// <summary>
        /// Dispose the object by making sure the stream is closed.
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                // Do nothing for the moment
                if (!_disposed)
                {
                    CloseConnection(_subscriptionConnection);
                    _subscriptionConnection.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}
