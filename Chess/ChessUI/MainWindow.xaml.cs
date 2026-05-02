using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ChessAI;
using ChessLogic;
using ChessPersistence;

namespace ChessUI
{
    public partial class MainWindow : Window
    {
        // ── Highlight colours ────────────────────────────────────────────────────
        private static readonly Color LastMoveColor  = Color.FromArgb(180, 255, 220,  50);
        private static readonly Color SelectedColor  = Color.FromArgb(180,  50, 160, 255);
        private static readonly Color LegalMoveColor = Color.FromArgb(150, 125, 255, 125);

        // ── Board representation ─────────────────────────────────────────────────
        private readonly Image[,]     pieceImages  = new Image[8, 8];
        private readonly Rectangle[,] highlights   = new Rectangle[8, 8];
        private readonly Dictionary<Position, Move> moveCache = new Dictionary<Position, Move>();
        private readonly ObservableCollection<string> moveHistoryItems = new ObservableCollection<string>();

        // ── Game state ───────────────────────────────────────────────────────────
        private GameState gameState;
        private Position  selectedPos     = null;
        private Position  lastMoveFromPos = null;
        private Position  lastMoveToPos   = null;

        // ── AI ───────────────────────────────────────────────────────────────────
        private bool   isAiMode     = false;
        private bool   isBoardLocked = false;
        private CancellationTokenSource aiCts = new CancellationTokenSource();
        private readonly ChessEngine aiEngine  = new ChessEngine { SearchDepth = 3 };

        // ── Evaluation bar ───────────────────────────────────────────────────────
        private double currentNormalizedScore = 0.5;

        // ── Fullscreen ───────────────────────────────────────────────────────────
        private WindowState _prevWindowState = WindowState.Normal;
        private WindowStyle _prevWindowStyle = WindowStyle.SingleBorderWindow;

        // ────────────────────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();
            MoveHistoryList.ItemsSource = moveHistoryItems;
            ShowModeMenu();
        }

        // ── Initialisation ───────────────────────────────────────────────────────
        private void InitializeBoard()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Image image = new Image();
                    pieceImages[r, c] = image;
                    PieceGrid.Children.Add(image);

                    Rectangle highlight = new Rectangle();
                    highlights[r, c] = highlight;
                    HighlightGrid.Children.Add(highlight);
                }
            }
        }

        private void ShowModeMenu()
        {
            var menu = new ModeMenu();
            MenuContainer.Content = menu;

            menu.ModeSelected += isAi =>
            {
                MenuContainer.Content = null;
                isAiMode = isAi;
                StartNewGame();
            };
        }

        private void StartNewGame()
        {
            aiCts.Cancel();
            aiCts = new CancellationTokenSource();

            selectedPos     = null;
            lastMoveFromPos = null;
            lastMoveToPos   = null;
            moveCache.Clear();
            isBoardLocked = false;

            gameState = new GameState(Player.White, Board.Initial());
            DrawBoard(gameState.Board);
            DrawHighlights();
            RefreshMoveHistory();
            UpdateStatusBar();
            UpdateEvalBar(gameState);
            SetCursor(gameState.CurrentPlayer);
        }

        // ── Drawing ──────────────────────────────────────────────────────────────
        private void DrawBoard(Board board)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    pieceImages[r, c].Source = Images.GetImage(board[r, c]);
        }

        private void DrawHighlights()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    highlights[r, c].Fill = Brushes.Transparent;

            if (lastMoveFromPos != null)
            {
                highlights[lastMoveFromPos.Row, lastMoveFromPos.Column].Fill = new SolidColorBrush(LastMoveColor);
                highlights[lastMoveToPos.Row,   lastMoveToPos.Column].Fill   = new SolidColorBrush(LastMoveColor);
            }

            if (selectedPos != null)
                highlights[selectedPos.Row, selectedPos.Column].Fill = new SolidColorBrush(SelectedColor);

            foreach (Position pos in moveCache.Keys)
                highlights[pos.Row, pos.Column].Fill = new SolidColorBrush(LegalMoveColor);
        }

        // ── Evaluation bar ───────────────────────────────────────────────────────

        // Called when the Viewbox (board) changes size — keeps eval bar in sync.
        private void BoardViewbox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double barHeight = Math.Min(BoardViewbox.ActualWidth, BoardViewbox.ActualHeight);
            if (barHeight <= 0) return;

            EvalBarOuter.Height = barHeight;

            // Remove any running animation and immediately snap to the correct height.
            EvalWhitePortion.BeginAnimation(HeightProperty, null);
            EvalWhitePortion.Height = currentNormalizedScore * barHeight;
        }

        private void UpdateEvalBar(GameState state)
        {
            currentNormalizedScore = EvaluationService.GetNormalizedScore(state);
            EvalScoreText.Text     = EvaluationService.GetScoreText(state);

            double barHeight = EvalBarOuter.ActualHeight;
            if (barHeight <= 0) return;

            double target = currentNormalizedScore * barHeight;

            var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };

            EvalWhitePortion.BeginAnimation(HeightProperty, animation);
        }

        // ── Input handling ───────────────────────────────────────────────────────
        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen() || isBoardLocked) return;

            Point    point = e.GetPosition(BoardGrid);
            Position pos   = ToSquarePosition(point);

            if (selectedPos == null)
                OnFromPositionSelected(pos);
            else
                OnToPositionSelected(pos);
        }

        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / 8;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);
            return new Position(row, col);
        }

        private void OnFromPositionSelected(Position pos)
        {
            IEnumerable<Move> moves = gameState.LegalMovesForPiece(pos);
            if (!moves.Any()) return;

            selectedPos = pos;
            CacheMoves(moves);
            DrawHighlights();
        }

        private void OnToPositionSelected(Position pos)
        {
            if (moveCache.TryGetValue(pos, out Move move))
            {
                selectedPos = null;
                moveCache.Clear();

                if (move.Type == MoveType.PawnPromotion)
                    HandlePromotion(move.FromPos, move.ToPos);
                else
                    HandleMove(move);
            }
            else
            {
                selectedPos = null;
                moveCache.Clear();
                DrawHighlights();
                OnFromPositionSelected(pos);
            }
        }

        private void HandlePromotion(Position from, Position to)
        {
            pieceImages[to.Row,   to.Column].Source   = Images.GetImage(gameState.CurrentPlayer, PieceType.Pawn);
            pieceImages[from.Row, from.Column].Source = null;

            var menu = new PromotionMenu(gameState.CurrentPlayer);
            MenuContainer.Content = menu;

            menu.PieceSelected += type =>
            {
                MenuContainer.Content = null;
                HandleMove(new PawnPromotion(from, to, type));
            };
        }

        private void HandleMove(Move move)
        {
            lastMoveFromPos = move.FromPos;
            lastMoveToPos   = move.ToPos;

            gameState.MakeMove(move);
            DrawBoard(gameState.Board);
            DrawHighlights();
            RefreshMoveHistory();
            UpdateStatusBar();
            UpdateEvalBar(gameState);
            SetCursor(gameState.CurrentPlayer);
            UpdateUndoButton();

            if (gameState.IsGameOver())
            {
                ShowGameOver();
                return;
            }

            if (isAiMode && gameState.CurrentPlayer == Player.Black)
                TriggerAiMove();
        }

        private void TriggerAiMove()
        {
            isBoardLocked = true;
            var token = aiCts.Token;

            Task.Run(() => aiEngine.GetBestMove(gameState), token)
                .ContinueWith(task =>
                {
                    if (token.IsCancellationRequested || task.IsFaulted) return;

                    Move aiMove = task.Result;
                    if (aiMove == null || gameState.IsGameOver()) return;

                    Dispatcher.Invoke(() =>
                    {
                        isBoardLocked = false;
                        if (!gameState.IsGameOver() && gameState.CurrentPlayer == Player.Black)
                            HandleMove(aiMove);
                    });
                }, TaskScheduler.Default);
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();
            foreach (Move move in moves)
                moveCache[move.ToPos] = move;
        }

        // ── UI updates ───────────────────────────────────────────────────────────
        private void UpdateStatusBar()
        {
            if (gameState.IsGameOver())
            {
                PlayerTurnText.Text = gameState.Result.Winner == Player.None
                    ? "Draw"
                    : $"{gameState.Result.Winner} wins";
            }
            else
            {
                PlayerTurnText.Text = $"{gameState.CurrentPlayer} to move";
            }

            GameModeText.Text = isAiMode ? "Human vs Computer" : "Human vs Human";
        }

        private void RefreshMoveHistory()
        {
            moveHistoryItems.Clear();
            var history = gameState.MoveHistory;

            for (int i = 0; i < history.Count; i++)
            {
                int    moveNum = i / 2 + 1;
                string prefix  = i % 2 == 0 ? $"{moveNum}.  " : "    ";
                moveHistoryItems.Add(prefix + history[i].Notation);
            }

            if (moveHistoryItems.Count > 0)
                MoveHistoryList.ScrollIntoView(moveHistoryItems[^1]);
        }

        private void UpdateUndoButton()
        {
            UndoButton.IsEnabled = gameState.CanUndo && !isBoardLocked;
        }

        private void SetCursor(Player player)
        {
            Cursor = player == Player.White ? ChessCursors.WhiteCursor : ChessCursors.BlackCursor;
        }

        private bool IsMenuOnScreen() => MenuContainer.Content != null;

        // ── Menus ────────────────────────────────────────────────────────────────
        private void ShowGameOver()
        {
            var menu = new GameOverMenu(gameState);
            MenuContainer.Content = menu;

            menu.OptionSelected += option =>
            {
                if (option == Option.Restart)
                {
                    MenuContainer.Content = null;
                    ShowModeMenu();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };
        }

        private void ShowPauseMenu()
        {
            var menu = new PauseMenu();
            MenuContainer.Content = menu;

            menu.OptionSelected += option =>
            {
                MenuContainer.Content = null;
                if (option == Option.Restart)
                    ShowModeMenu();
            };
        }

        // ── Keyboard ─────────────────────────────────────────────────────────────
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                return;
            }

            if (e.Key == Key.Escape)
            {
                if (IsFullscreen())
                    ToggleFullscreen();
                else if (!IsMenuOnScreen())
                    ShowPauseMenu();
            }
        }

        // ── Fullscreen ───────────────────────────────────────────────────────────
        private bool IsFullscreen() =>
            WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized;

        private void ToggleFullscreen()
        {
            if (IsFullscreen())
            {
                WindowStyle = _prevWindowStyle;
                WindowState = _prevWindowState;
            }
            else
            {
                _prevWindowState = WindowState;
                _prevWindowStyle = WindowStyle;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
        }

        // ── Button handlers ───────────────────────────────────────────────────────
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (!gameState.CanUndo || isBoardLocked) return;

            // In AI mode, undo the AI response and the human move together.
            if (isAiMode && gameState.MoveHistory.Count >= 2
                         && gameState.MoveHistory[^1].MovedBy == Player.Black)
            {
                gameState.UndoMove();
            }

            if (gameState.CanUndo)
                gameState.UndoMove();

            selectedPos     = null;
            lastMoveFromPos = null;
            lastMoveToPos   = null;
            moveCache.Clear();

            DrawBoard(gameState.Board);
            DrawHighlights();
            RefreshMoveHistory();
            UpdateStatusBar();
            UpdateEvalBar(gameState);
            UpdateUndoButton();
            SetCursor(gameState.CurrentPlayer);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title      = "Save Game",
                Filter     = "Chess Save (*.json)|*.json",
                DefaultExt = "json",
            };

            if (dialog.ShowDialog() == true)
            {
                try   { SaveManager.Save(gameState, dialog.FileName); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Load Game",
                Filter = "Chess Save (*.json)|*.json",
            };

            if (dialog.ShowDialog() == true)
            {
                try   { LoadGame(SaveManager.Load(dialog.FileName)); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadGame(GameState loaded)
        {
            aiCts.Cancel();
            aiCts         = new CancellationTokenSource();
            isBoardLocked = false;

            gameState       = loaded;
            selectedPos     = null;
            lastMoveFromPos = null;
            lastMoveToPos   = null;
            moveCache.Clear();

            DrawBoard(gameState.Board);
            DrawHighlights();
            RefreshMoveHistory();
            UpdateStatusBar();
            UpdateEvalBar(gameState);
            UpdateUndoButton();
            SetCursor(gameState.CurrentPlayer);

            if (isAiMode && !gameState.IsGameOver() && gameState.CurrentPlayer == Player.Black)
                TriggerAiMove();
        }
    }
}
