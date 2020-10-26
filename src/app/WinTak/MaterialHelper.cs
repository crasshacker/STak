using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace STak.WinTak
{
    public static class MaterialHelper
    {
        public static DiffuseMaterial GetDiffuseMaterial(string fileName)
        {
            string pathName = App.GetImagePathName(fileName);
            ImageBrush brush = new ImageBrush(new BitmapImage(new Uri(pathName, UriKind.Relative)))
            {
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile
            };
            return new DiffuseMaterial(brush);
        }


        public static EmissiveMaterial GetEmissiveMaterial(string fileName)
        {
            string pathName = App.GetImagePathName(fileName);
            var brush = new ImageBrush(new BitmapImage(new Uri(pathName, UriKind.Relative)));
            return new EmissiveMaterial(brush);
        }


        public static SpecularMaterial GetSpecularMaterial(Color color, double specularPower)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            return new SpecularMaterial(brush, specularPower);
        }
    }
}
