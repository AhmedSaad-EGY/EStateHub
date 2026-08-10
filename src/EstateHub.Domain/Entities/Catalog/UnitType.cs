namespace EstateHub.Domain.Entities.Catalog;

public class UnitType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<Unit> Units { get; set; } = [];
}
