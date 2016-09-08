using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptainSmackDown2.Api.Models
{
    public class VoteSummary
    {
        public string CaptainType { get; set; }
        public string Captain { get; set; }
        public long Votes { get; set; }
    }
}
