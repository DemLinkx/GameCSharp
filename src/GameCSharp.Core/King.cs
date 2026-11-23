using System.Drawing;

namespace GameCSharp.Core;

public sealed class King(bool isWhite) : Checker(isWhite)
{
    public override List<Point> GetPossibleMoves(Board board, Point from)
    {
        var moves = new List<Point>();
        bool white = GetColor();

        foreach (var (dx, dy) in Board.Diagonals)
        {
            bool jumpedOpponent = false;
            for (int x = from.X + dx, y = from.Y + dy; board.IsInside(x, y); x += dx, y += dy)
            {
                if (board.GetPiece(x, y) is null)
                {
                    moves.Add(new Point(x, y));
                    continue;
                }

                if (jumpedOpponent || !board.IsUncapturedOpponent(x, y, white))
                    break;

                jumpedOpponent = true;
            }
        }

        return moves;
    }
}
