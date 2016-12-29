using System;
using System.Collections.Generic;
using System.Linq;
using PlannerCalendarClient.Logging;
using PlannerCalendarClient.DataAccess;
using PlannerCalendarClient.ServiceDfdg;

namespace PlannerCalendarClient.PlannerCommunicatorService
{
    public class SubscriberResources
    {
        private static readonly ILogger Logger = Logging.Logger.GetLogger();

        private readonly IServiceRepository _serviceRepository;
        private readonly IECSClientExchangeDbEntities _entities;
        private System.Data.Entity.DbSet<PlannerResourceBlacklist> _blacklistedMails;
        private System.Data.Entity.DbSet<PlannerResourceWhitelist> _whitelistedMails;
        private List<ResourceItem> _plannerResources;
        private readonly string _jobcenterNumber;
        private readonly string _requestUserIdentifier;

        /// <summary>
        /// .ctor
        /// </summary>
        public SubscriberResources(IECSClientExchangeDbEntities entities, IServiceRepository serviceRepository, string jobcenterNumber, string requestUserIdentifier)
        {
            if (entities == null) throw new ArgumentNullException("entities");
            if (serviceRepository == null) throw new ArgumentNullException("serviceRepository");
            if (string.IsNullOrWhiteSpace(jobcenterNumber)) throw new ArgumentNullException("jobcenterNumber");
            if (string.IsNullOrWhiteSpace(requestUserIdentifier)) throw new ArgumentNullException("requestUserIdentifier");

            _entities = entities;
            _serviceRepository = serviceRepository;
            _jobcenterNumber = jobcenterNumber;
            _requestUserIdentifier = requestUserIdentifier;
        }

        /// <summary>
        /// Invoking this method is getting a list of resources from Planner (service-call) and comparing
        /// it to the local database-version. It will add new resources and mark old as deleted.
        /// Note it will also handle (and remove) duplicates of mailaddresses - meaning if a mailaddress
        /// is duplicated, it will not be handled at all!!
        /// Also not that it will load and filter return from Planner with the whitelist from the
        /// database (if any mailaddresses added to the whitelist - if nothing in the whitelist, all
        /// mailaddresses are accepted).
        /// </summary>
        public void UpdateSubscriberResources()
        {
            ServiceCallReferenceItem serviceCallReference;
            _plannerResources =
                _serviceRepository.GetResources(_jobcenterNumber, _requestUserIdentifier, new List<string>(), out serviceCallReference).ToList();

            if (!_plannerResources.Any())
            {
                Logger.LogWarning(LoggingEvents.WarningEvent.NoResourcesFoundInPlanner());
                IdentifyAndUpdateExistingAndRemoveMissing();
                return;
            }
            Logger.LogDebug(LoggingEvents.DebugEvent.UpdatedResourcesEndServiceCall(_plannerResources.Count, _jobcenterNumber));

            _blacklistedMails = _entities.PlannerResourceBlacklists;
            Logger.LogDebug(LoggingEvents.DebugEvent.LoadedBlacklistFromDatabase(_blacklistedMails.Count()));

            _whitelistedMails = _entities.PlannerResourceWhitelists;
            Logger.LogDebug(LoggingEvents.DebugEvent.LoadedWhitelistFromDatabase(_whitelistedMails.Count()));

            HandleDuplicates();
            IdentifyAndAddNew();
            IdentifyAndAddNewNonWhitelisted();
            IdentifyAndUpdateExistingAndRemoveMissing();
        }

        private void HandleDuplicates()
        {
            var affected = 0;
            var distinctMailaddresses = _plannerResources.Select(x => x.MailAddress).Distinct().ToList();
            foreach (var distinct in distinctMailaddresses)
            {
                var nettoList =
                    _plannerResources.Where(
                        x => x.MailAddress.Equals(distinct, StringComparison.InvariantCultureIgnoreCase)).ToList();
                if (nettoList.Count() > 1)
                {
                    Logger.LogWarning(LoggingEvents.WarningEvent.DuplicateInstanceOfMailAddress(distinct));

                    foreach (var toRemove in nettoList)
                    {
                        _plannerResources.Remove(toRemove);
                        affected++;
                    }
                }
            }
            Logger.LogDebug(LoggingEvents.DebugEvent.RemovingDuplicates(affected));
        }

        private void IdentifyAndUpdateExistingAndRemoveMissing()
        {
            var deletedMails = new List<string>();
            var updatedMails = new List<string>();
            var blacklistedMails = new List<string>();
            var whitelistedMails = new List<string>();

            if (!_whitelistedMails.Any())
            {
                Logger.LogInfo(LoggingEvents.InfoEvent.NoWhitelistFilterApplied());
            }

            foreach (var dbItem in _entities.PlannerResources)
            {
                // If mailaddress exists on the blacklist, do not touch the local (in entities) item
                if (_blacklistedMails.Any(x => x.MailAddress.Equals(dbItem.MailAddress, StringComparison.CurrentCultureIgnoreCase)))
                {
                    blacklistedMails.Add(dbItem.MailAddress);
                    continue;
                }

                // If whitelist contains any items and mailaddress is not in the list, mark it as "deleted"
                if (_whitelistedMails.Any() &&
                    !_whitelistedMails.Any(
                        x => x.MailAddress.Equals(dbItem.MailAddress, StringComparison.CurrentCultureIgnoreCase)))
                {
                    if (!dbItem.DeletedDate.HasValue)
                    {
                        dbItem.DeletedDate = DateTime.Now;
                        dbItem.ErrorCode = "P-NonWhitelist";
                        dbItem.ErrorDescription = "Mailaddress was not whitelisted";
                        dbItem.ErrorDate = DateTime.Now;
                    }
                    whitelistedMails.Add(dbItem.MailAddress);

                    continue;
                }

                var updatedCurrent = false;
                var plannerItem =
                    _plannerResources.FirstOrDefault(
                        x => x.MailAddress.Equals(dbItem.MailAddress, StringComparison.InvariantCultureIgnoreCase));
                if (plannerItem == null)
                {
                    dbItem.DeletedDate = DateTime.Now;
                    dbItem.ErrorCode = "P-NotInPlanner";
                    dbItem.ErrorDescription = "Mailaddress was not found in list from Planner";
                    dbItem.ErrorDate = DateTime.Now;
                    deletedMails.Add(dbItem.MailAddress);

                    continue;
                }
                if (dbItem.DeletedDate.HasValue || dbItem.ErrorCode != null)
                {
                    dbItem.DeletedDate = null;
                    dbItem.GroupAffinity = null;
                    dbItem.ErrorCode = null;
                    dbItem.ErrorDescription = null;
                    dbItem.ErrorDate = null;
                    updatedCurrent = true;
                }
                if (!AreNullableGuidEqual(dbItem.PlannerResourceId, plannerItem.Id))
                {
                    dbItem.PlannerResourceId = plannerItem.Id;
                    updatedCurrent = true;
                }
                if (updatedCurrent)
                {
                    dbItem.UpdatedDate = DateTime.Now;
                    updatedMails.Add(dbItem.MailAddress);
                }
            }
            Logger.LogDebug(LoggingEvents.DebugEvent.RemovingResources(deletedMails.Count(), string.Join(", ", deletedMails.ToArray())));
            Logger.LogDebug(LoggingEvents.DebugEvent.UpdatedResources(updatedMails.Count, string.Join(", ", updatedMails.ToArray())));
            Logger.LogDebug(LoggingEvents.DebugEvent.BlacklistedResources(blacklistedMails.Count, string.Join(", ", blacklistedMails.ToArray())));
            Logger.LogDebug(LoggingEvents.DebugEvent.WhitelistedResources(whitelistedMails.Count, string.Join(", ", whitelistedMails.ToArray())));
        }

        private bool AreNullableGuidEqual(Guid? valueA, Guid? valueB)
        {
            if (!valueA.HasValue || !valueB.HasValue)
            {
                return false;
            }
            return valueA.Value.Equals(valueB.Value);
        }

        private void IdentifyAndAddNew()
        {
            var existing = _entities.PlannerResources;

            var nettoList = _plannerResources
                .Where(
                    resource =>
                        !existing.Any(
                            x => x.MailAddress.Equals(resource.MailAddress, StringComparison.InvariantCultureIgnoreCase)) &&
                        !_blacklistedMails.Any(
                            x => x.MailAddress.Equals(resource.MailAddress, StringComparison.CurrentCultureIgnoreCase)) &&
                        ((_whitelistedMails.Any() && _whitelistedMails.Any(
                            x => x.MailAddress.Equals(resource.MailAddress, StringComparison.CurrentCultureIgnoreCase)))
                          || !_whitelistedMails.Any()))
                .Select(x => new PlannerResource
                {
                    CreatedDate = DateTime.Now,
                    DeletedDate = null,
                    GroupAffinity = null,
                    MailAddress = x.MailAddress,
                    PlannerResourceId = x.Id,
                    UpdatedDate = DateTime.Now
                }).ToArray();

            _entities.PlannerResources.AddRange(nettoList);

            Logger.LogDebug(LoggingEvents.DebugEvent.IdentifyingNewResources(nettoList.Count(), string.Join(", ",nettoList.Select( r => r.MailAddress).ToArray())));
        }

        private void IdentifyAndAddNewNonWhitelisted()
        {
            var existing = _entities.PlannerResources;

            var whitelist = _plannerResources
                .Where(
                    resource =>
                        !existing.Any(
                            x => x.MailAddress.Equals(resource.MailAddress, StringComparison.InvariantCultureIgnoreCase)) &&
                        (_whitelistedMails.Any() && !_whitelistedMails.Any(
                            x => x.MailAddress.Equals(resource.MailAddress, StringComparison.CurrentCultureIgnoreCase))))
                .Select(x => new PlannerResource
                {
                    CreatedDate = DateTime.Now,
                    GroupAffinity = null,
                    MailAddress = x.MailAddress,
                    PlannerResourceId = x.Id,
                    UpdatedDate = DateTime.Now,
                    DeletedDate = DateTime.Now,
                    ErrorCode = "P-NonWhitelist",
                    ErrorDescription = "Mailaddress was not whitelisted",
                    ErrorDate = DateTime.Now,
                }).ToArray();

            _entities.PlannerResources.AddRange(whitelist);

            Logger.LogDebug(LoggingEvents.DebugEvent.IdentifyingNewNonWhitelistedResources(whitelist.Count(), string.Join(", ", whitelist.Select(r => r.MailAddress).ToArray())));
        }
    }
}