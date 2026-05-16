using IssueTrackerApi.DTOs.Issues;
using IssueTrackerApi.Extensions;
using IssueTrackerApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IssueTrackerApi.Controllers;

[ApiController]
[Route("api/issues")]
public class IssuesController : ControllerBase
{
    private readonly IIssueService _service;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(IIssueService service, ILogger<IssuesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IssueResponseDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IssueResponseDto>> GetById(Guid id)
    {
        var issue = await _service.GetByIdAsync(id);
        return issue is null ? NotFound() : Ok(issue);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<IssueResponseDto>> Create([FromForm] CreateIssueDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto, User.GetUserId(), User.GetUserName());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create issue");
            return StatusCode(500, new { message = "Image upload failed. Please try again." });
        }
    }

    [Authorize]
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<IssueResponseDto>> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            var updated = await _service.UpdateStatusAsync(id, dto.Status);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, User.GetUserId());
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
