using System.Drawing;

namespace GameCSharp.Core;

public sealed class King(Point position, bool isWhite) : Piece(position, isWhite)
{
    public override List<Point> GetPossibleMoves(Board board)
    {
        var moves = new List<Point>();
        Point from = GetPosition();

        foreach (var (dx, dy) in Diagonals)
        {
            bool jumpedOpponent = false;
            for (int x = from.X + dx, y = from.Y + dy; board.IsInside(x, y); x += dx, y += dy)
            {
                if (board.GetPiece(x, y) is null)
                {
                    moves.Add(new Point(x, y));
                    continue;
                }

                if (jumpedOpponent || !IsOpponent(board, x, y))
                    break;

                jumpedOpponent = true;
            }
        }

        return moves;
    }
}
