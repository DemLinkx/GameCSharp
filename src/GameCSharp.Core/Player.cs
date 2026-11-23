namespace GameCSharp.Core;

public sealed class Player(string name, bool isWhite)
{
    public string GetName() => name;

    public bool IsWhite() => isWhite;
}
