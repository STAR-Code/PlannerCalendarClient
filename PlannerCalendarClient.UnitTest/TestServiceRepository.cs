using System;
using System.Collections.Generic;
using System.Linq;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.UnitTest
{
    public class TestServiceRepository : IServiceRepository
    {
        private ServiceCallReferenceItem CreateServiceCallReferenceItem()
        {
            return new ServiceCallReferenceItem
            {
                ServiceCallResponseReferenceId = Guid.NewGuid(),
                OperationName = EOperationName.CreateEvents,
                CallStarted = DateTime.Now,
                CallEnded = DateTime.Now,
                Success = true
            };
        }

        private IEnumerable<ResourceItem> GetDepartmentResources(string departmentNumber)
        {
            switch (departmentNumber)
            {
                case "10100":
                    return new List<ResourceItem>
                    {
                        new ResourceItem
                        {
                            Id = Guid.NewGuid(),
                            IsCaseWorker = true,
                            MailAddress = "person1@kk10100.kk",
                            Name = "Person 1 in KK"
                        },
                        new ResourceItem
                        {
                            Id = Guid.NewGuid(),
                            IsCaseWorker = true,
                            MailAddress = "person2@kk10100.kk",
                            Name = "Person 2 in KK"
                        },
                        new ResourceItem
                        {
                            Id = Guid.NewGuid(),
                            IsCaseWorker = true,
                            MailAddress = "person3@kk10100.kk",
                            Name = "Person 3 in KK"
                        },
                        new ResourceItem
                        {
                            Id = Guid.NewGuid(),
                            IsCaseWorker = true,
                            MailAddress = "person4@kk10100.kk",
                            Name = "Person 4 in KK"
                        },
                        new ResourceItem
                        {
                            Id = Guid.NewGuid(),
                            IsCaseWorker = true,
                            MailAddress = "person4@kk10100.kk",
                            Name = "Person 4 in KK"
                        },
                        new ResourceItem
                        {
                            Id = Guid.NewGuid(),
                            IsCaseWorker = true,
                            MailAddress = "person5@kk10100.kk",
                            Name = "Person 5 in KK"
                        },
                    };
                default:
                    return new List<ResourceItem>();
            }
        }

        public ServiceCallReferenceItem CreateEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier)
        {
            var response = CreateServiceCallReferenceItem();

            foreach (var item in calendarEventItems)
            {
                item.SyncLogItem = new SyncLogItem
                {
                    OperationName = EOperationName.CreateEvents,
                    PlannerConflictNotificationSent = false,
                    PlannerEventErrorCode = 0,
                    PlannerSyncSuccess = true,
                    ServiceCallReferenceId = response.ServiceCallResponseReferenceId.Value,
                    SyncDate = response.CallEnded
                };
            }

            return response;
        }

        public ServiceCallReferenceItem UpdateEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier)
        {
            throw new NotImplementedException();
        }

        public ServiceCallReferenceItem DeleteEvents(IEnumerable<CalendarEventItem> calendarEventItems, string departmentNumber, string requestUserIdentifier)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CalendarEventItem> GetEvents(string departmentNumber, string requestUserIdentifier, IEnumerable<string> mailAddresses, DateTime fromDate, DateTime toDate, out ServiceCallReferenceItem serviceCallReferenceItem)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourceItem> GetResources(string departmentNumber, string requestUserIdentifier, IEnumerable<string> mailAddresses, out ServiceCallReferenceItem serviceCallReferenceItem)
        {
            serviceCallReferenceItem = CreateServiceCallReferenceItem();
            var response = GetDepartmentResources(departmentNumber);
            if (mailAddresses != null && mailAddresses.Any())
            {
                response =
                    response.Where(x => mailAddresses.Contains(x.MailAddress, StringComparer.InvariantCultureIgnoreCase));
            }
            return response;
        }
    }
}
