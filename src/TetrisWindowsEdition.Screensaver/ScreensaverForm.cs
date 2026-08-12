using System.Windows.Forms;

namespace TetrisWindowsEdition.Screensaver;

/// <summary>
/// Animação leve de peças de Tetris caindo e formando linhas. Desenhada
/// diretamente via GDI+ (sem WebView2 nem dependências pesadas) para
/// manter a proteção de tela leve, conforme item 22 do spec.
/// </summary>
public sealed class ScreensaverForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly List<FallingPiece> _pieces = new();
    private readonly Random _rng = new();
    private readonly ScreensaverConfig _config;
    private readonly Point _startCursorPos;
    private bool _isPreview;

    private static readonly (int[,] Shape, Color Color)[] Tetrominoes =
    {
        (new[,]{{1,1,1,1}}, Color.FromArgb(0,229,255)),      // I
        (new[,]{{1,1},{1,1}}, Color.FromArgb(255,214,0)),     // O
        (new[,]{{0,1,0},{1,1,1}}, Color.FromArgb(170,0,255)), // T
        (new[,]{{0,1,1},{1,1,0}}, Color.FromArgb(0,230,118)), // S
        (new[,]{{1,1,0},{0,1,1}}, Color.FromArgb(255,23,68)), // Z
        (new[,]{{1,0,0},{1,1,1}}, Color.FromArgb(41,121,255)),// J
        (new[,]{{0,0,1},{1,1,1}}, Color.FromArgb(255,145,0)), // L
    };

    private sealed class FallingPiece
    {
        public required int[,] Shape;
        public required Color Color;
        public float X;
        public float Y;
        public float Speed;
        public int CellSize;
    }

    public ScreensaverForm(bool preview, IntPtr previewParentHandle)
    {
        _config = ScreensaverConfig.Load();
        _isPreview = preview;
        DoubleBuffered = true;
        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.None;

        if (preview)
        {
            // Modo miniatura dentro da caixa de seleção do Windows.
            Win32Interop.SetParent(Handle, previewParentHandle);
            Win32Interop.GetClientRect(previewParentHandle, out var rect);
            Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            Location = new Point(0, 0);
        }
        else
        {
            WindowState = FormWindowState.Maximized;
            Bounds = Screen.PrimaryScreen!.Bounds;
            TopMost = true;
            Cursor.Hide();
            _startCursorPos = Cursor.Position;

            MouseMove += (_, _) => { if (Cursor.Position != _startCursorPos) Application.Exit(); };
            MouseClick += (_, _) => Application.Exit();
            KeyDown += (_, _) => Application.Exit();
        }

        for (int i = 0; i < Math.Clamp(_config.PieceCount, 1, 40); i++)
            _pieces.Add(SpawnPiece());

        _timer.Interval = 1000 / 30;
        _timer.Tick += (_, _) => { Advance(); Invalidate(); };
        _timer.Start();
    }

    private FallingPiece SpawnPiece()
    {
        var (shape, color) = Tetrominoes[_rng.Next(Tetrominoes.Length)];
        return new FallingPiece
        {
            Shape = shape,
            Color = _config.ColorMode == "monochrome" ? Color.Gainsboro : color,
            X = _rng.Next(0, Math.Max(1, Width)),
            Y = -_rng.Next(50, 400),
            Speed = 0.5f + (float)_rng.NextDouble() * (_config.Speed / 2f),
            CellSize = Math.Clamp(_config.Size, 12, 48)
        };
    }

    private void Advance()
    {
        foreach (var piece in _pieces)
        {
            piece.Y += piece.Speed;
            if (piece.Y > Height + 100)
            {
                piece.Y = -100;
                piece.X = _rng.Next(0, Math.Max(1, Width));
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        foreach (var piece in _pieces)
        {
            for (int row = 0; row < piece.Shape.GetLength(0); row++)
            {
                for (int col = 0; col < piece.Shape.GetLength(1); col++)
                {
                    if (piece.Shape[row, col] == 0) continue;

                    var rect = new RectangleF(
                        piece.X + col * piece.CellSize,
                        piece.Y + row * piece.CellSize,
                        piece.CellSize - 2, piece.CellSize - 2);

                    using var brush = new SolidBrush(piece.Color);
                    e.Graphics.FillRectangle(brush, rect);
                    using var pen = new Pen(Color.FromArgb(40, Color.White), 1);
                    e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        base.OnFormClosed(e);
    }
}
