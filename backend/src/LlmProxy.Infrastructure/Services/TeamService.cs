using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Services;

/// <summary>
/// Сервис управления командами и RBAC
/// </summary>
public class TeamService : ITeamService
{
    private readonly LlmProxyDbContext _db;
    private readonly ILogger<TeamService> _logger;

    public TeamService(LlmProxyDbContext db, ILogger<TeamService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Team> CreateTeamAsync(string name, string ownerId, string? description = null, CancellationToken ct = default)
    {
        var team = new Team
        {
            Name = name,
            Description = description,
            OwnerId = ownerId,
            IsActive = true,
            Members = new List<TeamMember>
            {
                new TeamMember { UserId = ownerId, Role = TeamRoles.Owner }
            }
        };
        
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);
        
        return team;
    }

    public async Task<Team?> GetTeamAsync(Guid teamId, CancellationToken ct = default)
    {
        return await _db.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == teamId && t.IsActive, ct);
    }

    public async Task<IEnumerable<Team>> GetUserTeamsAsync(string userId, CancellationToken ct = default)
    {
        return await _db.Teams
            .Include(t => t.Members)
            .Where(t => t.IsActive && t.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);
    }

    public async Task<TeamMember> AddMemberAsync(Guid teamId, string userId, string role, string? allowedModels = null, CancellationToken ct = default)
    {
        var team = await GetTeamAsync(teamId, ct);
        if (team == null) throw new InvalidOperationException("Team not found");
        
        var existing = team.Members.FirstOrDefault(m => m.UserId == userId);
        if (existing != null)
        {
            existing.Role = role;
            existing.AllowedModels = allowedModels;
        }
        else
        {
            team.Members.Add(new TeamMember { TeamId = teamId, UserId = userId, Role = role, AllowedModels = allowedModels });
        }
        
        await _db.SaveChangesAsync(ct);
        return team.Members.First(m => m.UserId == userId);
    }

    public async Task<bool> RemoveMemberAsync(Guid teamId, string userId, CancellationToken ct = default)
    {
        var team = await GetTeamAsync(teamId, ct);
        if (team == null) return false;
        
        var member = team.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return false;
        
        if (member.Role == TeamRoles.Owner) 
            throw new InvalidOperationException("Cannot remove team owner");
        
        team.Members.Remove(member);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<string?> GetUserRoleAsync(Guid teamId, string userId, CancellationToken ct = default)
    {
        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
        return member?.Role;
    }

    public async Task<bool> HasPermissionAsync(Guid teamId, string userId, string permission, CancellationToken ct = default)
    {
        var role = await GetUserRoleAsync(teamId, userId, ct);
        if (role == null) return false;
        return HasPermission(role, permission);
    }

    public async Task<bool> CanAccessModelAsync(Guid teamId, string userId, string modelName, CancellationToken ct = default)
    {
        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
        if (member == null) return false;
        
        if (member.Role == TeamRoles.Owner || member.Role == TeamRoles.Admin)
            return true;
        
        if (string.IsNullOrEmpty(member.AllowedModels) || member.AllowedModels == "*")
            return true;
        
        var allowed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(member.AllowedModels);
        return allowed?.Any(m => modelName.StartsWith(m)) == true;
    }

    public async Task<bool> DeleteTeamAsync(Guid teamId, string userId, CancellationToken ct = default)
    {
        var team = await GetTeamAsync(teamId, ct);
        if (team == null) return false;
        
        var member = team.Members.FirstOrDefault(m => m.UserId == userId);
        if (member?.Role != TeamRoles.Owner)
            throw new InvalidOperationException("Only team owner can delete the team");
        
        team.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static bool HasPermission(string role, string permission)
    {
        return role switch
        {
            TeamRoles.Owner => true,
            TeamRoles.Admin => permission is "read" or "write" or "manage_members",
            TeamRoles.Member => permission is "read" or "write",
            TeamRoles.Viewer => permission == "read",
            _ => false
        };
    }
}

public interface ITeamService
{
    Task<Team> CreateTeamAsync(string name, string ownerId, string? description = null, CancellationToken ct = default);
    Task<Team?> GetTeamAsync(Guid teamId, CancellationToken ct = default);
    Task<IEnumerable<Team>> GetUserTeamsAsync(string userId, CancellationToken ct = default);
    Task<TeamMember> AddMemberAsync(Guid teamId, string userId, string role, string? allowedModels = null, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(Guid teamId, string userId, CancellationToken ct = default);
    Task<string?> GetUserRoleAsync(Guid teamId, string userId, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(Guid teamId, string userId, string permission, CancellationToken ct = default);
    Task<bool> CanAccessModelAsync(Guid teamId, string userId, string modelName, CancellationToken ct = default);
    Task<bool> DeleteTeamAsync(Guid teamId, string userId, CancellationToken ct = default);
}