namespace ICMarkets.Application.DTOs;

public class BlockchainDataDto
{
    public int Id { get; set; }
    public string Chain { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string RawJsonData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
