using System.Collections.Generic;
using System.Linq;

namespace PlannerCalendarClient.ExchangeStreamingService.Affinity
{
    class SubscriptionGroupDictionary : IEnumerable<KeyValuePair<string, SubscriptionGroupDictionary.SubscriptionGroup>>
    {
        public class SubscriptionGroup
        {
            public SubscriptionGroup(string groupName, string serviceUser, string servicePassword)
            {
                GroupName = groupName;
                ServiceUser = serviceUser;
                ServicePassword = servicePassword;
                Mails = new List<string>();
            }

            public string GroupName { get; private set; }
            public List<string> Mails { get; private set; }
            public string ServiceUser { get; private set; }
            public string ServicePassword { get; private set; }
        }

        private readonly  Dictionary<string, SubscriptionGroup> _dic = new Dictionary<string, SubscriptionGroup>();

        public string[] GroupNames
        {
            get { return _dic.Keys.ToArray(); }
        }

        public SubscriptionGroup Group(string groupName)
        {
            return _dic[groupName];
        }

        public bool ContainsGroup(string groupName)
        {
            return _dic.ContainsKey(groupName);
        }

        public void CreateGroup(string groupName, string serviceUser, string servicePassword)
        {
            var subGroup = new SubscriptionGroup(groupName, serviceUser, servicePassword);
            _dic.Add(subGroup.GroupName, subGroup);
        }

        public void AddMailToGroup(string groupName, string mail)
        {
            _dic[groupName].Mails.Add(mail);
        }

        #region implement IEnumerable

        public IEnumerator<KeyValuePair<string, SubscriptionGroupDictionary.SubscriptionGroup>> GetEnumerator()
        {
            return _dic.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dic.GetEnumerator();
        }

        #endregion
    }
}