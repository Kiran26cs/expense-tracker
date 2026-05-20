using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payment;

    public PaymentController(IPaymentService payment)
    {
        _payment = payment;
    }

    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? string.Empty;

    // GET /api/payment/status
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _payment.GetStatusAsync(UserId);
        return Ok(new { success = true, data = status });
    }

    // POST /api/payment/create-subscription
    [HttpPost("create-subscription")]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequestDto request)
    {
        try
        {
            var result = await _payment.CreateSubscriptionAsync(UserId, request.Plan);
            return Ok(new { success = true, data = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    // POST /api/payment/verify
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentRequestDto request)
    {
        try
        {
            var status = await _payment.VerifyAndActivateAsync(UserId, request);
            return Ok(new { success = true, data = status });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    // POST /api/payment/cancel
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelSubscriptionRequestDto request)
    {
        try
        {
            await _payment.CancelAsync(UserId, request.Immediately);
            return Ok(new { success = true });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
    }

    // POST /api/payment/webhook  — no auth, Razorpay calls this directly
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader   = new StreamReader(Request.Body);
        var payload        = await reader.ReadToEndAsync();
        var signature      = Request.Headers["X-Razorpay-Signature"].ToString();

        try
        {
            await _payment.HandleWebhookAsync(payload, signature);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
