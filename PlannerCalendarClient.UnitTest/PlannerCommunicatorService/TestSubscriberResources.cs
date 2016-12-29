using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.PlannerCommunicatorService;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.UnitTest.PlannerCommunicatorService
{
    [TestClass]
    public class TestSubscriberResources
    {
        private IServiceRepository _service;
        private string _jobcenterNummer;
        private string _requestUserIdentifier;

        [TestInitialize]
        public void SetupTest()
        {
            _service = new TestServiceRepository();
            _jobcenterNummer = "10100";
            _requestUserIdentifier = "UserId";
        }

        private TestEntities SetupDataSimpleList()
        {
            var db = new TestEntities();
            db.PlannerResources.RemoveRange(db.PlannerResources.ToList());
            db.PlannerResources.AddRange(new[]
            {
                new PlannerResource
                {
                    Id = 1,
                    MailAddress = "person1@kk10100.kk",
                    CreatedDate = DateTime.Now,
                    PlannerResourceId = Guid.NewGuid(),
                    GroupAffinity = "GRP1"
                },
                new PlannerResource
                {
                    Id = 2,
                    MailAddress = "person2@kk10100.kk",
                    CreatedDate = DateTime.Now,
                    PlannerResourceId = Guid.NewGuid(),
                    GroupAffinity = "GRP1"
                },
                new PlannerResource
                {
                    Id = 3,
                    MailAddress = "person3@kk10100.kk",
                    CreatedDate = DateTime.Now,
                    PlannerResourceId = Guid.NewGuid(),
                    GroupAffinity = "GRP1"
                },
                new PlannerResource
                {
                    Id = 6,
                    MailAddress = "person6@kk10100.kk",
                    CreatedDate = DateTime.Now,
                    PlannerResourceId = Guid.NewGuid(),
                    GroupAffinity = "GRP1"
                }
            });
            return db;
        }

        [TestMethod]
        public void AllSameNoUpdate()
        {
            var db = SetupDataSimpleList();
            var subRes = new SubscriberResources(db, _service, _jobcenterNummer, _requestUserIdentifier);
            subRes.UpdateSubscriberResources();
            Assert.IsTrue(db.PlannerResources.Any(x => x.MailAddress.Equals("person1@kk10100.kk")));
            Assert.IsTrue(db.PlannerResources.Any(x => x.MailAddress.Equals("person2@kk10100.kk")));
            Assert.IsTrue(db.PlannerResources.Any(x => x.MailAddress.Equals("person3@kk10100.kk")));
            Assert.IsTrue(db.PlannerResources.Any(x => x.MailAddress.Equals("person5@kk10100.kk")));
            Assert.AreEqual(4, db.PlannerResources.Where(x => !x.DeletedDate.HasValue).Count());
        }

        [TestMethod]
        public void RemovedDuplicates()
        {
            var db = SetupDataSimpleList();
            var subRes = new SubscriberResources(db, _service, _jobcenterNummer, _requestUserIdentifier);
            subRes.UpdateSubscriberResources();
            Assert.IsFalse(db.PlannerResources.Any(x => x.MailAddress.Equals("person4@kk10100.kk")));
            Assert.AreEqual(4, db.PlannerResources.Where(x => !x.DeletedDate.HasValue).Count());
        }

        [TestMethod]
        public void RemovedFromPlanner()
        {
            var db = SetupDataSimpleList();
            var subRes = new SubscriberResources(db, _service, _jobcenterNummer, _requestUserIdentifier);
            subRes.UpdateSubscriberResources();
            Assert.IsTrue(db.PlannerResources.Any(x => x.DeletedDate.HasValue));
        }

        [TestMethod]
        public void AddNew()
        {
            var db = SetupDataSimpleList();
            var subRes = new SubscriberResources(db, _service, _jobcenterNummer, _requestUserIdentifier);
            subRes.UpdateSubscriberResources();
            Assert.IsFalse(db.PlannerResources.Any(x => x.MailAddress.Equals("person4@kk10100.kk")));
            Assert.IsTrue(db.PlannerResources.Any(x => x.MailAddress.Equals("person5@kk10100.kk")));
            Assert.AreEqual(4, db.PlannerResources.Where(x => !x.DeletedDate.HasValue).Count());
        }
    }
}
