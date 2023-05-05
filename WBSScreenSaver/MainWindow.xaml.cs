#region copyright
// Copyright (c) 2015 Wm. Barrett Simms wbsimms.com
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
#endregion
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WBSScreenSaver
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
        const double VELOCITY_FACTOR = 0.3;

        static readonly Brush[][] BRUSHES =
        {
            new Brush[]
            {
                new SolidColorBrush(Color.FromRgb(255,   0,   0)), // Red
				new SolidColorBrush(Color.FromRgb(255, 100,   0)), // Orange
				new SolidColorBrush(Color.FromRgb(255,   0, 100))  // Magenta
			},
            new Brush[]
            {
                new SolidColorBrush(Color.FromRgb(  0, 255,   0)), // Green
                new SolidColorBrush(Color.FromRgb(100, 255,   0)), // Yellow-green
				new SolidColorBrush(Color.FromRgb(  0, 255, 100))  // Aqua
			},
            new Brush[]
            {
                new SolidColorBrush(Color.FromRgb(  0,   0, 255)), // Blue
				new SolidColorBrush(Color.FromRgb(100,   0, 255)), // Purple
				new SolidColorBrush(Color.FromRgb(  0, 100, 255))  // Cyan
			}
        };

        static Random randomGenerator = new Random();

        static int currentColorIndex;

		static byte[] dataToTransmit;
        static int bufferPos = 0;

        static double positionX = 0, positionY = 0;
		static double directionX = 1, directionY = 0.5;


		public MainWindow()
		{
			InitializeComponent();
			this.Loaded += MainWindow_Loaded;
		}

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            /* Load data from file */
            String fileContents;
            try
            {
                fileContents = File.ReadAllText(Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Desktop\\screensaver_input.txt"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                fileContents = "Hello, world!";
            }

            dataToTransmit = ASCIIEncoding.ASCII.GetBytes(fileContents);


            /* Start on first color */

            currentColorIndex = 0;

            WindowState = WindowState.Maximized;
            Mouse.OverrideCursor = Cursors.None;
			TextAnimation_Callback(sender, e);
        }

		private void TextAnimation_Callback(object sender, EventArgs e)
		{
            double newPositionX, newPositionY;
			double newDirectionX, newDirectionY;


            /* ###### Measure dimensions ###### */

            double screenWidth = this.MainGrid.RenderSize.Width;
            double screenHeight = this.MainGrid.RenderSize.Height;

            logo.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size desiredSize = logo.DesiredSize;

			logo.Width = desiredSize.Width;
			logo.Height = desiredSize.Height;


            /* ###### Get new color ###### */

			logo.Foreground = getColorToTransmit();


            /* ###### Compute new trajectory ###### */

            double adjustedScreenWidth = screenWidth - logo.Width;
			double adjustedScreenHeight = screenHeight - logo.Height;

            double screenBorderH = (directionY > 0) ? adjustedScreenHeight : 0;
			double screenBorderV = (directionX > 0) ? adjustedScreenWidth : 0;

            double distanceToBorderH = (directionY == 0) ? double.MaxValue : (screenBorderH - positionY) / directionY;
            double distanceToBorderV = (directionX == 0) ? double.MaxValue : (screenBorderV - positionX) / directionX;

            if (distanceToBorderH > distanceToBorderV)
			// Bounce on vertical plane
			{
                newPositionX = screenBorderV;
                newPositionY = positionY + (directionY * distanceToBorderV);

                // Ensure that the bounce doesn't happen too close to a corner
                double margin = 0.1 * adjustedScreenHeight + randomGenerator.NextDouble() * (0.2 * adjustedScreenHeight);
                newPositionY = Math.Max(newPositionY, margin);
				newPositionY = Math.Min(newPositionY, adjustedScreenHeight - margin);

                newDirectionX = -directionX;
                newDirectionY = directionY;
            }
			else
			// Bounce on horizontal plane
			{
                newPositionX = positionX + (directionX * distanceToBorderH);
                newPositionY = screenBorderH;

				// Ensure that the bounce doesn't happen too close to a corner
				double margin = 0.1 * adjustedScreenWidth + randomGenerator.NextDouble() * (0.2 * adjustedScreenWidth);
                newPositionX = Math.Max(newPositionX, margin);
                newPositionX = Math.Min(newPositionX, adjustedScreenWidth - margin);

                newDirectionX = directionX;
                newDirectionY = -directionY;
            }


            /* ###### Animation ###### */

            TransformGroup group = new TransformGroup();

            TranslateTransform tt = new TranslateTransform(0, 0);
            logo.RenderTransform = group;

            double offsetX = (logo.Width - screenWidth) / 2;
            double offsetY = (logo.Height - screenHeight) / 2;

            double velocity = screenWidth * VELOCITY_FACTOR;
            double duration = GetTimeToTraversePath(positionX, positionY, newPositionX, newPositionY, velocity);

            DoubleAnimation animationX = new DoubleAnimation(positionX + offsetX, newPositionX + offsetX, TimeSpan.FromSeconds(duration));
            DoubleAnimation animationY = new DoubleAnimation(positionY + offsetY, newPositionY + offsetY, TimeSpan.FromSeconds(duration));

            animationX.Completed += new EventHandler(TextAnimation_Callback);

            group.Children.Add(tt);

            tt.BeginAnimation(TranslateTransform.XProperty, animationX);
            tt.BeginAnimation(TranslateTransform.YProperty, animationY);


			/* ###### Prepare for next callback ###### */

			positionX = newPositionX;
			positionY = newPositionY;
			directionX = newDirectionX;
			directionY = newDirectionY;
        }

		private double GetTimeToTraversePath(double startX, double startY, double endX, double endY, double velocity)
		{
			double distance = Math.Sqrt(Math.Pow((endX - startX), 2) + Math.Pow((endY - startY), 2));
			return distance / velocity;
		}

		private Brush getColorToTransmit()
		{
            int bitToTransmit = getNextBitToTransmit();
            Console.WriteLine("Sending: " + bitToTransmit);

            int newColorIndex = currentColorIndex;
            if (bitToTransmit == 1)
            {
                newColorIndex = ++newColorIndex > 2 ? 0 : newColorIndex;
            }
            else
            {
                newColorIndex = --newColorIndex < 0 ? 2 : newColorIndex;
            }

            currentColorIndex = newColorIndex;

            return BRUSHES[newColorIndex][randomGenerator.Next(3)];
        }

        private int getNextBitToTransmit()
        {
            // At end of string, send null terminator and then random colors
            if (bufferPos / 8 == dataToTransmit.Length)
            {
                Console.WriteLine("Exhausted input buffer, sending null terminator");
                bufferPos++;
                return 0;
            } else if (bufferPos / 8 > dataToTransmit.Length)
            {
                Console.WriteLine("Sending random bit");
                bufferPos++;
                return randomGenerator.Next(2);
            }

            byte currentByte = dataToTransmit[bufferPos / 8];
            int currentBit = (currentByte & (1 << (7 - (bufferPos % 8)))) == 0 ? 0 : 1;
            bufferPos++;
            return currentBit;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			Application.Current.Shutdown();
		}
	}
}
