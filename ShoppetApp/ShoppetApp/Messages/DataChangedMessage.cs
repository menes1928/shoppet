namespace ShoppetApp.Messages;

public sealed class DataChangedMessage
{
    public static DataChangedMessage Instance { get; } = new();
}
