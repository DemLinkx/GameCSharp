using System.Drawing;

namespace GameCSharp.Core;

public sealed class Checker(Point position, bool isWhite) : Piece(position, isWhite)
{
    public override List<Point> GetPossibleMoves(Board board)
    {
        var moves = new List<Point>();
        Point from = GetPosition();
        int forward = IsWhite() ? -1 : 1;

        foreach (var (dx, dy) in Diagonals)
        {
            int x = from.X + dx;
            int y = from.Y + dy;
            if (!board.IsInside(x, y))
                continue;

            if (dy == forward && board.GetPiece(x, y) is null)
                moves.Add(new Point(x, y));

            if (IsOpponent(board, x, y) && board.IsInside(x + dx, y + dy)
                && board.GetPiece(x + dx, y + dy) is null)
                moves.Add(new Point(x + dx, y + dy));
        }

        return moves;
    }
}
