using ConsoleRpgEntities.Data;
using ConsoleRpgEntities.Models.Characters;
using ConsoleRpgEntities.Models.Characters.Monsters;
using ConsoleRpgEntities.Models.Equipments;
using ConsoleRpgEntities.Models.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace ConsoleRpg.Services;

/// <summary>
/// Handles all admin/developer CRUD operations and advanced queries
/// Separated from GameEngine to follow Single Responsibility Principle
/// </summary>
public class AdminService(GameContext context, ILogger<AdminService> logger)
{
    #region Basic CRUD Operations

    /// <summary>
    /// Add a new character to the database
    /// </summary>
    public void AddCharacter()
    {
        try
        {
            logger.LogInformation("User selected Add Character");
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

            // Ask if player wants to equip starting gear
            AnsiConsole.WriteLine();
            if (AnsiConsole.Confirm("[yellow]Would you like to equip starting gear?[/]"))
            {
                EquipStartingGear(player);
            }

            context.Players.Add(player);
            context.SaveChanges();

            logger.LogInformation("Character {Name} added to database with Id {Id}", name, player.Id);
            AnsiConsole.MarkupLine($"[green]Character '{name}' added successfully![/]");
            
            // Show equipped items if any - reload to get navigation properties
            if (player.Equipment != null)
            {
                // Reload player with equipment to get navigation properties properly loaded
                var reloadedPlayer = context.Players
                    .Include(p => p.Equipment)
                        .ThenInclude(e => e.Weapon)
                    .Include(p => p.Equipment)
                        .ThenInclude(e => e.Armor)
                    .FirstOrDefault(p => p.Id == player.Id);

                if (reloadedPlayer?.Equipment != null)
                {
                    AnsiConsole.MarkupLine($"[cyan]Equipped with:[/]");
                    if (reloadedPlayer.Equipment.Weapon != null)
                        AnsiConsole.MarkupLine($"  • Weapon: {reloadedPlayer.Equipment.Weapon.Name.EscapeMarkup()} (Attack: {reloadedPlayer.Equipment.Weapon.Attack})");
                    if (reloadedPlayer.Equipment.Armor != null)
                        AnsiConsole.MarkupLine($"  • Armor: {reloadedPlayer.Equipment.Armor.Name.EscapeMarkup()} (Defense: {reloadedPlayer.Equipment.Armor.Defense})");
                }
            }
            
            Thread.Sleep(1500);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding character");
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
            logger.LogInformation("User selected Edit Character");
            AnsiConsole.MarkupLine("[yellow]=== Edit Character ===[/]");

            // Display all characters first
            var allPlayers = context.Players.Include(p => p.Room).ToList();

            if (!allPlayers.Any())
            {
                AnsiConsole.MarkupLine("[red]No characters found.[/]");
                PressAnyKey();
                return;
            }

            var table = new Table();
            table.AddColumn("ID");
            table.AddColumn("Name");
            table.AddColumn("Health");
            table.AddColumn("Experience");
            table.AddColumn("Location");

            foreach (var p in allPlayers)
            {
                table.AddRow(
                    p.Id.ToString(),
                    p.Name,
                    p.Health.ToString(),
                    p.Experience.ToString(),
                    p.Room?.Name ?? "[dim]No Location[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var id = AnsiConsole.Ask<int>("Enter character [green]ID[/] to edit:");

            var player = context.Players.Find(id);
            if (player == null)
            {
                logger.LogWarning("Character with Id {Id} not found", id);
                AnsiConsole.MarkupLine($"[red]Character with ID {id} not found.[/]");
                PressAnyKey();
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

            context.SaveChanges();

            logger.LogInformation("Character {Name} (Id: {Id}) updated", player.Name, player.Id);
            AnsiConsole.MarkupLine($"[green]Character '{player.Name}' updated successfully![/]");
            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error editing character");
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
            logger.LogInformation("User selected Display All Characters");
            AnsiConsole.MarkupLine("[yellow]=== All Characters ===[/]");

            var players = context.Players.Include(p => p.Room).ToList();

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
                        player.Room?.Name ?? "[dim]No Location[/]"
                    );
                }

                AnsiConsole.Write(table);
            }

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error displaying all characters");
            AnsiConsole.MarkupLine($"[red]Error displaying characters: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    /// <summary>
    /// Search for characters by name
    /// </summary>
    public void SearchCharacterByName()
    {
        try
        {
            logger.LogInformation("User selected Search Character");
            AnsiConsole.MarkupLine("[yellow]=== Search Character ===[/]");

            var searchName = AnsiConsole.Ask<string>("Enter character [green]name[/] to search:");

            var players = context.Players
                .Include(p => p.Room)
                .Where(p => p.Name.ToLower().Contains(searchName.ToLower()))
                .ToList();

            if (!players.Any())
            {
                logger.LogInformation("No characters found matching '{SearchName}'", searchName);
                AnsiConsole.MarkupLine($"[red]No characters found matching '{searchName}'.[/]");
            }
            else
            {
                logger.LogInformation("Found {Count} character(s) matching '{SearchName}'", players.Count, searchName);

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
                        player.Room?.Name ?? "[dim]No Location[/]"
                    );
                }

                AnsiConsole.Write(table);
            }

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching for characters");
            AnsiConsole.MarkupLine($"[red]Error searching characters: {ex.Message}[/]");
            PressAnyKey();
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
            logger.LogInformation("User selected Add Ability to Character");
            AnsiConsole.MarkupLine("[yellow]=== Add Ability to Character ===[/]");

            var players = context.Players.Include(p => p.Abilities).ToList();

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
                    p.Abilities.Count.ToString()
                );
            }

            AnsiConsole.Write(characterTable);

            var characterId = AnsiConsole.Ask<int>("Enter character [green]ID[/]:");
            var player = context.Players
                .Include(p => p.Abilities)
                .FirstOrDefault(p => p.Id == characterId);

            if (player == null)
            {
                logger.LogWarning("Character with ID {Id} not found", characterId);
                AnsiConsole.MarkupLine($"[red]Character with ID {characterId} not found.[/]");
                PressAnyKey();
                return;
            }

            var abilities = context.Abilities.ToList();

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
                    ability.Description
                );
            }

            AnsiConsole.Write(abilityTable);

            var abilityId = AnsiConsole.Ask<int>("Enter ability [green]ID[/] to add:");
            var selectedAbility = context.Abilities.Find(abilityId);

            if (selectedAbility == null)
            {
                logger.LogWarning("Ability with ID {Id} not found", abilityId);
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

            player.Abilities.Add(selectedAbility);
            context.SaveChanges();

            logger.LogInformation("Added ability {AbilityName} to character {CharacterName} (ID: {Id})",
                selectedAbility.Name, player.Name, player.Id);
            AnsiConsole.MarkupLine(
                $"[green]Successfully added ability '{selectedAbility.Name}' to character '{player.Name}'.[/]");
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding ability to character");
            AnsiConsole.MarkupLine($"[red]Error adding ability: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    /// <summary>
    /// Implement this method
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
        try
        {
            logger.LogInformation("User selected Display Character Abilities");
            AnsiConsole.MarkupLine("[yellow]=== Display Character Abilities ===[/]");

            var players = context.Players.Include(p => p.Abilities).ToList();

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
            characterTable.AddColumn("Experience");

            foreach (var p in players)
            {
                characterTable.AddRow(
                    p.Id.ToString(),
                    p.Name,
                    p.Health.ToString(),
                    p.Experience.ToString()
                );
            }

            AnsiConsole.Write(characterTable);

            var characterId = AnsiConsole.Ask<int>("Enter character [green]ID[/]:");
            var player = context.Players
                .Include(p => p.Abilities)
                .FirstOrDefault(p => p.Id == characterId);

            if (player == null)
            {
                logger.LogWarning("Character with ID {Id} not found", characterId);
                AnsiConsole.MarkupLine($"[red]Character with ID {characterId} not found.[/]");
                PressAnyKey();
                return;
            }

            AnsiConsole.MarkupLine($"\n[cyan]{player.Name}[/] - HP: {player.Health} | XP: {player.Experience}");
            AnsiConsole.WriteLine();

            if (!player.Abilities.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]Character '{player.Name}' has no abilities.[/]");
            }
            else
            {
                var abilityTable = new Table();
                abilityTable.AddColumn("Name");
                abilityTable.AddColumn("Description");
                abilityTable.AddColumn("Type");
                foreach (var ability in player.Abilities)
                {
                    var type = ability.GetType().Name;
                    abilityTable.AddRow(
                        ability.Name,
                        ability.Description,
                        type
                    );
                }

                AnsiConsole.Write(abilityTable);
                logger.LogInformation("Displayed {Count} abilities for {CharacterName} (ID: {Id}",
                    player.Abilities.Count, player.Name, player.Id);
            }

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error displaying character abilities");
            AnsiConsole.MarkupLine($"[red]Error displaying abilities: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    #endregion

    #region B-Level Requirements

    /// <summary>
    /// Implement this method
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
        try
        {
            logger.LogInformation("User selected Add Room");
            AnsiConsole.MarkupLine("[yellow]=== Add Room ===[/]");

            var name = AnsiConsole.Ask<string>("Enter room [green]name[/]:");
            var description = AnsiConsole.Ask<string>("Enter room [green]description[/]:");

            var (x, y) = GetRoomCoordinates();

            var room = new Room
            {
                Name = name,
                Description = description,
                X = x,
                Y = y
            };

            context.Rooms.Add(room);
            context.SaveChanges();

            logger.LogInformation("Room {Name} added to database with Id {Id} at ({X},{Y})",
                name, room.Id, x, y);
            AnsiConsole.MarkupLine($"[green]Room '{name}' added successfully at coordinates ({x}, {y})![/]");

            if (AnsiConsole.Confirm("Add a character to this room?"))
            {
                AddCharacterToRoom(room);
            }

            if (AnsiConsole.Confirm("Add monsters to this room?"))
            {
                AddMonstersToRoom(room);
            }

            if (AnsiConsole.Confirm("Configure room connections (North/South/East/West)?"))
            {
                ConfigureRoomConnections(room);
            }

            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding room");
            AnsiConsole.MarkupLine($"[red]Error adding room: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    private (int x, int y) GetRoomCoordinates()
    {
        var existingRooms = context.Rooms.ToList();

        if (!existingRooms.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No existing rooms. Creating first room at (0, 0).[/]");
            return (0, 0);
        }

        // Get available coordinates
        var occupiedCoordinates = existingRooms.Select(r => (r.X, r.Y)).ToHashSet();

        // Interactive tile selector is now the DEFAULT
        // Only ask if they want manual entry instead
        if (AnsiConsole.Confirm("Use [green]manual coordinate entry[/] instead of interactive selector?"))
        {
            return GetManualCoordinates(existingRooms, occupiedCoordinates);
        }

        // Default to interactive tile selector
        return InteractiveTileSelector(existingRooms, occupiedCoordinates);
    }

    private (int x, int y) InteractiveTileSelector(List<Room> existingRooms, HashSet<(int, int)> occupied)
    {
        // Calculate boundaries with padding for new rooms
        int minX = existingRooms.Min(r => r.X) - 2;
        int maxX = existingRooms.Max(r => r.X) + 2;
        int minY = existingRooms.Min(r => r.Y) - 2;
        int maxY = existingRooms.Max(r => r.Y) + 2;

        int currentX = 0;
        int currentY = 0;

        // Start at first empty adjacent space
        foreach (var room in existingRooms)
        {
            var adjacentSpaces = new[]
            {
                (room.X, room.Y + 1), // North
                (room.X, room.Y - 1), // South
                (room.X + 1, room.Y), // East
                (room.X - 1, room.Y) // West
            };

            var firstEmpty = adjacentSpaces.FirstOrDefault(pos => !occupied.Contains(pos));
            if (firstEmpty != default)
            {
                currentX = firstEmpty.Item1;
                currentY = firstEmpty.Item2;
                break;
            }
        }

        while (true)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[cyan]═══ Interactive Room Placement ═══[/]");
            AnsiConsole.MarkupLine(
                "[yellow]Arrow Keys/WASD[/] = Move [green]@[/] cursor | [green]Enter[/] = Select [dim][[ ]][/] slot | [red]ESC[/] = Manual entry\n");

            RenderTileGrid(existingRooms, occupied, currentX, currentY, minX, maxX, minY, maxY);

            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    if (currentY < maxY) currentY++;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    if (currentY > minY) currentY--;
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    if (currentX < maxX) currentX++;
                    break;
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    if (currentX > minX) currentX--;
                    break;
                case ConsoleKey.Enter:
                    if (!occupied.Contains((currentX, currentY)))
                    {
                        AnsiConsole.MarkupLine($"\n[green]✓ Selected coordinates: ({currentX}, {currentY})[/]");
                        Thread.Sleep(800);
                        return (currentX, currentY);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("\n[red]✗ This space is occupied! Choose an empty space.[/]");
                        Thread.Sleep(1200);
                    }

                    break;
                case ConsoleKey.Escape:
                    AnsiConsole.MarkupLine("\n[yellow]Switching to manual coordinate entry...[/]");
                    Thread.Sleep(500);
                    return GetManualCoordinates(existingRooms, occupied);
            }
        }
    }

    private void RenderTileGrid(List<Room> rooms, HashSet<(int, int)> occupied,
        int cursorX, int cursorY, int minX, int maxX, int minY, int maxY)
    {
        var table = new Table();
        table.Border = TableBorder.Square;
        table.ShowHeaders = false;
        table.Expand = false;

        // Add column for Y-axis labels
        table.AddColumn(new TableColumn("[dim]Y\\X[/]").Width(4).RightAligned());

        // Add columns for each X coordinate
        for (int x = minX; x <= maxX; x++)
        {
            table.AddColumn(new TableColumn($"[dim]{x}[/]").Width(5).Centered());
        }

        // Add rows from top to bottom (high Y to low Y)
        for (int y = maxY; y >= minY; y--)
        {
            var row = new List<string> { $"[dim]{y}[/]" };

            for (int x = minX; x <= maxX; x++)
            {
                var isOccupied = occupied.Contains((x, y));
                var isCursor = (x == cursorX && y == cursorY);
                var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);

                string cell;

                if (isCursor && isOccupied)
                {
                    // Cursor on occupied space (invalid selection) - red @ on occupied room
                    cell = "[bold red on white][[@]][/]";
                }
                else if (isCursor)
                {
                    // Valid cursor position (empty space) - green @ 
                    cell = "[bold green][[@]][/]";
                }
                else if (isOccupied)
                {
                    // Existing room - blue cube ■
                    cell = "[blue][[■]][/]";
                }
                else
                {
                    // Empty selectable space - [  ]
                    cell = "[dim][[ ]][/]";
                }

                row.Add(cell);
            }

            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);

        // Show legend
        AnsiConsole.WriteLine();
        var legendGrid = new Grid();
        legendGrid.AddColumn();
        legendGrid.AddColumn();
        legendGrid.AddColumn();
        legendGrid.AddColumn();

        legendGrid.AddRow(
            "[green][[@]][/] Your cursor",
            "[red on white][[@]][/] Invalid position",
            "[blue][[■]][/] Existing room",
            "[dim][[ ]][/] Empty slot"
        );

        var legendPanel = new Panel(legendGrid)
        {
            Header = new PanelHeader("[yellow]Legend[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };
        AnsiConsole.Write(legendPanel);

        // Show current position info
        AnsiConsole.WriteLine();
        var roomAtCursor = rooms.FirstOrDefault(r => r.X == cursorX && r.Y == cursorY);
        if (roomAtCursor != null)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Position: ({cursorX}, {cursorY})[/] - [red]Occupied by: {roomAtCursor.Name}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[cyan]Position: ({cursorX}, {cursorY})[/] - [green]Empty (Ready to place)[/]");

            // Show adjacent rooms
            var adjacentRooms = rooms.Where(r =>
                (r.X == cursorX && Math.Abs(r.Y - cursorY) == 1) ||
                (r.Y == cursorY && Math.Abs(r.X - cursorX) == 1)
            ).ToList();

            if (adjacentRooms.Any())
            {
                var directions = new List<string>();
                foreach (var adj in adjacentRooms)
                {
                    if (adj.X == cursorX && adj.Y == cursorY + 1) directions.Add($"North: {adj.Name}");
                    if (adj.X == cursorX && adj.Y == cursorY - 1) directions.Add($"South: {adj.Name}");
                    if (adj.X == cursorX + 1 && adj.Y == cursorY) directions.Add($"East: {adj.Name}");
                    if (adj.X == cursorX - 1 && adj.Y == cursorY) directions.Add($"West: {adj.Name}");
                }

                AnsiConsole.MarkupLine($"[dim]Adjacent: {string.Join(", ", directions)}[/]");
            }
        }
    }

    private (int x, int y) GetManualCoordinates(List<Room> existingRooms, HashSet<(int, int)> occupied)
    {
        Console.Clear();
        AnsiConsole.MarkupLine("[yellow]=== Manual Coordinate Entry ===[/]\n");

        // Show suggested coordinates
        if (AnsiConsole.Confirm("View suggested adjacent coordinates?"))
        {
            DisplaySuggestedCoordinates(existingRooms, occupied);
            AnsiConsole.WriteLine();
        }

        int x, y;
        bool isValid;

        do
        {
            x = AnsiConsole.Ask<int>("Enter [green]X coordinate[/]:");
            y = AnsiConsole.Ask<int>("Enter [green]Y coordinate[/]:");

            isValid = !occupied.Contains((x, y));

            if (!isValid)
            {
                AnsiConsole.MarkupLine(
                    $"[red]Coordinates ({x}, {y}) are already occupied. Please choose different coordinates.[/]");
            }
        } while (!isValid);

        return (x, y);
    }

    private void DisplayCoordinateMap(List<Room> rooms)
    {
        if (!rooms.Any()) return;

        int minX = rooms.Min(r => r.X);
        int maxX = rooms.Max(r => r.X);
        int minY = rooms.Min(r => r.Y);
        int maxY = rooms.Max(r => r.Y);

        var table = new Table();
        table.Border = TableBorder.Square;

        // Add columns for coordinates
        table.AddColumn("");
        for (int x = minX; x <= maxX; x++)
        {
            table.AddColumn($"[cyan]{x}[/]");
        }

        // Add rows
        for (int y = maxY; y >= minY; y--)
        {
            var row = new List<string> { $"[cyan]{y}[/]" };

            for (int x = minX; x <= maxX; x++)
            {
                var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);
                row.Add(room != null ? "[green]■[/]" : "[dim]·[/]");
            }

            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);
    }

    private void DisplaySuggestedCoordinates(List<Room> rooms, HashSet<(int, int)> occupied)
    {
        var suggestions = new List<(int x, int y, string adjacent)>();

        foreach (var room in rooms)
        {
            // Check all four directions
            if (!occupied.Contains((room.X, room.Y + 1)))
                suggestions.Add((room.X, room.Y + 1, $"North of {room.Name}"));
            if (!occupied.Contains((room.X, room.Y - 1)))
                suggestions.Add((room.X, room.Y - 1, $"South of {room.Name}"));
            if (!occupied.Contains((room.X + 1, room.Y)))
                suggestions.Add((room.X + 1, room.Y, $"East of {room.Name}"));
            if (!occupied.Contains((room.X - 1, room.Y)))
                suggestions.Add((room.X - 1, room.Y, $"West of {room.Name}"));
        }

        if (suggestions.Any())
        {
            var table = new Table();
            table.AddColumn("[yellow]X[/]");
            table.AddColumn("[yellow]Y[/]");
            table.AddColumn("[yellow]Location[/]");

            foreach (var (x, y, adjacent) in suggestions.Distinct())
            {
                table.AddRow(x.ToString(), y.ToString(), adjacent);
            }

            AnsiConsole.Write(table);
        }
    }

    private void AddCharacterToRoom(Room room)
    {
        var players = context.Players.ToList();

        if (!players.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No characters available to add.[/]");
            return;
        }

        var playerTable = new Table();
        playerTable.AddColumn("ID");
        playerTable.AddColumn("Name");
        playerTable.AddColumn("Health");

        foreach (var p in players)
        {
            playerTable.AddRow(p.Id.ToString(), p.Name, p.Health.ToString());
        }

        AnsiConsole.Write(playerTable);

        var playerId = AnsiConsole.Ask<int>("Enter character [green]ID[/] to add:");
        var player = context.Players.Find(playerId);

        if (player != null)
        {
            player.RoomId = room.Id;
            context.SaveChanges();
            AnsiConsole.MarkupLine($"[green]Character '{player.Name}' added to room '{room.Name}'.[/]");
            logger.LogInformation("Character {PlayerName} added to room {RoomName}", player.Name, room.Name);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Character with ID {playerId} not found.[/]");
        }
    }

    private void AddMonstersToRoom(Room room)
    {
        var unassignedMonsters = context.Monsters.Where(m => m.RoomId == null).ToList();
        var allMonsters = context.Monsters.ToList();

        if (!allMonsters.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No monsters exist in the database.[/]");

            if (AnsiConsole.Confirm("Create a new monster?"))
            {
                CreateAndAddMonster(room);
            }

            return;
        }

        bool addingMonsters = true;

        while (addingMonsters)
        {
            AnsiConsole.MarkupLine("\n[cyan]Choose how to add a monster:[/]");
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .AddChoices(new[]
                    {
                        "Assign an unassigned monster",
                        "Copy/Clone an existing monster",
                        "Create a brand new monster",
                        "Done adding monsters"
                    }));

            switch (choice)
            {
                case "Assign an unassigned monster":
                    AssignUnassignedMonster(room, unassignedMonsters);
                    // Refresh the list
                    unassignedMonsters = context.Monsters.Where(m => m.RoomId == null).ToList();
                    break;

                case "Copy/Clone an existing monster":
                    CloneMonsterToRoom(room, allMonsters);
                    // Refresh both lists
                    unassignedMonsters = context.Monsters.Where(m => m.RoomId == null).ToList();
                    allMonsters = context.Monsters.ToList();
                    break;

                case "Create a brand new monster":
                    CreateAndAddMonster(room);
                    // Refresh both lists
                    unassignedMonsters = context.Monsters.Where(m => m.RoomId == null).ToList();
                    allMonsters = context.Monsters.ToList();
                    break;

                case "Done adding monsters":
                    addingMonsters = false;
                    break;
            }
        }
    }

    private void AssignUnassignedMonster(Room room, List<Monster> unassignedMonsters)
    {
        if (!unassignedMonsters.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No unassigned monsters available. Try copying or creating one instead.[/]");
            Thread.Sleep(1500);
            return;
        }

        var monsterTable = new Table();
        monsterTable.Title = new TableTitle("[yellow]Unassigned Monsters[/]");
        monsterTable.AddColumn("ID");
        monsterTable.AddColumn("Name");
        monsterTable.AddColumn("Type");
        monsterTable.AddColumn("Health");

        foreach (var m in unassignedMonsters)
        {
            monsterTable.AddRow(m.Id.ToString(), m.Name, m.MonsterType, m.Health.ToString());
        }

        AnsiConsole.Write(monsterTable);

        var monsterId = AnsiConsole.Ask<int>("Enter monster [green]ID[/] to assign (0 to cancel):");

        if (monsterId == 0) return;

        var monster = context.Monsters.Find(monsterId);

        if (monster != null && monster.RoomId == null)
        {
            monster.RoomId = room.Id;
            context.SaveChanges();
            AnsiConsole.MarkupLine($"[green]✓ Monster '{monster.Name}' assigned to room '{room.Name}'.[/]");
            logger.LogInformation("Monster {MonsterName} assigned to room {RoomName}", monster.Name, room.Name);
            Thread.Sleep(1000);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Monster with ID {monsterId} not found or already assigned.[/]");
            Thread.Sleep(1500);
        }
    }

    private void CloneMonsterToRoom(Room room, List<Monster> allMonsters)
    {
        var monsterTable = new Table();
        monsterTable.Title = new TableTitle("[cyan]All Monsters (Select one to copy)[/]");
        monsterTable.AddColumn("ID");
        monsterTable.AddColumn("Name");
        monsterTable.AddColumn("Type");
        monsterTable.AddColumn("Health");
        monsterTable.AddColumn("Current Location");

        foreach (var m in allMonsters)
        {
            var location = m.RoomId.HasValue
                ? context.Rooms.Find(m.RoomId.Value)?.Name ?? "Unknown Room"
                : "[dim]Unassigned[/]";

            monsterTable.AddRow(
                m.Id.ToString(),
                m.Name,
                m.MonsterType,
                m.Health.ToString(),
                location
            );
        }

        AnsiConsole.Write(monsterTable);

        var monsterId = AnsiConsole.Ask<int>("Enter monster [green]ID[/] to copy (0 to cancel):");

        if (monsterId == 0) return;

        var sourceMonster = context.Monsters.Find(monsterId);

        if (sourceMonster == null)
        {
            AnsiConsole.MarkupLine($"[red]Monster with ID {monsterId} not found.[/]");
            Thread.Sleep(1500);
            return;
        }

        // Ask if they want to customize the copy
        var customizeName =
            AnsiConsole.Confirm($"Customize the copy? (Default: copy '{sourceMonster.Name}' exactly)", false);

        string newName = sourceMonster.Name;
        int newHealth = sourceMonster.Health;
        int newAggression = sourceMonster.AggressionLevel;

        if (customizeName)
        {
            newName = AnsiConsole.Ask($"Enter name for copy (or press Enter for '{sourceMonster.Name}'):",
                sourceMonster.Name);
            newHealth = AnsiConsole.Ask($"Enter health (or press Enter for {sourceMonster.Health}):",
                sourceMonster.Health);
            newAggression = AnsiConsole.Ask($"Enter aggression (or press Enter for {sourceMonster.AggressionLevel}):",
                sourceMonster.AggressionLevel);
        }

        // Create a copy based on the monster type
        Monster newMonster;

        if (sourceMonster is Goblin goblin)
        {
            newMonster = new Goblin
            {
                Name = newName,
                MonsterType = sourceMonster.MonsterType,
                Health = newHealth,
                AggressionLevel = newAggression,
                RoomId = room.Id,
                Sneakiness = goblin.Sneakiness
            };
        }
        else
        {
            // Generic fallback - create a Goblin with default sneakiness
            newMonster = new Goblin
            {
                Name = newName,
                MonsterType = sourceMonster.MonsterType,
                Health = newHealth,
                AggressionLevel = newAggression,
                RoomId = room.Id,
                Sneakiness = 0
            };
        }

        context.Monsters.Add(newMonster);
        context.SaveChanges();

        AnsiConsole.MarkupLine(
            $"[green]✓ Created a copy of '{sourceMonster.Name}' → '{newMonster.Name}' in room '{room.Name}'![/]");
        logger.LogInformation("Cloned monster {SourceName} to {NewName} in room {RoomName}",
            sourceMonster.Name, newMonster.Name, room.Name);
        Thread.Sleep(1000);
    }


    private void CreateAndAddMonster(Room room)
    {
        var name = AnsiConsole.Ask<string>("Enter monster [green]name[/]:");

        var monsterType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select monster [green]type[/]:")
                .AddChoices(new[] { "Goblin", "Other" }));

        var health = AnsiConsole.Ask<int>("Enter monster [green]health[/]:");
        var aggression = AnsiConsole.Ask<int>("Enter [green]aggression level[/] (0-10):");

        Monster monster;

        switch (monsterType)
        {
            case "Goblin":
                var sneakiness = AnsiConsole.Ask<int>("Enter [green]sneakiness level[/] (0-10):");
                monster = new Goblin
                {
                    Name = name,
                    MonsterType = monsterType,
                    Health = health,
                    AggressionLevel = aggression,
                    RoomId = room.Id,
                    Sneakiness = sneakiness
                };
                break;
            default:
                // For "Other" or future monster types, we need a concrete implementation
                // Using Goblin as default with sneakiness = 0
                monster = new Goblin
                {
                    Name = name,
                    MonsterType = monsterType,
                    Health = health,
                    AggressionLevel = aggression,
                    RoomId = room.Id,
                    Sneakiness = 0
                };
                break;
        }

        context.Monsters.Add(monster);
        context.SaveChanges();

        AnsiConsole.MarkupLine($"[green]Monster '{name}' created and added to room '{room.Name}'![/]");
        logger.LogInformation("Monster {MonsterName} created and added to room {RoomName}", name, room.Name);
    }

    private void ConfigureRoomConnections(Room room)
    {
        var existingRooms = context.Rooms.Where(r => r.Id != room.Id).ToList();

        if (!existingRooms.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No other rooms available to connect.[/]");
            return;
        }

        // Auto-detect adjacent rooms based on coordinates
        var northRoom = existingRooms.FirstOrDefault(r => r.X == room.X && r.Y == room.Y + 1);
        var southRoom = existingRooms.FirstOrDefault(r => r.X == room.X && r.Y == room.Y - 1);
        var eastRoom = existingRooms.FirstOrDefault(r => r.X == room.X + 1 && r.Y == room.Y);
        var westRoom = existingRooms.FirstOrDefault(r => r.X == room.X - 1 && r.Y == room.Y);

        var connectionsMade = 0;

        // North connection
        if (northRoom != null)
        {
            if (AnsiConsole.Confirm(
                    $"Connect to [cyan]{northRoom.Name}[/] (ID: {northRoom.Id}) in the [bold]North[/]?"))
            {
                room.NorthRoomId = northRoom.Id;
                northRoom.SouthRoomId = room.Id;
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] Connected: [cyan]{room.Name}[/] ↔ North ↔ [cyan]{northRoom.Name}[/]");
                connectionsMade++;
            }
        }

        // South connection
        if (southRoom != null)
        {
            if (AnsiConsole.Confirm(
                    $"Connect to [cyan]{southRoom.Name}[/] (ID: {southRoom.Id}) in the [bold]South[/]?"))
            {
                room.SouthRoomId = southRoom.Id;
                southRoom.NorthRoomId = room.Id;
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] Connected: [cyan]{room.Name}[/] ↔ South ↔ [cyan]{southRoom.Name}[/]");
                connectionsMade++;
            }
        }

        // East connection
        if (eastRoom != null)
        {
            if (AnsiConsole.Confirm($"Connect to [cyan]{eastRoom.Name}[/] (ID: {eastRoom.Id}) in the [bold]East[/]?"))
            {
                room.EastRoomId = eastRoom.Id;
                eastRoom.WestRoomId = room.Id;
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] Connected: [cyan]{room.Name}[/] ↔ East ↔ [cyan]{eastRoom.Name}[/]");
                connectionsMade++;
            }
        }

        // West connection
        if (westRoom != null)
        {
            if (AnsiConsole.Confirm($"Connect to [cyan]{westRoom.Name}[/] (ID: {westRoom.Id}) in the [bold]West[/]?"))
            {
                room.WestRoomId = westRoom.Id;
                westRoom.EastRoomId = room.Id;
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] Connected: [cyan]{room.Name}[/] ↔ West ↔ [cyan]{westRoom.Name}[/]");
                connectionsMade++;
            }
        }

        // Offer manual connections if no adjacent rooms or user wants more
        if (connectionsMade == 0 ||
            AnsiConsole.Confirm("Configure additional manual connections (non-adjacent rooms)?"))
        {
            ConfigureManualConnections(room, existingRooms);
        }

        context.SaveChanges();
        AnsiConsole.MarkupLine(
            $"[green]Room connections configured successfully! ({connectionsMade} automatic connections made)[/]");
    }

    private void ConfigureManualConnections(Room room, List<Room> existingRooms)
    {
        var directions = new[] { "North", "South", "East", "West", "Done" };

        while (true)
        {
            var direction = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select direction to connect from [cyan]{room.Name}[/] (or Done to finish):")
                    .AddChoices(directions));

            if (direction == "Done") break;

            // Check if already connected
            bool alreadyConnected = direction switch
            {
                "North" => room.NorthRoomId.HasValue,
                "South" => room.SouthRoomId.HasValue,
                "East" => room.EastRoomId.HasValue,
                "West" => room.WestRoomId.HasValue,
                _ => false
            };

            if (alreadyConnected)
            {
                if (!AnsiConsole.Confirm($"[yellow]{direction} is already connected. Override?[/]"))
                    continue;
            }

            // Show available rooms
            var roomTable = new Table();
            roomTable.AddColumn("ID");
            roomTable.AddColumn("Name");
            roomTable.AddColumn("Coordinates");
            roomTable.AddColumn("Current Exits");

            foreach (var r in existingRooms)
            {
                var exits = new List<string>();
                if (r.NorthRoomId.HasValue) exits.Add("N");
                if (r.SouthRoomId.HasValue) exits.Add("S");
                if (r.EastRoomId.HasValue) exits.Add("E");
                if (r.WestRoomId.HasValue) exits.Add("W");

                roomTable.AddRow(
                    r.Id.ToString(),
                    r.Name,
                    $"({r.X}, {r.Y})",
                    exits.Any() ? string.Join(",", exits) : "-"
                );
            }

            AnsiConsole.Write(roomTable);

            var targetRoomId = AnsiConsole.Ask<int>($"Enter room ID to connect [bold]{direction}[/] (0 to cancel):");

            if (targetRoomId == 0) continue;

            var targetRoom = existingRooms.FirstOrDefault(r => r.Id == targetRoomId);

            if (targetRoom == null)
            {
                AnsiConsole.MarkupLine("[red]Invalid room ID![/]");
                continue;
            }

            // Make bidirectional connection
            switch (direction)
            {
                case "North":
                    room.NorthRoomId = targetRoom.Id;
                    targetRoom.SouthRoomId = room.Id;
                    break;
                case "South":
                    room.SouthRoomId = targetRoom.Id;
                    targetRoom.NorthRoomId = room.Id;
                    break;
                case "East":
                    room.EastRoomId = targetRoom.Id;
                    targetRoom.WestRoomId = room.Id;
                    break;
                case "West":
                    room.WestRoomId = targetRoom.Id;
                    targetRoom.EastRoomId = room.Id;
                    break;
            }

            AnsiConsole.MarkupLine($"[green]✓ Connected: {room.Name} --{direction}--> {targetRoom.Name}[/]");
            AnsiConsole.MarkupLine(
                $"[green]✓ Reverse: {targetRoom.Name} --{GetOppositeDirection(direction)}--> {room.Name}[/]");
        }
    }

    private string GetOppositeDirection(string direction)
    {
        return direction switch
        {
            "North" => "South",
            "South" => "North",
            "East" => "West",
            "West" => "East",
            _ => ""
        };
    }

    /// <summary>
    /// Manage connections for existing rooms - allows connecting/disconnecting rooms
    /// </summary>
    public void ManageRoomConnections()
    {
        try
        {
            logger.LogInformation("User selected Manage Room Connections");
            AnsiConsole.MarkupLine("[yellow]=== Manage Room Connections ===[/]");

            var rooms = context.Rooms.ToList();

            if (rooms.Count < 2)
            {
                AnsiConsole.MarkupLine("[red]Need at least 2 rooms to create connections.[/]");
                PressAnyKey();
                return;
            }

            // Display all rooms with their current connections
            var roomTable = new Table();
            roomTable.Title = new TableTitle("[cyan]Available Rooms[/]");
            roomTable.AddColumn("ID");
            roomTable.AddColumn("Name");
            roomTable.AddColumn("Coordinates");
            roomTable.AddColumn("Exits");

            foreach (var r in rooms)
            {
                var exits = new List<string>();
                if (r.NorthRoomId.HasValue) exits.Add($"N→{r.NorthRoomId}");
                if (r.SouthRoomId.HasValue) exits.Add($"S→{r.SouthRoomId}");
                if (r.EastRoomId.HasValue) exits.Add($"E→{r.EastRoomId}");
                if (r.WestRoomId.HasValue) exits.Add($"W→{r.WestRoomId}");

                roomTable.AddRow(
                    r.Id.ToString(),
                    r.Name,
                    $"({r.X}, {r.Y})",
                    exits.Any() ? string.Join(", ", exits) : "[dim]none[/]"
                );
            }

            AnsiConsole.Write(roomTable);
            AnsiConsole.WriteLine();

            // Select room to modify
            var roomId = AnsiConsole.Ask<int>("Enter room [green]ID[/] to manage connections for:");
            var selectedRoom = context.Rooms.Find(roomId);

            if (selectedRoom == null)
            {
                AnsiConsole.MarkupLine($"[red]Room with ID {roomId} not found.[/]");
                PressAnyKey();
                return;
            }

            AnsiConsole.MarkupLine($"\n[cyan]Managing connections for: {selectedRoom.Name}[/] (ID: {selectedRoom.Id})");
            AnsiConsole.MarkupLine($"[dim]Coordinates: ({selectedRoom.X}, {selectedRoom.Y})[/]\n");

            // Show current connections
            DisplayCurrentConnections(selectedRoom);

            // Ask what to do
            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(new[]
                    {
                        "Auto-connect to adjacent rooms",
                        "Manually add/modify connections",
                        "Remove a connection",
                        "Cancel"
                    }));

            var otherRooms = rooms.Where(r => r.Id != selectedRoom.Id).ToList();

            switch (action)
            {
                case "Auto-connect to adjacent rooms":
                    AutoConnectAdjacentRooms(selectedRoom, otherRooms);
                    break;
                case "Manually add/modify connections":
                    ConfigureManualConnections(selectedRoom, otherRooms);
                    break;
                case "Remove a connection":
                    RemoveConnection(selectedRoom);
                    break;
                case "Cancel":
                    AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
                    PressAnyKey();
                    return;
            }

            context.SaveChanges();
            logger.LogInformation("Room connections updated for {RoomName}", selectedRoom.Name);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]Connections updated successfully![/]");
            DisplayCurrentConnections(selectedRoom);

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error managing room connections");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    private void DisplayCurrentConnections(Room room)
    {
        AnsiConsole.MarkupLine("[yellow]Current Connections:[/]");
        var hasConnections = false;

        if (room.NorthRoomId.HasValue)
        {
            var northRoom = context.Rooms.Find(room.NorthRoomId.Value);
            AnsiConsole.MarkupLine($"  [cyan]North[/] → {northRoom?.Name ?? "Unknown"} (ID: {room.NorthRoomId})");
            hasConnections = true;
        }

        if (room.SouthRoomId.HasValue)
        {
            var southRoom = context.Rooms.Find(room.SouthRoomId.Value);
            AnsiConsole.MarkupLine($"  [cyan]South[/] → {southRoom?.Name ?? "Unknown"} (ID: {room.SouthRoomId})");
            hasConnections = true;
        }

        if (room.EastRoomId.HasValue)
        {
            var eastRoom = context.Rooms.Find(room.EastRoomId.Value);
            AnsiConsole.MarkupLine($"  [cyan]East[/] → {eastRoom?.Name ?? "Unknown"} (ID: {room.EastRoomId})");
            hasConnections = true;
        }

        if (room.WestRoomId.HasValue)
        {
            var westRoom = context.Rooms.Find(room.WestRoomId.Value);
            AnsiConsole.MarkupLine($"  [cyan]West[/] → {westRoom?.Name ?? "Unknown"} (ID: {room.WestRoomId})");
            hasConnections = true;
        }

        if (!hasConnections)
        {
            AnsiConsole.MarkupLine("  [dim]No connections[/]");
        }

        AnsiConsole.WriteLine();
    }

    private void AutoConnectAdjacentRooms(Room room, List<Room> otherRooms)
    {
        var northRoom = otherRooms.FirstOrDefault(r => r.X == room.X && r.Y == room.Y + 1);
        var southRoom = otherRooms.FirstOrDefault(r => r.X == room.X && r.Y == room.Y - 1);
        var eastRoom = otherRooms.FirstOrDefault(r => r.X == room.X + 1 && r.Y == room.Y);
        var westRoom = otherRooms.FirstOrDefault(r => r.X == room.X - 1 && r.Y == room.Y);

        var connectionsMade = 0;

        if (northRoom != null)
        {
            room.NorthRoomId = northRoom.Id;
            northRoom.SouthRoomId = room.Id;
            AnsiConsole.MarkupLine($"[green]✓[/] Connected North to [cyan]{northRoom.Name}[/]");
            connectionsMade++;
        }

        if (southRoom != null)
        {
            room.SouthRoomId = southRoom.Id;
            southRoom.NorthRoomId = room.Id;
            AnsiConsole.MarkupLine($"[green]✓[/] Connected South to [cyan]{southRoom.Name}[/]");
            connectionsMade++;
        }

        if (eastRoom != null)
        {
            room.EastRoomId = eastRoom.Id;
            eastRoom.WestRoomId = room.Id;
            AnsiConsole.MarkupLine($"[green]✓[/] Connected East to [cyan]{eastRoom.Name}[/]");
            connectionsMade++;
        }

        if (westRoom != null)
        {
            room.WestRoomId = westRoom.Id;
            westRoom.EastRoomId = room.Id;
            AnsiConsole.MarkupLine($"[green]✓[/] Connected West to [cyan]{westRoom.Name}[/]");
            connectionsMade++;
        }

        if (connectionsMade == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No adjacent rooms found to auto-connect.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Made {connectionsMade} automatic connection(s).[/]");
        }
    }

    private void RemoveConnection(Room room)
    {
        var connections = new List<string>();
        if (room.NorthRoomId.HasValue) connections.Add("North");
        if (room.SouthRoomId.HasValue) connections.Add("South");
        if (room.EastRoomId.HasValue) connections.Add("East");
        if (room.WestRoomId.HasValue) connections.Add("West");

        if (!connections.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No connections to remove.[/]");
            return;
        }

        connections.Add("Cancel");

        var direction = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which connection to remove?")
                .AddChoices(connections));

        if (direction == "Cancel") return;

        Room? targetRoom = null;
        string reverseDirection = GetOppositeDirection(direction);

        switch (direction)
        {
            case "North":
                targetRoom = context.Rooms.Find(room.NorthRoomId!.Value);
                room.NorthRoomId = null;
                if (targetRoom != null) targetRoom.SouthRoomId = null;
                break;
            case "South":
                targetRoom = context.Rooms.Find(room.SouthRoomId!.Value);
                room.SouthRoomId = null;
                if (targetRoom != null) targetRoom.NorthRoomId = null;
                break;
            case "East":
                targetRoom = context.Rooms.Find(room.EastRoomId!.Value);
                room.EastRoomId = null;
                if (targetRoom != null) targetRoom.WestRoomId = null;
                break;
            case "West":
                targetRoom = context.Rooms.Find(room.WestRoomId!.Value);
                room.WestRoomId = null;
                if (targetRoom != null) targetRoom.EastRoomId = null;
                break;
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Removed {direction} connection");
        if (targetRoom != null)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Removed reverse {reverseDirection} connection from {targetRoom.Name}");
        }
    }

    /// <summary>
    /// Implement this method
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
        try
        {
            logger.LogInformation("User selected Display Room Details");
            AnsiConsole.MarkupLine("[yellow]=== Display Room Details ===[/]");

            var rooms = context.Rooms.ToList();

            if (!rooms.Any())
            {
                AnsiConsole.MarkupLine("[red]No rooms found in database.[/]");
                PressAnyKey();
                return;
            }

            var roomTable = new Table();
            roomTable.AddColumn("ID");
            roomTable.AddColumn("Name");
            roomTable.AddColumn("Description");

            foreach (var r in rooms)
            {
                roomTable.AddRow(
                    r.Id.ToString(),
                    r.Name,
                    r.Description.Length > 50 ? r.Description.Substring(0, 47) + "..." : r.Description
                );
            }

            AnsiConsole.Write(roomTable);

            var roomId = AnsiConsole.Ask<int>("Enter room [green]ID[/]:");
            var room = context.Rooms
                .Include(r => r.Players)
                .Include(r => r.Monsters)
                .Include(r => r.NorthRoom)
                .Include(r => r.SouthRoom)
                .Include(r => r.EastRoom)
                .Include(r => r.WestRoom)
                .FirstOrDefault(r => r.Id == roomId);

            if (room == null)
            {
                logger.LogWarning("Room with ID {Id} not found", roomId);
                AnsiConsole.MarkupLine($"[red]Room with ID {roomId} not found.[/]");
                PressAnyKey();
                return;
            }

            var panel = new Panel(
                new Markup($"[bold cyan]{room.Name}[/]\n\n{room.Description}")
            );
            panel.Header = new PanelHeader("[yellow]Room Details[/]");
            panel.Border = BoxBorder.Rounded;
            AnsiConsole.Write(panel);

            AnsiConsole.WriteLine();

            if (room.Players.Any())
            {
                AnsiConsole.MarkupLine("[green]Characters in this room:[/]");
                var playerTable = new Table();
                playerTable.AddColumn("Name");
                playerTable.AddColumn("Health");
                playerTable.AddColumn("Experience");

                foreach (var player in room.Players)
                {
                    playerTable.AddRow(player.Name, player.Health.ToString(), player.Experience.ToString());
                }

                AnsiConsole.Write(playerTable);
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No characters in this room.[/]");
            }

            AnsiConsole.WriteLine();

            if (room.Monsters.Any())
            {
                AnsiConsole.MarkupLine("[red]Monsters in this room:[/]");
                var monsterTable = new Table();
                monsterTable.AddColumn("Name");
                monsterTable.AddColumn("Health");

                foreach (var monster in room.Monsters)
                {
                    monsterTable.AddRow(monster.Name, monster.Health.ToString());
                }

                AnsiConsole.Write(monsterTable);
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No monsters in this room.[/]");
            }

            logger.LogInformation("Displayed details for room {RoomName} (ID: {Id})", room.Name, room.Id);

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error displaying room details");
            AnsiConsole.MarkupLine($"[red]Error displaying room details: {ex.Message}[/]");
            PressAnyKey();
        }
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
        try
        {
            logger.LogInformation("User selected List Characters in Room by Attribute");
            AnsiConsole.MarkupLine("[yellow]=== List Characters in Room by Attribute ===[/]");

            // Get all rooms with players
            var rooms = context.Rooms
                .Include(r => r.Players)
                .Where(r => r.Players.Any())
                .ToList();

            if (!rooms.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No rooms with characters found.[/]");
                PressAnyKey();
                return;
            }

            // Display rooms to choose from
            var roomTable = new Table();
            roomTable.Title = new TableTitle("[cyan]Rooms with Characters[/]");
            roomTable.AddColumn("ID");
            roomTable.AddColumn("Room Name");
            roomTable.AddColumn("Character Count");

            foreach (var room in rooms)
            {
                roomTable.AddRow(
                    room.Id.ToString(),
                    room.Name,
                    room.Players.Count.ToString()
                );
            }

            AnsiConsole.Write(roomTable);
            AnsiConsole.WriteLine();

            // Select room
            var roomId = AnsiConsole.Ask<int>("Enter [green]Room ID[/] to search in:");
            var selectedRoom = rooms.FirstOrDefault(r => r.Id == roomId);

            if (selectedRoom == null)
            {
                AnsiConsole.MarkupLine($"[red]Room with ID {roomId} not found.[/]");
                PressAnyKey();
                return;
            }

            // Select search criteria
            var criteria = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select [green]search criteria[/]:")
                    .AddChoices(new[] { "Health", "Experience", "Name" }));

            List<Player> results;
            string searchDescription;

            switch (criteria)
            {
                case "Health":
                    var healthOperator = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select [green]comparison[/]:")
                            .AddChoices(new[] { "Greater than", "Less than", "Equal to" }));

                    var healthValue = AnsiConsole.Ask<int>("Enter [green]health value[/]:");

                    results = healthOperator switch
                    {
                        "Greater than" => selectedRoom.Players.Where(p => p.Health > healthValue).ToList(),
                        "Less than" => selectedRoom.Players.Where(p => p.Health < healthValue).ToList(),
                        "Equal to" => selectedRoom.Players.Where(p => p.Health == healthValue).ToList(),
                        _ => new List<Player>()
                    };

                    searchDescription = $"Health {healthOperator.ToLower()} {healthValue}";
                    break;

                case "Experience":
                    var expOperator = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select [green]comparison[/]:")
                            .AddChoices(new[] { "Greater than", "Less than", "Equal to" }));

                    var expValue = AnsiConsole.Ask<int>("Enter [green]experience value[/]:");

                    results = expOperator switch
                    {
                        "Greater than" => selectedRoom.Players.Where(p => p.Experience > expValue).ToList(),
                        "Less than" => selectedRoom.Players.Where(p => p.Experience < expValue).ToList(),
                        "Equal to" => selectedRoom.Players.Where(p => p.Experience == expValue).ToList(),
                        _ => new List<Player>()
                    };

                    searchDescription = $"Experience {expOperator.ToLower()} {expValue}";
                    break;

                case "Name":
                    var nameSearch = AnsiConsole.Ask<string>("Enter [green]name[/] (or part of name) to search:");

                    results = selectedRoom.Players
                        .Where(p => p.Name.Contains(nameSearch, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    searchDescription = $"Name contains '{nameSearch}'";
                    break;

                default:
                    results = new List<Player>();
                    searchDescription = "Unknown";
                    break;
            }

            // Display results
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[cyan]Search Results in {selectedRoom.Name}:[/]");
            AnsiConsole.MarkupLine($"[dim]Criteria: {searchDescription}[/]");
            AnsiConsole.WriteLine();

            if (!results.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No characters found matching the criteria.[/]");
            }
            else
            {
                var resultsTable = new Table();
                resultsTable.Title = new TableTitle($"[green]Found {results.Count} Character(s)[/]");
                resultsTable.AddColumn("ID");
                resultsTable.AddColumn("Name");
                resultsTable.AddColumn("Health");
                resultsTable.AddColumn("Experience");

                foreach (var player in results)
                {
                    resultsTable.AddRow(
                        player.Id.ToString(),
                        player.Name,
                        player.Health.ToString(),
                        player.Experience.ToString()
                    );
                }

                AnsiConsole.Write(resultsTable);

                logger.LogInformation("Found {Count} characters in room {RoomName} matching criteria: {Criteria}",
                    results.Count, selectedRoom.Name, searchDescription);
            }

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing characters in room by attribute");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            PressAnyKey();
        }
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
        try
        {
            logger.LogInformation("User selected List All Rooms with Characters");
            AnsiConsole.MarkupLine("[yellow]=== List All Rooms with Characters ===[/]");
            AnsiConsole.WriteLine();

            // Get all rooms with their players
            var rooms = context.Rooms
                .Include(r => r.Players)
                .OrderBy(r => r.Name)
                .ToList();

            if (!rooms.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No rooms found in the database.[/]");
                PressAnyKey();
                return;
            }

            int totalRooms = rooms.Count;
            int roomsWithCharacters = rooms.Count(r => r.Players.Any());
            int totalCharacters = rooms.Sum(r => r.Players.Count);

            // Display summary
            var summaryPanel = new Panel(
                $"[cyan]Total Rooms:[/] {totalRooms}\n" +
                $"[green]Rooms with Characters:[/] {roomsWithCharacters}\n" +
                $"[yellow]Total Characters:[/] {totalCharacters}"
            )
            {
                Header = new PanelHeader("[bold cyan]Summary[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(summaryPanel);
            AnsiConsole.WriteLine();

            // Display each room with its characters
            foreach (var room in rooms)
            {
                var roomPanel = new Panel(BuildRoomCharacterList(room))
                {
                    Header = new PanelHeader($"[bold yellow]{room.Name}[/]"),
                    Border = BoxBorder.Rounded
                };

                AnsiConsole.Write(roomPanel);
                AnsiConsole.WriteLine();
            }

            logger.LogInformation("Displayed {RoomCount} rooms with {CharacterCount} total characters",
                totalRooms, totalCharacters);

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing all rooms with characters");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    private string BuildRoomCharacterList(Room room)
    {
        var sb = new System.Text.StringBuilder();

        // Room description
        sb.AppendLine($"[dim]{room.Description}[/]");
        sb.AppendLine($"[dim]Location: ({room.X}, {room.Y})[/]");
        sb.AppendLine();

        // Characters in this room
        if (room.Players.Any())
        {
            sb.AppendLine($"[green]Characters ({room.Players.Count}):[/]");

            var characterTable = new Table();
            characterTable.Border = TableBorder.Minimal;
            characterTable.AddColumn("Name");
            characterTable.AddColumn("Health");
            characterTable.AddColumn("Experience");

            foreach (var player in room.Players.OrderBy(p => p.Name))
            {
                characterTable.AddRow(
                    $"[cyan]{player.Name}[/]",
                    player.Health.ToString(),
                    player.Experience.ToString()
                );
            }

            // Render table to string using AnsiConsole
            var tableString = new System.IO.StringWriter();
            using (var consoleOut = new System.IO.StringWriter())
            {
                // We'll use markup directly instead of trying to render table to string
                foreach (var player in room.Players.OrderBy(p => p.Name))
                {
                    sb.AppendLine(
                        $"  • [cyan]{player.Name}[/] - " +
                        $"HP: {player.Health} | " +
                        $"XP: {player.Experience}"
                    );
                }
            }
        }
        else
        {
            sb.AppendLine("[dim]No characters in this room[/]");
        }

        return sb.ToString();
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
        try
        {
            logger.LogInformation("User selected Find Equipment Location");
            AnsiConsole.MarkupLine("[yellow]=== Find Equipment Location ===[/]");
            AnsiConsole.WriteLine();

            // Display all available items first
            var allItems = context.Items.OrderBy(i => i.Name).ToList();
            
            if (!allItems.Any())
            {
                AnsiConsole.MarkupLine("[red]No items found in the database.[/]");
                logger.LogWarning("No items found in database");
                PressAnyKey();
                return;
            }

            AnsiConsole.MarkupLine("[cyan]Available Items in Database:[/]");
            var itemsTable = new Table();
            itemsTable.AddColumn("Name");
            itemsTable.AddColumn("Type");
            itemsTable.AddColumn("Attack");
            itemsTable.AddColumn("Defense");
            itemsTable.AddColumn("Value");

            foreach (var i in allItems)
            {
                itemsTable.AddRow(
                    $"[green]{i.Name}[/]",
                    i.Type,
                    i.Attack.ToString(),
                    i.Defense.ToString(),
                    $"{i.Value}g"
                );
            }

            AnsiConsole.Write(itemsTable);
            AnsiConsole.WriteLine();

            // Get item name to search for
            var itemName = AnsiConsole.Ask<string>("Enter [green]item name[/] to search for:");

            // Search for the item in the Items table (case-insensitive using ToLower which translates to SQL)
            var itemNameLower = itemName.ToLower();
            var item = context.Items
                .FirstOrDefault(i => i.Name.ToLower() == itemNameLower);

            if (item == null)
            {
                AnsiConsole.MarkupLine($"[red]Item '{itemName}' not found in the database.[/]");
                logger.LogWarning("Item search failed - '{ItemName}' not found", itemName);
                PressAnyKey();
                return;
            }

            // Display item details
            var itemPanel = new Panel(
                $"[cyan]Name:[/] {item.Name}\n" +
                $"[yellow]Type:[/] {item.Type}\n" +
                $"[green]Attack:[/] {item.Attack}\n" +
                $"[blue]Defense:[/] {item.Defense}\n" +
                $"[magenta]Value:[/] {item.Value} gold\n" +
                $"[dim]Weight:[/] {item.Weight} lbs"
            )
            {
                Header = new PanelHeader("[bold cyan]Item Details[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(itemPanel);
            AnsiConsole.WriteLine();

            // Find which equipment has this item (either as weapon or armor)
            var equipmentWithItem = context.Equipments
                .Where(e => e.WeaponId == item.Id || e.ArmorId == item.Id)
                .ToList();

            if (!equipmentWithItem.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]Item '{item.Name}' exists but is not currently equipped by anyone.[/]");
                logger.LogInformation("Item '{ItemName}' found but not equipped", item.Name);
                PressAnyKey();
                return;
            }

            // Find players with this equipment
            var playersWithItem = context.Players
                .Include(p => p.Equipment)
                    .ThenInclude(e => e.Weapon)
                .Include(p => p.Equipment)
                    .ThenInclude(e => e.Armor)
                .Include(p => p.Room)
                .Where(p => p.Equipment != null &&
                           (p.Equipment.WeaponId == item.Id || p.Equipment.ArmorId == item.Id))
                .ToList();

            if (!playersWithItem.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]Item '{item.Name}' is in an equipment set but not held by any character.[/]");
                logger.LogInformation("Item '{ItemName}' in equipment but no character found", item.Name);
                PressAnyKey();
                return;
            }

            // Display results
            AnsiConsole.MarkupLine($"[green]Found {playersWithItem.Count} character(s) with '{item.Name}':[/]");
            AnsiConsole.WriteLine();

            foreach (var player in playersWithItem)
            {
                var equipmentSlot = player.Equipment.WeaponId == item.Id ? "Weapon" : "Armor";
                var locationName = player.Room?.Name ?? "Unknown Location";
                var coordinates = player.Room != null ? $"({player.Room.X}, {player.Room.Y})" : "N/A";

                var resultPanel = new Panel(
                    $"[cyan]Character:[/] [bold]{player.Name}[/]\n" +
                    $"[yellow]Equipment Slot:[/] {equipmentSlot}\n" +
                    $"[green]Location:[/] {locationName}\n" +
                    $"[dim]Coordinates:[/] {coordinates}\n" +
                    $"[blue]Health:[/] {player.Health}\n" +
                    $"[magenta]Experience:[/] {player.Experience}"
                )
                {
                    Header = new PanelHeader($"[bold green]{player.Name}[/]"),
                    Border = BoxBorder.Rounded
                };

                AnsiConsole.Write(resultPanel);
                AnsiConsole.WriteLine();

                logger.LogInformation(
                    "Found item '{ItemName}' equipped by {PlayerName} as {Slot} in {Location}",
                    item.Name, player.Name, equipmentSlot, locationName);
            }

            PressAnyKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error finding equipment location");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            PressAnyKey();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Allows player to select starting weapon and armor during character creation
    /// Prevents duplicate equipment assignments to the same character
    /// </summary>
    private void EquipStartingGear(Player player)
    {
        try
        {
            AnsiConsole.MarkupLine("[cyan]=== Select Starting Equipment ===[/]");
            
            // Create Equipment object if not exists
            var equipment = new Equipment();
            
            // Select Weapon
            var weapons = context.Items
                .Where(i => i.Type == "Weapon")
                .OrderBy(i => i.Name)
                .ToList();

            if (weapons.Any())
            {
                AnsiConsole.WriteLine();
                var weaponTable = new Table();
                weaponTable.AddColumn("Name");
                weaponTable.AddColumn("Attack");
                weaponTable.AddColumn("Value");

                foreach (var w in weapons)
                {
                    weaponTable.AddRow(w.Name.EscapeMarkup(), w.Attack.ToString(), $"{w.Value}g");
                }

                AnsiConsole.Write(weaponTable);
                AnsiConsole.WriteLine();

                // Create a selection list with Item objects instead of strings
                var weaponWithSkip = new List<Item> { new Item { Id = -1, Name = "[Skip - No Weapon]" } };
                weaponWithSkip.AddRange(weapons);

                var selectedWeaponItem = AnsiConsole.Prompt(
                    new SelectionPrompt<Item>()
                        .Title("[yellow]Select starting weapon:[/]")
                        .PageSize(10)
                        .AddChoices(weaponWithSkip)
                        .UseConverter(item => item.Name.EscapeMarkup())
                );

                if (selectedWeaponItem.Id != -1)
                {
                    equipment.WeaponId = selectedWeaponItem.Id;
                    equipment.Weapon = selectedWeaponItem;
                    AnsiConsole.MarkupLine($"[green]✓ Equipped: {selectedWeaponItem.Name.EscapeMarkup()}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No weapons available in database.[/]");
            }

            // Select Armor
            var armors = context.Items
                .Where(i => i.Type == "Armor")
                .OrderBy(i => i.Name)
                .ToList();

            if (armors.Any())
            {
                AnsiConsole.WriteLine();
                var armorTable = new Table();
                armorTable.AddColumn("Name");
                armorTable.AddColumn("Defense");
                armorTable.AddColumn("Value");

                foreach (var a in armors)
                {
                    armorTable.AddRow(a.Name.EscapeMarkup(), a.Defense.ToString(), $"{a.Value}g");
                }

                AnsiConsole.Write(armorTable);
                AnsiConsole.WriteLine();

                // Create a selection list with Item objects instead of strings
                var armorWithSkip = new List<Item> { new Item { Id = -1, Name = "[Skip - No Armor]" } };
                armorWithSkip.AddRange(armors);

                var selectedArmorItem = AnsiConsole.Prompt(
                    new SelectionPrompt<Item>()
                        .Title("[yellow]Select starting armor:[/]")
                        .PageSize(10)
                        .AddChoices(armorWithSkip)
                        .UseConverter(item => item.Name.EscapeMarkup())
                );

                if (selectedArmorItem.Id != -1)
                {
                    equipment.ArmorId = selectedArmorItem.Id;
                    equipment.Armor = selectedArmorItem;
                    AnsiConsole.MarkupLine($"[green]✓ Equipped: {selectedArmorItem.Name.EscapeMarkup()}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No armor available in database.[/]");
            }

            // Only create equipment if at least one item was selected
            if (equipment.WeaponId != null || equipment.ArmorId != null)
            {
                player.Equipment = equipment;
                logger.LogInformation("Equipment assigned to player {PlayerName}: Weapon={WeaponId}, Armor={ArmorId}", 
                    player.Name, equipment.WeaponId, equipment.ArmorId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error equipping starting gear");
            AnsiConsole.MarkupLine($"[red]Error selecting equipment: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Character will be created without equipment.[/]");
        }
    }

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