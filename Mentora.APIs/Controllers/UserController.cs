using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mentora.APIs.service;
using Microsoft.AspNetCore.Mvc;

namespace Mentora.APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly cloudinaryService _cloudinary;

        public UserController(cloudinaryService cloudinary)
        {
            _cloudinary = cloudinary;

        }

[HttpPost("upload")]
        public async Task<IActionResult> upload(IFormFile form)
        {
            try
            {
                var imgUrl = await _cloudinary.upload(form);
                return Ok(new { url = imgUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred during file upload" });
            }
        }
    }
}
