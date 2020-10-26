using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace STak.WinTak
{
    public static class GeometryExtensions
    {
        public static Point ToPoint2D(this Point3D point)
        {
            return new Point(point.X, point.Z);
        }


        public static Point3D ToPoint3D(this Point point)
        {
            return new Point3D(point.X, 0, point.Y);
        }


        public static (int X, int Y) RoundToInteger(this Point point)
        {
            return ((int)Math.Round(point.X), (int)Math.Round(point.Y));
        }


        public static (int X, int Y, int Z) RoundToInteger(this Point3D point)
        {
            return ((int)Math.Round(point.X), (int)Math.Round(point.Y), (int)Math.Round(point.Z));
        }
    }
}
