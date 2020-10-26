using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using NodaTime;
using NLog;
using STak.TakEngine;
using STak.TakEngine.Trackers;
using STak.TakEngine.Extensions;
using STak.WinTak.Properties;
using System.Globalization;

using AnimationDuration = System.Windows.Duration;


namespace STak.WinTak
{
    public partial class TableView : UserControl
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private static readonly int MouseWheelDeltaIncrement = 120; // This is what Windows always seems to use.

        private static string[] BannerFontNames             => UIAppConfig.Appearance.Banner.FontNames;
        private static int[]    BannerFontSizes             => UIAppConfig.Appearance.Banner.FontSizes;
        private static int      BannerAnimationTime         => UIAppConfig.Appearance.Banner.AnimationTime;
        private static string[] GameWinQuips                => UIAppConfig.Appearance.Banner.GameWinQuips;
        private static string[] GameLossQuips               => UIAppConfig.Appearance.Banner.GameLossQuips;
        private static string[] GameDrawQuips               => UIAppConfig.Appearance.Banner.GameDrawQuips;
        private static string[] GameOverQuips               => UIAppConfig.Appearance.Banner.GameOverQuips;
        private static bool     AnimateBoard                => UIAppConfig.Appearance.Animation.AnimateBoard;
        private static bool     RotateBoardToFacePlayer     => UIAppConfig.Appearance.Animation.RotateBoardToFacePlayer;
        private static int      MoveAnimationTime           => UIAppConfig.Appearance.Animation.MoveAnimationTime;
        private static int      BoardZoomAnimationTime      => UIAppConfig.Appearance.Animation.BoardZoomAnimationTime;
        private static int      BoardResetAnimationTime     => UIAppConfig.Appearance.Animation.BoardResetAnimationTime;
        private static bool     HighlightWhenInGrabZone     => UIAppConfig.Appearance.Animation.HighlightWhenInGrabZone;
        private static bool     HighlightWhenInDropZone     => UIAppConfig.Appearance.Animation.HighlightWhenInDropZone;
        private static bool     HighlightMovingPlayerStones => UIAppConfig.Appearance.HighlightMovingPlayerStones;
        private static bool     HighlightMovingAIStones     => UIAppConfig.Appearance.HighlightMovingAIStones;
        private static bool     HighlightWinningStones      => UIAppConfig.Appearance.HighlightWinningStones;
        private static bool     ClipTableViewToBounds       => UIAppConfig.Appearance.ClipTableViewToBounds;
        private static bool     AllowInfiniteZoom           => UIAppConfig.Appearance.AllowInfiniteZoom;
        private static double   BoardZoomDistance           => UIAppConfig.Appearance.BoardZoomDistance;
        private static double   StoneSnappingZone           => UIAppConfig.MoveTracking.StoneSnappingZone;
        private static double   StoneHighlightingZone       => UIAppConfig.MoveTracking.StoneHighlightingZone;
        private static int      TrackingInterval            => UIAppConfig.MoveTracking.NotificationInterval;
        private static bool     ShouldAnimateMoves          => UIAppConfig.Appearance.Animation.AnimateMoves
                                                                                  && MoveAnimation.IsEnabled;

        private const double DefaultHitHeight = Double.MinValue;

        private static readonly double InitialZoomFactor = 2500.0; // not necessarily optimal

        private IGame              m_game;
        private double             m_zoomFactor;
        private TableModel         m_tableModel;
        private Trackball          m_trackball;
        private bool               m_reverseTable;
        private int                m_fromTurn;
        private Instant            m_lastMoveTrackingInstant;
        private Model3D            m_modelHitByCursor;
        private Point3D            m_pointHitByCursor;
        private StoneModel         m_selectedStoneModel;
        private BoardCell          m_activeCell;
        private List<StoneModel>   m_highlightedModels;
        private Vector3D?          m_stoneMoveOffset;
        private bool               m_tableInitialized;
        private PulsatingAnimation m_bannerAnimation;
        private int                m_bannerFontIndex;

        public TableModel     TableModel    => m_tableModel;
        public BoardModel     BoardModel    => m_tableModel.BoardModel;
        public ReserveModel[] ReserveModels => m_tableModel.ReserveModels;

        private delegate void HitTestCallback(Model3D modelHit, Point3D pointHit);


        public TableView()
        {
            InitializeComponent();

            m_tableViewport.ClipToBounds = ClipTableViewToBounds;
            m_tableViewBackground.Background = new SolidColorBrush(Scheme.Current.BackgroundColor);

            // NOTE: The MouseMove event isn't sent when the mouse cursor is over the background rather than
            //       the board or a stone, so we use the PreviewMouseMove event instead.
            this.PreviewMouseMove += PreviewMouseMoveHandler;
            this.MouseDown        += MouseDownHandler;
            this.MouseEnter       += MouseEnterHandler;
            this.MouseLeave       += MouseLeaveHandler;
            this.MouseWheel       += MouseWheelHandler;
        }


        public void Observe(IEventBasedGameActivityTracker tracker)
        {
            tracker.GameCreated    += HandleGameCreated;
            tracker.TurnStarted    += HandleTurnStarted;
            tracker.TurnCompleted  += HandleTurnCompleted;
            tracker.StoneDrawn     += HandleStoneDrawn;
            tracker.StonePlaced    += HandleStonePlaced;
            tracker.StoneReturned  += HandleStoneReturned;
            tracker.StackDropped   += HandleStackDropped;
            tracker.StackModified  += HandleStackModified;
            tracker.AbortInitiated += HandleAbortInitiated;
            tracker.AbortCompleted += HandleAbortCompleted;
            tracker.MoveInitiated  += HandleMoveInitiated;
            tracker.MoveCompleted  += HandleMoveCompleted;
            tracker.UndoInitiated  += HandleUndoInitiated;
            tracker.UndoCompleted  += HandleUndoCompleted;
            tracker.RedoInitiated  += HandleRedoInitiated;
            tracker.RedoCompleted  += HandleRedoCompleted;
            tracker.MoveCommencing += HandleMoveCommencing;
            tracker.CurrentTurnSet += HandleCurrentTurnSet;
            tracker.MoveTracked    += HandleMoveTracked;
        }


        public void Unobserve(IEventBasedGameActivityTracker tracker)
        {
            tracker.GameCreated    -= HandleGameCreated;
            tracker.TurnStarted    -= HandleTurnStarted;
            tracker.TurnCompleted  -= HandleTurnCompleted;
            tracker.StoneDrawn     -= HandleStoneDrawn;
            tracker.StonePlaced    -= HandleStonePlaced;
            tracker.StoneReturned  -= HandleStoneReturned;
            tracker.StackDropped   -= HandleStackDropped;
            tracker.StackModified  -= HandleStackModified;
            tracker.AbortInitiated -= HandleAbortInitiated;
            tracker.AbortCompleted -= HandleAbortCompleted;
            tracker.MoveInitiated  -= HandleMoveInitiated;
            tracker.MoveCompleted  -= HandleMoveCompleted;
            tracker.UndoInitiated  -= HandleUndoInitiated;
            tracker.UndoCompleted  -= HandleUndoCompleted;
            tracker.RedoInitiated  -= HandleRedoInitiated;
            tracker.RedoCompleted  -= HandleRedoCompleted;
            tracker.MoveCommencing -= HandleMoveCommencing;
            tracker.CurrentTurnSet -= HandleCurrentTurnSet;
            tracker.MoveTracked    -= HandleMoveTracked;
        }


        public void UpdateGame(IGame game)
        {
            m_game = game;
            m_tableModel.UpdateGame(game);
        }


        public async Task InitializeTable(IGame game, bool animate = true)
        {
            m_tableInitialized = false;

            s_logger.Debug("Initializing the table.");

            m_game         = game;
            m_reverseTable = RotateBoardToFacePlayer && m_game.PlayerOne.IsLocalHuman;

            bool isFirstTime  = m_tableModel == null;
            int milliseconds  = isFirstTime ? BoardZoomAnimationTime : BoardResetAnimationTime;
            TimeSpan duration = TimeSpan.FromMilliseconds(milliseconds);

            ClearTable();
            BuildTableModel();
            BuildTableLighting();
            InitializeTrackball();
            UpdateUndoRedoSlider();
            SetDefaultCameraPosition();
            AnimateBanner();

            if (animate)
            {
                await TransformTableToDefaultPosition(duration, isFirstTime);
            }
            else
            {
                m_trackball.Reset();
            }

            s_logger.Debug("Finished initializing the table.");

            m_tableInitialized = true;
        }


        public void ApplyScheme(Scheme scheme)
        {
            m_tableViewBackground.Background = new SolidColorBrush(scheme.BackgroundColor);
            m_tableModel.ApplyScheme(scheme);
        }


        public async void ResetView(bool animate = true)
        {
            async Task handler()
            {
                if (animate)
                {
                    TimeSpan duration = TimeSpan.FromMilliseconds(BoardResetAnimationTime);
                    await TransformTableToDefaultPosition(duration, false);
                }
                else
                {
                    m_trackball.Reset(m_reverseTable);
                }
            }

            await handler();
        }


        public void SynchronizeToGameState(IGame game, IEnumerable<IMove> moves)
        {
            foreach (var move in moves)
            {
                game.MakeMove(move);

                if (move is StoneMove stoneMove && ! move.HasExecuted)
                {
                    var reserveModel = ReserveModels[stoneMove.Stone.PlayerId];
                    var stoneModel   = reserveModel.DrawStoneModel(stoneMove.Stone.Id);
                    stoneModel.Stone = game.DrawnStone;
                }
                else if (move is StackMove stackMove && ! move.HasExecuted)
                {
                    foreach (var stone in game.StackMove.GrabbedStack.Stones)
                    {
                        var reserveModel = ReserveModels[game.ActivePlayer.Id];
                        var stoneModel   = reserveModel.StoneModels.Where(sm => sm.Id == stone.Id).Single();
                        stoneModel.Stone = stone;
                    }
                }
            }

            for (int file = 0; file < game.Board.Size; ++file)
            {
                for (int rank = 0; rank < game.Board.Size; ++rank)
                {
                    var boardCell = BoardModel.Cells[file, rank];
                    double height = BoardModel.Height;

                    foreach (var stone in boardCell.Stack.Stones)
                    {
                        var reserveModel = ReserveModels[stone.PlayerId];
                        var stoneModel   = TableModel.StoneModels.Where(sm => sm.Id == stone.Id).Single();

                        // TODO - This bizarre dance with the stone type is needed because of the horrible way
                        //        the StoneModel Stone and Type properties are written.  This should be fixed.
                        bool stand = stone.Type == StoneType.Standing;
                        stoneModel.Stone = stone;
                        if (stand)
                        {
                            stoneModel.Unflatten();
                        }

                        double x = boardCell.Center.X;
                        double y = height;
                        double z = boardCell.Center.Y;
                        height += stoneModel.Height;

                        stoneModel.SetPosition(new Point3D(x, y, z));
                        stoneModel.AlignToStone();
                    }
                }
            }
        }


        private Point3D GetAdjustedStackPosition(Point3D point)
        {
            // Get the plane at the board level.
            Plane plane = Plane.GetPlane(PlaneType.XZ, BoardModel.Height);

            // Transform the camera orientation to the proper viewpoint.
            Point3D cameraPosition = m_tableVisual.Transform.Inverse.Transform(m_tableCamera.Position);

            // Get the intersection of the camera=>hit ray with the plane.
            point = Plane.GetIntersection(plane, cameraPosition, point);

            // Place the stone just above any existing stones.
            point.Y = GetHeightForStoneInMotion(point);
            return point;
        }


        private double GetHeightForStoneInMotion(Point3D point)
        {
            return m_tableModel.GetMinimumHeightForStoneCenteredAt(point.ToPoint2D());
        }


        private void ClearTable()
        {
            m_tableModel?.Clear();

            m_tableModel              = null;
            m_modelHitByCursor        = null;
            m_activeCell              = null;
            m_selectedStoneModel      = null;
            m_highlightedModels       = new List<StoneModel>();
            m_lastMoveTrackingInstant = SystemClock.Instance.GetCurrentInstant();

            m_tableGroup.Children.Clear();
            m_tableVisual.Children.Clear();
        }


        private void BuildTableModel()
        {
            m_tableModel = new TableModel(m_game);
            m_tableGroup.Children.Add(m_tableModel.BoardModel.ModelGroup);
            foreach (var stoneModel in m_tableModel.StoneModels)
            {
                m_tableGroup.Children.Add(stoneModel.Model);
            }
        }


        private IEnumerable<Stone> GetStonesInReserves()
        {
            var stones = new List<Stone>();

            foreach (var playerReserve in m_game.Reserves)
            {
                stones.AddRange(playerReserve.FlatStoneReserve.AvailableStones);
                stones.AddRange(playerReserve.CapstoneReserve.AvailableStones);
            }

            return stones;
        }


        private IEnumerable<Stone> GetStonesInPlay()
        {
            var stones = new List<Stone>();
            var board  = BoardModel.Board;

            for (int file = 0; file < board.Size; ++file)
            {
                for (int rank = 0; rank < board.Size; ++rank)
                {
                    stones.AddRange(board[file, rank].Stones);
                }
            }

            return stones;
        }


        private IEnumerable<Stone> GetGameStones()
        {
            return GetStonesInReserves().Concat(GetStonesInPlay());
        }


        private void BuildTableLighting()
        {
            var lights = UIAppConfig.Appearance.Lights.DirectionalLights;

            if (lights != null)
            {
                foreach (var light in lights)
                {
                    Color    color  = (Color) ColorConverter.ConvertFromString(light.Color);
                    Vector3D vector = new Vector3D(light.Direction[0],
                                                   light.Direction[1],
                                                   light.Direction[2]);

                    m_lightGroup.Children.Add(new DirectionalLight(color, vector));
                }
            }
        }


        private void InitializeTrackball()
        {
            if (m_trackball == null)
            {
                m_trackball = new Trackball();
                m_trackball.Attach(this);
                m_trackball.Viewports.Add(m_tableViewport);
                m_trackball.IsEnabled = true;
                m_trackball.EventHandlingFilter = ShouldTrackballProcessEvent;
            }
        }


        private void SetDefaultCameraPosition()
        {
            m_zoomFactor = InitialZoomFactor;

            double x = m_tableCamera.Position.X;
            double y = m_tableCamera.Position.Y;
            double z = m_tableCamera.Position.Z;

            double length = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
            double factor = 100000.0;

            Point3D position = new Point3D((x / length * factor),
                                           (y / length * factor),
                                           (z / length * factor));
            m_tableCamera.Position = position;

            while (! GeometryHelper.IsModelClippedByViewport(m_tableViewport, m_tableGroup))
            {
                x = m_tableCamera.Position.X;
                y = m_tableCamera.Position.Y;
                z = m_tableCamera.Position.Z;

                length = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));

                position = m_tableCamera.Position;
                m_tableCamera.Position = new Point3D((x - (m_zoomFactor*x/length)),
                                                     (y - (m_zoomFactor*y/length)),
                                                     (z - (m_zoomFactor*z/length)));
            }

            // Move back to pre-clipped location.
            m_tableCamera.Position = position;

            // Zoom in a bit slower from here on out.
            m_zoomFactor = InitialZoomFactor * 0.1;

            while (! GeometryHelper.IsModelClippedByViewport(m_tableViewport, m_tableGroup))
            {
                x = m_tableCamera.Position.X;
                y = m_tableCamera.Position.Y;
                z = m_tableCamera.Position.Z;

                length = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));

                position = m_tableCamera.Position;
                m_tableCamera.Position = new Point3D((x - (m_zoomFactor*x/length)),
                                                     (y - (m_zoomFactor*y/length)),
                                                     (z - (m_zoomFactor*z/length)));
            }

            // Move back to pre-clipped location.
            m_tableCamera.Position = position;
        }


        private async Task TransformTableToDefaultPosition(TimeSpan duration, bool zoom = false)
        {
            if (duration.TotalMilliseconds == 0)
            {
                ResetView(false);
                return;
            }

            FrameworkElement scope = this;

            string animationRName = "TableRotation";
            string animationXName = "TableRotationScalingX";
            string animationYName = "TableRotationScalingY";
            string animationZName = "TableRotationScalingZ";

            var transform = m_tableVisual.Transform as Transform3DGroup;
            var endState  = new Quaternion(new Vector3D(0, 1, 0), (m_reverseTable ? 180 : 0));

            if (! AnimateBoard)
            {
                transform.Children[1] = new RotateTransform3D(new QuaternionRotation3D(endState));
            }
            else
            {
                if (zoom)
                {
                    m_trackball.Zoom(0.00001, m_reverseTable);
                }

                //
                // Set up quaternion rotation animation.
                //

                var rotateTransform = transform.Children[1] as RotateTransform3D;
                var unknownRotation = rotateTransform.Rotation;
                QuaternionRotation3D rotation = unknownRotation as QuaternionRotation3D;

                if (unknownRotation is AxisAngleRotation3D axisAngle)
                {
                    rotation = GeometryHelper.AxisAngleToQuaternionRotation(axisAngle);
                    transform.Children[1] = new RotateTransform3D(rotation);
                }

                var propertyR     = QuaternionRotation3D.QuaternionProperty;
                var propertyPathR = new PropertyPath(propertyR);
                var animationR    = new QuaternionAnimation
                {
                    From         = rotation.Quaternion,
                    To           = endState,
                    Duration     = duration,
                    FillBehavior = FillBehavior.Stop
                };

                //
                // Set up three scaling animations, one for each axis.
                //

                static DoubleAnimation BuildScalingAnimation(double from, double to, TimeSpan duration, FillBehavior fill)
                    => new DoubleAnimation { From = from, To = to, Duration = duration, FillBehavior = fill };

                var scaleTransform = transform.Children[0] as ScaleTransform3D;
                var animationX = BuildScalingAnimation(zoom ? 0 : scaleTransform.ScaleX, 1, duration, FillBehavior.Stop);
                var animationY = BuildScalingAnimation(zoom ? 0 : scaleTransform.ScaleY, 1, duration, FillBehavior.Stop);
                var animationZ = BuildScalingAnimation(zoom ? 0 : scaleTransform.ScaleZ, 1, duration, FillBehavior.Stop);

                var propertyX = ScaleTransform3D.ScaleXProperty;
                var propertyY = ScaleTransform3D.ScaleYProperty;
                var propertyZ = ScaleTransform3D.ScaleZProperty;

                var propertyPathX = new PropertyPath(propertyX);
                var propertyPathY = new PropertyPath(propertyY);
                var propertyPathZ = new PropertyPath(propertyZ);

                //
                // Set up the storyboard for the animations.
                //

                // Create a task to be triggered when the animation completes.
                var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                Storyboard storyboard = new Storyboard();

                // Set up the code to be called upon animation completion.
                void finisher(object s, EventArgs e)
                {
                    //
                    // Reset the view to its default state.  This is the same as the state the table
                    // is in after the animation completes, but for reasons I don't understand if we
                    // don't explicitly set the view like this, the next time the table is rotated
                    // by the user it will first pop back to the position it was in prior to this
                    // method ever being called.  In other words, the original transformation isn't
                    // in effect after the animation, but it's still there lurking somewhere waiting
                    // to reinstate itself.  Or something.
                    //
                    // TODO - Figure out why we need to do this, and how to fix it.
                    //
                    ResetView(false);
                    animationR.BeginAnimation(propertyR, null);
                    animationX.BeginAnimation(propertyX, null);
                    animationY.BeginAnimation(propertyY, null);
                    animationZ.BeginAnimation(propertyZ, null);
                    source.SetResult(true);
                }

                NameScope.SetNameScope(scope, new NameScope());

                scope.RegisterName(animationRName, rotation);
                scope.RegisterName(animationXName, scaleTransform);
                scope.RegisterName(animationYName, scaleTransform);
                scope.RegisterName(animationZName, scaleTransform);

                Storyboard.SetTargetName(animationR, animationRName);
                Storyboard.SetTargetName(animationX, animationXName);
                Storyboard.SetTargetName(animationY, animationYName);
                Storyboard.SetTargetName(animationZ, animationZName);

                Storyboard.SetTargetProperty(animationR, propertyPathR);
                Storyboard.SetTargetProperty(animationX, propertyPathX);
                Storyboard.SetTargetProperty(animationY, propertyPathY);
                Storyboard.SetTargetProperty(animationZ, propertyPathZ);

                storyboard.Children.Add(animationR);
                storyboard.Children.Add(animationX);
                storyboard.Children.Add(animationY);
                storyboard.Children.Add(animationZ);

                // Run the animations and return when they're done.
                storyboard.Completed += finisher;
                storyboard.Begin(scope);
                await source.Task;
            }
        }


        private bool Zoom(double distance)
        {
            bool zoomed = false;

            double x = m_tableCamera.Position.X;
            double y = m_tableCamera.Position.Y;
            double z = m_tableCamera.Position.Z;

            double length = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));

            Point3D originalPosition = m_tableCamera.Position;
            m_tableCamera.Position = new Point3D((x - (distance * x / length)),
                                                 (y - (distance * y / length)),
                                                 (z - (distance * z / length)));

            if (AllowInfiniteZoom ||
                (distance > 0 && ! GeometryHelper.IsModelClippedByViewport(m_tableViewport, m_tableGroup)))
            {
                if (m_tableCamera.Position.Y < BoardModel.Bottom + BoardModel.Height)
                {
                    m_tableCamera.Position = originalPosition;
                    zoomed = true;
                }
            }

            return zoomed;
        }


        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += KeyDownHandler;
            this.PreviewKeyUp   += KeyUpHandler;

            this.Focusable = true;
            this.Focus();
        }


        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            double distance = BoardZoomDistance;

            if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if      (e.Key == Key.OemPlus ) { Zoom( distance); }
                else if (e.Key == Key.OemMinus) { Zoom(-distance); }
            }
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                ShowInformationOverlay();
            }
        }


        private void KeyUpHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                HideInformationOverlay();
            }
        }


        private bool ShouldTrackballProcessEvent(TrackballEventType type, object sender, EventArgs e)
        {
            bool shouldProcess = true;

            if (type == TrackballEventType.MouseDown)
            {
                var uiElement = (UIElement) sender;
                MouseButtonEventArgs mouseArgs = (MouseButtonEventArgs) e;
                Point point = mouseArgs.MouseDevice.GetPosition(uiElement);

                ExecuteHitTest(SetObjectHitCallback);
                shouldProcess = IsBoardModelOrBackgroundHit();
            }

            return shouldProcess;
        }


        private void MouseEnterHandler(object sender, MouseEventArgs e)
        {
            // Not (yet?) in use.
        }


        private void MouseLeaveHandler(object sender, MouseEventArgs e)
        {
            if (m_tableInitialized)
            {
                ClearStoneHighlights();
            }
        }


        private void MouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            // If a shift key is down we resize the banner text and mark the event as handled,
            // so that the Trackball handler is not called, which would zoom the board as well.

            bool zoomFontSize     = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            bool changeFontFamily = Keyboard.IsKeyDown(Key.LeftCtrl)  || Keyboard.IsKeyDown(Key.RightCtrl);

            if (zoomFontSize)
            {
                int joinerSize = (int) m_connector.FontSize;
                int playerSize = (int) m_playerOne.FontSize;
                int bannerSize = (int) m_bannerText.FontSize;

                int increment = (e.Delta / MouseWheelDeltaIncrement) * ((playerSize / 10) + 1);
                int minimum   = 5;

                joinerSize = Math.Max(minimum, joinerSize + increment);
                playerSize = Math.Max(minimum, playerSize + increment);
                bannerSize = Math.Max(minimum, bannerSize + increment);

                m_connector .FontSize = joinerSize;
                m_playerOne .FontSize = playerSize;
                m_playerTwo .FontSize = playerSize;
                m_bannerText.FontSize = bannerSize;
            }

            if (changeFontFamily)
            {
                string[] fontNames = BannerFontNames;
                int      increment = Math.Min(e.Delta/MouseWheelDeltaIncrement, fontNames.Length);

                //
                // This loop is here "just in case", to prevent looping indefinitely in the case where none of the
                // font names listed in the configuration file can be found on the machine.
                //
                for (int i = 0; i < fontNames.Length; ++i)
                {
                    // Get the font whose name is 'increment' names away from the current font in the list.
                    m_bannerFontIndex = (fontNames.Length + m_bannerFontIndex + increment) % fontNames.Length;
                    string fontName = fontNames[m_bannerFontIndex];
                    var family = Fonts.SystemFontFamilies.Where(f => f.Source == fontName).SingleOrDefault();

                    if (family == null)
                    {
                        // See if the font is one that's present in the WinTak/Resources/Fonts directory.
                        try { family = new FontFamily($"{App.GetFontDirectoryName()}/#{fontName}"); } catch { }
                    }

                    if (family != null)
                    {
                        var language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name);
                        Debug.WriteLine("Changing banner font to " + family.FamilyNames[language]);
                        m_playerOne .FontFamily = family;
                        m_playerTwo .FontFamily = family;
                        m_connector .FontFamily = family;
                        m_bannerText.FontFamily = family;
                        break;
                    }
                }
            }

            e.Handled = zoomFontSize || changeFontFamily;
        }


        private void MouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (m_game?.IsStarted == true && ! MoveAnimation.IsActive)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (m_game.ActivePlayer.IsLocalHuman && m_game.IsStoneMoving)
                    {
                        if (m_selectedStoneModel != null)
                        {
                            if (m_activeCell != null && m_game.CanPlaceStone(m_activeCell.Cell))
                            {
                                s_logger.Debug("Placing stone onto board.");
                                AudioPlayer.PlaySound(StoneModel.GetSoundFileName(m_selectedStoneModel.Type));
                                var point = new Point3D(m_activeCell.Center.X, m_selectedStoneModel.GetPosition().Y,
                                                                                              m_activeCell.Center.Y);
                                m_selectedStoneModel.SetPosition(point);
                                m_game.PlaceStone(m_activeCell.Cell, m_selectedStoneModel.Stone.Type);
                                s_logger.Debug("Placed stone onto board.");
                                m_selectedStoneModel = null;
                                m_activeCell = null;
                            }
                            else if (m_game.ActiveTurn > 1)
                            {
                                if (m_selectedStoneModel.Type == StoneType.Flat)
                                {
                                    m_selectedStoneModel.Unflatten();
                                }
                                else if (m_selectedStoneModel.Type == StoneType.Standing)
                                {
                                    m_selectedStoneModel.Flatten();
                                }
                            }
                        }
                    }
                    else
                    {
                        ExecuteHitTest(SetObjectHitCallback);

                        if (m_game.ActivePlayer.IsLocalHuman && m_game.IsStackMoving)
                        {
                            //
                            // Drop a stone from the stack.
                            //
                            if (m_activeCell != null && m_game.CanDropStack(m_activeCell.Cell, 1))
                            {
                                s_logger.Debug("Dropping stack onto board.");
                                var stoneModel = m_tableModel.GetStoneModel(m_game.StackMove.GrabbedStack[0].Id);
                                AudioPlayer.PlaySound(StoneModel.GetSoundFileName(stoneModel.Type));

                                // Get snapped position for the bottommost stone.
                                double x = m_activeCell.Center.X;
                                double y = stoneModel.GetPosition().Y;
                                double z = m_activeCell.Center.Y;
                                var point = new Point3D(x, y, z);

                                // Update the Y value for each stone.
                                foreach (var stone in m_game.GrabbedStack.Stones)
                                {
                                    stoneModel = m_tableModel.GetStoneModel(stone.Id);
                                    stoneModel.SetPosition(point);
                                    point.Y += stoneModel.Height;
                                }

                                m_game.DropStack(m_activeCell.Cell, 1);
                                s_logger.Debug("Dropped stack stone onto board.");
                            }
                        }
                        else if (m_game.ActivePlayer.IsLocalHuman && IsStoneModelHit())
                        {
                            //
                            // The mouse is over a stone, which might be a stone in a reserve or one in play.
                            // If it's the top stone in a reserve stack, grab it.  Otherwise, if it's in play
                            // grab the portion of the stack with this stone at the bottom, but only if doing
                            // so doesn't conflict with the carry limit.
                            //
                            StoneModel stoneModel = m_tableModel.StoneModels.SingleOrDefault(s => m_modelHitByCursor == s.Model);
                            Stack stack = null;

                            if (stoneModel.Stone != null)
                            {
                                // It might be a stone in a stack that's in play, so let's check.
                                stack = m_tableModel.GetStackContainingStone(stoneModel.Stone);
                            }

                            if (stack != null)
                            {
                                int stoneCount = stack.Count - stack.Stones.IndexOf(stoneModel.Stone);

                                if (m_game.CanGrabStack(stack.Cell, stoneCount))
                                {
                                    m_stoneMoveOffset = null;
                                    m_game.GrabStack(stack.Cell, stoneCount);
                                    if (! HighlightMovingPlayerStones)
                                    {
                                        ClearStoneHighlights();
                                    }
                                    else
                                    {
                                        // Unhighlight any stones not being grabbed.  The correct stones (and only
                                        // those stones) *should* be highlighted already.  This is fallback code.
                                        var modelsToDim = new List<StoneModel>();
                                        foreach (var highlightedModel in m_highlightedModels)
                                        {
                                            if (! m_game.GrabbedStack.Stones.Where(s =>
                                                    highlightedModel.Stone.Id == s.Id).Any())
                                            {
                                                modelsToDim.Add(highlightedModel);
                                            }
                                        }
                                        foreach (var modelToDim in modelsToDim)
                                        {
                                            modelToDim.Highlight(false);
                                        }
                                    }
                                }
                            }
                            else if (stoneModel.Type != StoneType.Cap || m_game.ActiveTurn > 1)
                            {
                                if (ReserveModels[m_game.ActiveReserve].CanDrawStoneModel(stoneModel))
                                {
                                    m_stoneMoveOffset = null;
                                    // The stone was a topmost reserve stone and thus available to draw.
                                    ReserveModels[m_game.ActiveReserve].DrawStoneModel(stoneModel.Id);
                                    m_game.DrawStone(stoneModel.Type, stoneModel.Id);
                                    if (! HighlightMovingPlayerStones)
                                    {
                                        ClearStoneHighlights();
                                    }
                                }
                            }
                        }
                    }
                }
                else if (m_game.ActivePlayer.IsLocalHuman && e.RightButton == MouseButtonState.Pressed)
                {
                    if (m_game.IsStoneMoving || m_game.IsStackMoving)
                    {
                        m_activeCell = null;
                        var move = (IMove) m_game.StoneMove ?? (IMove) m_game.StackMove;
                        m_game.InitiateAbort(move, MoveAnimationTime);
                    }
                }
            }
        }


        private void PreviewMouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (m_game?.IsStarted == true && m_game.ActivePlayer.IsLocalHuman)
            {
                if (m_game.IsMoveInProgress)
                {
                    ExecuteHitTest(MoveStoneHitTestCallback);
                }
                else if (! m_game.IsCompleted)
                {
                    ExecuteHitTest(HighlightStackHitTestCallback);
                }
            }
        }


        private static RayHitTestParameters RayFromViewportPoint(Viewport3D viewport, Point point)
        {
            MethodInfo method = typeof(Camera).GetMethod("RayFromViewportPoint", BindingFlags.NonPublic
                                                                               | BindingFlags.Instance);
            double distanceAdjustment = 0;
            Size size = new Size { Width = viewport.ActualWidth, Height = viewport.ActualHeight };
            object[] parameters = new object[] { point, size, null, distanceAdjustment };

            return (RayHitTestParameters) method.Invoke(viewport.Camera, parameters);
        }


        private void HighlightStackHitTestCallback(Model3D modelHit, Point3D pointHit)
        {
            if (! m_game.IsCompleted)
            {
                if (IsStoneModelHit())
                {
                    StoneModel stoneModel = m_tableModel.StoneModels.SingleOrDefault(s => m_modelHitByCursor == s.Model);
                    bool isStoneInStack = false;
                    Stack stack = null;

                    if (stoneModel.Stone != null)
                    {
                        // If the model has a stone, the model is in play and must have a stack.
                        stack = m_tableModel.GetStackContainingStone(stoneModel.Stone);
                    }

                    if (stack == null)
                    {
                        // The model hit is in a reserve; unhighlight everything (will be updated below).
                        foreach (var highlightedModel in m_highlightedModels)
                        {
                            if (highlightedModel.Stone != null)
                            {
                                highlightedModel.Highlight(false);
                            }
                        }
                    }
                    else
                    {
                        // The model hit by the cursort is on the board, in play.

                        isStoneInStack = true;

                        int stoneIndex = stack.Stones.IndexOf(stoneModel.Stone);
                        int stoneCount = stack.Count - stoneIndex;

                        if (HighlightWhenInGrabZone)
                        {
                            if (m_game.CanGrabStack(stack.Cell, stoneCount))
                            {
                                var highlightedModels = new List<StoneModel>();

                                for (int i = stoneIndex; i < stack.Count; ++i)
                                {
                                    highlightedModels.Add(m_tableModel.GetStoneModel(stack.Stones[i].Id));
                                }

                                foreach (var highlightedModel in m_highlightedModels.Where(s =>
                                                                 ! highlightedModels.Contains(s)))
                                {
                                    highlightedModel.Highlight(false);
                                }
                                foreach (var highlightedModel in highlightedModels.Where(s =>
                                                             ! m_highlightedModels.Contains(s)))
                                {
                                    highlightedModel.Highlight(true);
                                }

                                m_highlightedModels.Clear();
                                m_highlightedModels.AddRange(highlightedModels);
                            }
                            else
                            {
                                foreach (var highlightedModel in m_highlightedModels)
                                {
                                    highlightedModel.Highlight(false);
                                }
                                m_highlightedModels.Clear();
                            }
                        }
                    }

                    if (! isStoneInStack && HighlightWhenInGrabZone)
                    {
                        if (ReserveModels[m_game.ActiveReserve].CanDrawStoneModel(stoneModel))
                        {
                            if (m_game.ActiveTurn > 1 || stoneModel.Type != StoneType.Cap)
                            {
                                if (! stoneModel.IsHighlighted)
                                {
                                    ReserveModels[m_game.ActiveReserve].AddHighlight(stoneModel);
                                    m_highlightedModels.Add(stoneModel);
                                }
                            }
                            else
                            {
                                ClearStoneHighlights();
                            }
                        }
                        else
                        {
                            ClearStoneHighlights();
                        }
                    }
                }
                else if (! m_game.IsMoveInProgress)
                {
                    ClearStoneHighlights();
                }
            }
        }


        private void ClearStoneHighlights()
        {
            ReserveModels[Player.One].RemoveHighlight();
            ReserveModels[Player.Two].RemoveHighlight();
            foreach (var highlightedModel in m_highlightedModels)
            {
                highlightedModel.Highlight(false);
            }
            m_highlightedModels.Clear();
        }


        private void SetObjectHitCallback(Model3D modelHit, Point3D pointHit)
        {
            // NOTE: This empty method serves as a callback for the purposes of determining which object
            //       (if any) lies underer the cursor.  The actual setting of the targeted object is done
            //       by ExecuteHitTest, so this method serves only to provide a callback for that method
            //       to call.
        }


        private void MoveStoneHitTestCallback(Model3D modelHit, Point3D pointHit)
        {
            var stoneModel = m_tableModel.StoneModels.Where(m => m.Model == modelHit).SingleOrDefault();

            // If a stone or stack was just drawn/grabbed, take note of the offset of the location on the
            // stone where the cursor intersected it, and use this offset during the move to prevent the
            // stone from jumping by an amount equal to the offset of the cursor point to the stone center.
            if (stoneModel != null && m_stoneMoveOffset == null)
            {
                var position = stoneModel.GetPosition();
                m_stoneMoveOffset = position - pointHit;
            }

            if (m_stoneMoveOffset != null)
            {
                pointHit += (Vector3D) m_stoneMoveOffset;
            }

            UpdateStackPosition(pointHit);
        }


        private void ExecuteHitTest(Action<Model3D, Point3D> callback, double height = DefaultHitHeight,
                                                                                 Point? hitPoint = null)
        {
            HitTestResultBehavior wrapper(HitTestResult r)
            {
                var result = r as RayMeshGeometry3DHitTestResult;
                m_modelHitByCursor = result.ModelHit;
                m_pointHitByCursor = result.PointHit;
                callback?.Invoke(m_modelHitByCursor, m_pointHitByCursor);
                return HitTestResultBehavior.Stop;
            }

            if (height == DefaultHitHeight)
            {
                height = BoardModel.Height;
            }

            m_modelHitByCursor = null;
            m_pointHitByCursor = new Point3D();

            Point point = hitPoint ?? Mouse.GetPosition(m_tableViewport);
            PointHitTestParameters hitParams = new PointHitTestParameters(point);
            VisualTreeHelper.HitTest(m_tableViewport, null, wrapper, hitParams);

            //
            // If the cursor was over the background rather than one of the models we need to
            // manually perform the computations needed to find the point in world space that
            // would have been hit by the cursor at the scene height specified by the caller
            // (by default the top of the board).
            //
            if (m_modelHitByCursor == null)
            {
                var inverse = m_tableVisual.Transform.Inverse;

                if (inverse != null)
                {
                    RayHitTestParameters parameters = RayFromViewportPoint(m_tableViewport, point);
                    Point3D viewDirection = (Point3D) parameters.Direction;
                    Point3D viewPosition  = parameters.Origin;

                    viewDirection = inverse.Transform(viewDirection);
                    viewPosition  = inverse.Transform(viewPosition);

                    Plane plane = Plane.GetPlane(PlaneType.XZ, height);
                    Point3D boardPlanePoint = Plane.GetIntersection((Vector3D) viewDirection, viewPosition,
                                       plane.Normal, BoardModel.GetBoardPlaneAsTriangle(BoardPlane.Top)[0]);

                    if (callback == HighlightStackHitTestCallback)
                    {
                        if (! m_game.IsMoveInProgress)
                        {
                            ClearStoneHighlights();
                        }
                    }
                    if (callback == MoveStoneHitTestCallback)
                    {
                        if (m_stoneMoveOffset != null)
                        {
                            // Adjust for offset of mouse cursor from bottom center of stone.
                            boardPlanePoint += (Vector3D) m_stoneMoveOffset;
                        }
                        UpdateStackPosition(boardPlanePoint);
                    }
                }
            }
        }


        private void ShowInformationOverlay()
        {
            ExecuteHitTest(SetObjectHitCallback);
            Point point = Mouse.GetPosition(m_tableViewport);
            ShowInformationOverlay(point, m_modelHitByCursor, m_pointHitByCursor);
        }


        private void ShowInformationOverlay(Point point, Model3D modelHit, Point3D pointHit)
        {
            string text = "";
            string location = $"Location: ({(int)pointHit.X}, {(int)pointHit.Y}, {(int)pointHit.Z}";

            if (IsBoardModelHit(modelHit))
            {
                text += location + "\n";

                var boardCell = m_tableModel.BoardModel.GetCellContaining(pointHit, 1.0);
                if (boardCell != null)
                {
                    text += $"Board Cell: {boardCell.Cell}";
                }
            }
            else if (IsStoneModelHit(modelHit))
            {
                var stoneModel = m_tableModel.GetStoneModel(modelHit);
                if (stoneModel.Stone != null)
                {
                    var boardCell = BoardModel.GetCellContainingStone(stoneModel.Stone);
                    if (boardCell != null)
                    {
                        text += $"{location}\nBoard Cell: {boardCell.Cell}\n";
                    }
                }
                text += $"Model ID: {stoneModel.Id}\n"
                      + $"Stone ID: {stoneModel.Stone?.Id.ToString() ?? "(none)"}";
            }

            SetOverlayText(point, text);
        }


        private void HideInformationOverlay()
        {
            SetOverlayText(new Point(), null);
        }


        private void SetOverlayText(Point point, string text)
        {
            m_infoOverlay.Text       = text ?? string.Empty;
            m_infoOverlay.Visibility = (text != null) ? Visibility.Visible : Visibility.Hidden;

            if (text != null)
            {
                Canvas.SetLeft(m_infoOverlay, point.X + 10);
                Canvas.SetTop (m_infoOverlay, point.Y - 10);
            }
        }


        private void UpdateStackPosition(Point3D point, bool track = true)
        {
            point = GetAdjustedStackPosition(point);

            var snappingCell     = BoardModel.GetCellContaining(point, StoneSnappingZone);
            var highlightingCell = BoardModel.GetCellContaining(point, StoneHighlightingZone);

            var activeCell = snappingCell ?? highlightingCell;

            if (activeCell != null)
            {
                if ((m_game.IsStackMoving && ! m_game.CanDropStack(activeCell.Cell, 1))
                 || (m_game.IsStoneMoving && ! m_game.CanPlaceStone(activeCell.Cell)))
                {
                    activeCell   = null;
                    snappingCell = null;
                }
            }

            m_activeCell = activeCell;

            if (snappingCell != null)
            {
                // Snap stone to the center of the cell.
                point.X = snappingCell.Center.X;
                point.Z = snappingCell.Center.Y;
            }

            if (m_game.IsStoneMoving)
            {
                if (m_selectedStoneModel != null)
                {
                    m_selectedStoneModel.SetPosition(point);
                }
                else
                {
                    Stone stone = m_game.DrawnStone;
                    m_selectedStoneModel = m_tableModel.GetStoneModel(stone.Id);
                }
                if (HighlightWhenInDropZone)
                {
                    if (m_activeCell != null && m_game.CanPlaceStone(m_activeCell.Cell))
                    {
                        m_selectedStoneModel.Highlight(true);
                    }
                    else if (! HighlightMovingPlayerStones)
                    {
                        m_selectedStoneModel.Highlight(false);
                    }
                }
                else if (! HighlightMovingPlayerStones)
                {
                    m_selectedStoneModel.Highlight(false);
                }
            }
            else if (m_game.IsStackMoving)
            {
                foreach (Stone stone in m_game.GrabbedStack.Stones)
                {
                    StoneModel stoneModel = m_tableModel.GetStoneModel(stone.Id);
                    stoneModel.SetPosition(point);
                    point.Y += stoneModel.Height;
                    if (HighlightWhenInDropZone)
                    {
                        if (m_activeCell != null && m_game.CanDropStack(m_activeCell.Cell, 1))
                        {
                            stoneModel.Highlight(true);
                        }
                        else if (! HighlightMovingPlayerStones)
                        {
                            stoneModel.Highlight(false);
                        }
                    }
                    else if (! HighlightMovingPlayerStones)
                    {
                        stoneModel.Highlight(false);
                    }
                }
            }

            if (track && m_game.ActivePlayer.IsLocalHuman && m_game.LastPlayer.IsRemote)
            {
                Instant now = SystemClock.Instance.GetCurrentInstant();
                NodaTime.Duration timeSinceLastTrack = now - m_lastMoveTrackingInstant;

                if ((timeSinceLastTrack.TotalMilliseconds) > TrackingInterval)
                {
                    double file = point.X / BoardModel.Width;
                    double rank = point.Z / BoardModel.Width;
                    m_game.TrackMove(new BoardPosition(file, rank));
                    m_lastMoveTrackingInstant = now;
                }
            }
        }


        private bool IsBoardModelOrBackgroundHit(Model3D modelHit = null)
        {
            return IsBoardModelHit(modelHit) || (modelHit == null && m_modelHitByCursor == null);
        }


        private bool IsBoardModelHit(Model3D modelHit = null)
        {
            return BoardModel.ContainsModel(modelHit ?? m_modelHitByCursor);
        }


        private bool IsStoneModelHit(Model3D modelHit = null)
        {
            return ! IsBoardModelOrBackgroundHit(modelHit);
        }


        private void HandleGameCreated(object sender, GameCreatedEventArgs e)
        {
            s_logger.Debug("=> HandleGameCreated");
            m_game = MainWindow.Instance.GetMirroringGame(e.Prototype.Id);
            s_logger.Debug("<= HandleGameCreated");
        }


        private void HandleTurnStarted(object sender, TurnStartedEventArgs e)
        {
            s_logger.Debug("=> HandleTurnStarted");
            UpdateUndoRedoSlider();
            s_logger.Debug("<= HandleTurnStarted");
        }


        private async void HandleTurnCompleted(object sender, TurnCompletedEventArgs e)
        {
            async Task handler()
            {
                s_logger.Debug("=> HandleTurnCompleted");

                m_selectedStoneModel = null;
                m_activeCell = null;
                UpdateUndoRedoSlider();

                if (RotateBoardToFacePlayer && e.Turn == 1 && e.PlayerId == Player.Two
                                 && ! (m_game.PlayerOne.IsAI && m_game.PlayerTwo.IsAI))
                {
                    m_reverseTable = (m_game.PlayerOne.IsAI || m_game.PlayerOne.IsRemote)
                                                        && m_game.PlayerTwo.IsLocalHuman;
                    await TransformTableToDefaultPosition(TimeSpan.FromMilliseconds(500), false);
                }

                s_logger.Debug("<= HandleTurnCompleted");
            }

            await handler();
        }


        private void HandleStoneDrawn(object sender, StoneDrawnEventArgs e)
        {
            if (! ShouldAnimateMoves || m_game.ActivePlayer.IsHuman)
            {
                s_logger.Debug("=> HandleStoneDrawn");

                Stone stone  = e.Stone;
                int stoneId  = stone.Id;
                int playerId = stone.PlayerId;

                s_logger.Debug("Drawing stone for player={0} with id={1} from reserve.", playerId, stoneId);

                ReserveModel reserveModel = ReserveModels[playerId];
                StoneModel stoneModelDrawn = reserveModel.DrawnStoneModel;

                if (stoneModelDrawn == null)
                {
                    // An AI or remote player must have drawn the stone, so grab the associated model.
                    stoneModelDrawn = reserveModel.DrawStoneModel(stone.Id);
                }

                // NOTE: This statement may look pointless at first glance.  It isn't, because stoneModelDrawn
                //       may be aliasing reserveModel.DrawnStoneModel, which needs to reference this stone, as
                //       reserveModel.DrawnStoneModel is referenced elsewhere.
                stoneModelDrawn.Stone = stone;

                s_logger.Debug("<= HandleStoneDrawn");
            }
        }


        private void HandleStonePlaced(object sender, StonePlacedEventArgs e)
        {
            s_logger.Debug("=> HandleStonePlaced");

            ReserveModels[e.Stone.PlayerId].PlaceStoneModel(e.Stone);
            StoneModel stoneModel = m_tableModel.GetStoneModel(e.Stone.Id);
            stoneModel.Highlight(false);

            s_logger.Debug("<= HandleStonePlaced");
        }


        private void HandleStoneReturned(object sender, StoneReturnedEventArgs e)
        {
            if (! ShouldAnimateMoves || m_game.ActivePlayer.IsHuman)
            {
                s_logger.Debug("=> HandleStoneReturned");

                //
                // Either a human player aborted a StoneMove, or a Stonemove is being Undone.
                //
                s_logger.Debug($"Returning stone with id={e.Stone.Id}.");

                if (m_selectedStoneModel != null)
                {
                    ReserveModels[m_game.ActiveReserve].ReturnStoneModel(m_selectedStoneModel);
                    m_selectedStoneModel.Highlight(false);
                    m_selectedStoneModel = null;
                }

                s_logger.Debug("<= HandleStoneReturned");
            }
        }


        private void HandleStackDropped(object sender, StackDroppedEventArgs e)
        {
            if (! ShouldAnimateMoves || m_game.ActivePlayer.IsHuman)
            {
                s_logger.Debug("=> HandleStackDropped");

                if (m_activeCell != null)
                {
                    // Remove the highlight from the stone we just dropped.
                    BoardCell boardCell = BoardModel.Cells[m_activeCell.Cell.File, m_activeCell.Cell.Rank];
                    StoneModel stoneModel = m_tableModel.GetStoneModel(boardCell.Stack.TopStone.Id);
                    stoneModel.Highlight(false);
                }

                s_logger.Debug("=> HandleStackDropped");
            }
        }


        private void HandleStackModified(object sender, StackModifiedEventArgs e)
        {
            s_logger.Debug("=> HandleStackModified");

            //
            // Move StoneModels affected by the move into the proper location and alignment,
            // so that they reflect the current Stone positions on the board.
            //
            double height = BoardModel.Height;
            BoardCell boardCell = BoardModel.Cells[e.Stack.Cell.File, e.Stack.Cell.Rank];

            foreach (Stone stone in e.Stack.Stones)
            {
                StoneModel stoneModel = m_tableModel.GetStoneModel(stone.Id);

                // Check for null because if a StoneMove is being animated this method will be called prior to
                // HandleMoveCompleted, so the StoneModel being animated won't yet have its stone assigned.
                if (stoneModel != null)
                {
                    if (stone.Type == StoneType.Flat && stoneModel.Type == StoneType.Standing)
                    {
                        stoneModel.Flatten();
                    }
                    else if (stone.Type == StoneType.Standing && stoneModel.Type == StoneType.Flat)
                    {
                        stoneModel.Unflatten();
                    }

                    // Move the stone into the proper board cell (if it's not already there).
                    stoneModel.SetPosition(new Point3D(boardCell.Center.X, height, boardCell.Center.Y));

                    // Update the stack height, in preparation for the next stone;
                    height += stoneModel.Height;
                }
            }

            s_logger.Debug("<= HandleStackModified");
        }


        private async void HandleAbortInitiated(object sender, AbortInitiatedEventArgs e)
        {
            async Task handler()
            {
                s_logger.Debug("=> HandleAbortInitiated");

                if (ShouldAnimateMoves)
                {
                    await AnimateMove(e.Move, AnimationType.AbortMove, e.Duration);
                }

                if (! m_game.Players[e.PlayerId].IsRemoteHuman &&
                     (m_game.PlayerOne.IsLocal || m_game.PlayerTwo.IsLocal))
                {
                    m_game.CompleteAbort(e.Move);
                }

                s_logger.Debug("<= HandleAbortInitiated");
            }

            await handler();
        }


        private void HandleAbortCompleted(object sender, AbortCompletedEventArgs e)
        {
            s_logger.Debug("=> HandleAbortCompleted");
            s_logger.Debug("<= HandleAbortCompleted");
        }


        private async void HandleMoveInitiated(object sender, MoveInitiatedEventArgs e)
        {
            async Task handler()
            {
                s_logger.Debug("=> HandleMoveInitiated");

                if (ShouldAnimateMoves)
                {
                    await AnimateMove(e.Move, AnimationType.MakeMove, e.Duration);
                }

                if (! m_game.Players[e.PlayerId].IsRemoteHuman &&
                     (m_game.PlayerOne.IsLocal || m_game.PlayerTwo.IsLocal))
                {
                    m_game.CompleteMove(e.Move);
                }

                s_logger.Debug("<= HandleMoveInitiated");
            }

            await handler();
        }


        private void HandleMoveCompleted(object sender, MoveCompletedEventArgs e)
        {
            s_logger.Debug("=> HandleMoveCompleted");

            if (e.Move is StoneMove stoneMove)
            {
                var reserveModel = ReserveModels[stoneMove.Stone.PlayerId];
                if (reserveModel.DrawnStoneModel?.Stone?.Id == -1)
                {
                    reserveModel.DrawnStoneModel.Stone = stoneMove.Stone;
                }
            }

            s_logger.Debug("<= HandleMoveCompleted");
        }


        private async void HandleUndoInitiated(object sender, UndoInitiatedEventArgs e)
        {
            async Task handler()
            {
                s_logger.Debug("=> HandleUndoInitiated");

                if (RotateBoardToFacePlayer && m_game.ActiveTurn == 2 && m_game.ActivePlayer.Id == Player.One
                                 && ! (m_game.PlayerOne.IsAI && m_game.PlayerTwo.IsAI))
                {
                    m_reverseTable = m_game.PlayerOne.IsLocalHuman && (m_game.PlayerTwo.IsAI || m_game.PlayerOne.IsRemote);
                    await TransformTableToDefaultPosition(TimeSpan.FromMilliseconds(500), false);
                }

                if (ShouldAnimateMoves)
                {
                    await AnimateMove(e.Move, AnimationType.UndoMove, e.Duration);
                }

                if (! m_game.Players[e.PlayerId].IsRemoteHuman &&
                     (m_game.PlayerOne.IsLocal || m_game.PlayerTwo.IsLocal))
                {
                    m_game.CompleteUndo();
                }

                s_logger.Debug("<= HandleUndoInitiated");
            }

            await handler();
        }


        private void HandleUndoCompleted(object sender, UndoCompletedEventArgs e)
        {
            s_logger.Debug("=> HandleUndoCompleted");

            if (e.Move is StoneMove stoneMove)
            {
                StoneModel stoneModel = m_tableModel.GetStoneModel(stoneMove.Stone.Id);
                ReserveModels[stoneMove.Stone.PlayerId].ReturnStoneModel(stoneModel, true);
            }
            UpdateUndoRedoSlider();

            s_logger.Debug("<= HandleUndoCompleted");
        }


        private async void HandleRedoInitiated(object sender, RedoInitiatedEventArgs e)
        {
            async Task handler()
            {
                s_logger.Debug("=> HandleRedoInitiated");

                if (ShouldAnimateMoves)
                {
                    await AnimateMove(e.Move, AnimationType.MakeMove, e.Duration);
                }

                if (! m_game.Players[e.PlayerId].IsRemoteHuman &&
                     (m_game.PlayerOne.IsLocal || m_game.PlayerTwo.IsLocal))
                {
                    m_game.CompleteRedo();
                }

                s_logger.Debug("<= HandleRedoInitiated");
            }

            await handler();
        }


        private async void HandleRedoCompleted(object sender, RedoCompletedEventArgs e)
        {
            async Task handler()
            {
                s_logger.Debug("=> HandleRedoCompleted");

                if (RotateBoardToFacePlayer && m_game.ActiveTurn == 2 && m_game.ActivePlayer.Id == Player.One
                                 && ! (m_game.PlayerOne.IsAI && m_game.PlayerTwo.IsAI))
                {
                    m_reverseTable = ! (m_game.PlayerOne.IsLocalHuman && (m_game.PlayerTwo.IsAI
                                                                       || m_game.PlayerOne.IsRemote));
                    await TransformTableToDefaultPosition(TimeSpan.FromMilliseconds(500), false);
                }

                UpdateUndoRedoSlider();

                s_logger.Debug("<= HandleRedoCompleted");
            }

            await handler();
        }


        private void HandleMoveCommencing(object sender, MoveCommencingEventArgs e)
        {
            s_logger.Debug("=> HandleMoveCommencing");

            if (MoveAnimation.IsActive)
            {
                // TODO? - Should we enable move commencement notifications in the Game class,
                //         and immediately end animations when a move commences?  We can do so
                //         by setting m_animation to the animation created in AnimateMove, but
                //         aborting has the side effect of stones jumping to their final spots
                //         rather than moving normally when the move commences earlier than we
                //         expected it to.
                //
                // m_moveAnimation.Abort();
            }

            s_logger.Debug("<= HandleMoveCommencing");
        }


        private void HandleCurrentTurnSet(object sender, CurrentTurnSetEventArgs e)
        {
            s_logger.Debug("=> HandleCurrentTurnSet");

            // No need to reverse RevertedMoves; we don't care about order.
            var allMoves = m_game.ExecutedMoves.Concat(m_game.RevertedMoves);
            Stone[] flattenedStones = allMoves.OfType<StackMove>().Where(m => m.FlattenedStone != null)
                                                                 .Select(m => m.FlattenedStone).ToArray();

            var moves = (e.Turn < m_fromTurn) ? m_game.RevertedMoves : m_game.ExecutedMoves;
            TableModel.UpdateStandingStoneModels(flattenedStones, moves, m_fromTurn, e.Turn);
            TableModel.UpdateStoneModelPositions(GetGameStones());

            s_logger.Debug("<= HandleCurrentTurnSet");
        }


        private void OnUndoRedoSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            s_logger.Debug("<= OnUndoRedoSliderChanged");

            var animateMoves  = UIAppConfig.Appearance.Animation.AnimateMoves;
            var animationTime = UIAppConfig.Appearance.Animation.MoveAnimationTime;

            UIAppConfig.Appearance.Animation.AnimateMoves      = false;
            UIAppConfig.Appearance.Animation.MoveAnimationTime = 0;

            int turn = (int) e.NewValue;

            // Don't set the turn to one that would make an AI the active player, unless we're setting it to
            // the last turn of a completed game.  We do this because otherwise the AI would immediately make
            // a new move, forcing any reverted game moves to be removed and the game to be continued from this
            // point.  That is, we would lose all game moves that were made in turns following the turn we're
            // making current.  Note that we don't need to take this step if we're setting the current turn to
            // the end of a completed game.

            if (((turn != m_game.ExecutedMoves.Count + m_game.RevertedMoves.Count) || ! m_game.WasCompleted)
                  && ((m_game.PlayerOne.IsAI && (turn % 2 == 0)) || (m_game.PlayerTwo.IsAI && (turn % 2 == 1))))
            {
                // Move back one turn if possible, otherwise forward one turn.
                turn = (turn > 1) ? turn-1 : turn+1;
            }

            m_fromTurn = m_game.ActivePly-1;
            m_game.SetCurrentTurn(turn);

            UIAppConfig.Appearance.Animation.AnimateMoves      = animateMoves;
            UIAppConfig.Appearance.Animation.MoveAnimationTime = animationTime;

            s_logger.Debug("=> OnUndoRedoSliderChanged");
        }


        private void UpdateUndoRedoSlider()
        {
            int executed = m_game.ExecutedMoves.Count;
            int reverted = m_game.RevertedMoves.Count;
            int total    = executed + reverted;

            // Disable this event handler while we change the value.
            m_undoRedoSlider.ValueChanged -= OnUndoRedoSliderChanged;

            m_undoRedoSlider.Value          = executed;
            m_undoRedoSlider.Maximum        = total;
            m_undoRedoSlider.IsThumbVisible = total > 0;

            // Re-enable the event handler that we disabled above.
            m_undoRedoSlider.ValueChanged  += OnUndoRedoSliderChanged;

            m_undoRedoSlider.IsEnabled = MainWindow.Instance.CanUndoMove()
                                      || MainWindow.Instance.CanRedoMove();
        }


        private void OnAnimationSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue > 0)
            {
                var value = e.NewValue;

                // Increase the speed factor as the slider moves closer to the min/max values.

                if (value < 10) { value *= 0.75; }
                if (value <  5) { value *= 0.75; }
                if (value <  2) { value *= 0.75; }

                if (value > 90) { value *= 1.50; }
                if (value > 95) { value *= 1.50; }
                if (value > 98) { value *= 1.50; }

                value /= 100.0; // Note that this may be greater than 1.

                MoveAnimation.SetAnimationSpeed(value);
                MoveAnimation.IsEnabled = true;
            }
            else
            {
                MoveAnimation.IsEnabled = false;
            }
        }


        private void HandleMoveTracked(object sender, MoveTrackedEventArgs e)
        {
            // s_logger.Debug("=> HandleMoveTracked");

            if (m_game.ActivePlayer.IsRemote && m_game.IsMoveInProgress)
            {
                // s_logger.Debug("Received move tracking notification for position "
                //                         + $"({e.Position.File},{e.Position.Rank})");

                // Get the position in world coordinates.
                double x = e.Position.File * BoardModel.Width;
                double z = e.Position.Rank * BoardModel.Width;
                double y = BoardModel.Height;

                // Move the stack to the proper location.
                UpdateStackPosition(new Point3D(x, y, z), false);
            }

            // s_logger.Debug("<= HandleMoveTracked");
        }


        private static PulsatingStoneModelHighlighter GetGameOverStoneHighlighter(StoneModel stoneModel)
        {
            PulsatingStoneModelHighlighter highlighter = null;

            if (UIAppConfig.Appearance.Animation.GameWinHighlighting != null)
            {
                foreach (var highlightSettings in UIAppConfig.Appearance.Animation.GameWinHighlighting)
                {
                    if (String.Equals(highlightSettings.PulseType, "opacity", StringComparison.OrdinalIgnoreCase))
                    {
                        int    pulseCount     = highlightSettings.PulseCount;
                        int    pulseDuration  = highlightSettings.PulseDuration;
                        double stoppingRate   = highlightSettings.StoppingRate;
                        double minimumOpacity = highlightSettings.MinimumValue;
                        double maximumOpacity = highlightSettings.MaximumValue;
                        double defaultOpacity = highlightSettings.DefaultValue;

                        var duration = TimeSpan.FromMilliseconds(pulseDuration);

                        highlighter = new PulsatingStoneModelHighlighter(stoneModel.Id, pulseCount, duration,
                                                minimumOpacity, maximumOpacity, defaultOpacity, stoppingRate,
                                                () => stoneModel.GetOpacity(), (v) => stoneModel.SetOpacity(v));
                        break;
                    }
                }
            }

            return highlighter;
        }


        private void HighlightWin()
        {
            ulong cells = GetCellsInvolvedInWin();

            for (int file = 0; file < m_game.Board.Size; ++file)
            {
                for (int rank = 0; rank < m_game.Board.Size; ++rank)
                {
                    if (m_game.BitBoard.IsBitSet(cells, file, rank))
                    {
                        var stone = m_game.Board[file, rank].TopStone;
                        var stoneModel = m_tableModel.GetStoneModel(stone.Id);
                        var highlighter = GetGameOverStoneHighlighter(stoneModel);

                        if (highlighter != null)
                        {
                            var oldHighlighter = stoneModel.Highlighter;
                            // TODO - Generalize this event to all IModelHighlighters?
                            highlighter.HighlightingComplete += (s, o) => stoneModel.Highlighter = oldHighlighter;
                            stoneModel.Highlighter = highlighter;
                            stoneModel.Highlight(true);
                        }

                        stoneModel.Highlight(true);
                    }
                }
            }
        }


        private ulong GetCellsInvolvedInWin()
        {
            return (m_game.Result.WinType == WinType.Road) ? m_game.BitBoard.GetRoad        (m_game.Result.Winner)
                 : (m_game.Result.WinType == WinType.Flat) ? m_game.BitBoard.GetFlatWinCells(m_game.Result.Winner)
                                                           : 0;
        }


        public async Task AnimateGameOver()
        {
            if (HighlightWinningStones)
            {
                HighlightWin();
            }

            // Build an appropriate message based on the game result as well as the user's involvement in the game
            // (that is, whether the user is a player or was just watching an AI vs. AI or kibitzing a remote game).

            int playerId = m_game.Result.Winner;
            string message;

            if ((m_game.PlayerOne.IsLocalHuman && ! m_game.PlayerOne.WasAI)
             || (m_game.PlayerTwo.IsLocalHuman && ! m_game.PlayerTwo.WasAI))
            {
                string[] list = (playerId == Player.None) ? GameDrawQuips
                  : m_game.Players[playerId].IsLocalHuman ? GameWinQuips
                                                          : GameLossQuips;
                message = list[new Random().Next(list.Length)];
            }
            else
            {
                message = m_game.Result.WinType switch
                {
                    WinType.Draw => GameOverQuips[1],
                               _ => GameOverQuips[0]
                };
            }

            if (playerId != Player.None)
            {
                string winner = (playerId != Player.None) ? m_game.Players[playerId].Name : "Nobody";
                message = Regex.Replace(message, "{winner}", winner);
            }

            m_bannerText.Text = message;

            // Create an animation to display the game over quip.

            var animation = new StoryboardAnimation(this);

            // Set up the animation property info.

            string name         = "TextOverlay_FontSize";
            var    property     = TextBlock.FontSizeProperty;
            var    propertyPath = new PropertyPath(property);
            var    animatable   = m_bannerText;

            var fontSizeProperty = new AnimatableProperty
            {
                Name         = name,
                Property     = property,
                PropertyPath = propertyPath,
                Animatable   = animatable
            };

            // Zoom/scale/enlarge the announcement.

            int       expandTime = 500;
            TimeSpan? beginTime  = TimeSpan.Zero;

            double from = 1;
            double to   = m_playerOne.FontSize * 1.25;

            var duration  = new AnimationDuration(TimeSpan.FromMilliseconds(expandTime));
            var timeline  = new DoubleAnimation { From = from, To = to, Duration = duration, BeginTime = beginTime };

            animation.AddTimeline(timeline, fontSizeProperty);

            // Run the animation.

            await animation.Run();
        }


        public void HideGameOverAnimation()
        {
            m_bannerText.Text     = String.Empty;
            m_bannerText.FontSize = 1;
        }


        private void AnimateBanner()
        {
            int small = BannerFontSizes[0];
            int large = BannerFontSizes[2];

            var duration = TimeSpan.FromMilliseconds(BannerAnimationTime);

            m_playerOne.Text = m_game.PlayerOne.Name + (m_game.PlayerOne.IsAI ? " (AI)" : "");
            m_playerTwo.Text = m_game.PlayerTwo.Name + (m_game.PlayerTwo.IsAI ? " (AI)" : "");

            var fontFamily = new FontFamily(String.Join(",", BannerFontNames));

            m_playerOne .FontFamily = fontFamily;
            m_playerTwo .FontFamily = fontFamily;
            m_connector .FontFamily = fontFamily;
            m_bannerText.FontFamily = fontFamily;

            m_bannerAnimation = new PulsatingAnimation(1, duration, small, large, 0);
            m_bannerAnimation.AnimationStateUpdated += UpdateBannerAnimationState;
            m_bannerAnimation.Start(small, large);
        }


        private void UpdateBannerAnimationState(object sender, AnimationStateUpdatedEventArgs e)
        {
            double playerNameSize = m_bannerAnimation.CurrentValue;
            double connectorSize  = BannerFontSizes[1];

            m_connector.FontSize = Math.Min(playerNameSize, connectorSize);
            m_playerOne.FontSize = playerNameSize;
            m_playerTwo.FontSize = playerNameSize;
        }


        private async Task AnimateMove(IMove move, AnimationType moveType, int durationMs)
        {
            MoveAnimation animation = null;
            TimeSpan duration = TimeSpan.FromMilliseconds(durationMs != 0 ? durationMs : MoveAnimationTime);
            int playerId = (moveType == AnimationType.UndoMove) ? m_game.LastPlayer.Id : m_game.ActivePlayer.Id;
            s_logger.Debug($"Animating {moveType} for {duration.TotalMilliseconds} milliseconds.");

            if (move is StoneMove stoneMove)
            {
                StoneModel stoneModel = null;

                if (moveType == AnimationType.MakeMove)
                {
                    stoneModel = ReserveModels[m_game.ActiveReserve].DrawStoneModel(stoneMove.Stone.Id);
                }
                else if ((moveType == AnimationType.UndoMove)
                      || (moveType == AnimationType.AbortMove))
                {
                    stoneModel = m_tableModel.GetStoneModel(stoneMove.Stone.Id);
                }

                animation = new StoneMoveAnimation(m_tableModel, stoneMove, playerId, duration, moveType,
                                                                     HighlightMovingAIStones, stoneModel);
            }
            else if (move is StackMove stackMove)
            {
                animation = new StackMoveAnimation(m_tableModel, stackMove, playerId, duration, moveType,
                                                                                 HighlightMovingAIStones);
            }

            // Run the animation and wait for it to complete.
            await animation.Start();
        }
    }
}
