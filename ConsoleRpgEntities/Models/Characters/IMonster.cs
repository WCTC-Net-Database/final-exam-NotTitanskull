using ConsoleRpgEntities.Models.Attributes;

namespace ConsoleRpgEntities.Models.Characters;

public interface IMonster
{
    int Id { get; set; }
    string Name { get; set; }

    void Attack(ITargetable target);
}