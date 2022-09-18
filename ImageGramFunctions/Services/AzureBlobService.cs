using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageGramFunctions.Models;
using ImageGramFunctions.Options;

namespace ImageGramFunctions.Services
{
    /// <summary>
    /// Azure blob service
    /// References: 
    /// https://github.com/Azure-Samples/storage-blob-upload-from-webapp/blob/892a64f743938c94c042796762b53614a653ef20/ImageResizeWebApp/ImageResizeWebApp/Helpers/StorageHelper.cs#L17
    /// </summary>
    public interface IAzureBlobService
    {
        bool IsImage(IFormFile file);
        Task<bool> UploadBlob(string containerName, Stream fileStream, string fileName);
        Task<bool> UploadImageBlob(string containerName, Stream fileStream, string fileName);
        Task<string> GetBlob(string containerName, string blobName);
        Task<List<string>> GetBlobs(string containerName);
        Task<List<BlobModel>> GetBlobModels(string containerName);
        Task<List<BlobModel>> GetBlobModelsByPostId(string containerName, string postId);
    }


    
    public class AzureBlobService : IAzureBlobService
    {
        private readonly AzureStorageConfig _storageConfig;

        private string BlobUrl(string containerName)
        {
            return "https://" +
                        _storageConfig.AccountName +
                        ".blob.core.windows.net/" +
                        containerName +
                        "/";
        }

        public AzureBlobService(IOptions<AzureStorageConfig> options)
        {
            _storageConfig = options.Value;
        }

        public async Task<string> GetBlob(string containerName, string blobName)
        {
            try
            {
                string imageUrl = string.Empty;

                // Create StorageSharedKeyCredentials object by reading
                // the values from the configuration (appsettings.json)
                StorageSharedKeyCredential storageCredentials =
                    new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);

                // Create the blob client.
                BlobClient blobClient = new BlobClient(new Uri($"{BlobUrl(containerName)}{blobName}"), storageCredentials);
                
                imageUrl = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTime.Now.AddYears(1000)).ToString();

                return await Task.FromResult(imageUrl);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> UploadImageBlob(string containerName, Stream fileStream, string fileName)
        {
            try
            {
                // Create StorageSharedKeyCredentials object by reading
                // the values from the configuration (appsettings.json)
                StorageSharedKeyCredential storageCredentials =
                    new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);

                // Create the blob client.
                BlobClient blobClient = new BlobClient(new Uri($"{BlobUrl(containerName)}{fileName}"), storageCredentials);

                // Upload the file
                BlobUploadOptions blobUploadOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpg" } };
                await blobClient.UploadAsync(fileStream, blobUploadOptions);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool IsImage(IFormFile file)
        {
            if (file.ContentType.Contains("image"))
            {
                return true;
            }

            string[] formats = new string[] { ".jpg", ".png", ".gif", ".jpeg" };

            return formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> UploadBlob(string containerName, Stream fileStream, string fileName)
        {
            try
            {
                // Create StorageSharedKeyCredentials object by reading
                // the values from the configuration (appsettings.json)
                StorageSharedKeyCredential storageCredentials =
                    new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);

                string blobUrl = BlobUrl(containerName);
                // Create the blob client.
                BlockBlobClient blobClient = new BlockBlobClient(new Uri($"{blobUrl}{fileName}"), storageCredentials);
                
                // Upload the file
                fileStream.Position = 0;
                await blobClient.UploadAsync(fileStream);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<string>> GetBlobs(string containerName)
        {
            List<string> filesUrls = new List<string>();

            // Create a URI to the storage account
            Uri accountUri = new Uri("https://" + _storageConfig.AccountName + ".blob.core.windows.net/");

            StorageSharedKeyCredential storageCredentials =
                    new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create BlobServiceClient from the account URI
            BlobServiceClient blobServiceClient = new BlobServiceClient(accountUri, storageCredentials);

            // Get reference to the container
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);

            if (container.Exists())
            {
                foreach (BlobItem blobItem in container.GetBlobs())
                {
                    filesUrls.Add(container.Uri + "/" + blobItem.Name);
                }
            }

            return await Task.FromResult(filesUrls);
        }

        public async Task<List<BlobModel>> GetBlobModels(string containerName)
        {
            List<BlobModel> blobModels = new List<BlobModel>();

            // Create a URI to the storage account
            Uri accountUri = new Uri("https://" + _storageConfig.AccountName + ".blob.core.windows.net/");

            StorageSharedKeyCredential storageCredentials =
                    new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create BlobServiceClient from the account URI
            BlobServiceClient blobServiceClient = new BlobServiceClient(accountUri, storageCredentials);

            // Get reference to the container
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);

            if (container.Exists())
            {
                foreach (BlobItem blobItem in container.GetBlobs())
                {
                    var blob = container.GetBlobClient(blobItem.Name);

                    blobModels.Add(
                        new BlobModel
                        {
                            BlobName = blob.Name,
                            BlobSasUrl = blob.GenerateSasUri(BlobSasPermissions.Read, DateTime.Now).ToString()
                        });
                }
            }

            return await Task.FromResult(blobModels);
        }

        public async Task<List<BlobModel>> GetBlobModelsByPostId(string containerName, string postId)
        {
            List<BlobModel> blobModels = new List<BlobModel>();

            // Create a URI to the storage account
            Uri accountUri = new Uri("https://" + _storageConfig.AccountName + ".blob.core.windows.net/");

            StorageSharedKeyCredential storageCredentials =
                    new StorageSharedKeyCredential(_storageConfig.AccountName, _storageConfig.AccountKey);

            // Create BlobServiceClient from the account URI
            BlobServiceClient blobServiceClient = new BlobServiceClient(accountUri, storageCredentials);

            // Get reference to the container
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient(containerName);

            if (container.Exists())
            {
                foreach (BlobItem blobItem in container.GetBlobs())
                {
                    if (blobItem.Name.Split('-')[0] == postId)
                    {

                        var blob = container.GetBlobClient(blobItem.Name);

                        blobModels.Add(
                            new BlobModel
                            {
                                BlobName = blob.Name,
                                BlobSasUrl = blob.GenerateSasUri(BlobSasPermissions.Read, DateTime.Now).ToString()
                            });
                    }
                }
            }

            return await Task.FromResult(blobModels);
        }
    }
}
