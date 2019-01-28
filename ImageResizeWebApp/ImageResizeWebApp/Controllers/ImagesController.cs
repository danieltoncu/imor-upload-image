using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ImageResizeWebApp.Models;
using Microsoft.Extensions.Options;

using System.IO;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using ImageResizeWebApp.Helpers;

namespace ImageResizeWebApp.Controllers
{
    [Route("api/[controller]")]
    public class ImagesController : Controller
    {
        private readonly AzureStorageConfig storageConfig = null;

        public ImagesController(IOptions<AzureStorageConfig> config)
        {
            storageConfig = config.Value;
        }

        // POST /api/images/upload
        [HttpPost("[action]")]
        public async Task<IActionResult> Upload(ICollection<IFormFile> files)
        {
            bool isUploaded = false;

            try
            {
                if (files.Count == 0)
                {
                    return BadRequest("No files received from the upload.");
                }

                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)
                {
                    return BadRequest("Could not find Azure Storage AccountKey and AccountName.");
                }

                if (storageConfig.ImageContainer == string.Empty)
                {
                    return BadRequest("Could not find Azure Blob Storage Image Container Name.");
                }

                List<string> fileNames = new List<string>();

                foreach (var formFile in files)
                {
                    if (StorageHelper.IsImage(formFile))
                    {
                        if (formFile.Length > 0)
                        {
                            using (Stream stream = formFile.OpenReadStream())
                            {
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(formFile.FileName);

                                isUploaded = await StorageHelper.UploadFileToStorage(stream, fileName, storageConfig);

                                fileNames.Add(fileName);
                            }
                        }
                    }
                    else
                    {
                        return new UnsupportedMediaTypeResult();
                    }
                }

                if (isUploaded)
                {
                    if (storageConfig.ThumbnailContainer != string.Empty)
                    {
                        // return new AcceptedAtActionResult("GetThumbNails", "Images", null, null);
                        return new ObjectResult(fileNames);
                    }
                    else
                    {
                        return new AcceptedResult();
                    }
                }
                else
                {
                    return BadRequest("Could not upload image to Azure Storage.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET /api/images/thumbnails
        [HttpGet("thumbnails")]
        public async Task<IActionResult> GetThumbNails()
        {
            try
            {
                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)
                {
                    return BadRequest("Could not find Azure Storage AccountKey and AccountName.");
                }

                if (storageConfig.ImageContainer == string.Empty)
                {
                    return BadRequest("Could not find Azure Blob Storage Image Container Name.");
                }

                List<string> thumbnailUrls = await StorageHelper.GetThumbNailUrls(storageConfig);

                return new ObjectResult(thumbnailUrls);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET /api/images/images
        [HttpGet("images")]
        public async Task<IActionResult> GetImages()
        {
            try
            {
                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)
                {
                    return BadRequest("Could not find Azure Storage AccountKey and AccountName.");
                }

                if (storageConfig.ImageContainer == string.Empty)
                {
                    return BadRequest("Could not find Azure Blob Storage Image Container Name.");
                }

                List<string> imageUrls = await StorageHelper.GetImageUrls(storageConfig);

                return new ObjectResult(imageUrls);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}