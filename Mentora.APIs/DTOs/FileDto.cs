using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mentora.APIs.DTOs
{
    public class FileDto
    {
        public class FileUpload
        {
            public IFormFile file { get; set; }

        }

        public class FileUploadResult
        {
            public string url { get; set; }
        }
    }
}
