using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LlmProxy.Core.Entities;

[Table("request_logs")]
public class RequestLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("api_key_hash")]
    [StringLength(64)]
    public string ApiKeyHash { get; set; } = string.Empty;

    [Column("provider_name")]
    [StringLength(50)]
    public string ProviderName { get; set; } = string.Empty;

    [Column("model_requested")]
    [StringLength(200)]
    public string ModelRequested { get; set; } = string.Empty;

    [Column("model_used")]
    [StringLength(200)]
    public string ModelUsed { get; set; } = string.Empty;

    [Column("latency_ms")]
    public int LatencyMs { get; set; }

    [Column("tokens_prompt")]
    public int? TokensPrompt { get; set; }

    [Column("tokens_completion")]
    public int? TokensCompletion { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "pending"; // pending, success, error

    [Column("error_message")]
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    [Column("response_id")]
    [StringLength(100)]
    public string? ResponseId { get; set; }

    [Column("is_streaming")]
    public bool IsStreaming { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}