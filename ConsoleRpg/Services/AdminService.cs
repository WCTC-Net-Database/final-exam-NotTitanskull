using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Abilities;
using ConsoleRpgEntities.Models.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ConsoleRpg.Services;

/// <summary>
/// Handles all admin/developer CRUD operations and advanced queries
/// Separated from GameEngine to follow Single Responsibility Principle
/// </summary>
public class AdminService
{
    private readonly GameContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(GameContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Basic CRUD Operations

    /// <summary>
    /// Add a new character to the database
    /// </summary>
    public void AddCharacter()
    {
        try
        {
            _logger.LogInformation("User selected Add Character");
            AnsiConsole.MarkupLine("[yellow]=== Add New Character ===[/]");

            var name = AnsiConsole.Ask<string>("Enter character [green]name[/]:");
            var health = AnsiConsole.Ask<int>("Enter [green]health[/]:");
            var experience = AnsiConsole.Ask<int>("Enter [green]experience[/]:");

            var player = new Player
            {
                Name = name,
                Health = health,
                Experience = experience
            };

            _context.Players.Add(player);
            _context.SaveChanges();

            _logger.LogInformation("Character {Name} added to database with Id {Id}", name, player.Id);
            AnsiConsole.MarkupLine($"[green]Character '{name}' added successfully![/]");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character");
            AnsiConsole.MarkupLine($"[red]Error adding character: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    /// <summary>
    /// Edit an existing character's properties
    /// </summary>
    public void EditCharacter()
    {
        try
        {
            _logger.LogInformation("User selected Edit Character");
            AnsiConsole.MarkupLine("[yellow]=== Edit Character ===[/]");

            var id = AnsiConsole.Ask<int>("Enter character [green]ID[/] to edit:");

            var player = _context.Players.Find(id);
            if (player == null)
            {
                _logger.LogWarning("Character with Id {Id} not found", id);
                AnsiConsole.MarkupLine($"[red]Character with ID {id} not found.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"Editing: [cyan]{player.Name}[/]");

            if (AnsiConsole.Confirm("Update name?"))
            {
                player.Name = AnsiConsole.Ask<string>("Enter new [green]name[/]:");
            }

            if (AnsiConsole.Confirm("Update health?"))
            {
                player.Health = AnsiConsole.Ask<int>("Enter new [green]health[/]:");
            }

            if (AnsiConsole.Confirm("Update experience?"))
            {
                player.Experience = AnsiConsole.Ask<int>("Enter new [green]experience[/]:");
            }

            _context.SaveChanges();

            _logger.LogInformation("Character {Name} (Id: {Id}) updated", player.Name, player.Id);
            AnsiConsole.MarkupLine($"[green]Character '{player.Name}' updated successfully![/]");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing character");
            AnsiConsole.MarkupLine($"[red]Error editing character: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    /// <summary>
    /// Display all characters in the database
    /// </summary>
    public void DisplayAllCharacters()
    {
        try
        {
            _logger.LogInformation("User selected Display All Characters");
            AnsiConsole.MarkupLine("[yellow]=== All Characters ===[/]");

            var players = _context.Players.Include(p => p.Room).ToList();

            if (!players.Any())
            {
                AnsiConsole.MarkupLine("[red]No characters found.[/]");
            }
            else
            {
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Health");
                table.AddColumn("Experience");
                table.AddColumn("Location");

                foreach (var player in players)
                {
                    table.AddRow(
                        player.Id.ToString(),
                        player.Name,
                        player.Health.ToString(),
                        player.Experience.ToString(),
                        player.Room?.Name ?? "Unknown"
                    );
                }

                AnsiConsole.Write(table);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying all characters");
            AnsiConsole.MarkupLine($"[red]Error displaying characters: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Search for characters by name
    /// </summary>
    public void SearchCharacterByName()
    {
        try
        {
            _logger.LogInformation("User selected Search Character");
            AnsiConsole.MarkupLine("[yellow]=== Search Character ===[/]");

            var searchName = AnsiConsole.Ask<string>("Enter character [green]name[/] to search:");

            var players = _context.Players
                .Include(p => p.Room)
                .Where(p => p.Name.ToLower().Contains(searchName.ToLower()))
                .ToList();

            if (!players.Any())
            {
                _logger.LogInformation("No characters found matching '{SearchName}'", searchName);
                AnsiConsole.MarkupLine($"[red]No characters found matching '{searchName}'.[/]");
            }
            else
            {
                _logger.LogInformation("Found {Count} character(s) matching '{SearchName}'", players.Count, searchName);

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Name");
                table.AddColumn("Health");
                table.AddColumn("Experience");
                table.AddColumn("Location");

                foreach (var player in players)
                {
                    table.AddRow(
                        player.Id.ToString(),
                        player.Name,
                        player.Health.ToString(),
                        player.Experience.ToString(),
                        player.Room?.Name ?? "Unknown"
                    );
                }

                AnsiConsole.Write(table);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for characters");
            AnsiConsole.MarkupLine($"[red]Error searching characters: {ex.Message}[/]");
        }
    }

    #endregion

    #region C-Level Requirements

    /// <summary>
    /// Implement this method
    /// Requirements:
    /// - Display a list of existing characters
    /// - Prompt user to select a character (by ID)
    /// - Display a list of available abilities from the database
    /// - Prompt user to select an ability to add
    /// - Associate the ability with the character using the many-to-many relationship
    /// - Save changes to the database
    /// - Display confirmation message with the character name and ability name
    /// - Log the operation
    /// </summary>
    public void AddAbilityToCharacter()
    {
        try
        {


            _logger.LogInformation("User selected Add Ability to Character");
            AnsiConsole.MarkupLine("[yellow]=== Add Ability to Character ===[/]");

            // TODO: Implement this method
            var players = _context.Players.Include(p => p.Abilities).ToList();

            if (!players.Any())
            {
                AnsiConsole.MarkupLine("[red]No characters found in database.[/]");
                PressAnyKey();
                return;
            }

            var characterTable = new Table();
            characterTable.AddColumn("ID");
            characterTable.AddColumn("Name");
            characterTable.AddColumn("Health");
            characterTable.AddColumn("Current Abilities");

            foreach (var p in players)
            {
                characterTable.AddRow(
                    p.Id.ToString(),
                    p.Name,
                    p.Health.ToString(),
                    p.Abilities?.Count.ToString() ?? "0"
                );
            }

            AnsiConsole.Write(characterTable);

            var characterId = AnsiConsole.Ask<int>("Enter character [green]ID[/]:");
            var player = _context.Players
                .Include(p => p.Abilities)
                .FirstOrDefault(p => p.Id == characterId);

            if (player == null)
            {
                _logger.LogWarning("Character with ID {Id} not found", characterId);
                AnsiConsole.MarkupLine($"[red]Character with ID {characterId} not found.[/]");
                PressAnyKey();
                return;
            }

            var abilities = _context.Abilities.ToList();

            if (!abilities.Any())
            {
                AnsiConsole.MarkupLine("[red]No abilities found in database[/]");
                PressAnyKey();
                return;
            }

            var abilityTable = new Table();
            abilityTable.AddColumn("ID");
            abilityTable.AddColumn("Name");
            abilityTable.AddColumn("Description");

            foreach (var ability in abilities)
            {
                abilityTable.AddRow(
                    ability.Id.ToString(),
                    ability.Name,
                    ability.Description ?? "N/A"
                );
            }

            AnsiConsole.Write(abilityTable);

            var abilityId = AnsiConsole.Ask<int>("Enter ability [green]ID[/] to add:");
            var selectedAbility = _context.Abilities.Find(abilityId);

            if (selectedAbility == null)
            {
                _logger.LogWarning("Ability with ID {Id} not found", abilityId);
                AnsiConsole.MarkupLine($"[red]Ability with ID {abilityId} not found.[/]");
                PressAnyKey();
                return;
            }

            if (player.Abilities.Any(a => a.Id == abilityId))
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Character '{player.Name}' already has ability '{selectedAbility.Name}'.[/]");
                PressAnyKey();
                return;
            }

            player.Abilities ??= new List<Ability>();
            player.Abilities.Add(selectedAbility);
            _context.SaveChanges();

            _logger.LogInformation("Added ability {AbilityName} to character {CharacterName} (ID: {Id})",
                selectedAbility.Name, player.Name, player.Id);
            AnsiConsole.MarkupLine(
                $"[green]Successfully added ability '{selectedAbility.Name}' to character '{player.Name}'.[/]");
            Thread.Sleep(1000);
            // AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
            // AnsiConsole.MarkupLine("[yellow]TODO: Allow users to add abilities to existing characters.[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding ability to character");
            AnsiConsole.MarkupLine($"[red]Error adding ability: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Prompt the user to select a character (by ID or name)
    /// - Retrieve the character and their abilities from the database (use Include or lazy loading)
    /// - Display the character's name and basic info
    /// - Display all abilities associated with that character in a formatted table
    /// - For each ability, show: Name, Description, and any other relevant properties (e.g., Damage, Distance for ShoveAbility)
    /// - Handle the case where the character has no abilities
    /// - Log the operation
    /// </summary>
    public void DisplayCharacterAbilities()
    {
        _logger.LogInformation("User selected Display Character Abilities");
        AnsiConsole.MarkupLine("[yellow]=== Display Character Abilities ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Display all abilities for a selected character.[/]");

        PressAnyKey();
    }

    #endregion

    #region B-Level Requirements

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Prompt user for room name
    /// - Prompt user for room description
    /// - Optionally prompt for navigation (which rooms connect in which directions)
    /// - Create a new Room entity
    /// - Save to the database
    /// - Display confirmation with room details
    /// - Log the operation
    /// </summary>
    public void AddRoom()
    {
        _logger.LogInformation("User selected Add Room");
        AnsiConsole.MarkupLine("[yellow]=== Add New Room ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Allow users to create new rooms and connect them to the world.[/]");

        PressAnyKey();
    }

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Display a list of all rooms
    /// - Prompt user to select a room (by ID or name)
    /// - Retrieve room from database with related data (Include Players and Monsters)
    /// - Display room name, description, and exits
    /// - Display list of all players in the room (or message if none)
    /// - Display list of all monsters in the room (or message if none)
    /// - Handle case where room is empty gracefully
    /// - Log the operation
    /// </summary>
    public void DisplayRoomDetails()
    {
        _logger.LogInformation("User selected Display Room Details");
        AnsiConsole.MarkupLine("[yellow]=== Display Room Details ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Display detailed information about a room and its inhabitants.[/]");

        PressAnyKey();
    }

    #endregion

    #region A-Level Requirements

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Display list of all rooms
    /// - Prompt user to select a room
    /// - Display a menu of attributes to filter by (Health, Name, Experience, etc.)
    /// - Prompt user for filter criteria
    /// - Query the database for characters in that room matching the criteria
    /// - Display matching characters with relevant details in a formatted table
    /// - Handle case where no characters match
    /// - Log the operation
    /// </summary>
    public void ListCharactersInRoomByAttribute()
    {
        _logger.LogInformation("User selected List Characters in Room by Attribute");
        AnsiConsole.MarkupLine("[yellow]=== List Characters in Room by Attribute ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Find characters in a room matching specific criteria.[/]");

        PressAnyKey();
    }

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Query database for all rooms
    /// - For each room, retrieve all characters (Players) in that room
    /// - Display in a formatted list grouped by room
    /// - Show room name and description
    /// - Under each room, list all characters with their details
    /// - Handle rooms with no characters gracefully
    /// - Consider using Spectre.Console panels or tables for nice formatting
    /// - Log the operation
    /// </summary>
    public void ListAllRoomsWithCharacters()
    {
        _logger.LogInformation("User selected List All Rooms with Characters");
        AnsiConsole.MarkupLine("[yellow]=== List All Rooms with Characters ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Group and display all characters by their rooms.[/]");

        PressAnyKey();
    }

    /// <summary>
    /// TODO: Implement this method
    /// Requirements:
    /// - Prompt user for equipment/item name to search for
    /// - Query the database to find which character has this equipment
    /// - Use Include to load Equipment -> Weapon/Armor -> Item
    /// - Also load the character's Room information
    /// - Display the character's name who has the equipment
    /// - Display the room/location where the character is located
    /// - Handle case where equipment is not found
    /// - Handle case where equipment exists but isn't equipped by anyone
    /// - Use Spectre.Console for nice formatting
    /// - Log the operation
    /// </summary>
    public void FindEquipmentLocation()
    {
        _logger.LogInformation("User selected Find Equipment Location");
        AnsiConsole.MarkupLine("[yellow]=== Find Equipment Location ===[/]");

        // TODO: Implement this method
        AnsiConsole.MarkupLine("[red]This feature is not yet implemented.[/]");
        AnsiConsole.MarkupLine("[yellow]TODO: Find which character has specific equipment and where they are located.[/]");

        PressAnyKey();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method for user interaction consistency
    /// </summary>
    private void PressAnyKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    #endregion
}
