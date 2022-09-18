using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGramFunctions.Models
{
    public class CommentModel
    {
        public string PostId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Comment { get; set; }
    }
}
