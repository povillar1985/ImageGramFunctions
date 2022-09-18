using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageGramFunctions.Messages
{
    public class ProcessCreatePostMessage: BaseMessage
    {
        public IFormFile ImageFile { get; set; }
        public string ImageCaption { get; set; }
    }
}
