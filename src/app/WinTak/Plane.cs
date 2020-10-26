using System;
using System.Windows.Media.Media3D;

namespace STak.WinTak
{
    public enum PlaneType
    {
        XY,
        XZ,
        YZ
    }


    public struct Plane
    {
        public double   Distance { get; private set; }
        public Vector3D Normal   { get; private set; }


        public Plane(Vector3D normal, double d)
        {
            Normal   = normal;
            Distance = d;
        }


        public Plane(Vector3D normal, Point3D point)
        {
            Distance = -((point.X * normal.X) + (point.Y * normal.Y) + (point.Z * normal.Z));
            Normal   = normal;
        }


        public Plane(double a, double b, double c, double d)
            : this(new Vector3D(a, b, c), d)
        {
        }


        public Plane(Point3D a, Point3D b, Point3D c)
        {
            Vector3D ab = b - a;
            Vector3D ac = c - a;

            Normal = Vector3D.CrossProduct(ab, ac);
            Normal.Normalize();

            Distance = -(Vector3D.DotProduct(Normal, (Vector3D)a));
        }


        public static Plane GetPlane(PlaneType planeType, double offset)
        {
            return planeType switch
            {
                PlaneType.XY => new Plane(new Vector3D(0,0,-1), offset),
                PlaneType.XZ => new Plane(new Vector3D(0,-1,0), offset),
                PlaneType.YZ => new Plane(new Vector3D(-1,0,0), offset),
                _            => throw new Exception($"Invalid plane type: {planeType}.")
            };
        }


        public static bool GetIntersection(Plane plane1, Plane plane2, out Point3D point, out Vector3D normal)
        {
            bool success = false;

            point  = new Point3D();
            normal = new Vector3D();

            Vector3D crossNormal = Vector3D.CrossProduct(plane1.Normal, plane2.Normal);
            double   determinant = crossNormal.Length * crossNormal.Length;

            if (Math.Abs(determinant) > Double.Epsilon)
            {
                point = (Point3D) (((Vector3D.CrossProduct(crossNormal, plane2.Normal) * plane1.Distance)
                                  + (Vector3D.CrossProduct(plane1.Normal, crossNormal) * plane2.Distance))
                                  / determinant);
                normal = crossNormal;
                success = true;
            }

            return success;
        }


        public static Point3D GetIntersection(Vector3D rayVector, Point3D rayPoint, Vector3D planeNormal,
                                                                                     Point3D planePoint)
        {
            var diff  = rayPoint - planePoint;
            var prod1 = Vector3D.DotProduct(diff, planeNormal);
            var prod2 = Vector3D.DotProduct(rayVector, planeNormal);
            var prod3 = prod1 / prod2;

            return rayPoint - rayVector * prod3;
        }


        public static Point3D GetIntersection(Plane plane, Point3D point1, Point3D point2)
        {
            Point3D point;

            double A = plane.Normal.X;
            double B = plane.Normal.Y;
            double C = plane.Normal.Z;
            double D = plane.Distance;

            double x1 = point1.X;
            double y1 = point1.Y;
            double z1 = point1.Z;
            double x2 = point2.X;
            double y2 = point2.Y;
            double z2 = point2.Z;

            double num = (A*x1) + (B*y1) + (C*z1) + D;
            double den = (A * (x1-x2)) + (B * (y1-y2)) + (C * (z1-z2));
            double u   = num / den;

            point = point1 + Vector3D.Multiply(u, point2-point1);
            return point;
        }


        public override string ToString()
        {
            return $"Plane: Normal=({Normal}), Distance={Distance}";
        }
    }
}
