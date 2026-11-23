using System.Drawing;

namespace GameCSharp.Core;

public class Checker
{
    private readonly bool isWhite;

    public Checker(bool isWhite) => this.isWhite = isWhite;

    public bool GetColor() => isWhite;

    public virtual List<Point> GetPossibleMoves(Board board, Point from)
    {
        var moves = new List<Point>();
        int forward = isWhite ? -1 : 1;

        foreach (var (dx, dy) in Board.Diagonals)
        {
            int x = from.X + dx;
            int y = from.Y + dy;
            if (!board.IsInside(x, y))
                continue;

            if (dy == forward && board.GetPiece(x, y) is null)
                moves.Add(new Point(x, y));

            if (board.IsUncapturedOpponent(x, y, isWhite) && board.IsInside(x + dx, y + dy)
                && board.GetPiece(x + dx, y + dy) is null)
                moves.Add(new Point(x + dx, y + dy));
        }

        return moves;
    }
}
