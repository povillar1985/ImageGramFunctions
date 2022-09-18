using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGramFunctions.Models.Requests
{
    public class CreateCommentsRequest
    {
        public string PostId { get; set; }
        public string Comments { get; set; }
    }
}
