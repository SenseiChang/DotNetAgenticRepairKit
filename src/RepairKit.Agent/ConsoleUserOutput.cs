namespace RepairKit.Agent;

public sealed class ConsoleUserOutput : IUserOutput
{
    public void WriteLine()
    {
        Console.WriteLine();
    }

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}

