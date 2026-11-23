using GameCSharp.Core;

namespace GameCSharp.WinForms;

internal sealed class GameForm : Form
{
    private static readonly Color Background = Color.FromArgb(18, 28, 27);
    private static readonly Color PanelBackground = Color.FromArgb(30, 46, 43);
    private static readonly Color TextColor = Color.FromArgb(242, 245, 238);
    private static readonly Color MutedText = Color.FromArgb(193, 208, 199);
    private static readonly Color Accent = Color.FromArgb(253, 207, 116);

    private readonly Game game = new();
    private readonly BoardControl board;
    private readonly Label turn;
    private readonly Label hint;
    private readonly Label whiteScore;
    private readonly Label blackScore;
    private readonly Button drawButton;
    private readonly Font headingFont = new("Segoe UI", 24f, FontStyle.Bold);
    private readonly Font statusFont = new("Segoe UI", 15f, FontStyle.Bold);

    public GameForm()
    {
        Text = "Русские шашки";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(918, 644);
        MinimumSize = new Size(670, 505);
        BackColor = Background;
        ForeColor = TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(16)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        board = new BoardControl(game) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 16, 0), TabIndex = 0 };
        board.MovePlayed += (_, _) => RefreshStatus();
        layout.Controls.Add(board, 0, 0);

        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9,
            Padding = new Padding(16), Margin = Padding.Empty, BackColor = PanelBackground, TabIndex = 1
        };
        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int height in new[] { 52, 40, 54, 8, 36, 36 })
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var title = CreateLabel("Шашки", TextColor);
        title.Font = headingFont;
        sidebar.Controls.Add(title, 0, 0);
        turn = CreateLabel("", Accent);
        turn.Font = statusFont;
        sidebar.Controls.Add(turn, 0, 1);
        hint = CreateLabel("", MutedText);
        hint.TextAlign = ContentAlignment.TopLeft;
        sidebar.Controls.Add(hint, 0, 2);

        whiteScore = CreateLabel("", TextColor);
        blackScore = CreateLabel("", TextColor);
        sidebar.Controls.Add(whiteScore, 0, 4);
        sidebar.Controls.Add(blackScore, 0, 5);

        var newGameButton = CreateButton("Новая партия", Accent, Background);
        newGameButton.Click += (_, _) => StartNewGame();
        sidebar.Controls.Add(newGameButton, 0, 7);
        drawButton = CreateButton("Согласиться на ничью", Background, TextColor);
        drawButton.Click += (_, _) => AgreeDraw();
        sidebar.Controls.Add(drawButton, 0, 8);

        layout.Controls.Add(sidebar, 1, 0);
        Controls.Add(layout);
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        whiteScore.Text = $"Белые   ·   {game.CountPieces(isWhite: true)}";
        blackScore.Text = $"Чёрные   ·   {game.CountPieces(isWhite: false)}";
        drawButton.Enabled = !game.IsFinished && game.ContinuingFrom is null;

        if (game.Winner is Player winner)
        {
            turn.Text = $"Победа: {winner.GetName()}";
            hint.Text = "У соперника нет доступных ходов.";
        }
        else if (game.Draw is DrawReason reason)
        {
            turn.Text = "Ничья";
            hint.Text = reason switch
            {
                DrawReason.Repetition => "Позиция повторилась трижды.",
                DrawReason.KingMoves => "По 15 ходов дамками без взятий и ходов простых шашек.",
                _ => "Игроки согласились на ничью."
            };
        }
        else
        {
            turn.Text = game.CurrentPlayer.IsWhite() ? "Ход белых" : "Ход чёрных";
            if (game.ContinuingFrom is not null)
                hint.Text = "Продолжите взятие той же шашкой.";
            else if (game.LegalMoves.Any(move => move.Captured is not null))
                hint.Text = "Есть взятие. Выберите отмеченную шашку.";
            else
                hint.Text = "Выберите шашку и клетку для хода.";
        }
    }

    private void StartNewGame()
    {
        game.StartGame();
        board.RefreshBoard();
        RefreshStatus();
        board.Focus();
    }

    private void AgreeDraw()
    {
        DialogResult answer = MessageBox.Show(this, "Оба игрока согласны завершить партию вничью?",
            "Ничья", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes || !game.AgreeDraw())
            return;

        board.RefreshBoard();
        RefreshStatus();
        board.Focus();
    }

    private static Label CreateLabel(string text, Color color) => new()
    {
        Text = text, ForeColor = color, Dock = DockStyle.Fill, Margin = Padding.Empty,
        TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false
    };

    private static Button CreateButton(string text, Color background, Color foreground)
    {
        var button = new Button
        {
            Text = text, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4),
            UseVisualStyleBackColor = false, FlatStyle = FlatStyle.Flat,
            BackColor = background, ForeColor = foreground
        };
        button.FlatAppearance.BorderColor = background;
        return button;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            headingFont.Dispose();
            statusFont.Dispose();
        }
    }
}
