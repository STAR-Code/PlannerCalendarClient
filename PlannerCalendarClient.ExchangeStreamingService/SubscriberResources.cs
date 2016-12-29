//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using ExternalCalendarService.Client.Business;
//using ExternalCalendarService.Client.Domain;
//using ExternalCalendarService.Client.Exchange.DataAccess;
//using log4net;

//namespace ExternalCalendarService.Client.ExchangeStreamingService
//{
//    class SubscriberResources
//    {
//        private static ILog _log = LogManager.GetLogger(typeof(StreamingManager));

//        /// <summary>
//        /// 
//        /// </summary>
//        private readonly string _rootDepartmentNumber;
//        /// <summary>
//        /// 
//        /// </summary>
//        private readonly IServiceFactoryMethods _factory;


//        private List<PlannerResourceItem> _subscribers;

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="rootDepartmentNumber"></param>
//        /// <param name="factory"></param>
//        public SubscriberResources(string rootDepartmentNumber, IServiceFactoryMethods factory)
//        {
//            _rootDepartmentNumber = rootDepartmentNumber;
//            _factory = factory;
//        }

//        public void UpdateSubscriberResources()
//        {
//            try
//            {
//                var rootDepartment = GetRootDepartmentItem();

//                var serviceProvider = _factory.ServiceProviderBusiness();

//                ServiceCallReferenceItem serviceCallReference;
//                IEnumerable<ResourceItem> resources = serviceProvider.GetResources(rootDepartment, new List<string>(),
//                    out serviceCallReference);

//                _subscribers = (resources.Select(i => new PlannerResourceItem(i))).ToList();
//                var repository = new PlannerResourceRepository();
//                repository.Save(_subscribers);
//            }
//            catch (Exception ex)
//            {
//                _log.Error("UpdateSubscriberResources throw an exception.", ex);
//            }
//        }

//        public List<IPlannerResourceItem> GetSubscriberResources()
//        {
//            var repository = new PlannerResourceRepository();
//            return repository.Load().ToList();
//        }

//        private DepartmentItem GetRootDepartmentItem() {  return new DepartmentItem(Guid.Empty, _rootDepartmentNumber, null, ""); }
//    }
//}
