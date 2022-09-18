using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGramFunctions.Messages
{
    public class ProcessCreateCommentsMessage
    {
        public string PostId { get; set; }
        public string Comments { get; set; }
    }
}
