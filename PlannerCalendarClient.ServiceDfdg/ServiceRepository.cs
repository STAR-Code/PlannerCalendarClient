using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.ServiceDfdg.DfdgService;
using PlannerCalendarClient.Utility;

namespace PlannerCalendarClient.ServiceDfdg
{
    public class ServiceRepository : IServiceRepository
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private delegate Guid CallDelegate(
            X509Certificate certificate,
            string deptNumber,
            string requestUserIdentifier,
            IEnumerable<CalendarEventType> items,
            PlannerExternalCalendarServiceClient service,
            out CalendarEventReceiptType[] statuses);

        private readonly string _wcfEndpointName;
        private readonly X509Certificate _userCertificate;

        /// <summary>
        /// Constructor that use the default endpoint (ie. only one endpoint define for the service interface).
        /// </summary>
        public ServiceRepository()
        {
            _wcfEndpointName = "";
            _userCertificate = null;

            Logger.LogDebug(LoggingEvents.DebugEvent.ServiceRepositoryConstructed("none", "(empty)"));
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="wcfEndpointName">The environment/endpoint name</param>
        public ServiceRepository(string wcfEndpointName)
        {
            if (wcfEndpointName == null) throw new ArgumentNullException("wcfEndpointName");

            _wcfEndpointName = wcfEndpointName;
            _userCertificate = null;

            Logger.LogDebug(LoggingEvents.DebugEvent.ServiceRepositoryConstructed("none", wcfEndpointName));
        }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="userCertificate">The user's Certificate use in the call to planner</param>
        /// <param name="wcfEndpointName">The environment/endpoint name</param>
        public ServiceRepository(X509Certificate2 userCertificate, string wcfEndpointName)
        {
            if (userCertificate == null) throw new ArgumentNullException("userCertificate");
            if (wcfEndpointName == null) throw new ArgumentNullException("wcfEndpointName");

            _wcfEndpointName = wcfEndpointName;
            _userCertificate = userCertificate;

            Logger.LogDebug(LoggingEvents.DebugEvent.ServiceRepositoryConstructed(userCertificate.GetNameInfo(X509NameType.SimpleName, false), wcfEndpointName));
        }

        public ServiceCallReferenceItem CreateEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier)
        {
            if (calendarEventItems == null) throw new ArgumentNullException("calendarEventItems");
            if (string.IsNullOrWhiteSpace(departmentNumber)) throw new ArgumentNullException("departmentNumber");
            if (string.IsNullOrWhiteSpace(requestUserIdentifier)) throw new ArgumentNullException("requestUserIdentifier");

            var calendarEvents = calendarEventItems as IList<CalendarEventItem> ?? calendarEventItems.ToList();

            var result = MakeTheCall(calendarEvents, departmentNumber, requestUserIdentifier, EOperationName.CreateEvents, MakeCreateCall);
            
            LogEventUpdateWarnings(calendarEvents);

            return result;
        }

        public ServiceCallReferenceItem UpdateEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier)
        {
            if (calendarEventItems == null) throw new ArgumentNullException("calendarEventItems");
            if (string.IsNullOrWhiteSpace(departmentNumber)) throw new ArgumentNullException("departmentNumber");
            if (string.IsNullOrWhiteSpace(requestUserIdentifier)) throw new ArgumentNullException("requestUserIdentifier");

            var calendarEvents = calendarEventItems as IList<CalendarEventItem> ?? calendarEventItems.ToList();

            var result = MakeTheCall(calendarEvents, departmentNumber, requestUserIdentifier, EOperationName.UpdateEvents, MakeUpdateCall);

            LogEventUpdateWarnings(calendarEvents);

            return result;
        }

        public ServiceCallReferenceItem DeleteEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier)
        {
            if (calendarEventItems == null) throw new ArgumentNullException("calendarEventItems");
            if (string.IsNullOrWhiteSpace(departmentNumber)) throw new ArgumentNullException("departmentNumber");
            if (string.IsNullOrWhiteSpace(requestUserIdentifier)) throw new ArgumentNullException("requestUserIdentifier");

            var calendarEvents = calendarEventItems as IList<CalendarEventItem> ?? calendarEventItems.ToList();

            var result = MakeTheCall(calendarEvents, departmentNumber, requestUserIdentifier, EOperationName.DeleteEvents, MakeDeleteCall);

            MarkDeletedCalendarItems(calendarEvents);
            LogEventUpdateWarnings(calendarEvents);
            
            return result;
        }

        private static void MarkDeletedCalendarItems(IEnumerable<CalendarEventItem> calendarEventItems)
        {
            // Mark all the calendar items that have been successfully deleted as deleted.
            foreach (var cal in calendarEventItems)
            {
                if (cal.SyncLogItem != null && cal.SyncLogItem.PlannerSyncSuccess)
                {
                    cal.HasBeenDeleted = true;
                }
            }
        }

        private void LogEventUpdateWarnings(IEnumerable<CalendarEventItem> calendarEventItems)
        {
            foreach (var calendarEventItem in calendarEventItems)
            {
                if (calendarEventItem.SyncLogItem.PlannerEventErrorCode != 0)
                    Logger.LogWarning(
                        LoggingEvents.WarningEvent.GetPlannerErrorCodeWarning(
                            calendarEventItem.SyncLogItem.PlannerEventErrorCode,
                            string.Format("{0} (ID={1})", calendarEventItem.SyncLogItem.PlannerSyncResponse,
                                calendarEventItem.OriginId)));
            }
        }
        
        public IEnumerable<CalendarEventItem> GetEvents(string departmentNumber, string requestUserIdentifier, IEnumerable<string> mailAddresses, DateTime fromDate, DateTime toDate, out ServiceCallReferenceItem serviceCallReferenceItem)
        {
            if (string.IsNullOrWhiteSpace(departmentNumber)) throw new ArgumentNullException("departmentNumber");

            var addresses = mailAddresses as IList<string> ?? mailAddresses.ToList();
            var request = new GetEventsRequest
            {
                ActiveOrganisationHeader = OrgHeader(departmentNumber),
                GetEventsRequest1 = new GetEventsRequestType
                {
                    CalendarResource = mailAddresses == null
                        ? new CalendarResourceType[] { }
                        : addresses.Select(
                            mailAdr => new CalendarResourceType
                            {
                                CalendarResourceEmailAddress = mailAdr,
                                CalendarResourceDescription = string.Empty,
                                CalendarResourceName = string.Empty,
                                IsCaseWorker = true,
                                PlannerResourceIdentifier = Guid.NewGuid(),
                                PlannerResourceIdentifierSpecified = false,
                                RID = mailAdr.Length > 64 ? mailAdr.Substring(0, 64) : mailAdr,
                                WorkingHours = null
                            }).ToArray(),
                    EventStartFromDateTime = fromDate,
                    EventStartFromDateTimeSpecified = true,
                    EventStartToDateTime = toDate,
                    EventStartToDateTimeSpecified = true
                },
                RequestUserMetadataHeader = BuildRequestUserMetadata(departmentNumber, requestUserIdentifier, "GetEvents")
            };

            Logger.LogInfo(LoggingEvents.InfoEvent.CallGetEvents(departmentNumber, fromDate, toDate, string.Join(", ", addresses), _wcfEndpointName));

            using (var service = new PlannerExternalCalendarServiceClient(_wcfEndpointName))
            {
                Logger.LogInfo(LoggingEvents.InfoEvent.EndpointUrl(service.Endpoint.Address.Uri.AbsoluteUri));

                try
                {
                    var startTime = DateTime.Now;
                    var response = service.GetEvents(request);
                    var responseReferenceId = response.GetEventsResponse1.ServiceRequestLogReferenceIdentifier;
                    var calendarEventItems = response.GetEventsResponse1.CalendarEvent == null ? new List<CalendarEventItem>() : response.GetEventsResponse1.CalendarEvent.Select(FromServiceData).ToList();

                    serviceCallReferenceItem = CreateServiceCallReference(responseReferenceId, startTime, EOperationName.GetEvents);
                    UpdateGetEventsStatusOnCalendarEventItems(serviceCallReferenceItem, calendarEventItems);

                    return calendarEventItems;
                }
                catch (FaultException<FaultDetails> fault)
                {
                    Logger.LogError(fault, LoggingEvents.ErrorEvent.UnexpectedException);
                    // Need to abort the service call to suppress an exception that can occure when closing the service.
                    service.Abort();
                    throw new ServiceProviderException("Service failed (FaultException<FaultDetailsType>) - see inner exception for more details", fault);
                }
                catch (FaultException fault)
                {
                    Logger.LogError(fault, LoggingEvents.ErrorEvent.UnexpectedException);
                    // Need to abort the service call to suppress an exception that can occure when closing the service.
                    service.Abort();
                    throw new ServiceProviderException("Service failed (FaultException) - see inner exception for more details", fault);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, LoggingEvents.ErrorEvent.UnexpectedException);
                    // Need to abort the service call to suppress an exception that can occure when closing the service.
                    service.Abort();
                    throw new ServiceProviderException("Service failed - see inner exception for more details", ex);
                }
            }
        }

        public IEnumerable<ResourceItem> GetResources(string departmentNumber, string requestUserIdentifier, IEnumerable<string> mailAddresses, out ServiceCallReferenceItem serviceCallReferenceItem)
        {
            if (string.IsNullOrWhiteSpace(departmentNumber)) throw new ArgumentNullException("departmentNumber");
            if (mailAddresses == null) throw new ArgumentNullException("mailAddresses");

            var request = new GetResourcesRequest
            {
                ActiveOrganisationHeader = OrgHeader(departmentNumber),
                GetResourcesRequest1 = mailAddresses.ToArray(),
                RequestUserMetadataHeader = BuildRequestUserMetadata(departmentNumber, requestUserIdentifier, "CreateEvents")
            };

            using (var service = new PlannerExternalCalendarServiceClient(_wcfEndpointName))
            {
                Logger.LogInfo(LoggingEvents.InfoEvent.EndpointUrl(service.Endpoint.Address.Uri.AbsoluteUri));

                try
                {
                    var startTime = DateTime.Now;
                    var response = service.GetResources(request);

                    serviceCallReferenceItem = CreateServiceCallReference(response.GetResourcesResponse1.ServiceRequestLogReferenceIdentifier, startTime, EOperationName.GetResources);

                    return response.GetResourcesResponse1.CalendarResource.Select(ToResourceItem).ToList();
                }
                catch (FaultException<FaultDetails> fault)
                {
                    if (fault.Detail != null)
                    {
                        var msg = string.Format("Service failed (FaultException<FaultDetailsType>) - ErrorCode:{0}\nSource:{1}\nMessage:\n{2}\nInner except: {3}",
                            fault.Detail.ErrorCode, fault.Detail.Source, fault.Detail.Message, fault.Detail.InnerException);
                        Logger.LogError(fault, LoggingEvents.ErrorEvent.UnexpectedException);
                        throw new ServiceProviderException(msg);
                    }
                    Logger.LogError(fault, LoggingEvents.ErrorEvent.UnexpectedException);
                    // Need to abort the service call to suppress an exception that can occure when closing the service.
                    service.Abort();
                    throw new ServiceProviderException("Service failed (FaultException<FaultDetailsType>) - see inner exception for more details", fault);
                }
                catch (FaultException fault)
                {
                    Logger.LogError(fault, LoggingEvents.ErrorEvent.UnexpectedException);
                    // Need to abort the service call to suppress an exception that can occure when closing the service.
                    service.Abort();
                    throw new ServiceProviderException("Service failed (FaultException) - see inner exception for more details", fault);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, LoggingEvents.ErrorEvent.UnexpectedException);
                    // Need to abort the service call to suppress an exception that can occure when closing the service.
                    service.Abort();
                    throw new ServiceProviderException("Service failed - see inner exception for more details", ex);
                }
            }
        }

        private static ResourceItem ToResourceItem(CalendarResourceType resource)
        {
            return new ResourceItem
            {
                Description = resource.CalendarResourceDescription,
                ExternalId = resource.RID,
                Id = resource.PlannerResourceIdentifier,
                IsCaseWorker = resource.IsCaseWorker,
                MailAddress = resource.CalendarResourceEmailAddress,
                Name = resource.CalendarResourceName,
                WorkingDayInfo =
                    resource.WorkingHours != null
                        ? resource.WorkingHours.Select(ToWorkHourItem).ToList()
                        : new List<WorkingHourItem>()
            };
        }

        private static WorkingHourItem ToWorkHourItem(WorkingHoursType workTime)
        {
            return new WorkingHourItem
            {
                EndTime = workTime.WorkingHoursEndTime.TimeOfDay,
                StartTime = workTime.WorkingHoursStartTime.TimeOfDay,
                WeekDay = (DayOfWeek)workTime.WorkingHoursWeekday
            };
        }

        private static ActiveOrganisationHeaderType OrgHeader(string departmentNumber)
        {
            return new ActiveOrganisationHeaderType
            {
                OrganisationCode = departmentNumber,
                OrganisationTypeIdentifier = 8 //JobCenterState
            };
        }

        private static RequestUserMetadataType BuildRequestUserMetadata(string deptNumber, string requestUserIdentifier, string methodName)
        {
            return new RequestUserMetadataType
            {
                RequestUserStructure = new RequestUserStructureType
                {
                    UserFullName = string.Format("PlannerCalendarClient : {0}", methodName),
                    UserIdentifier = requestUserIdentifier,
                    RequestUserTypeIdentifier = new RequestUserTypeIdentifierType
                    {
                        CodeListItemIdentifier = 3 // System
                    }
                },
                RegistrationDateTime = DateTime.Now,
                RequestOrganisationStructure = new RequestOrganisationStructureType
                {
                    OrganisationCode = deptNumber,
                    OrganisationTypeIdentifier = new OrganisationTypeIdentifierType
                    {
                        CodeListItemIdentifier = 8 // JobCenter
                    }
                }
            };
        }

        /// <summary>
        /// Map the Calendar event item format to planner's calendar format.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static CalendarEventType ToCalendarEventType(CalendarEventItem data)
        {
            var evtId = NullableGuid(data.PlannerCalendarEventId);
            var resId = NullableGuid(data.PlannerResourceId);
            return new CalendarEventType
            {
                CalendarEventIdentifier = evtId.HasValue ? evtId.Value : Guid.Empty,
                CalendarEventEndTime = data.End,
                OriginSystemIdentifier = data.OriginId,
                OriginEmailAddress = data.OriginMailAddress,
                PlannerResourceIdentifier = resId.HasValue ? resId.Value : Guid.Empty,
                CalendarEventStartTime = data.Start
            };
        }

        /// <summary>
        /// Map the calendar event from planner's format to Calendar event item.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static CalendarEventItem FromServiceData(CalendarEventType data)
        {
            return new CalendarEventItem
            {
                PlannerCalendarEventId = data.CalendarEventIdentifier,
                Start = data.CalendarEventStartTime,
                HasBeenDeleted = false,
                End = data.CalendarEventEndTime,
                OriginId = data.OriginSystemIdentifier,
                OriginMailAddress = data.OriginEmailAddress,
                PlannerResourceId = data.PlannerResourceIdentifier,
            };
        }

        private static Guid? NullableGuid(Guid? value)
        {
            if (!value.HasValue) return null;
            if (value.Value.Equals(Guid.Empty)) return null;
            return value.Value;
        }

        private ServiceCallReferenceItem MakeTheCall(IEnumerable<CalendarEventItem> calendarEvents, string departmentNumber, string requestUserIdentifier, EOperationName method, CallDelegate action)
        {
            var calendarEventItems = calendarEvents as IList<CalendarEventItem> ?? calendarEvents.ToList();
            if (!calendarEventItems.Any())
            {
                throw new InvalidOperationException("No CalendarEvent-items to process");
            }

            try
            {
                var calendarEventServiceItems = calendarEventItems.Select(ToCalendarEventType).ToList();

                Logger.LogInfo(LoggingEvents.InfoEvent.CallServiceMethod(method.ToString(), calendarEventServiceItems.Count(), _wcfEndpointName));

                using (var service = new PlannerExternalCalendarServiceClient(_wcfEndpointName))
                {
                    Logger.LogInfo(LoggingEvents.InfoEvent.EndpointUrl(service.Endpoint.Address.Uri.AbsoluteUri));

                    var startTime = DateTime.Now;
                    ServiceCallReferenceItem serviceCallLogRef;
                    try
                    {
                        CalendarEventReceiptType[] statuses;
                        var serviceCallResponseReferenceId = action.Invoke(_userCertificate, departmentNumber, requestUserIdentifier, calendarEventServiceItems, service, out statuses);

                        serviceCallLogRef = CreateServiceCallReference(serviceCallResponseReferenceId, startTime, method);
                        UpdateStatusOnCalendarEventItems(serviceCallLogRef, calendarEventItems, statuses);

                        Logger.LogInfo(LoggingEvents.InfoEvent.CallSuccess(serviceCallResponseReferenceId));
                    }
                    //catch (AMS.Planner.ServiceExtensions.SoapFaultException fault)
                    //{
                    //}
                    catch (FaultException<FaultDetails> fault)
                    {
                        var exMsg = string.Format("Service failed ({0})\nMessage: {1}\nDetail Error-code: {2}\nDetail Source: {3}\nDetail Message: {4}\nDetail Inner except: {5}",
                            method, fault.Message, fault.Detail.ErrorCode, fault.Detail.Source, fault.Detail.Message, fault.Detail.InnerException);
                        serviceCallLogRef = CreateExceptionServiceCallReference(exMsg, startTime, method);
                        UpdateExceptionStatusOnCalendarEventItems(serviceCallLogRef, calendarEventItems, fault.Detail.ErrorCode, exMsg);
                        Logger.LogError(fault, LoggingEvents.ErrorEvent.FaultDetailException);
                        // Need to abort the service call to suppress an exception that can occurre when closing the service.
                        service.Abort();
                    }
                    catch (FaultException fault)
                    {
                        // A catch for soap exception that is not created in the Planner code...
                        var exceptMsg = ExceptionUtils.ExceptionToStringMessage(fault);
                        var exMsg = string.Format("Service failed ({0})\nMessage: {1}", method, exceptMsg);
                        serviceCallLogRef = CreateExceptionServiceCallReference(exMsg, startTime, method);
                        UpdateExceptionStatusOnCalendarEventItems(serviceCallLogRef, calendarEventItems, LoggingEvents.ErrorEvent.FaultException.EventId, exMsg);
                        Logger.LogError(fault, LoggingEvents.ErrorEvent.FaultException);
                        // Need to abort the service call to suppress an exception that can occurre when closing the service.
                        service.Abort();
                    }
                    catch (TimeoutException timeoutEx)
                    {
                        // A catch for timeout exception that is not created in the Planner code...
                        var exceptMsg = ExceptionUtils.ExceptionToStringMessage(timeoutEx);
                        var exMsg = string.Format("Service failed ({0})\nMessage: {1}", method, exceptMsg);
                        serviceCallLogRef = CreateExceptionServiceCallReference(exMsg, startTime, method);
                        UpdateExceptionStatusOnCalendarEventItems(serviceCallLogRef, calendarEventItems, LoggingEvents.ErrorEvent.TimeoutException.EventId, exMsg);
                        Logger.LogError(timeoutEx, LoggingEvents.ErrorEvent.TimeoutException);
                        // Need to abort the service call to suppress an exception that can occurre when closing the service.
                        service.Abort();
                    }
                    catch (WebException webEx)
                    {
                        // A catch for web exception that is not created in the Planner code...
                        var exceptMsg = ExceptionUtils.ExceptionToStringMessage(webEx);
                        var exMsg = string.Format("Service failed ({0})\nMessage: {1}", method, exceptMsg);
                        serviceCallLogRef = CreateExceptionServiceCallReference(exMsg, startTime, method);
                        UpdateExceptionStatusOnCalendarEventItems(serviceCallLogRef, calendarEventItems, LoggingEvents.ErrorEvent.WebException.EventId, exMsg);
                        Logger.LogError(webEx, LoggingEvents.ErrorEvent.WebException);
                        // Need to abort the service call to suppress an exception that can occurre when closing the service.
                        service.Abort();
                    }
                    catch (Exception ex)
                    {
                        var exceptMsg = ExceptionUtils.ExceptionToStringMessage(ex);
                        var exMsg = string.Format("Service failed ({0})\n{1}", method, exceptMsg);
                        serviceCallLogRef = CreateExceptionServiceCallReference(exMsg, startTime, method);
                        UpdateExceptionStatusOnCalendarEventItems(serviceCallLogRef, calendarEventItems, LoggingEvents.ErrorEvent.UnexpectedException.EventId, exMsg);
                        Logger.LogError(ex, LoggingEvents.ErrorEvent.UnexpectedException);
                        // Need to abort the service call to suppress an exception that can occurre when closing the service.
                        service.Abort();
                    }

                    return serviceCallLogRef;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LoggingEvents.ErrorEvent.UnexpectedException);
                throw;
            }
        }

        private static ServiceCallReferenceItem CreateExceptionServiceCallReference(string exMessage, DateTime startTime, EOperationName method)
        {
            DateTime endTime = DateTime.Now;
            var serviceCallLogRef = new ServiceCallReferenceItem
            {
                ServiceCallResponseReferenceId = null,
                OperationName = method,
                CallStarted = startTime,
                CallEnded = endTime,
                Success = false,
                ResponsText = exMessage
            };

            return serviceCallLogRef;
        }

        private static void UpdateExceptionStatusOnCalendarEventItems(ServiceCallReferenceItem serviceCallReference, IEnumerable<CalendarEventItem> calendarEvents, int exceptionCode, string exMessage)
        {
            foreach (var currentCalEventItem in calendarEvents)
            {
                currentCalEventItem.SyncLogItem =
                    new SyncLogItem
                    {
                        SyncDate = serviceCallReference.CallStarted,
                        OperationName = serviceCallReference.OperationName,
                        PlannerSyncSuccess = false,
                        PlannerEventErrorCode = exceptionCode,
                        PlannerSyncResponse = exMessage,
                        ServiceCallReferenceId = serviceCallReference.ServiceCallResponseReferenceId.HasValue ? serviceCallReference.ServiceCallResponseReferenceId.Value : Guid.Empty
                    };
            }
        }

        private static ServiceCallReferenceItem CreateServiceCallReference(Guid serviceCallResponseReferenceId, DateTime startTime, EOperationName method)
        {
            DateTime endTime = DateTime.Now;

            var serviceCallLogRef = new ServiceCallReferenceItem
            {
                ServiceCallResponseReferenceId = serviceCallResponseReferenceId,
                OperationName = method,
                CallStarted = startTime,
                CallEnded = endTime,
                Success = true,
                ResponsText = ""
            };

            return serviceCallLogRef;
        }

        private static void UpdateStatusOnCalendarEventItems(ServiceCallReferenceItem serviceCallReference, IEnumerable<CalendarEventItem> calendarEvents, CalendarEventReceiptType[] statuses)
        {
            int len = statuses.Count();
            int i = 0;

            foreach (var currentCalEventItem in calendarEvents)
            {
                CalendarEventReceiptType currentStatus = null;

                if (!(i < len))
                {
                    throw new ServiceProviderOperationException(
                        string.Format("CalendarEventStatusType from request was not found in response (ID={0})",
                            currentCalEventItem.OriginId));
                }

                // Statuses array should map to calendarEvents by both CallId and Mailbox. In release 2015-02(2015.07), mailbox is not included in statuses array.
                // Here is to use array index plus CallId for mapping. The correct solution is to include Mailbox in statues array.
                if ( statuses[i].OriginSystemIdentifier.Equals(currentCalEventItem.OriginId, StringComparison.InvariantCultureIgnoreCase))
                {
                    currentStatus = statuses[i];
                }
 
                if (currentStatus == null)
                {
                    // We have to throw this Exception because if an item is missing the service has
                    // thrown it away and this is considered a critical error.
                    throw new ServiceProviderOperationException(
                        string.Format("CalendarEventStatusType from request was not found in response (ID={0})",
                            currentCalEventItem.OriginId));
                }

                // Verify the the PlannerCalendarEventId do not change when it has first been set
                if (currentCalEventItem.PlannerCalendarEventId.HasValue == false)
                {
                    currentCalEventItem.PlannerCalendarEventId = currentStatus.CalendarEventIdentifier;
                }
                else
                {
                    // Assert that the received Planner CalendarEventId is the same as the exist one!
                    //Debug.Assert(currentCalEventItem.PlannerCalendarEventId == currentStatus.CalendarEventIdentifier,
                    //    "The received Planner CalendarEventId is not equal with the existing PlannerCalendarEventId!");
                }

                currentCalEventItem.PlannerResourceId = currentStatus.PlannerResourceIdentifier;

                currentCalEventItem.SyncLogItem =
                    new SyncLogItem
                    {
                        SyncDate = currentStatus.EventReceivalTime,
                        OperationName = serviceCallReference.OperationName,
                        PlannerSyncSuccess = (currentStatus.EventErrorCode != null && currentStatus.EventErrorCode.CodeListItemIdentifier == 0),
                        PlannerEventErrorCode = (currentStatus.EventErrorCode == null) ? 0 : currentStatus.EventErrorCode.CodeListItemIdentifier,
                        PlannerSyncResponse = currentStatus.EventMessage,
                        PlannerConflictNotificationSent = currentStatus.ConflictNotificationSent,
                        ServiceCallReferenceId = serviceCallReference.ServiceCallResponseReferenceId.HasValue ? serviceCallReference.ServiceCallResponseReferenceId.Value : Guid.Empty,
                    };

                i = i + 1;
            }
        }

        private static void UpdateGetEventsStatusOnCalendarEventItems(ServiceCallReferenceItem serviceCallReference, IEnumerable<CalendarEventItem> calendarEvents)
        {
            foreach (var currentCalEventItem in calendarEvents)
            {
                currentCalEventItem.SyncLogItem =
                    new SyncLogItem
                    {
                        SyncDate = serviceCallReference.CallStarted,
                        OperationName = serviceCallReference.OperationName,
                        PlannerSyncSuccess = true,
                        PlannerSyncResponse = "",
                        PlannerEventErrorCode = 0,
                        ServiceCallReferenceId = serviceCallReference.ServiceCallResponseReferenceId.HasValue ? serviceCallReference.ServiceCallResponseReferenceId.Value : Guid.Empty,
                    };
            }
        }

        private static Guid MakeDeleteCall(X509Certificate certificate, string deptNumber, string requestUserIdentifier, IEnumerable<CalendarEventType> items, PlannerExternalCalendarServiceClient service, out CalendarEventReceiptType[] statuses)
        {
            var request = new DeleteEventsRequest
            {
                ActiveOrganisationHeader = OrgHeader(deptNumber),
                DeleteEventsRequest1 = items.ToArray(),
                RequestUserMetadataHeader = BuildRequestUserMetadata(deptNumber, requestUserIdentifier, "DeleteEvents")
            };
            var response = service.DeleteEvents(request);
            var responseReference = response.DeleteEventsResponse1.ServiceRequestLogReferenceIdentifier;
            statuses = response.DeleteEventsResponse1.CalendarEventReceipt;
            return responseReference;
        }

        private static Guid MakeUpdateCall(X509Certificate certificate, string deptNumber, string requestUserIdentifier, IEnumerable<CalendarEventType> items, PlannerExternalCalendarServiceClient service, out CalendarEventReceiptType[] statuses)
        {
            var request = new UpdateEventsRequest
            {
                ActiveOrganisationHeader = OrgHeader(deptNumber),
                UpdateEventsRequest1 = items.ToArray(),
                RequestUserMetadataHeader = BuildRequestUserMetadata(deptNumber, requestUserIdentifier, "UpdateEvents")
            };
            var response = service.UpdateEvents(request);
            var responseReference = response.UpdateEventsResponse1.ServiceRequestLogReferenceIdentifier;
            statuses = response.UpdateEventsResponse1.CalendarEventReceipt;
            return responseReference;
        }

        private static Guid MakeCreateCall(X509Certificate certificate, string deptNumber, string requestUserIdentifier, IEnumerable<CalendarEventType> items, PlannerExternalCalendarServiceClient service, out CalendarEventReceiptType[] statuses)
        {
            var request = new CreateEventsRequest
            {
                ActiveOrganisationHeader = OrgHeader(deptNumber),
                CreateEventsRequest1 = items.ToArray(),
                RequestUserMetadataHeader = BuildRequestUserMetadata(deptNumber, requestUserIdentifier, "CreateEvents")
            };
            var response = service.CreateEvents(request);
            var responseReference = response.CreateEventsResponse1.ServiceRequestLogReferenceIdentifier;
            statuses = response.CreateEventsResponse1.CalendarEventReceipt;
            return responseReference;
        }
    }
}
