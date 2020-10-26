using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using STak.TakEngine;

namespace STak.WinTak
{
    public interface IMeshModel
    {
        public GeometryModel3D      Model              { get; set; }
        public MeshGeometry3D       Mesh               { get; set; }
        public Material             Material           { get; set; }
        public Transform3D          Transform          { get; set; }
        public ScaleTransform3D     ScaleTransform     { get; }
        public TranslateTransform3D TranslateTransform { get; }

        public double          Extent    { get; }
        public double          Height    { get; }
        public double          Width     { get; }
        public double          Depth     { get; }
        public double          Front     { get; }
        public double          Rear      { get; }
        public double          Left      { get; }
        public double          Right     { get; }
        public double          Bottom    { get; }
        public double          Top       { get; }

        public double  GetConstant(string key);
        public void    SetPosition(Point3D point);
        public Point3D GetPosition();
        public void    SetScale(double scale);
        public Size3D  GetScale();
    }
}
