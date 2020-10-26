using System;
using System.Windows;
using System.Windows.Media;

namespace STak.WinTak
{
    public class Scheme
    {
        public static Scheme Default     { get; private set; }
        public static Scheme Current     { get; set; }

        public Color  BackgroundColor    { get; set; }
        public string BoardTextureFile   { get; set; }
        public string P1StoneTextureFile { get; set; }
        public string P2StoneTextureFile { get; set; }


        static Scheme()
        {
            Scheme.Default = new Scheme
            {
                BackgroundColor    = Color.FromRgb(0x00, 0x2D, 0x64),
                BoardTextureFile   = App.GetImagePathName(@"Default\Board.jpg"),
                P1StoneTextureFile = App.GetImagePathName(@"Default\Player1Stone.jpg"),
                P2StoneTextureFile = App.GetImagePathName(@"Default\Player2Stone.jpg")
            };
        }


        public Scheme Clone()
        {
            Scheme scheme = this.MemberwiseClone() as Scheme;
            scheme.BackgroundColor = Color.FromArgb(BackgroundColor.A, BackgroundColor.R,
                                                    BackgroundColor.G, BackgroundColor.B);
            return scheme;
        }
    }
}
