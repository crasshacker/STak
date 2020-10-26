using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using STak.TakEngine;

namespace STak.WinTak
{
    public class MeshModel : IMeshModel
    {
        private Dictionary<string, double> m_constants;

        // We're basically just a wrapper for a Model.
        public GeometryModel3D Model     { get; set; }

        // All remaining properties just reference properties of the Model.
        public Material        Material  { set => Model.Material = value;
                                           get => Model.Material; }
        public Transform3D     Transform { set => Model.Transform = value;
                                           get => Model.Transform; }
        public MeshGeometry3D  Mesh      { set => Model.Geometry = value;
                                           get => Model.Geometry as MeshGeometry3D; }

        private Transform3DGroup     TransformGroup     => (Transform3DGroup) Transform;
        public  ScaleTransform3D     ScaleTransform     => TransformGroup.Children[0] as ScaleTransform3D;
        public  TranslateTransform3D TranslateTransform => TransformGroup.Children[1] as TranslateTransform3D;

        public Rect3D Bounds => Model.Bounds;
        public double Extent => Math.Max(Model.Bounds.SizeX, Model.Bounds.SizeZ);

        public double Height => Model.Bounds.SizeY;
        public double Width  => Model.Bounds.SizeX;
        public double Depth  => Model.Bounds.SizeZ;
        public double Front  => Model.Bounds.Z + Model.Bounds.SizeZ;
        public double Rear   => Model.Bounds.Z;
        public double Left   => Model.Bounds.X;
        public double Right  => Model.Bounds.X + Model.Bounds.SizeX;
        public double Bottom => Model.Bounds.Y;
        public double Top    => Bottom + Height;


        public MeshModel(GeometryModel3D model)
        {
            SetModel(model);
        }


        public MeshModel(RawMeshModel rawModel)
        {
            SetModel(BuildModel(rawModel));
        }


        public double GetConstant(string key)
        {
            return m_constants[key];
        }


        public void SetScale(double scale)
        {
            SetScale(scale, scale, scale);
        }


        public void SetScale(double scaleX, double scaleY, double scaleZ)
        {
            var group = Model.Transform as Transform3DGroup;
            var transform = group.Children[0] as ScaleTransform3D;
            transform.ScaleX = scaleX;
            transform.ScaleY = scaleY;
            transform.ScaleZ = scaleZ;
        }


        public Size3D GetScale()
        {
            var group = Model.Transform as Transform3DGroup;
            var transform = group.Children[0] as ScaleTransform3D;
            return new Size3D(transform.ScaleX, transform.ScaleY, transform.ScaleZ);
        }


        public void SetPosition(Point3D point)
        {
            var group = Model.Transform as Transform3DGroup;
            var transform = group.Children[1] as TranslateTransform3D;
            transform.OffsetX = point.X;
            transform.OffsetY = point.Y;
            transform.OffsetZ = point.Z;
        }


        public Point3D GetPosition()
        {
            var group = Model.Transform as Transform3DGroup;
            var transform = group.Children[1] as TranslateTransform3D;
            return new Point3D(transform.OffsetX, transform.OffsetY, transform.OffsetZ);
        }


        private void SetModel(GeometryModel3D model)
        {
            var group = new Transform3DGroup();
            group.Children.Add(new ScaleTransform3D(1,1,1));
            group.Children.Add(new TranslateTransform3D(0,0,0));

            Model = model;
            Model.Transform = group;
        }


        private GeometryModel3D BuildModel(RawMeshModel rawModel)
        {
            m_constants = rawModel.Constants ?? new Dictionary<string, double>();

            var vertices   = rawModel.Vertices;
            var triangles  = rawModel.Triangles;
            var textureMap = rawModel.TextureMap;

            var mesh = new MeshGeometry3D();

            foreach (var vertex in vertices)
            {
                double x = vertex[0];
                double y = vertex[1];
                double z = vertex[2];

                mesh.Positions.Add(new Point3D(x, y, z));
            }

            foreach (var triangle in triangles)
            {
                int a = triangle[0];
                int b = triangle[1];
                int c = triangle[2];

                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(c);
            }

            if (textureMap != null)
            {
                foreach (var uv in textureMap)
                {
                    double u = uv[0];
                    double v = uv[1];

                    mesh.TextureCoordinates.Add(new Point(u, v));
                }
            }

            return new GeometryModel3D(mesh, null);
        }
    }
}
