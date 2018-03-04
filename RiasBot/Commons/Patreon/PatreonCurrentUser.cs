using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Commons.Patreon
{
    public class PatreonCurrentUser
    {
        public IncludedCurrentUser[] Included { get; set; }
    }

    public class IncludedCurrentUser
    {
        public RelationshipsCurrentUser Relationships { get; set; }
    }

    public class RelationshipsCurrentUser
    {
        public CampaignCurrentUser Campaign { get; set; }
    }

    public class CampaignCurrentUser
    {
        public CampaignDataCurrentUser Data { get; set; }
    }

    public class CampaignDataCurrentUser
    {
        public int Id { get; set; }
    }
}
