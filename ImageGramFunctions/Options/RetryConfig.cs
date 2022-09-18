using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGramFunctions.Options
{
    public class RetryConfig
    {
        public int MessageExpiration { get; set; }

        public int ScheduledEnqueueTime { get; set; }
    }
}
