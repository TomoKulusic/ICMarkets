namespace ICMarkets.Domain.Entities;

public class BlockchainData
{
    public int Id { get; set; }
    public required string Chain { get; set; }
    public required string Network { get; set; }
    
    // Store the complete JSON response as-is from the API
    public required string RawJsonData { get; set; }
    
    // Timestamp when data was fetched from API
    public DateTime CreatedAt { get; set; }
}
