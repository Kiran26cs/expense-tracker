using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/currency")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyConversionService _fx;

    public CurrencyController(ICurrencyConversionService fx) => _fx = fx;

    // GET api/currency/rate?from=USD&to=INR&date=2024-01-15
    [HttpGet("rate")]
    public async Task<ActionResult<ApiResponse<CurrencyRateDto>>> GetRate(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] string? date)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return BadRequest(ApiResponse<CurrencyRateDto>.ErrorResponse("from and to are required"));

        var d = DateTime.TryParse(date, out var parsed) ? parsed : DateTime.UtcNow;
        var rate = await _fx.GetRateAsync(from, to, d);
        return Ok(ApiResponse<CurrencyRateDto>.SuccessResponse(new CurrencyRateDto { Rate = rate }));
    }
}
