using Microsoft.AspNetCore.Http;

namespace ImageGramFunctions.Models.Requests
{
    public class UploadImageRequest
    {
        public string ImageCaption { get; set; }
        public IFormFile ImageFile { get; set; }
    }
}
