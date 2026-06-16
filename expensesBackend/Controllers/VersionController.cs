using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() =>
        Ok(new { version = configuration["App:ApiVersion"] ?? "1.0.0" });
}
