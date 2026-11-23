using System.Drawing.Drawing2D;
using GameCSharp.Core;

namespace GameCSharp.WinForms;

internal sealed class BoardControl : Control
{
    private static readonly Color LightSquare = Color.FromArgb(232, 235, 222);
    private static readonly Color DarkSquare = Color.FromArgb(60, 105, 93);
    private static readonly Color Selection = Color.FromArgb(253, 207, 116);

    private readonly Game game;
    private Point? selected;
    private Point keyboardSquare = new(0, 5);
    private bool showKeyboardCursor;

    public event EventHandler? MovePlayed;

    public BoardControl(Game game)
    {
        this.game = game;
        SetStyle(ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        TabStop = true;
        AccessibleName = "Шашечная доска";
        BackColor = Color.FromArgb(24, 36, 34);
        DescribeKeyboardSquare();
    }

    public void RefreshBoard()
    {
        selected = game.ContinuingFrom;
        DescribeKeyboardSquare();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Rectangle board = BoardBounds();
        int cell = board.Width / Board.Size;
        if (cell < 10)
            return;

        using var light = new SolidBrush(LightSquare);
        using var dark = new SolidBrush(DarkSquare);
        using var highlight = new SolidBrush(Selection);
        using var focus = new Pen(Color.White, Math.Max(1f, cell / 28f)) { DashStyle = DashStyle.Dash };
        var playable = game.LegalMoves.Select(move => move.From).ToHashSet();
        var destinations = game.LegalMoves.Where(move => move.From == selected).Select(move => move.To).ToHashSet();
        Color coordinateColor = Color.FromArgb(194, 208, 198);
        const TextFormatFlags centered = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;

        int gutter = ScaleLogical(24);
        for (int index = 0; index < Board.Size; index++)
        {
            var rowLabel = new Rectangle(board.Left - gutter, board.Top + index * cell, gutter, cell);
            var columnLabel = new Rectangle(board.Left + index * cell, board.Bottom, cell, gutter);
            TextRenderer.DrawText(e.Graphics, (Board.Size - index).ToString(), Font, rowLabel, coordinateColor, centered);
            TextRenderer.DrawText(e.Graphics, ((char)('a' + index)).ToString(), Font, columnLabel, coordinateColor, centered);
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        for (int row = 0; row < Board.Size; row++)
        for (int column = 0; column < Board.Size; column++)
        {
            var square = new Point(column, row);
            var bounds = new Rectangle(board.Left + column * cell, board.Top + row * cell, cell, cell);
            bool darkSquare = (column + row) % 2 == 1;
            e.Graphics.FillRectangle(darkSquare ? dark : light, bounds);

            if (selected == square)
            {
                int border = Math.Max(2, cell / 15);
                e.Graphics.FillRectangle(highlight, bounds);
                e.Graphics.FillRectangle(dark, Rectangle.Inflate(bounds, -border, -border));
            }

            if (game.PieceAt(square) is Checker piece)
                DrawPiece(e.Graphics, bounds, piece, game.WasCapturedThisTurn(square));

            if (destinations.Contains(square))
            {
                int diameter = Math.Max(4, cell / 4);
                e.Graphics.FillEllipse(highlight, bounds.X + (cell - diameter) / 2, bounds.Y + (cell - diameter) / 2, diameter, diameter);
            }
            else if (playable.Contains(square) && selected != square)
            {
                int diameter = Math.Max(3, cell / 10);
                e.Graphics.FillEllipse(highlight, bounds.Left + diameter, bounds.Top + diameter, diameter, diameter);
            }

            if (Focused && showKeyboardCursor && keyboardSquare == square)
            {
                focus.Color = darkSquare ? Color.White : Color.FromArgb(24, 36, 34);
                e.Graphics.DrawRectangle(focus, Rectangle.Inflate(bounds, -4, -4));
            }
        }
    }

    private static void DrawPiece(Graphics graphics, Rectangle cell, Checker piece, bool captured)
    {
        bool white = piece.GetColor();
        int inset = Math.Max(2, cell.Width / 8);
        Rectangle disc = Rectangle.Inflate(cell, -inset, -inset);
        using var shadow = new SolidBrush(Color.FromArgb(captured ? 35 : 94, 13, 24, 24));
        using var face = new LinearGradientBrush(disc,
            Color.FromArgb(captured ? 115 : 255, white ? Color.White : Color.FromArgb(70, 83, 92)),
            Color.FromArgb(captured ? 115 : 255, white ? Color.FromArgb(220, 223, 210) : Color.FromArgb(21, 29, 36)),
            LinearGradientMode.Vertical);
        using var rim = new Pen(Color.FromArgb(captured ? 105 : 255, white ? Color.FromArgb(148, 151, 139) : Color.FromArgb(124, 141, 151)),
            Math.Max(1f, cell.Width / 38f));

        graphics.FillEllipse(shadow, disc.X, disc.Y + Math.Max(1, cell.Height / 19), disc.Width, disc.Height);
        graphics.FillEllipse(face, disc);
        graphics.DrawEllipse(rim, disc);
        int ringInset = Math.Max(1, disc.Width / 8);
        graphics.DrawEllipse(rim, Rectangle.Inflate(disc, -ringInset, -ringInset));

        if (piece is King)
        {
            float size = disc.Width;
            float x = disc.X;
            float y = disc.Y;
            using var crown = new SolidBrush(Color.FromArgb(captured ? 105 : 255,
                white ? Color.FromArgb(126, 81, 22) : Selection));
            graphics.FillPolygon(crown,
            [
                new PointF(x + size * .25f, y + size * .39f),
                new PointF(x + size * .39f, y + size * .47f),
                new PointF(x + size * .50f, y + size * .31f),
                new PointF(x + size * .61f, y + size * .47f),
                new PointF(x + size * .75f, y + size * .39f),
                new PointF(x + size * .69f, y + size * .65f),
                new PointF(x + size * .31f, y + size * .65f)
            ]);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Rectangle board = BoardBounds();
        if (e.Button != MouseButtons.Left || !board.Contains(e.Location))
            return;

        Focus();
        showKeyboardCursor = false;
        int cell = board.Width / Board.Size;
        keyboardSquare = new Point((e.X - board.X) / cell, (e.Y - board.Y) / cell);
        ActivateSquare(keyboardSquare);
    }

    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is
        Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Enter or Keys.Space || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Left:
                keyboardSquare.X = Math.Max(0, keyboardSquare.X - 1);
                break;
            case Keys.Right:
                keyboardSquare.X = Math.Min(Board.Size - 1, keyboardSquare.X + 1);
                break;
            case Keys.Up:
                keyboardSquare.Y = Math.Max(0, keyboardSquare.Y - 1);
                break;
            case Keys.Down:
                keyboardSquare.Y = Math.Min(Board.Size - 1, keyboardSquare.Y + 1);
                break;
            case Keys.Enter:
            case Keys.Space:
                ActivateSquare(keyboardSquare);
                break;
            default:
                return;
        }

        showKeyboardCursor = true;
        e.SuppressKeyPress = true;
        DescribeKeyboardSquare();
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        showKeyboardCursor = true;
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    private void ActivateSquare(Point square)
    {
        if (selected is Point from && game.MakeMove(from, square))
        {
            selected = game.ContinuingFrom;
            MovePlayed?.Invoke(this, EventArgs.Empty);
        }
        else if (game.ContinuingFrom is null)
        {
            selected = selected != square && game.LegalMoves.Any(move => move.From == square) ? square : null;
        }

        DescribeKeyboardSquare();
        Invalidate();
    }

    private void DescribeKeyboardSquare()
    {
        string contents = game.PieceAt(keyboardSquare) switch
        {
            King king => king.GetColor() ? "белая дамка" : "чёрная дамка",
            Checker checker => checker.GetColor() ? "белая шашка" : "чёрная шашка",
            _ => "пустая клетка"
        };
        string address = $"{(char)('a' + keyboardSquare.X)}{Board.Size - keyboardSquare.Y}";
        AccessibleDescription = $"{address}: {contents}. Стрелки перемещают указатель. Enter или пробел выбирает клетку.";
    }

    private int ScaleLogical(int pixels) => pixels * DeviceDpi / 96;

    private Rectangle BoardBounds()
    {
        int padding = ScaleLogical(16);
        int gutter = ScaleLogical(24);
        int cell = Math.Max(0, (Math.Min(ClientSize.Width, ClientSize.Height) - padding * 2 - gutter) / Board.Size);
        int size = cell * Board.Size;
        return new Rectangle((ClientSize.Width - size + gutter) / 2, (ClientSize.Height - size - gutter) / 2, size, size);
    }
}
