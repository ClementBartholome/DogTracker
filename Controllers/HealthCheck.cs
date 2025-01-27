using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace DogTracker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet("db-connection")]
        public IActionResult CheckDatabaseConnection()
        {
            var connectionString = Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_prod");

            try
            {
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                return Ok("Database connection successful");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database connection failed: {ex.Message}");
            }
        }
    }
}