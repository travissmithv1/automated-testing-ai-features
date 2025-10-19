using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace MetricsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=onboarding_chatbot;Username=chatbot_user;Password=local_dev_password";

    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return Ok(new { status = "healthy", database = "connected" });
        }
        catch
        {
            return StatusCode(503, new { status = "unhealthy", database = "disconnected" });
        }
    }
}
