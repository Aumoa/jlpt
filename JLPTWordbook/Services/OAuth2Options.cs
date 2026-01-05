namespace JLPTWordbook.Services;

public record OAuth2Options
{
    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }
}
