namespace ConsoleRpg.Helpers;

public class MenuManager
{
    private readonly OutputManager _outputManager;

    public MenuManager(OutputManager outputManager)
    {
        _outputManager = outputManager;
    }

    public void ShowMainMenu(Action<string> handleChoice)
    {
        _outputManager.Clear();
        _outputManager.WriteLine("=================================", ConsoleColor.Yellow);
        _outputManager.WriteLine("      ADMIN / DEVELOPER MENU", ConsoleColor.Yellow);
        _outputManager.WriteLine("=================================", ConsoleColor.Yellow);
        _outputManager.WriteLine("");
        _outputManager.WriteLine("E. Return to Exploration Mode", ConsoleColor.Yellow);
        _outputManager.WriteLine("");
        _outputManager.WriteLine("BASIC FEATURES:", ConsoleColor.Green);
        _outputManager.WriteLine("1. Add New Character", ConsoleColor.Cyan);
        _outputManager.WriteLine("2. Edit Character", ConsoleColor.Cyan);
        _outputManager.WriteLine("3. Display All Characters", ConsoleColor.Cyan);
        _outputManager.WriteLine("4. Search Character by Name", ConsoleColor.Cyan);
        _outputManager.WriteLine("");
        _outputManager.WriteLine("'C' LEVEL FEATURES:", ConsoleColor.Green);
        _outputManager.WriteLine("5. Add Ability to Character", ConsoleColor.Cyan);
        _outputManager.WriteLine("6. Display Character Abilities", ConsoleColor.Cyan);
        _outputManager.WriteLine("7. Attack with Ability (use Exploration Mode)", ConsoleColor.DarkGray);
        _outputManager.WriteLine("");
        _outputManager.WriteLine("'B' LEVEL FEATURES:", ConsoleColor.Green);
        _outputManager.WriteLine("8. Add New Room", ConsoleColor.Cyan);
        _outputManager.WriteLine("9. Display Room Details", ConsoleColor.Cyan);
        _outputManager.WriteLine("10. Manage Room Connections", ConsoleColor.Cyan);
        _outputManager.WriteLine("11. Navigate Rooms (use Exploration Mode)", ConsoleColor.DarkGray);
        _outputManager.WriteLine("");
        _outputManager.WriteLine("'A' LEVEL FEATURES:", ConsoleColor.Green);
        _outputManager.WriteLine("12. List Characters in Room by Attribute", ConsoleColor.Cyan);
        _outputManager.WriteLine("13. List All Rooms with Characters", ConsoleColor.Cyan);
        _outputManager.WriteLine("14. Find Equipment Location", ConsoleColor.Cyan);
        _outputManager.WriteLine("");
        _outputManager.WriteLine("Select an option:", ConsoleColor.White);
        _outputManager.Display();

        var input = Console.ReadLine();
        handleChoice(input);
    }
}
