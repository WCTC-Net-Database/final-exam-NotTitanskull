using ConsoleRpg.Models;
using ConsoleRpg.Services.Interfaces;
using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ConsoleRpg.Services;

/// <summary>
/// Handles all player-related actions and interactions
/// Implements IPlayerService interface - follows Dependency Inversion Principle
/// Separated from GameEngine to follow Single Responsibility Principle
/// Returns ServiceResult objects to decouple from UI concerns
/// </summary>
public class PlayerService : IPlayerService
{
    private readonly GameContext _context;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(GameContext context, ILogger<PlayerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Move the player to a different room
    /// </summary>
    public ServiceResult<Room> MoveToRoom(Player player, Room currentRoom, int? roomId, string direction)
    {
        try
        {
            if (!roomId.HasValue)
            {
                return ServiceResult<Room>.Fail(
                    $"[red]Cannot go {direction}[/]",
                    $"[red]You cannot go {direction} from here - there is no exit in that direction.[/]");
            }

            var newRoom = _context.Rooms
                .Include(r => r.Players)
                .Include(r => r.Monsters)
                .Include(r => r.NorthRoom)
                .Include(r => r.SouthRoom)
                .Include(r => r.EastRoom)
                .Include(r => r.WestRoom)
                .FirstOrDefault(r => r.Id == roomId.Value);

            if (newRoom == null)
            {
                _logger.LogWarning("Attempted to move to non-existent room {RoomId}", roomId.Value);
                return ServiceResult<Room>.Fail(
                    $"[red]Room not found[/]",
                    $"[red]Error: Room {roomId.Value} does not exist.[/]");
            }

            // Update player's room
            player.RoomId = roomId.Value;
            _context.SaveChanges();

            _logger.LogInformation("Player {PlayerName} moved {Direction} to {RoomName}",
                player.Name, direction, newRoom.Name);

            return ServiceResult<Room>.Ok(
                newRoom,
                $"[green]→ {direction}[/]",
                $"[green]You travel {direction} and arrive at {newRoom.Name}.[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving player {PlayerName} to room {RoomId}", player.Name, roomId);
            return ServiceResult<Room>.Fail(
                "[red]Movement failed[/]",
                $"[red]An error occurred while moving: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Show player character stats
    /// </summary>
    public ServiceResult ShowCharacterStats(Player player)
    {
        try
        {
            var output = $"[yellow]Character:[/] {player.Name}\n" +
                         $"[green]Health:[/] {player.Health}\n" +
                         $"[cyan]Experience:[/] {player.Experience}";

            _logger.LogInformation("Displaying stats for player {PlayerName}", player.Name);

            return ServiceResult.Ok(
                "[cyan]Viewing stats[/]",
                output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying stats for player {PlayerName}", player.Name);
            return ServiceResult.Fail(
                "[red]Error[/]",
                $"[red]Failed to display stats: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Show player inventory and stats
    /// </summary>
    public ServiceResult ShowInventory(Player player)
    {
        try
        {
            var output = $"[magenta]Equipment:[/] {(player.Equipment != null ? "Equipped" : "None")}\n" +
                         $"[blue]Abilities:[/] {player.Abilities?.Count ?? 0}";

            _logger.LogInformation("Displaying inventory for player {PlayerName}", player.Name);

            return ServiceResult.Ok(
                "[magenta]Viewing inventory[/]",
                output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying inventory for player {PlayerName}", player.Name);
            return ServiceResult.Fail(
                "[red]Error[/]",
                $"[red]Failed to display inventory: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Monster attack logic
    /// </summary>
    public ServiceResult AttackMonster(Player player, Room currentRoom)
    {
        try
        {
            _logger.LogInformation("Player {PlayerName} is attempting to attack a monster", player.Name);

            if (!currentRoom.Monsters.Any())
            {
                return ServiceResult.Fail(
                    "[red]No Targets[/]",
                    "[red]There are no monsters to attack in this room.[/]");
            }

            Monster targetMonster;
            if (currentRoom.Monsters.Count == 1)
            {
                targetMonster = currentRoom.Monsters.First();
            }
            else
            {
                var monsterChoices = currentRoom.Monsters
                    .Select(m => $"{m.Name} (HP: {m.Health})")
                    .ToList();

                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Select a monster to attack:[/]")
                        .AddChoices(monsterChoices));

                var monsterName = selection.Split(" (HP:")[0];
                targetMonster = currentRoom.Monsters.First(m => m.Name == monsterName);
            }

            int baseDamage = 5;
            int weaponDamage = player.Equipment?.Weapon?.Attack ?? 0;
            int totalDamage = baseDamage + weaponDamage;

            targetMonster.Health -= totalDamage;

            _logger.LogInformation("Player {PlayerName} dealt {Damage} damage to monster {MonsterName}",
                player.Name, totalDamage, targetMonster.Name);

            if (targetMonster.Health <= 0)
            {
                _context.Monsters.Remove(targetMonster);
                player.Experience += 50;
                _context.SaveChanges();

                return ServiceResult.Ok(
                    "[green]Monster Defeated![/]",
                    $"[green]You defeated {targetMonster.Name}! (+50 XP)[/]");
            }

            _context.SaveChanges();

            return ServiceResult.Ok(
                "[yellow]Attack Successful[/]",
                $"[yellow]You attacked {targetMonster.Name} for {totalDamage} damage.\nMonster HP: {targetMonster.Health}[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during attack by player {PlayerName}", player.Name);
            return ServiceResult.Fail(
                "[red]Attack failed[/]",
                $"[red]An error occurred during the attack: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Use a player ability on a monster
    /// </summary>
    public ServiceResult UseAbilityOnMonster(Player player, Room currentRoom)
    {
        try
        {
            _logger.LogInformation("Player {PlayerName} is attempting to use ability on Monster", player.Name);

            if (!player.Abilities.Any())
            {
                return ServiceResult.Fail(
                    "[red]No Abilities[/]",
                    "[red]You don't have any abilities to use.[/]");
            }

            if (!currentRoom.Monsters.Any())
            {
                return ServiceResult.Fail(
                    "[red]No Targets[/]",
                    "[red]There are no monsters to target in this room.[/]");
            }

            // Select ability
            var abilityChoices = player.Abilities
                .Select(a => $"{a.Name} - {a.Description}")
                .ToList();
            
            var abilitySelection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Select an ability to use:[/]")
                    .AddChoices(abilityChoices));
            
            var abilityName = abilitySelection.Split(" - ")[0];
            var selectedAbility = player.Abilities.First(a => a.Name == abilityName);
            
            // Select target monster
            Monster targetMonster;
            if (currentRoom.Monsters.Count == 1)
            {
                targetMonster = currentRoom.Monsters.First();
            }
            else
            {
                var monsterChoices = currentRoom.Monsters
                    .Select(m => $"{m.Name} (HP: {m.Health})")
                    .ToList();

                var monsterSelection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Select a monster to target:[/]")
                        .AddChoices(monsterChoices));

                var monsterName = monsterSelection.Split(" (HP:")[0];
                targetMonster = currentRoom.Monsters.First(m => m.Name == monsterName);
            }

            // Calculate damage (base ability damage - can be enhanced later)
            int abilityDamage = 15; // Base ability damage
            targetMonster.Health -= abilityDamage;
            
            _logger.LogInformation("Player {PlayerName} used {AbilityName} on {MonsterName} for {Damage} damage",
                player.Name, selectedAbility.Name, targetMonster.Name, abilityDamage);

            if (targetMonster.Health <= 0)
            {
                _context.Monsters.Remove(targetMonster);
                player.Experience += 50;
                _context.SaveChanges();
                
                return ServiceResult.Ok(
                    "[green]Monster Defeated![/]",
                    $"[green]You used {selectedAbility.Name} and defeated {targetMonster.Name}! (+50 XP)[/]");
            }
            
            _context.SaveChanges();
            
            return ServiceResult.Ok(
                "[cyan]Ability Used[/]",
                $"[cyan]You used {selectedAbility.Name} on {targetMonster.Name} for {abilityDamage} damage.\nMonster HP: {targetMonster.Health}[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using ability by player {PlayerName}", player.Name);
            return ServiceResult.Fail(
                "[red]Ability use failed[/]",
                $"[red]An error occurred while using the ability: {ex.Message}[/]");
        }
    }
}