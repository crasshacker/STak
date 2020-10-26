using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using _3DTools;

namespace STak.WinTak
{
    public static class GeometryHelper
    {
        public static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }


        public static Vector3D GetNormal(Point3D[] triangle)
        {
            return GetNormal(triangle[0], triangle[1], triangle[2]);
        }


        public static double GetDistanceBetween(Point3D point1, Point3D point2)
        {
            return Math.Sqrt(Math.Pow(point2.X-point1.X, 2)
                           + Math.Pow(point2.Y-point1.Y, 2)
                           + Math.Pow(point2.Z-point1.Z, 2));
        }


        public static Vector3D GetVectorBetween(Point3D point1, Point3D point2)
        {
            return point2 - point1;
        }


        public static QuaternionRotation3D AxisAngleToQuaternionRotation(AxisAngleRotation3D rotation)
        {
            var r = DegreesToRadians(rotation.Angle);
            var s = Math.Sin(r/2);
            var x = rotation.Axis.X * s;
            var y = rotation.Axis.Y * s;
            var z = rotation.Axis.Z * s;
            var w = Math.Cos(r/2);
            return new QuaternionRotation3D(new Quaternion(x, y, z, w));
        }


        public static Vector3D GetNormal(Point3D a, Point3D b, Point3D c)
        {
            Vector3D normal = Vector3D.CrossProduct(b - a, c - a);
            normal.Normalize();
            return normal;
        }


        public static Visual GetContainingVisual2D(DependencyObject reference)
        {
            Visual visual = null;

            while (reference != null)
            {
                visual = reference as Visual;
                if (visual != null)
                {
                    break;
                }
                reference = VisualTreeHelper.GetParent(reference);
            }

            return visual;
        }


        public static Point Get2DPoint(Point3D point, Viewport3D viewport)
        {

            var visual = (Viewport3DVisual) GetContainingVisual2D(viewport);
            var matrix = MathUtils.TryWorldToViewportTransform(visual, out bool result);

            if (! result)
            {
                return new Point();
            }

            point = matrix.Transform(point);
            return new Point(point.X, point.Y);
        }


        public static Rect Get2DBoundingBox(Viewport3D viewport, Model3D model)
        {
            Rect rect = new Rect();

            IEnumerable<Model3D> models = new Model3D[] { model };
            Transform3D groupTransform = null;

            if (model is Model3DGroup modelGroup)
            {
                groupTransform = modelGroup.Transform;
                models = modelGroup.Children;
            }

            Viewport3DVisual vpv = VisualTreeHelper.GetParent(viewport.Children[0]) as Viewport3DVisual;
            Matrix3D viewportTransform = MathUtils.TryWorldToViewportTransform(vpv, out bool bSuccess);

            if (bSuccess)
            {
                bool bFirst = true;

                foreach (Model3D currentModel in models)
                {
                    if (currentModel is GeometryModel3D)
                    {
                        GeometryModel3D geometryModel = currentModel as GeometryModel3D;

                        if (geometryModel.Geometry is MeshGeometry3D mesh)
                        {
                            foreach (Point3D point3D in mesh.Positions)
                            {
                                Point3D transformedPoint = geometryModel.Transform.Transform(point3D);

                                if (groupTransform != null)
                                {
                                    transformedPoint = groupTransform.Transform(transformedPoint);
                                }

                                transformedPoint = viewportTransform.Transform(transformedPoint);
                                Point point2D = new Point(transformedPoint.X, transformedPoint.Y);

                                if (bFirst)
                                {
                                    rect = new Rect(point2D, new Size(1, 1));
                                    bFirst = false;
                                }
                                else
                                {
                                    rect.Union(point2D);
                                }
                            }
                        }
                    }
                }
            }

            return rect;
        }


        public static bool IsModelClippedByViewport(Viewport3D viewport, Model3D model, double factor = 1.0)
        {
            Rect rect = Get2DBoundingBox(viewport, model);

            double width  = viewport.ActualWidth  * factor;
            double height = viewport.ActualHeight * factor;

            return (rect.Left < 0 || rect.Right  > width
                 || rect.Top  < 0 || rect.Bottom > height);
        }
    }
}
