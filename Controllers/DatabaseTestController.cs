
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Xerox_Centre.Data;

namespace Smart_Xerox_Centre.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseTestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DatabaseTestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            bool isConnected = await _context.Database.CanConnectAsync();

            if (isConnected)
            {
                return Ok(new
                {
                    Status = "Success",
                    Message = "Smart Xerox Database Connected Successfully!"
                });
            }

            return StatusCode(500, new
            {
                Status = "Failed",
                Message = "Database Connection Failed!"
            });
        }
    }
}
