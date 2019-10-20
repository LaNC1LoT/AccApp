using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace AccApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        EllipseView ball;
        Vector2 ballPosition;
        Stopwatch stopwatch = new Stopwatch();
        Vector3 acceleration;
        bool isBallInPlay = false;
        TimeSpan lastElapsedTime;
        Vector2 ballVelocity = Vector2.Zero;
        const float GRAVITY = 1000;
        const float BOUNCE = 2f / 3;

        float WrapAngle(float angle)
        {
            const float pi = (float)Math.PI;

            angle = (float)Math.IEEERemainder(angle, 2 * pi);

            if (angle <= -pi)
            {
                angle += 2 * pi;
            }
            if (angle > pi)
            {
                angle -= 2 * pi;
            }
            return angle;
        }

        private void MoveBall(float deltaSeconds)
        {
            float t = deltaSeconds;
            Vector2 r0 = ballPosition;
            Vector2 r = new Vector2();
            Vector2 v0 = ballVelocity;
            Vector2 v = new Vector2();
            Vector2 a = GRAVITY * new Vector2(-acceleration.X, acceleration.Y);

            while (t > 0)
            {
                r = r0 + v0 * t + 0.5f * a * t * t;
                v = v0 + a * t;

                LabelX.Text = $"X = {r.X}";
                LabelY.Text = $"Y = {r.Y}";

                Line2D horzRollLine = new Line2D();
                Line2D vertRollLine = new Line2D();

                foreach (Line2D line in borders)
                {
                    Line2D shiftedLine = line.ShiftOut(BALL_RADIUS * line.Normal);
                    Vector2 pt1 = shiftedLine.Point1;
                    Vector2 pt2 = shiftedLine.Point2;
                    Vector2 normal = shiftedLine.Normal;

                    if (normal.X == 0 && r0.X > Math.Min(pt1.X, pt2.X) &&
                                         r0.X < Math.Max(pt1.X, pt2.X))
                    {
                        float y = pt1.Y;

                        if (normal.Y > 0 && Math.Abs(r0.Y - y) < 0.1f && r.Y < y)
                        {
                            r.Y = y + 0.01f;
                            v.Y = 0;
                            horzRollLine = line;
                        }
                        else if (normal.Y < 0 && Math.Abs(y - r0.Y) < 0.1f && r.Y > y)
                        {
                            r.Y = y - 0.01f;
                            v.Y = 0;
                            horzRollLine = line;
                        }
                    }

                    else if (normal.Y == 0 && r0.Y > Math.Min(pt1.Y, pt2.Y) && r0.Y < Math.Max(pt1.Y, pt2.Y))
                    {
                        float x = pt1.X;

                        if (normal.X > 0 && Math.Abs(r0.X - x) < 0.1f && r.X < x)
                        {
                            r.X = x + 0.01f;
                            v.X = 0;
                            vertRollLine = line;
                        }
                        else if (normal.X < 0 && Math.Abs(x - r0.X) < 0.1f && r.X > x)
                        {
                            r.X = x - 0.01f;
                            v.X = 0;
                            vertRollLine = line;
                        }
                    }
                }

                float distanceToCollision = float.MaxValue;
                Line2D collisionLine = new Line2D();
                Vector2 collisionPoint = new Vector2();

                foreach (Line2D line in borders)
                {
                    if (line.Equals(horzRollLine) || line.Equals(vertRollLine))
                    {
                        continue;
                    }

                    Line2D shiftedLine = line.ShiftOut(BALL_RADIUS * line.Normal);
                    Line2D ballTrajectory = new Line2D(r0, r);
                    Vector2 intersection = shiftedLine.SegmentIntersection(ballTrajectory);
                    float angleDiff = WrapAngle(line.Angle - ballTrajectory.Angle);

                    if (Line2D.IsValid(intersection) && angleDiff > 0)
                    {
                        float distance = (intersection - r0).Length();

                        if (distance < distanceToCollision)
                        {
                            distanceToCollision = distance;
                            collisionLine = line;
                            collisionPoint = intersection;
                        }
                    }
                }
                
                if (distanceToCollision < float.MaxValue)
                {
                    if (distanceToCollision < 0.1f)
                        Debug.WriteLine("Unexpected distanceToCCollision < 0.1f");

                    float vMag = (float)Math.Sqrt(v0.LengthSquared() + 2 * a.Length() * distanceToCollision);
                    v = vMag * Vector2.Normalize(v0);
                    float tCollision = (vMag - v0.Length()) / a.Length();
 
                    t -= tCollision;
                    r0 = collisionPoint;
                    v0 = BOUNCE * Vector2.Reflect(v, collisionLine.Normal);

                    if (tCollision == 0)
                    {
                        Debug.WriteLine("Unexpected tCollision == 0");
                        break;
                    }
                }
                else
                {
                    t = 0;
                }
            }

            ballPosition = r;
            ballVelocity = v;

            Rectangle ballRect = new Rectangle(ballPosition.X - BALL_RADIUS,
                                               ballPosition.Y - BALL_RADIUS,
                                               AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize);

            AbsoluteLayout.SetLayoutBounds(ball, ballRect);
        }

        private void StartTimer()
        {
            Device.StartTimer(TimeSpan.FromMilliseconds(33), () =>
            {
                TimeSpan elapsedTime = stopwatch.Elapsed;
                float deltaSeconds = (float)(elapsedTime - lastElapsedTime).TotalSeconds;
                lastElapsedTime = elapsedTime;

                if (isBallInPlay)
                    MoveBall(deltaSeconds);

                return true;
            });
        }

        private void BtnStart_Clicked(object sender, EventArgs e)
        {
            if (Accelerometer.IsMonitoring)
                return;

            try
            {
                Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
                Accelerometer.Start(SensorSpeed.UI);
                isBallInPlay = true;

                StartTimer();
            }
            catch(Exception ex)
            {
                Label label = new Label
                {
                    Text = $"Ошибка: {ex.InnerException.Message ?? ex.Message}",
                    FontSize = 24,
                    TextColor = Color.White,
                    BackgroundColor = Color.DarkGray,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(48, 0)
                };

                CircleLayout.Children.Add(label,
                      new Rectangle(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize),
                      AbsoluteLayoutFlags.PositionProportional);
            }
            stopwatch.Start();
        }

        private void BtnStop_Clicked(object sender, EventArgs e)
        {
            if (!Accelerometer.IsMonitoring)
                return;

            isBallInPlay = false;
            Accelerometer.ReadingChanged -= Accelerometer_ReadingChanged;
            Accelerometer.Stop();
            stopwatch.Stop();
        }

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            acceleration = 0.5f * e.Reading.Acceleration + 0.5f * acceleration;
        }

        private void CircleLayout_SizeChanged(object sender, EventArgs e)
        {
            if (CircleLayout.Width > 0 && CircleLayout.Height > 0)
            {
                Load((float)CircleLayout.Width, (float)CircleLayout.Height);

                CircleLayout.SizeChanged -= CircleLayout_SizeChanged;
               
            }
        }

        MazeGrid mazeGrid;
        List<Line2D> borders = new List<Line2D>();

        const int WALL_WIDTH = 16;
        const int BALL_RADIUS = 12;
        const int HOLE_RADIUS = 18;

        private void Load(float width, float height)
        {
            mazeGrid = new MazeGrid(1, 1);
            borders.Clear();

            float cellWidth = width / mazeGrid.Width;
            float cellHeight = height / mazeGrid.Height;
            int halfWallWidth = WALL_WIDTH / 2;

            for (int x = 0; x < mazeGrid.Width; x++)
                for (int y = 0; y < mazeGrid.Height; y++)
                {
                    MazeCell mazeCell = mazeGrid.Cells[x, y];
                    Vector2 ll = new Vector2(x * cellWidth, (y + 1) * cellHeight);
                    Vector2 ul = new Vector2(x * cellWidth, y * cellHeight);
                    Vector2 ur = new Vector2((x + 1) * cellWidth, y * cellHeight);
                    Vector2 lr = new Vector2((x + 1) * cellWidth, (y + 1) * cellHeight);
                    Vector2 right = halfWallWidth * Vector2.UnitX;
                    Vector2 left = -right;
                    Vector2 down = halfWallWidth * Vector2.UnitY;
                    Vector2 up = -down;

                    if (mazeCell.HasLeft)
                    {
                        borders.Add(new Line2D(ll + down, ll + down + right));
                        borders.Add(new Line2D(ll + down + right, ul + up + right));
                        borders.Add(new Line2D(ul + up + right, ul + up));
                    }
                    if (mazeCell.HasTop)
                    {
                        borders.Add(new Line2D(ul + left, ul + left + down));
                        borders.Add(new Line2D(ul + left + down, ur + right + down));
                        borders.Add(new Line2D(ur + right + down, ur + right));
                    }
                    if (mazeCell.HasRight)
                    {
                        borders.Add(new Line2D(ur + up, ur + up + left));
                        borders.Add(new Line2D(ur + up + left, lr + down + left));
                        borders.Add(new Line2D(lr + down + left, lr + down));
                    }
                    if (mazeCell.HasBottom)
                    {
                        borders.Add(new Line2D(lr + right, lr + right + up));
                        borders.Add(new Line2D(lr + right + up, ll + left + up));
                        borders.Add(new Line2D(ll + left + up, ll + left));
                    }
                }

            CircleLayout.Children.Clear();

            BoxView createBoxView() => new BoxView { Color = Color.Green };

            for (int x = 0; x < mazeGrid.Width; x++)
                for (int y = 0; y < mazeGrid.Height; y++)
                {
                    MazeCell mazeCell = mazeGrid.Cells[x, y];

                    if (mazeCell.HasLeft)
                    {
                        Rectangle rect = new Rectangle(x * cellWidth,
                                                       y * cellHeight - halfWallWidth,
                                                       halfWallWidth, cellHeight + WALL_WIDTH);

                        CircleLayout.Children.Add(createBoxView(), rect);
                    }

                    if (mazeCell.HasRight)
                    {
                        Rectangle rect = new Rectangle((x + 1) * cellWidth - halfWallWidth,
                                                       y * cellHeight - halfWallWidth,
                                                       halfWallWidth, cellHeight + WALL_WIDTH);

                        CircleLayout.Children.Add(createBoxView(), rect);
                    }

                    if (mazeCell.HasTop)
                    {
                        Rectangle rect = new Rectangle(x * cellWidth - halfWallWidth,
                                                       y * cellHeight,
                                                       cellWidth + WALL_WIDTH, halfWallWidth);

                        CircleLayout.Children.Add(createBoxView(), rect);
                    }

                    if (mazeCell.HasBottom)
                    {
                        Rectangle rect = new Rectangle(x * cellWidth - halfWallWidth,
                                                       (y + 1) * cellHeight - halfWallWidth,
                                                       cellWidth + WALL_WIDTH, halfWallWidth);

                        CircleLayout.Children.Add(createBoxView(), rect);
                    }
                }

            ball = new EllipseView
            {
                Color = Color.Red,
                WidthRequest = 2 * 12,
                HeightRequest = 2 * 12
            };

            ballPosition = new Vector2(width / 2, height / 2);

            CircleLayout.Children.Add(ball, new Point(ballPosition.X - 12,  ballPosition.Y - 12));
        }
    }
}
