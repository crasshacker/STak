using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace STak.WinTak
{
    // TODO - If this class becomes useful again (it's not currently in use at all) it should be modified
    //        to use a MeshModel along with an appropriate model definition Json file.

    public class RoomModel
    {
        public double          Size  { get; }
        public GeometryModel3D Model { get; private set; }
        public MeshGeometry3D  Mesh  { get; private set; }


        public RoomModel(double size)
        {
            Size = size;
            BuildRoom();
        }


        private void BuildRoom()
        {
            double[] roomPoints = (double[]) c_roomPoints.Clone();

            Mesh = new MeshGeometry3D();

            for (int index = 0; index < roomPoints.Length; index += 3)
            {
                double x = roomPoints[index+0] * Size;
                double y = roomPoints[index+1] * Size;
                double z = roomPoints[index+2] * Size;

                Mesh.Positions.Add(new Point3D(x, y, z));
            }

            for (int index = 0; index < c_roomTriangleIndices.Length; index += 3)
            {
                int a = c_roomTriangleIndices[index+0];
                int b = c_roomTriangleIndices[index+1];
                int c = c_roomTriangleIndices[index+2];

                Mesh.TriangleIndices.Add(a);
                Mesh.TriangleIndices.Add(b);
                Mesh.TriangleIndices.Add(c);
            }

            var material = new DiffuseMaterial(Brushes.Transparent);
            Model = new GeometryModel3D(Mesh, material);
        }


        private static readonly double c_sizeUnit = 0.5;

        private static readonly double[] c_roomPoints =
        {
           -c_sizeUnit, -c_sizeUnit, -c_sizeUnit,   // 0: left  rear bottom
            c_sizeUnit, -c_sizeUnit, -c_sizeUnit,   // 1: right rear bottom
           -c_sizeUnit, -c_sizeUnit,  c_sizeUnit,   // 2: left  front bottom
            c_sizeUnit, -c_sizeUnit,  c_sizeUnit,   // 3: right front bottom

           -c_sizeUnit,  c_sizeUnit, -c_sizeUnit,   // 4: left  rear top
            c_sizeUnit,  c_sizeUnit, -c_sizeUnit,   // 5: right rear top
           -c_sizeUnit,  c_sizeUnit,  c_sizeUnit,   // 6: left  front top
            c_sizeUnit,  c_sizeUnit,  c_sizeUnit    // 7: right front top
        };

        private static readonly int[] c_roomTriangleIndices =
        {
            0, 3, 1,         // bottom
            0, 2, 3,
            0, 5, 4,         // rear
            0, 1, 5,
            7, 2, 6,         // front
            7, 3, 2,
            0, 6, 2,         // left
            0, 4, 6,
            1, 7, 5,         // right
            1, 3, 7,
            4, 7, 6,         // top
            4, 5, 7
        };
    }
}
