using Amazon.CognitoIdentityProvider.Model;
using IssueTrackerApi.DTOs.Auth;
using IssueTrackerApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IssueTrackerApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpDto dto)
    {
        try
        {
            await _auth.SignUpAsync(dto);
            return Ok(new { message = "Registration successful. Check your email for the confirmation code." });
        }
        catch (UsernameExistsException)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }
        catch (InvalidPasswordException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignUp failed for {Email}", dto.Email);
            return StatusCode(500, new { message = "Registration failed. Please try again." });
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmSignUp([FromBody] ConfirmSignUpDto dto)
    {
        try
        {
            await _auth.ConfirmSignUpAsync(dto);
            return Ok(new { message = "Account confirmed. You can now sign in." });
        }
        catch (CodeMismatchException)
        {
            return BadRequest(new { message = "Invalid confirmation code." });
        }
        catch (ExpiredCodeException)
        {
            return BadRequest(new { message = "Confirmation code has expired. Please request a new one." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Confirm failed for {Email}", dto.Email);
            return StatusCode(500, new { message = "Confirmation failed. Please try again." });
        }
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AuthResponseDto>> SignIn([FromBody] SignInDto dto)
    {
        try
        {
            var result = await _auth.SignInAsync(dto);
            return Ok(result);
        }
        catch (NotAuthorizedException)
        {
            return Unauthorized(new { message = "Incorrect email or password." });
        }
        catch (UserNotConfirmedException)
        {
            return BadRequest(new { message = "Please confirm your account before signing in." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignIn failed for {Email}", dto.Email);
            return StatusCode(500, new { message = "Sign in failed. Please try again." });
        }
    }
}
