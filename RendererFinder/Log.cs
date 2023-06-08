using System;

namespace RendererFinder;

internal static class Log
{
    internal static void Debug(object data)
    {
        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine(data);

        Console.ResetColor();
    }

    internal static void Error(object data)
    {
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(data);

        Console.ResetColor();
    }

    internal static void Fatal(object data)
    {
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(data);

        Console.ResetColor();
    }

    internal static void Info(object data)
    {
        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine(data);

        Console.ResetColor();
    }

    internal static void Message(object data)
    {
        Console.ForegroundColor = ConsoleColor.White;

        Console.WriteLine(data);

        Console.ResetColor();
    }

    internal static void Warning(object data)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine(data);

        Console.ResetColor();
    }
}