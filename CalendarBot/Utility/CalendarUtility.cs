using System;
using System.Drawing;
using System.Linq;
using Color = System.Drawing.Color;

namespace CalendarBot
{
    internal static class CalendarUtility
    {
        private readonly static string[] Header = { "S", "M", "T", "W", "T", "F", "S" };

        public static Bitmap GenerateCalendarPage(int width, int height, int month, int year, int[] highlights = null)
        {
            var font = new Font("Arial", width / 20, FontStyle.Bold, GraphicsUnit.Pixel);
            var coloredBrush = new SolidBrush(Color.Orange);
            var whiteBrush = new SolidBrush(Color.White);
            var highlighPen = new Pen(Color.GreenYellow, width / 100);

            var bmp = new Bitmap(width, height);
            var graphics = Graphics.FromImage(bmp);

            var point = new Point(width / 100, height / 100);
            var firstDay = (int)(new DateTime(year, month, 1).DayOfWeek);

            for (var i = 0; i < 7; i++) {
                graphics.DrawString(Header[i], font, coloredBrush, point);
                point.X += width / 7;
            }

            for (var i = 0; i < Math.Ceiling((decimal)(DateTime.DaysInMonth(year, month) + firstDay) / 7); i++) {
                point.X = width / 100;
                point.Y += font.Height * 2;

                for (var j = 0; j < 7; j++) {
                    var day = i * 7 + j - firstDay + 1;
                    graphics.DrawString(day <= 0 || day > DateTime.DaysInMonth(year, month) ? " " : day.ToString(), font, whiteBrush, point);

                    if ((highlights?.Contains(day)).GetValueOrDefault())
                        graphics.DrawEllipse(highlighPen, point.X - width / 35, point.Y - width / 35, font.Height * 2, font.Height * 2);

                    point.X += width / 7;
                }
            }

            return bmp;
        }
    }
}
