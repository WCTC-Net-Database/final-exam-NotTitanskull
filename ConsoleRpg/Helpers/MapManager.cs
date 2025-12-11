using ConsoleRpgEntities.Models.Rooms;
using Spectre.Console;
using System.Text;

namespace ConsoleRpg.Helpers
{
    public class MapManager
    {
        private readonly OutputManager _outputManager;

        public MapManager(OutputManager outputManager)
        {
            _outputManager = outputManager;
        }

        /// <summary>
        /// Displays a compact visual map of the world using Spectre.Console Layout
        /// </summary>
        public Panel GetCompactMapPanel(IEnumerable<Room> rooms, Room currentRoom)
        {
            if (!rooms.Any())
            {
                return new Panel("[red]No rooms available.[/]")
                {
                    Header = new PanelHeader("[yellow]World Map[/]"),
                    Border = BoxBorder.Rounded
                };
            }

            var mapContent = BuildMapContent(rooms, currentRoom);
            var panel = new Panel(mapContent)
            {
                Header = new PanelHeader("[yellow]World Map[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0, 1, 0)
            };

            return panel;
        }

        /// <summary>
        /// Displays a visual map of the world using Spectre.Console
        /// </summary>
        public void DisplayMap(IEnumerable<Room> rooms, Room currentRoom)
        {
            if (!rooms.Any())
            {
                AnsiConsole.MarkupLine("[red]No rooms available to display.[/]");
                return;
            }

            // Calculate map boundaries
            int minX = rooms.Min(r => r.X);
            int maxX = rooms.Max(r => r.X);
            int minY = rooms.Min(r => r.Y);
            int maxY = rooms.Max(r => r.Y);

            // Create a grid for the map
            var grid = new Grid();
            grid.AddColumn();

            // Add title
            var title = new Rule("[yellow]World Map[/]");
            title.Centered();
            AnsiConsole.Write(title);
            AnsiConsole.WriteLine();

            // Create the map from top to bottom (Y descends from max to min)
            var mapLines = new List<string>();

            for (int y = maxY; y >= minY; y--)
            {
                var line = new System.Text.StringBuilder();

                for (int x = minX; x <= maxX; x++)
                {
                    var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);

                    if (room != null)
                    {
                        // Current room is highlighted
                        if (room.Id == currentRoom?.Id)
                        {
                            line.Append("[green on white][[@]][/]");
                        }
                        // Room with monsters
                        else if (room.Monsters?.Any() == true)
                        {
                            line.Append("[red][[M]][/]");
                        }
                        // Room with players
                        else if (room.Players?.Any() == true)
                        {
                            line.Append("[cyan][[P]][/]");
                        }
                        // Empty room
                        else
                        {
                            line.Append("[blue][[■]][/]");
                        }

                        // Add connections
                        if (room.EastRoom != null && x < maxX)
                        {
                            line.Append("[dim]═[/]");
                        }
                        else if (x < maxX)
                        {
                            line.Append(" ");
                        }
                    }
                    else
                    {
                        line.Append("   ");
                        if (x < maxX)
                        {
                            line.Append(" ");
                        }
                    }
                }

                mapLines.Add(line.ToString());

                // Add vertical connections if not the last row
                if (y > minY)
                {
                    var verticalLine = new System.Text.StringBuilder();
                    for (int x = minX; x <= maxX; x++)
                    {
                        var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);

                        if (room != null && room.SouthRoom != null)
                        {
                            verticalLine.Append("[dim] ║ [/]");
                        }
                        else
                        {
                            verticalLine.Append("   ");
                        }

                        if (x < maxX)
                        {
                            verticalLine.Append(" ");
                        }
                    }
                    mapLines.Add(verticalLine.ToString());
                }
            }

            // Display the map
            foreach (var line in mapLines)
            {
                AnsiConsole.MarkupLine(line);
            }

            AnsiConsole.WriteLine();

            // Legend
            var legend = new Panel(
                "[green on white][[@]][/] = Current Location\n" +
                "[blue][[■]][/] = Empty Room\n" +
                "[red][[M]][/] = Monster Present\n" +
                "[cyan][[P]][/] = Player Present"
            );
            legend.Header = new PanelHeader("[yellow]Legend[/]");
            legend.Border = BoxBorder.Rounded;
            AnsiConsole.Write(legend);
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Builds the map content as a markup string
        /// </summary>
        private Markup BuildMapContent(IEnumerable<Room> rooms, Room currentRoom)
        {
            var sb = new StringBuilder();

            // Calculate map boundaries
            int minX = rooms.Min(r => r.X);
            int maxX = rooms.Max(r => r.X);
            int minY = rooms.Min(r => r.Y);
            int maxY = rooms.Max(r => r.Y);

            // Create the map from top to bottom
            for (int y = maxY; y >= minY; y--)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);

                    if (room != null)
                    {
                        // Current room is highlighted
                        if (room.Id == currentRoom?.Id)
                        {
                            sb.Append("[green on white][[@]][/]");
                        }
                        // Room with monsters
                        else if (room.Monsters?.Any() == true)
                        {
                            sb.Append("[red][[M]][/]");
                        }
                        // Room with players
                        else if (room.Players?.Any() == true)
                        {
                            sb.Append("[cyan][[P]][/]");
                        }
                        // Empty room
                        else
                        {
                            sb.Append("[blue][[■]][/]");
                        }

                        // Add connections
                        if (room.EastRoom != null && x < maxX)
                        {
                            sb.Append("[dim]═[/]");
                        }
                        else if (x < maxX)
                        {
                            sb.Append(" ");
                        }
                    }
                    else
                    {
                        sb.Append("   ");
                        if (x < maxX)
                        {
                            sb.Append(" ");
                        }
                    }
                }

                sb.AppendLine();

                // Add vertical connections if not the last row
                if (y > minY)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        var room = rooms.FirstOrDefault(r => r.X == x && r.Y == y);

                        if (room != null && room.SouthRoom != null)
                        {
                            sb.Append("[dim] ║ [/]");
                        }
                        else
                        {
                            sb.Append("   ");
                        }

                        if (x < maxX)
                        {
                            sb.Append(" ");
                        }
                    }
                    sb.AppendLine();
                }
            }

            // Add compact legend
            sb.Append("[dim][[@]]=You [[M]]=Monster [[P]]=Player [[■]]=Empty[/]");

            return new Markup(sb.ToString());
        }

        /// <summary>
        /// Gets a compact room details panel
        /// </summary>
        public Panel GetCompactRoomDetailsPanel(Room room)
        {
            if (room == null)
            {
                return new Panel("[red]No room information.[/]")
                {
                    Header = new PanelHeader("[yellow]Current Room[/]"),
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(1, 0, 1, 0)
                };
            }

            var sb = new StringBuilder();

            // Room description
            sb.AppendLine($"[bold]{room.Description}[/]");

            // Exits - check ID properties which are always loaded
            var exits = new List<string>();
            if (room.NorthRoomId.HasValue) exits.Add("[cyan]N[/]orth");
            if (room.SouthRoomId.HasValue) exits.Add("[cyan]S[/]outh");
            if (room.EastRoomId.HasValue) exits.Add("[cyan]E[/]ast");
            if (room.WestRoomId.HasValue) exits.Add("[cyan]W[/]est");

            if (exits.Any())
            {
                sb.AppendLine($"[green]Exits:[/] {string.Join(", ", exits)}");
            }
            else
            {
                sb.AppendLine("[dim]No exits.[/]");
            }

            // Monsters
            if (room.Monsters?.Any() == true)
            {
                sb.AppendLine($"[red]Monsters:[/] {string.Join(", ", room.Monsters.Select(m => $"{m.Name} (HP:{m.Health})"))}");
            }

            // Players
            if (room.Players?.Any() == true)
            {
                sb.AppendLine($"[cyan]Players:[/] {string.Join(", ", room.Players.Select(p => p.Name))}");
            }

            return new Panel(sb.ToString().TrimEnd())
            {
                Header = new PanelHeader($"[yellow]{room.Name}[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0, 1, 0)
            };
        }

        /// <summary>
        /// Gets a compact actions panel
        /// </summary>
        public Panel GetCompactActionsPanel(Room currentRoom, bool hasMonsters)
        {
            var actions = BuildActionsList(currentRoom, hasMonsters);
            return new Panel(actions)
            {
                Header = new PanelHeader("[green]Available Actions[/]"),
                Border = BoxBorder.Rounded
            };
        }

        /// <summary>
        /// Displays detailed information about the current room
        /// </summary>
        public void DisplayRoomDetails(Room room)
        {
            if (room == null)
            {
                AnsiConsole.MarkupLine("[red]No room information available.[/]");
                return;
            }

            // Room name and description
            var panel = new Panel(new Markup($"[bold]{room.Description}[/]"));
            panel.Header = new PanelHeader($"[yellow]{room.Name}[/]");
            panel.Border = BoxBorder.Double;
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Display available exits
            var exits = new List<string>();
            if (room.NorthRoom != null) exits.Add($"[cyan]North[/] → {room.NorthRoom.Name}");
            if (room.SouthRoom != null) exits.Add($"[cyan]South[/] → {room.SouthRoom.Name}");
            if (room.EastRoom != null) exits.Add($"[cyan]East[/] → {room.EastRoom.Name}");
            if (room.WestRoom != null) exits.Add($"[cyan]West[/] → {room.WestRoom.Name}");

            if (exits.Any())
            {
                var exitsPanel = new Panel(string.Join("\n", exits));
                exitsPanel.Header = new PanelHeader("[green]Available Exits[/]");
                exitsPanel.Border = BoxBorder.Rounded;
                AnsiConsole.Write(exitsPanel);
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No exits available from this room.[/]");
                AnsiConsole.WriteLine();
            }

            // Display monsters in the room
            if (room.Monsters?.Any() == true)
            {
                var monstersTable = new Table();
                monstersTable.Border = TableBorder.Rounded;
                monstersTable.Title = new TableTitle("[red]Monsters in this room[/]");
                monstersTable.AddColumn("[yellow]Name[/]");
                monstersTable.AddColumn("[yellow]Type[/]");
                monstersTable.AddColumn("[yellow]Health[/]");
                monstersTable.AddColumn("[yellow]Aggression[/]");

                foreach (var monster in room.Monsters)
                {
                    monstersTable.AddRow(
                        monster.Name,
                        monster.MonsterType,
                        monster.Health.ToString(),
                        monster.AggressionLevel.ToString()
                    );
                }

                AnsiConsole.Write(monstersTable);
                AnsiConsole.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine("[green]No monsters in this room.[/]");
                AnsiConsole.WriteLine();
            }

            // Display other players in the room
            if (room.Players?.Any(p => p != null) == true)
            {
                var playersTable = new Table();
                playersTable.Border = TableBorder.Rounded;
                playersTable.Title = new TableTitle("[cyan]Other Players in this room[/]");
                playersTable.AddColumn("[yellow]Name[/]");
                playersTable.AddColumn("[yellow]Health[/]");
                playersTable.AddColumn("[yellow]Experience[/]");

                foreach (var player in room.Players)
                {
                    playersTable.AddRow(
                        player.Name,
                        player.Health.ToString(),
                        player.Experience.ToString()
                    );
                }

                AnsiConsole.Write(playersTable);
                AnsiConsole.WriteLine();
            }
        }

        /// <summary>
        /// Gets a panel showing available actions for selection
        /// </summary>
        public Panel GetAvailableActionsPanel(Room currentRoom)
        {
            var actions = GetAvailableActions(currentRoom);

            var sb = new StringBuilder();
            sb.AppendLine("[white]Available Actions:[/]");
            sb.AppendLine();

            foreach (var action in actions)
            {
                sb.AppendLine($"  [cyan]•[/] {action}");
            }

            return new Panel(sb.ToString().TrimEnd())
            {
                Header = new PanelHeader("[green]Actions[/]"),
                Border = BoxBorder.Rounded
            };
        }

        /// <summary>
        /// Gets available actions based on the current room context
        /// </summary>
        public List<string> GetAvailableActions(Room currentRoom)
        {
            var actions = new List<string>();

            // Navigation actions - check ID properties instead of navigation properties
            if (currentRoom?.NorthRoomId.HasValue == true)
                actions.Add("Go North");
            if (currentRoom?.SouthRoomId.HasValue == true)
                actions.Add("Go South");
            if (currentRoom?.EastRoomId.HasValue == true)
                actions.Add("Go East");
            if (currentRoom?.WestRoomId.HasValue == true)
                actions.Add("Go West");

            // Combat actions (only if monsters present)
            if (currentRoom?.Monsters?.Any() == true)
            {
                actions.Add("Attack Monster");
                actions.Add("Use Ability");
            }

            // General actions
            actions.Add("View Character Stats");
            actions.Add("View Inventory");
            actions.Add("View Map");
            actions.Add("Return to Main Menu");

            return actions;
        }

        /// <summary>
        /// Displays available actions based on the current room context
        /// </summary>
        public void DisplayAvailableActions(Room currentRoom, bool hasMonsters)
        {
            var actionsPanel = new Panel(BuildActionsList(currentRoom, hasMonsters));
            actionsPanel.Header = new PanelHeader("[green]Available Actions[/]");
            actionsPanel.Border = BoxBorder.Rounded;
            AnsiConsole.Write(actionsPanel);
            AnsiConsole.WriteLine();
        }

        private string BuildActionsList(Room currentRoom, bool hasMonsters)
        {
            var actions = new List<string>();

            // Navigation - check ID properties
            actions.Add("[cyan]Navigation:[/]");
            if (currentRoom?.NorthRoomId.HasValue == true)
                actions.Add("  [white]N[/] - Go North");
            if (currentRoom?.SouthRoomId.HasValue == true)
                actions.Add("  [white]S[/] - Go South");
            if (currentRoom?.EastRoomId.HasValue == true)
                actions.Add("  [white]E[/] - Go East");
            if (currentRoom?.WestRoomId.HasValue == true)
                actions.Add("  [white]W[/] - Go West");

            // Combat (if monsters present)
            if (hasMonsters)
            {
                actions.Add("");
                actions.Add("[red]Combat:[/]");
                actions.Add("  [white]A[/] - Attack Monster");
                actions.Add("  [white]B[/] - Use Ability");
            }

            // General actions
            actions.Add("");
            actions.Add("[yellow]Other:[/]");
            actions.Add("  [white]M[/] - View Map");
            actions.Add("  [white]I[/] - View Inventory");
            actions.Add("  [white]X[/] - Admin Mode");
            actions.Add("  [white]Q[/] - Quit Game");

            return string.Join("\n", actions);
        }
    }
}
