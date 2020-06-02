using System;
using System.Collections.Generic;
using System.Linq;

namespace iess_api.Models
{
    interface ISupportFiltering
    {
        bool CheckTextFilter(string filter);

        bool CheckGroupFilters(List<string> groupfilters);

        bool CheckCreator(string senderId);

        bool CheckDateStatus(List<string> dateLabels);
    }

    public class BriefPollBaseResponse:ISupportFiltering
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public IList<string> EligibleGroups { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CreatorUserId { get; set; }
        public bool? IsAllowedToReanswer { get; set; }
        public bool? AreStatsPublic { get; set; }
        public bool? HasAnswer { get; set; }
        public bool? IsAnswerCompleted { get; set; }

        public bool CheckTextFilter(string filter)
        {
            return FirstName.ToLower().Contains(filter) ||LastName.ToLower().Contains(filter) ||Title.ToLower().Contains(filter);
        }

        public bool CheckGroupFilters(List<string> groupfilters)
        {
            return groupfilters.All(group => EligibleGroups.Contains(group));
        }

        public bool CheckCreator(string senderId)
        {
            return CreatorUserId == senderId;
        }

        public bool CheckDateStatus(List<string> dateLabels)
        {
            return (dateLabels.Contains("notstarted") && StartDate > DateTime.Now) ||
                   (dateLabels.Contains("live") && StartDate < DateTime.Now && ExpireDate > DateTime.Now) ||
                   (dateLabels.Contains("ended") && ExpireDate < DateTime.Now);
        }
    }

    public class HistoryPollBaseResponse:ISupportFiltering
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public IList<string> EligibleGroups { get; set; }
        public string PollBaseCreatorId { get; set; }
        public string AnswerId { get; set; }
        public bool? IsCompleted { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? AnswerDate { get; set; }

        public bool CheckTextFilter(string filter)
        {
            return FirstName.ToLower().Contains(filter) ||LastName.ToLower().Contains(filter) ||Title.ToLower().Contains(filter);
        }

        public bool CheckGroupFilters(List<string> groupfilters)
        {
            return groupfilters.All(group => EligibleGroups.Contains(group));
        }

        public bool CheckCreator(string senderId)
        {
            return PollBaseCreatorId == senderId;
        }

        public bool CheckDateStatus(List<string> dateLabels)
        {
            return (dateLabels.Contains("notstarted") && StartDate > DateTime.Now) ||
                   (dateLabels.Contains("live") && StartDate < DateTime.Now && ExpireDate > DateTime.Now) ||
                   (dateLabels.Contains("ended") && ExpireDate < DateTime.Now);
        }
    }
}
