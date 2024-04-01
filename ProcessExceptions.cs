using System;

namespace HellPie_Tools.Utility;

public sealed class GameProcessException : Exception
{
    public GameProcessException() : base("Process has exited, been lost, or privileges have changed.")
    {
    }

    public GameProcessException(string source) : base("Process has exited, been lost, or privileges have changed.")
    {
        Source = source;
    }

    public GameProcessException(Exception innerException) : base("Process has exited, been lost, or privileges have changed.", innerException)
    {
    }

    public GameProcessException(string source, Exception innerException) : base("Process has exited, been lost, or privileges have changed.", innerException)
    {
        Source = source;
    }
}