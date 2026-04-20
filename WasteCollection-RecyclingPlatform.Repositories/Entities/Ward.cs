namespace WasteCollection_RecyclingPlatform.Repositories.Entities;

public class Ward
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal CollectedKg { get; set; }
    public int CompletedRequests { get; set; }

    public long AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public ICollection<User> Collectors { get; set; } = new List<User>();
}
