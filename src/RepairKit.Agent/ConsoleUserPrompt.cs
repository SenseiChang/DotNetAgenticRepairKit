namespace RepairKit.Agent;

public sealed class ConsoleUserPrompt : IUserPrompt
{
    public string? ReadLine()
    {
        return Console.ReadLine();
    }
}

