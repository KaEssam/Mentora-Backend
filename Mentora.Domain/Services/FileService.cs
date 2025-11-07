using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mentora.Domain.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Mentora.Domain.Services
{
public class FileService
{
    private readonly Cloudinary cloudinary;

    public FileService()
    {
        Account account = new Account("Name", "753144599687334", "y4GEIAKY1IZvXwrDgHyIZqixt_E");
        cloudinary = new Cloudinary(account);
    }

    // public ImageUploadResult Upload(ImageUploadParams parameters)
    // {
    //     var uploadParams = new ImageUploadParams()
    //     {
    //         File = new FileDescription(@"c:\my_image.jpg")
    //     };
    //         var uploadResult = cloudinary.Upload(uploadParams);


    //   }
    }
}
