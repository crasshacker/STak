using System;
using System.Windows.Media.Media3D;

namespace STak.WinTak
{
    public class PathSegment
    {
        public Point3D  StartPoint { get; set; }
        public Point3D  EndPoint   { get; set; }
        public DateTime StartTime  { get; set; }
        public DateTime EndTime    { get; set; }
        public double   Distance   { get; set; }
        public double   Fraction   { get; set; }
        public Vector3D Vector     { get; set; }
        public string   SoundFile  { get; set; }


        public void Compile()
        {
            Distance = GeometryHelper.GetDistanceBetween(StartPoint, EndPoint);
            Vector = GeometryHelper.GetVectorBetween(StartPoint, EndPoint);
            Vector.Normalize();
        }


        public void CopyTo(PathSegment segment)
        {
            segment.StartPoint = StartPoint;
            segment.EndPoint   = EndPoint;
            segment.StartTime  = StartTime;
            segment.EndTime    = EndTime;
            segment.Distance   = Distance;
            segment.Fraction   = Fraction;
            segment.Vector     = Vector;
            segment.SoundFile  = SoundFile;
        }
    }
}
