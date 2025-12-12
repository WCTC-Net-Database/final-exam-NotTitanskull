using ConsoleRpg.Helpers;
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
/// Main game engine - orchestrates game flow
/// Uses interfaces for dependencies - follows Dependency Inversion Principle
/// </summary>
public class GameEngine(
    GameContext context,
    MenuManager menuManager,
    MapManager mapManager,
    ExplorationUI explorationUi,
    IPlayerService playerService,
    AdminService adminService,
    ILogger<GameEngine> logger,
    Player currentPlayer,
    Room currentRoom)
{
    private readonly MapManager _mapManager = mapManager;

    // Now uses interface!

    private Player? _currentPlayer = currentPlayer;
    private Room _currentRoom = currentRoom;
    private GameMode _currentMode = GameMode.Exploration;

    public void Run()
    {
        logger.LogInformation("Game engine started");

        // Initialize game - get or create first player
        InitializeGame();

        // Main game loop
        while (true)
        {
            if (_currentMode == GameMode.Exploration)
            {
                ExplorationMode();
            }
            else
            {
                AdminMode();
            }
        }
    }

    /// <summary>
    /// Initialize the game by getting the first player or creating one
    /// </summary>
    private void InitializeGame()
    {
        // Try to get the first player
        _currentPlayer = context.Players
            .Include(p => p.Room)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.Weapon)
            .Include(p => p.Equipment)
                .ThenInclude(e => e.Armor)
            .Include(p => p.Abilities)
            .FirstOrDefault();

        if (_currentPlayer == null)
        {
            AnsiConsole.MarkupLine("[yellow]No players found! Please create a character first.[/]");
            _currentMode = GameMode.Admin;
            return;
        }

        // Get current room or default to first room
        _currentRoom = _currentPlayer.Room ?? context.Rooms.Include(r => r.Players).Include(r => r.Monsters).FirstOrDefault();

        if (_currentRoom == null)
        {
            AnsiConsole.MarkupLine("[red]No rooms found! Database may not be properly seeded.[/]");
            _currentMode = GameMode.Admin;
            return;
        }

        logger.LogInformation("Game initialized with player {PlayerName} in room {RoomName}",
            _currentPlayer.Name, _currentRoom.Name);
    }

    #region Exploration Mode

    /// <summary>
    /// Main exploration mode - this is where the player navigates the world
    /// </summary>
    private void ExplorationMode()
    {
        // Reload room with all related data
        _currentRoom = context.Rooms
            .Include(r => r.Players)
            .Include(r => r.Monsters)
            .Include(r => r.NorthRoom)
            .Include(r => r.SouthRoom)
            .Include(r => r.EastRoom)
            .Include(r => r.WestRoom)
            .FirstOrDefault(r => r.Id == _currentRoom.Id);

        // Get all rooms for map
        var allRooms = context.Rooms.ToList();
        bool hasMonsters = _currentRoom.Monsters != null && _currentRoom.Monsters.Any();

        // Render UI and get player's action choice
        var selectedAction = explorationUi.RenderAndGetAction(allRooms, _currentRoom);

        // Handle the selected action
        HandleExplorationAction(selectedAction, hasMonsters);
    }

    /// <summary>
    /// Handles player action selection during exploration mode
    /// Processes service results and displays them through ExplorationUI
    /// </summary>
    private void HandleExplorationAction(string action, bool hasMonsters)
    {
        switch (action)
        {
            case "Go North":
                HandleMoveResult(playerService.MoveToRoom(_currentPlayer, _currentRoom, _currentRoom.NorthRoomId, "North"));
                break;
            case "Go South":
                HandleMoveResult(playerService.MoveToRoom(_currentPlayer, _currentRoom, _currentRoom.SouthRoomId, "South"));
                break;
            case "Go East":
                HandleMoveResult(playerService.MoveToRoom(_currentPlayer, _currentRoom, _currentRoom.EastRoomId, "East"));
                break;
            case "Go West":
                HandleMoveResult(playerService.MoveToRoom(_currentPlayer, _currentRoom, _currentRoom.WestRoomId, "West"));
                break;
            case "View Map":
                explorationUi.AddMessage("[cyan]Viewing map[/]");
                explorationUi.AddOutput("[cyan]The map is displayed above showing your current location and surroundings.[/]");
                break;
            case "View Inventory":
                HandleActionResult(playerService.ShowInventory(_currentPlayer));
                break;
            case "View Character Stats":
                HandleActionResult(playerService.ShowCharacterStats(_currentPlayer));
                break;
            case "Attack Monster":
                HandleActionResult(playerService.AttackMonster(_currentPlayer, _currentRoom));
                break;
            case "Use Ability": 
                HandleActionResult(playerService.UseAbilityOnMonster(_currentPlayer, _currentRoom));
                break;
            case "Return to Main Menu":
                _currentMode = GameMode.Admin;
                explorationUi.AddMessage("[yellow]→ Admin Mode[/]");
                explorationUi.AddOutput("[yellow]Switching to Admin Mode for database management and testing.[/]");
                break;
            default:
                explorationUi.AddMessage($"[red]Unknown action[/]");
                explorationUi.AddOutput($"[red]Unknown action: {action}[/]");
                break;
        }
    }

    /// <summary>
    /// Handles the result of a move operation
    /// </summary>
    private void HandleMoveResult(ServiceResult<Room> result)
    {
        explorationUi.AddMessage(result.Message);
        explorationUi.AddOutput(result.DetailedOutput);

        if (result.Success && result.Value != null)
        {
            _currentRoom = result.Value;
        }
    }

    /// <summary>
    /// Handles the result of a general player action
    /// </summary>
    private void HandleActionResult(ServiceResult result)
    {
        explorationUi.AddMessage(result.Message);
        explorationUi.AddOutput(result.DetailedOutput);
    }

    #endregion

    #region Admin Mode

    /// <summary>
    /// Admin mode - provides access to CRUD operations and template methods
    /// </summary>
    private void AdminMode()
    {
        menuManager.ShowMainMenu(AdminMenuChoice);
    }

    private void AdminMenuChoice(string choice)
    {
        switch (choice?.ToUpper())
        {
            // World Exploration / Return to Exploration Mode
            case "E":
            case "0":
                ExploreWorld();
                break;

            // Basic Features
            case "1":
                adminService.AddCharacter();
                break;
            case "2":
                adminService.EditCharacter();
                break;
            case "3":
                adminService.DisplayAllCharacters();
                break;
            case "4":
                adminService.SearchCharacterByName();
                break;

            // C-Level Features
            case "5":
                adminService.AddAbilityToCharacter();
                break;
            case "6":
                adminService.DisplayCharacterAbilities();
                break;
            case "7":
                // Attack with ability - redirect to exploration mode
                AnsiConsole.MarkupLine("[yellow]Please use this feature in Exploration Mode[/]");
                PressAnyKey();
                break;

            // B-Level Features
            case "8":
                adminService.AddRoom();
                break;
            case "9":
                adminService.DisplayRoomDetails();
                break;
            case "10":
                adminService.ManageRoomConnections();
                break;
            case "11":
                // Navigate rooms - redirect to exploration mode
                AnsiConsole.MarkupLine("[yellow]Please use this feature in Exploration Mode[/]");
                PressAnyKey();
                break;

            // A-Level Features
            case "12":
                adminService.ListCharactersInRoomByAttribute();
                break;
            case "13":
                adminService.ListAllRoomsWithCharacters();
                break;
            case "14":
                adminService.FindEquipmentLocation();
                break;

            default:
                AnsiConsole.MarkupLine("[red]Invalid selection.[/]");
                PressAnyKey();
                break;
        }
    }

    #endregion

    #region Mode Switching

    /// <summary>
    /// Switch from Admin Mode to Exploration Mode
    /// </summary>
    private void ExploreWorld()
    {
        logger.LogInformation("User selected Explore World - switching to Exploration Mode");

        // Simply switch to exploration mode
        _currentMode = GameMode.Exploration;
        explorationUi.AddMessage("[green]Entered world[/]");
        explorationUi.AddOutput("[green]Welcome to the world! Use the menu below to explore, fight monsters, and manage your character.[/]");
    }

    #endregion

    #region Helper Methods

    private void PressAnyKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[dim]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    #endregion
}

/// <summary>
/// Represents the current game mode
/// </summary>
public enum GameMode
{
    Exploration,
    Admin
}
