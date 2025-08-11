using System;
using System.Linq;
using System.Collections.Generic;

namespace CSharpPatchDroid;

public class Print
{
    public static void sucess(string value)
    {

        Console.ForegroundColor = ConsoleColor.Green;
        print(value);
        Console.ForegroundColor = ConsoleColor.White;


    }
    public static void error(string value)
    {

        Console.ForegroundColor = ConsoleColor.Red;
        print(value);
        Console.ForegroundColor = ConsoleColor.White;


    }
    public static void warning(string value)
    {

        Console.ForegroundColor = ConsoleColor.Yellow;
        print(value);
        Console.ForegroundColor = ConsoleColor.White;


    }
    public static void print(string value)
    {
        Console.WriteLine(value);
    }

}
