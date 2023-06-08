using System;

namespace Logger;

public static class Log
{
    public static void Debug(object data)
    {
        WriteLine(data, ConsoleColor.White);
    }

    public static void Error(object data)
    {
        WriteLine(data, ConsoleColor.Red);
    }

    public static void Fatal(object data)
    {
        WriteLine(data, ConsoleColor.Red);
    }

    public static void Info(object data)
    {
        WriteLine(data, ConsoleColor.White);
    }

    public static void Message(object data)
    {
        WriteLine(data, ConsoleColor.White);
    }

    public static void Warning(object data)
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
