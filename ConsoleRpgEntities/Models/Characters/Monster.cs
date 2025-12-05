using ConsoleRpgEntities.Models.Attributes;
using ConsoleRpgEntities.Models.Rooms;

namespace ConsoleRpgEntities.Models.Characters;

public abstract class Monster : IMonster, ITargetable
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Health { get; set; }
    public int AggressionLevel { get; set; }
    public string MonsterType { get; set; }

    // Foreign key
    public int? RoomId { get; set; }

    // Navigation property
    public virtual Room Room { get; set; }

    protected Monster()
    {

    }

    public abstract void Attack(ITargetable target);

}