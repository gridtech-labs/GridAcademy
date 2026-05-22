namespace GridAcademy.Data.Entities;

/// <summary>
/// Junction table: maps a User to a Group (many-to-many).
/// One user can belong to multiple groups; one group can contain many users.
/// </summary>
public class UserGroup
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Admin who added this user to the group.</summary>
    public Guid? AddedBy { get; set; }
}
