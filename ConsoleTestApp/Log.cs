internal static class Log
{
    internal static void Debug(object data)
    {
        WriteLine(data, ConsoleColor.White);
    }

    internal static void Error(object data)
    {
        WriteLine(data, ConsoleColor.Red);
    }

    internal static void Fatal(object data)
    {
        WriteLine(data, ConsoleColor.Red);
    }

    internal static void Info(object data)
    {
        WriteLine(data, ConsoleColor.White);
    }

    internal static void Message(object data)
    {
        WriteLine(data, ConsoleColor.White);
    }

    internal static void Warning(object data)
    {
        WriteLine(data, ConsoleColor.Yellow);
    }

    private static void WriteLine(object data, ConsoleColor color)
    {
        Console.ForegroundColor = color;

        Console.WriteLine(data);

        Console.ResetColor();
    }
}