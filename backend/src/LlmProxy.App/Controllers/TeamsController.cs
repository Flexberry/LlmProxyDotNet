using LlmProxy.Infrastructure.Services;
using LlmProxy.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LlmProxy.App.Controllers;

[ApiController]
[Route("admin/[controller]")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(ITeamService teamService, ILogger<TeamsController> logger)
    {
        _teamService = teamService;
        _logger = logger;
    }

    /// <summary>
    /// Создает команду
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            var team = await _teamService.CreateTeamAsync(request.Name, userId, request.Description, ct);
            return CreatedAtAction(nameof(GetTeam), new { teamId = team.Id }, team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team");
            return StatusCode(500, new { error = "Failed to create team" });
        }
    }

    /// <summary>
    /// Получает команду
    /// </summary>
    [HttpGet("{teamId}")]
    public async Task<IActionResult> GetTeam(Guid teamId, CancellationToken ct)
    {
        var team = await _teamService.GetTeamAsync(teamId, ct);
        
        if (team == null)
            return NotFound(new { error = "Team not found" });
        
        return Ok(team);
    }

    /// <summary>
    /// Получает команды пользователя
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserTeams(CancellationToken ct)
    {
        var userId = GetUserId();
        var teams = await _teamService.GetUserTeamsAsync(userId, ct);
        return Ok(teams);
    }

    /// <summary>
    /// Добавляет участника
    /// </summary>
    [HttpPost("{teamId}/members")]
    public async Task<IActionResult> AddMember(Guid teamId, [FromBody] AddMemberRequest request, CancellationToken ct)
    {
        try
        {
            var member = await _teamService.AddMemberAsync(teamId, request.UserId, request.Role, request.AllowedModels, ct);
            return Ok(member);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member to team {TeamId}", teamId);
            return StatusCode(500, new { error = "Failed to add member" });
        }
    }

    /// <summary>
    /// Удаляет участника
    /// </summary>
    [HttpDelete("{teamId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid teamId, string userId, CancellationToken ct)
    {
        try
        {
            var result = await _teamService.RemoveMemberAsync(teamId, userId, ct);
            
            if (!result)
                return NotFound(new { error = "Member not found" });
            
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member from team {TeamId}", teamId);
            return StatusCode(500, new { error = "Failed to remove member" });
        }
    }

    /// <summary>
    /// Проверяет роль пользователя
    /// </summary>
    [HttpGet("{teamId}/members/{userId}/role")]
    public async Task<IActionResult> GetUserRole(Guid teamId, string userId, CancellationToken ct)
    {
        var role = await _teamService.GetUserRoleAsync(teamId, userId, ct);
        
        if (role == null)
            return NotFound(new { error = "User is not a member of this team" });
        
        return Ok(new { role });
    }

    /// <summary>
    /// Удаляет команду
    /// </summary>
    [HttpDelete("{teamId}")]
    public async Task<IActionResult> DeleteTeam(Guid teamId, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            var result = await _teamService.DeleteTeamAsync(teamId, userId, ct);
            
            if (!result)
                return NotFound(new { error = "Team not found" });
            
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting team {TeamId}", teamId);
            return StatusCode(500, new { error = "Failed to delete team" });
        }
    }

    private string GetUserId()
    {
        // В реальной реализации нужно брать из JWT или другого auth механизма
        return User.Identity?.Name ?? "admin";
    }
}

public class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AddMemberRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public string? AllowedModels { get; set; }
}