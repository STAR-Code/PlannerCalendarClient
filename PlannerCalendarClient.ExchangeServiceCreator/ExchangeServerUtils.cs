using System;
using System.Threading;
using Microsoft.Exchange.WebServices.Data;
using PlannerCalendarClient.Logging;

namespace PlannerCalendarClient.ExchangeServiceCreator
{
    public class ExchangeServerUtils
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();
        /// <summary>
        /// The default number of retry when getting the ServerBusyException.
        /// </summary>
        public const int _DefaultBusyRetryCount = 4;
        /// <summary>
        /// Default wait time before recall the exchange service, if the _DefaultUseExceptionBackOffTime is false or
        /// the ServerBusyException.BackOffMilliseconds property is zero.
        /// </summary>
        public const int _DefaultRetrySleepTime = 200;
        /// <summary>
        /// The maximum retry wait time. The wait time can't grow beone this size.
        /// </summary>
        public const int _MaxRetrySleepTime = 60000; // One minut.
        /// <summary>
        /// Cancellation taken that support forcing stopping waiting for outstanding server busy retry. Just return for service shootdown.
        /// </summary>
        private readonly static CancellationTokenSource _shutdownCancellationToken = new CancellationTokenSource();

        public delegate void DelegateRetry();

        public delegate T DelegateRetry<out T>();

        public delegate T ImpersonationRetry<out T>(ExchangeService exchangeService);

        #region  CallImpersonated

        /// <summary>
        /// This method wrap the call to exchange EWS method in impersonate mode
        /// </summary>
        /// <param name="exchangeService"></param>
        /// <param name="mailbox"></param>
        /// <param name="useImpersonation">True if the call should be done with impersonate</param>
        /// <param name="method">The delegate method to call</param>
        /// <param name="methodName">The name of the method. Use for logging</param>
        public static T CallImpersonated<T>(ExchangeService exchangeService, string mailbox, bool useImpersonation, ImpersonationRetry<T> method, string methodName)
        {
            try
            {
                if (useImpersonation)
                {
                    exchangeService.ImpersonatedUserId = new ImpersonatedUserId
                    {
                        Id = mailbox,
                        IdType = ConnectingIdType.SmtpAddress
                    };

                }

                T t = method(exchangeService);

                if (useImpersonation)
                {
                    Logger.LogDebug(LoggingEvents.DebugEvent.ImpersonatedCallSuccessed(methodName, mailbox));
                }

                return t;
            }
            catch (ServiceResponseException)
            {
                Logger.LogWarning(LoggingEvents.WarningEvent.CallFailedWithMissingImpersonateRight(methodName, mailbox));
                throw;
            }
            finally
            {
                if (useImpersonation)
                {
                    exchangeService.ImpersonatedUserId = null;
                }
            }
        }

        #endregion  CallImpersonated

        #region ServerBusy

        /// <summary>
        /// Call this method to release any outstanding retry waits.
        /// Any ServerBusyRetry call after this call will fail with ShutdownInProgressException
        /// </summary>
        public static void ForceCancelWait()
        {
            _shutdownCancellationToken.Cancel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="methodName"></param>
        public static void ServerBusyRetry(DelegateRetry method, string methodName)
        {
            ServerBusyRetry(method, _DefaultBusyRetryCount, methodName);
        }

        /// <summary>
        /// The server can throw an ServerBusyException. This delegate method retry the call the specified number of time and then
        /// it rethrow the ServerBusyException.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="maxRetryCount"></param>
        /// <param name="methodNameForLog"></param>
        public static void ServerBusyRetry(DelegateRetry method, int maxRetryCount, string methodNameForLog)
        {
            var handleServerBusy = new HandleServerBusy(methodNameForLog, maxRetryCount, _shutdownCancellationToken.Token);
            do
            {
                try
                {
                    method();

                    return;
                }
                catch (ServerBusyException ex)
                {
                    if (handleServerBusy.HandleServerBusyException(ex))
                    {
                        throw;
                    }
                }
            } while (true);
        }

        /// <summary>
        /// The server can throw an ServerBusyException. This delegate method retry the call the specified number of time and then
        /// it rethrow the ServerBusyException.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="methodName"></param>
        public static T ServerBusyRetry<T>(DelegateRetry<T> method, string methodName)
        {
            return ServerBusyRetry<T>(method, _DefaultBusyRetryCount, methodName);
        }

        /// <summary>
        /// The server can throw an ServerBusyException. This delegate method retry the call the specified number of time and then
        /// it rethrow the ServerBusyException.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="maxRetryCount"></param>
        /// <param name="methodNameForLog"></param>
        public static T ServerBusyRetry<T>(DelegateRetry<T> method, int maxRetryCount, string methodNameForLog)
        {
            if (_shutdownCancellationToken.IsCancellationRequested)
            {
                throw new ShutdownInProgressException();
            }

            var handleServerBusy = new HandleServerBusy(methodNameForLog, maxRetryCount, _shutdownCancellationToken.Token);
            do
            {
                try
                {
                    T t = method();

                    return t;
                }
                catch (ServerBusyException ex)
                {
                    if (handleServerBusy.HandleServerBusyException(ex))
                    {
                        throw;
                    }
                }
            } while (true);
        }

        /// <summary>
        /// Helper structure to handle the server busy exception.
        /// It contain the algorithm for when to retry and the wait before continue with a retry.
        /// </summary>
        struct HandleServerBusy
        {
            /// <summary>
            /// Do user the value in the ServerBusyException's BackOffMilliseconds property for the waiting time if this setting is true
            /// and use the _DefaultRetrySleepTime if not.
            /// </summary>
            private const bool _DefaultUseExceptionBackOffTime = true;
            /// <summary>
            /// The name of the method to call. This value is only for logging
            /// </summary>
            private readonly string _methodNameForLog;
            /// <summary>
            /// The maximum number of calling the method.
            /// </summary>
            private readonly int _maxRetryCount;
            /// <summary>
            /// The current retry call number
            /// </summary>
            private int _retryCount;
            /// <summary>
            /// The sleep time before returning to the caller asking for doing a a retry of the call.
            /// </summary>
            private int _retrySleepTime;
            /// <summary>
            /// 
            /// </summary>
            private CancellationToken _internalShutdownCancellationToken;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="methodNameForLog"></param>
            /// <param name="maxRetryCount"></param>
            /// <param name="shutdownCancellationToken"></param>
            internal HandleServerBusy(string methodNameForLog, int maxRetryCount, CancellationToken shutdownCancellationToken)
            {
                _methodNameForLog = methodNameForLog;
                _maxRetryCount = maxRetryCount;
                _retrySleepTime = ExchangeServerUtils._DefaultRetrySleepTime; ;
                _retryCount = 0;
                _internalShutdownCancellationToken = shutdownCancellationToken;
            }

            /// <summary>
            /// Handle the ServerBusyException flow
            /// </summary>
            /// <param name="ex"></param>
            /// <returns>true if the exception should be rethrow</returns>
            internal bool HandleServerBusyException(ServerBusyException ex)
            {
                bool rethrowException = false;
                _retryCount++;

                if (_retryCount >= _maxRetryCount)
                {
                    Logger.LogError(LoggingEvents.ErrorEvent.CallFailedNoRetry(_methodNameForLog, _retryCount, _maxRetryCount));
                    // Stop retrying by ask the caller to rethrow the server busy exception
                    rethrowException = true;
                }
                else
                {
                    // wait a little and retry the call.
                    if (_DefaultUseExceptionBackOffTime == true && ex.BackOffMilliseconds != 0)
                    {
                        _retrySleepTime = ex.BackOffMilliseconds;
                    }
                    else
                    {
                        _retrySleepTime *= _retryCount;
                    }

                    if (_retrySleepTime >  ExchangeServerUtils._MaxRetrySleepTime)
                    {
                        _retrySleepTime =  ExchangeServerUtils._MaxRetrySleepTime;
                    }

                    Logger.LogWarning(LoggingEvents.WarningEvent.CallFailedDoRetry(_methodNameForLog, _retryCount, _maxRetryCount, _retrySleepTime, ex.BackOffMilliseconds));

                    // Ask the caller to retry the call...
                    bool cancelled = _internalShutdownCancellationToken.WaitHandle.WaitOne(_retrySleepTime);
                    if (cancelled)
                    {
                        //rethrowException = true; // Stop calling the exchange server. The service is shutting down.
                        throw new ShutdownInProgressException();
                    }
                }

                return rethrowException;
            }
        }

        #endregion
    }
}
