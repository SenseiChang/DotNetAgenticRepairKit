namespace RepairKit.Agent;

public sealed class NullUserOutput : IUserOutput
{
    public void WriteLine()
    {
    }

    public void WriteLine(string message)
    {
    }
}

