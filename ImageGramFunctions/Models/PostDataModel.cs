using System.Collections.Generic;

namespace ImageGramFunctions.Models
{
    /// <summary>
    /// This is the model to response to caller
    /// </summary>
    public class PostDataModel
    {
        public string PostId { get; set; } //== PartitionKey
        public string ImageCaption { get; set; }
        public string ImageSasUrl { get; set; }

        public List<CommentModel> Comments { get; set; }
    }
}
