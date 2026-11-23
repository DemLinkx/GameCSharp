using System.Drawing;

namespace GameCSharp.Core;

public enum DrawReason { Agreement, Repetition, KingMoves }

public readonly record struct Move(Point From, Point To, Point? Captured);

public sealed class Game
{
    private const int QuietKingMoveLimit = 30; // 15 ходов каждого игрока.
    private readonly Board board = new();
    private readonly Player whitePlayer;
    private readonly Player blackPlayer;
    private readonly Dictionary<string, int> repetitions = [];
    private readonly List<string> moveHistory = [];
    private readonly List<Point> currentRoute = [];
    private int quietKingMoves;

    public Player CurrentPlayer { get; private set; }
    public Player? Winner { get; private set; }
    public DrawReason? Draw { get; private set; }
    public bool IsFinished => Winner is not null || Draw is not null;
    public Point? ContinuingFrom { get; private set; }
    public IReadOnlyList<Move> LegalMoves { get; private set; } = Array.Empty<Move>();
    public IReadOnlyList<string> MoveHistory { get; }

    public Game(string whiteName = "Белые", string blackName = "Чёрные")
    {
        whitePlayer = new Player(whiteName, isWhite: true);
        blackPlayer = new Player(blackName, isWhite: false);
        CurrentPlayer = whitePlayer;
        MoveHistory = moveHistory.AsReadOnly();
        StartGame();
    }

    public void StartGame()
    {
        board.Initialize();
        CurrentPlayer = whitePlayer;
        ContinuingFrom = null;
        Winner = null;
        Draw = null;
        quietKingMoves = 0;
        currentRoute.Clear();
        moveHistory.Clear();
        repetitions.Clear();
        LegalMoves = FindLegalMoves();
        RecordPosition();
    }

    public Checker? PieceAt(Point point) => board.GetPiece(point.X, point.Y);

    public bool WasCapturedThisTurn(Point point) => board.WasCaptured(point);

    public int CountPieces(bool isWhite) => OccupiedCells().Count(cell => cell.Figure.GetColor() == isWhite);

    public bool MakeMove(Point from, Point to)
    {
        foreach (Move move in LegalMoves)
        {
            if (move.From != from || move.To != to)
                continue;

            Apply(move);
            return true;
        }

        return false;
    }

    public bool CheckWinner() => Winner is not null;

    public bool AgreeDraw()
    {
        if (IsFinished || ContinuingFrom is not null)
            return false;

        FinishDraw(DrawReason.Agreement);
        return true;
    }

    private void SwitchPlayer() => CurrentPlayer = CurrentPlayer.IsWhite() ? blackPlayer : whitePlayer;

    private void Apply(Move move)
    {
        Checker checker = PieceAt(move.From)!;
        bool isWhite = checker.GetColor();
        board.MovePiece(move.From, move.To);

        if (currentRoute.Count == 0)
            currentRoute.Add(move.From);
        currentRoute.Add(move.To);

        int promotionRow = isWhite ? 0 : Board.Size - 1;
        if (checker is not King && move.To.Y == promotionRow)
            board.Promote(move.To, isWhite);

        if (move.Captured is Point captured)
        {
            // Взятые шашки остаются препятствиями до конца серии.
            board.MarkCaptured(captured);
            List<Move> nextCaptures = MovesFor(PieceAt(move.To)!, move.To).Where(step => step.Captured is not null).ToList();
            if (nextCaptures.Count > 0)
            {
                ContinuingFrom = move.To;
                LegalMoves = nextCaptures.AsReadOnly();
                return;
            }

            board.RemoveCapturedPieces();
        }

        ContinuingFrom = null;
        bool quietKingMove = checker is King && move.Captured is null;
        quietKingMoves = quietKingMove ? quietKingMoves + 1 : 0;
        if (!quietKingMove)
            repetitions.Clear();

        Player mover = CurrentPlayer;
        RecordMove(mover, move.Captured is not null);
        SwitchPlayer();
        LegalMoves = FindLegalMoves();
        if (LegalMoves.Count == 0)
            Winner = mover;
        else if (RecordPosition() == 3)
            FinishDraw(DrawReason.Repetition);
        else if (quietKingMoves == QuietKingMoveLimit)
            FinishDraw(DrawReason.KingMoves);
    }

    private void RecordMove(Player mover, bool isCapture)
    {
        string separator = isCapture ? ":" : "–";
        string route = string.Join(separator, currentRoute.Select(FormatSquare));
        moveHistory.Add($"{moveHistory.Count + 1}. {mover.GetName()}  {route}");
        currentRoute.Clear();
    }

    private static string FormatSquare(Point point) => $"{(char)('a' + point.X)}{Board.Size - point.Y}";

    private IEnumerable<(Point Cell, Checker Figure)> OccupiedCells()
    {
        for (int y = 0; y < Board.Size; y++)
        for (int x = (y + 1) % 2; x < Board.Size; x += 2)
        {
            if (board.GetPiece(x, y) is Checker checker)
                yield return (new Point(x, y), checker);
        }
    }

    private IEnumerable<Move> MovesFor(Checker checker, Point from) =>
        checker.GetPossibleMoves(board, from).Select(to => new Move(from, to, board.FindJumpedPiece(from, to)));

    private IReadOnlyList<Move> FindLegalMoves()
    {
        List<Move> moves = OccupiedCells()
            .Where(cell => cell.Figure.GetColor() == CurrentPlayer.IsWhite())
            .SelectMany(cell => MovesFor(cell.Figure, cell.Cell)).ToList();
        List<Move> captures = moves.Where(move => move.Captured is not null).ToList();
        return (captures.Count == 0 ? moves : captures).AsReadOnly();
    }

    private int RecordPosition()
    {
        Span<char> key = stackalloc char[Board.Size * Board.Size + 1];
        key.Fill('.');
        key[0] = CurrentPlayer.IsWhite() ? 'w' : 'b';
        foreach (var (cell, figure) in OccupiedCells())
        {
            char side = figure.GetColor() ? 'w' : 'b';
            key[cell.Y * Board.Size + cell.X + 1] = figure is King ? char.ToUpperInvariant(side) : side;
        }

        string position = new(key);
        int count = repetitions.GetValueOrDefault(position) + 1;
        repetitions[position] = count;
        return count;
    }

    private void FinishDraw(DrawReason reason)
    {
        Draw = reason;
        LegalMoves = Array.Empty<Move>();
    }
}
