using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Commons.Patreon
{
    public class PatreonCampaign
    {
        public PatronPledge[] Data { get; set; }
        public PatronUser[] Included { get; set; }
    }
}
