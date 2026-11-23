using System.Drawing;

namespace GameCSharp.Core;

public sealed class Board
{
    public const int Size = 8;
    private readonly Piece?[,] pieces = new Piece?[Size, Size];
    private readonly HashSet<Point> captured = [];

    public void Initialize()
    {
        Array.Clear(pieces);
        captured.Clear();

        for (int y = 0; y < Size; y++)
        for (int x = (y + 1) % 2; x < Size; x += 2)
        {
            if (y < 3 || y >= Size - 3)
                pieces[x, y] = new Checker(new Point(x, y), isWhite: y >= Size - 3);
        }
    }

    public bool IsInside(int x, int y) => x is >= 0 and < Size && y is >= 0 and < Size;

    public Piece? GetPiece(int x, int y) => IsInside(x, y) ? pieces[x, y] : null;

    public void MovePiece(Point from, Point to)
    {
        var piece = GetPiece(from.X, from.Y) ?? throw new ArgumentException("Клетка пуста.", nameof(from));
        if (!IsInside(to.X, to.Y) || GetPiece(to.X, to.Y) is not null)
            throw new ArgumentException("Для перемещения нужна свободная клетка на доске.", nameof(to));

        pieces[from.X, from.Y] = null;
        pieces[to.X, to.Y] = piece;
        piece.Position = to;
    }

    public void RemovePiece(int x, int y) => pieces[x, y] = null;

    internal void Promote(Point point, bool isWhite) => pieces[point.X, point.Y] = new King(point, isWhite);

    internal bool WasCaptured(Point point) => captured.Contains(point);

    internal void MarkCaptured(Point point) => captured.Add(point);

    internal void RemoveCapturedPieces()
    {
        foreach (Point point in captured)
            RemovePiece(point.X, point.Y);

        captured.Clear();
    }

    internal Point? FindJumpedPiece(Point from, Point to)
    {
        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to.Y - from.Y);
        for (var point = new Point(from.X + dx, from.Y + dy); point != to;
            point = new Point(point.X + dx, point.Y + dy))
        {
            if (GetPiece(point.X, point.Y) is not null)
                return point;
        }

        return null;
    }
}
