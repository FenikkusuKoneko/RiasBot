using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Commons.Patreon
{
    public class PatronPledge
    {
        public AttributesPatron attributes { get; set; }
        public int id { get; set; }
        public RelationshipsPatron relationships { get; set; }
        public string type { get; set; }

    }

    public class AttributesPatron
    {
        public int amount_cents { get; set; }
        public string created_at { get; set; }
        public object declined_since { get; set; }
        public bool is_twitch_pledge { get; set; }
        public bool patron_pays_fees { get; set; }
        public int? pledge_cap_cents { get; set; }
    }

    public class RelationshipsPatron
    {
        public AdressPatron address { get; set; }
        public Creator creator { get; set; }
        public Patron patron { get; set; }
    }

    public class AdressPatron
    {
        public string data { get; set; }
    }

    public class Creator
    {
        public Data data { get; set; }
        public Links links { get; set; }
    }

    public class Patron
    {
        public Data data { get; set; }
        public Links links { get; set; }
    }

    public class Data
    {
        public int id { get; set; }
        public string type { get; set; }
    }

    public class Links
    {
        public string related { get; set; }
    }
}
