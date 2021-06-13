using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Overlay
{
    public class MapRenderer
    {

        public Canvas Canvas { get; private set; }

        public MapRenderer(Canvas canvas)
        {
            this.Canvas = canvas;
        }

        /// <summary>
        /// Renders the <see cref="MapData.ColorCoordinates"/> onto the given canvas
        /// </summary>
        /// <param name="data"></param>
        public void Render(Vector2 playerPosition, MapData data)
        {
            Canvas.Children.Clear();
            // sets how many blocks around the player will be shown on the map
            const int blockCount = 64;
            var blockWidth = Canvas.ActualWidth / blockCount;            

            Func<Vector2, Vector2> CalculatePosition = (Vector2 worldPosition) =>
            {
                Vector2 position = worldPosition - playerPosition;
                var offset = (float)Canvas.ActualWidth / 2f;
                return new Vector2(position.X * (float)blockWidth + offset, position.Y * (float)blockWidth + offset);
            };

            foreach (var block in data.ColorCoordinates)
            {
                Vector2 position = CalculatePosition(block.Key);
                AddBlock(block.Value, new Rect(position.X, position.Y, blockWidth, blockWidth));
            }

            foreach (var player in data.PlayerCoordinates)
            {
                Vector2 position = CalculatePosition(player.Key);
                AddPlayer(player.Value, new Rect(position.X, position.Y, blockWidth, blockWidth));
            }
        }

        private void AddBlock(Color color, Rect area)
        {
            Rectangle rect = new Rectangle();
            rect.Fill = new SolidColorBrush(color);
            rect.Stroke = rect.Fill;
            rect.StrokeThickness = 1;            

            Canvas.Children.Add(rect);

            Canvas.SetLeft(rect, area.Left);
            Canvas.SetTop(rect, area.Top);
            rect.Width = area.Width;
            rect.Height = area.Width;
        }
        private void AddPlayer(MapPlayerData player, Rect area)
        {
            Rectangle rect = new Rectangle();

            if (player.isCurrentPlayer)
            {
                rect.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,/Res/player.png")));
            }
            else
            {
                rect.Fill = new ImageBrush(new BitmapImage(new Uri("pack://application:,,/Res/player2.png")));
            }
            
            rect.Stroke = new SolidColorBrush(Colors.Transparent);
            rect.StrokeThickness = 1;

            Canvas.Children.Add(rect);

            Canvas.SetLeft(rect, area.Left);
            Canvas.SetTop(rect, area.Top);
            rect.Width = area.Width;
            rect.Height = area.Width;

            rect.RenderTransform = new RotateTransform((player.yRot + 180) % 360, area.Width / 2, area.Height / 2);

            // == Test == //

            TextBlock textBlock = new TextBlock();
            textBlock.Text = player.name;
            textBlock.Foreground = new SolidColorBrush(Colors.Black);

            Canvas.Children.Add(textBlock);

            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Canvas.SetLeft(textBlock, area.Left + area.Height / 2 - textBlock.DesiredSize.Width / 2);
            Canvas.SetTop(textBlock, area.Top - textBlock.DesiredSize.Height * 1.5);
        }
    }
}
