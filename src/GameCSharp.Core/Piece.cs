using System.Drawing;

namespace GameCSharp.Core;

public abstract class Piece(Point position, bool isWhite)
{
    private static readonly (int X, int Y)[] DiagonalOffsets = [(-1, -1), (1, -1), (-1, 1), (1, 1)];

    protected static ReadOnlySpan<(int X, int Y)> Diagonals => DiagonalOffsets;

    internal Point Position { get; set; } = position;

    public Point GetPosition() => Position;

    public bool IsWhite() => isWhite;

    public abstract List<Point> GetPossibleMoves(Board board);

    protected bool IsOpponent(Board board, int x, int y) =>
        board.GetPiece(x, y) is Piece piece && piece.IsWhite() != IsWhite()
        && !board.WasCaptured(new Point(x, y));
}
