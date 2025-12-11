using ConsoleRpg.Models;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Rooms;

namespace ConsoleRpg.Services.Interfaces;

/// <summary>
/// Interface for player-related game actions
/// Follows Interface Segregation Principle - focused on player operations only
/// </summary>
public interface IPlayerService
{
    ServiceResult<Room> MoveToRoom(Player player, Room currentRoom, int? roomId, string direction);
    ServiceResult ShowInventory(Player player);
    ServiceResult ShowCharacterStats(Player player);
    ServiceResult AttackMonster(Player player, Room currentRoom);
    ServiceResult UseAbilityOnMonster(Player player, Room currentRoom);
}

