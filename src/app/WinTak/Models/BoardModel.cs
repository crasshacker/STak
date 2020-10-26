using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media;
using NodaTime;
using NLog;
using STak.TakEngine;
using STak.TakEngine.Trackers;
using STak.WinTak.Properties;

namespace STak.WinTak
{
    public enum BoardPlane
    {
        Bottom,
        Top,
        Left,
        Right,
        Back,
        Front
    };


    public class BoardModel
    {
        private static string BoardModelFileName    => UIAppConfig.Appearance.Models.BoardModelFileName;
        private static string GridLineModelFileName => UIAppConfig.Appearance.Models.GridLineModelFileName;

        private BoardCell[,]    m_boardCells;
        private IMeshModel      m_boardMeshModel;
        private IMeshModel      m_gridLineMeshModel;
        private double          m_boardExtent;          // Cell area only; See Resources/Models/BoardModel.txt
        private double          m_bevelExtent;          // Includes bevel; see Resources/Models/BoardModel.txt.

        public Board            Board         { get; }
        public Model3DGroup     ModelGroup    { get; private set; }
        public double           StoneExtent   { get; private set; }

        public BoardCell[,]     Cells           => m_boardCells;
        public BoardCell        this[Cell cell] => m_boardCells[cell.File, cell.Rank];

        public Transform3DGroup Transform  => ModelGroup.Transform as Transform3DGroup;
        public double           CellExtent => (Extent - ((Board.Size+1) * GridLineWidth)) / Board.Size;

        public GeometryModel3D  Model      => m_boardMeshModel.Model;
        public MeshGeometry3D   Mesh       => m_boardMeshModel.Mesh;
        public Material         Material   => m_boardMeshModel.Material;

        public double Extent => m_boardExtent;
        public double Width  => m_boardMeshModel.Width;
        public double Height => m_boardMeshModel.Height;
        public double Bottom => m_boardMeshModel.Bottom;
        public double Top    => m_boardMeshModel.Top;
        public double Front  => m_boardMeshModel.Front;
        public double Rear   => m_boardMeshModel.Rear;
        public double Left   => m_boardMeshModel.Left;
        public double Right  => m_boardMeshModel.Right;

        public double GridLineWidth => m_gridLineMeshModel.Width;


        private BoardModel()
        {
        }


        public BoardModel(Board board, double stoneToCellWidthRatio)
        {
            Board = board;
            BuildModel();

            // Compute this AFTER model has been built.
            StoneExtent = CellExtent * stoneToCellWidthRatio;
        }


        public void ApplyScheme(Scheme scheme)
        {
            DiffuseMaterial diffuseMaterial = MaterialHelper.GetDiffuseMaterial(scheme.BoardTextureFile);
            SpecularMaterial specularMaterial = MaterialHelper.GetSpecularMaterial(Colors.White, 150);
            MaterialGroup materialGroup = new MaterialGroup();
            materialGroup.Children.Add(diffuseMaterial);
            materialGroup.Children.Add(specularMaterial);
            Model.Material = materialGroup;
        }


        public bool ContainsModel(Model3D model) => ModelGroup.Children.Contains(model);
        public bool ContainsPoint(Point   point) => GetBoardRectangle().Contains(point);


        public BoardCell GetCellContainingStone(Stone stone) => GetCellContainingStone(stone.Id);

        public BoardCell GetCellContainingStone(int stoneId)
        {
            for (int file = 0; file < Board.Size; ++file)
            {
                for (int rank = 0; rank < Board.Size; ++rank)
                {
                    var boardCell = Cells[file, rank];
                    if (boardCell.Stack.Stones.Where(s => s.Id == stoneId).Any())
                    {
                        return boardCell;
                    }
                }
            }

            return null;
        }


        public Point3D[] GetBoardPlaneAsTriangle(BoardPlane boardPlane)
        {
            return (boardPlane == BoardPlane.Top)
                ? new Point3D[] { new Point3D(0,Top,   0), new Point3D(1,Top,   0), new Point3D(1,Top,   1) }
                : new Point3D[] { new Point3D(0,Bottom,0), new Point3D(1,Bottom,0), new Point3D(1,Bottom,1) };
        }


        public BoardCell GetCellContaining(Point3D stoneCenter, double cellSnappingTendency)
        {
            Point stonePoint = stoneCenter.ToPoint2D();

            if (GetBoardRectangle().Contains(stonePoint))
            {
                for (int file = 0; file < Board.Size; ++file)
                {
                    for (int rank = 0; rank < Board.Size; ++rank)
                    {
                        BoardCell boardCell = m_boardCells[file, rank];

                        if (boardCell.Contains(stonePoint, cellSnappingTendency))
                        {
                            return boardCell;
                        }
                    }
                }
            }

            return null;
        }


        public Rect GetBoardRectangle()
        {
            return new Rect(Left, Rear, m_bevelExtent, m_bevelExtent);
        }


        public bool IsIntersectedByRect(Rect rect)
        {
            return GetBoardRectangle().IntersectsWith(rect);
        }


        public List<BoardCell> GetCellsIntersectedBy(Rect stoneRect)
        {
            List<BoardCell> intersectedCells = new List<BoardCell>();

            double x = Left  - (StoneExtent / 2);
            double y = Rear  - (StoneExtent / 2);
            double w = Right - Left + StoneExtent;
            double h = Front - Rear + StoneExtent;

            Rect boardRect = new Rect(x, y, w, h);

            if (boardRect.IntersectsWith(stoneRect))
            {
                for (int file = 0; file < Board.Size; ++file)
                {
                    for (int rank = 0; rank < Board.Size; ++rank)
                    {
                        BoardCell boardCell = m_boardCells[file, rank];

                        if (boardCell.Intersects(stoneRect))
                        {
                            intersectedCells.Add(boardCell);
                        }
                    }
                }
            }

            return intersectedCells;
        }


        private void BuildModel()
        {
            // TODO - Get rid of these hardwired model builders.
            var meshModelBuilder = new JsonMeshModelBuilder(BoardModelFileName);
            m_boardMeshModel = meshModelBuilder.Build();

            meshModelBuilder = new JsonMeshModelBuilder(GridLineModelFileName);
            m_gridLineMeshModel = meshModelBuilder.Build();

            ModelGroup = new Model3DGroup
            {
                Transform = new Transform3DGroup()
            };
            ModelGroup.Children.Add(m_boardMeshModel.Model);

            Color color = Colors.Black;
            Brush gridBrush = new SolidColorBrush(color);
            DiffuseMaterial gridImageMaterial = new DiffuseMaterial(gridBrush);

            m_boardExtent = m_boardMeshModel.GetConstant("boardExtent");
            m_bevelExtent = m_boardMeshModel.GetConstant("bevelExtent");

            double boardHeight     = m_boardMeshModel.Height;
            double boardHalfExtent = Extent / 2;

            GeometryModel3D gridLineModel;

            for (int gridIndex = 0; gridIndex <= Board.Size; ++gridIndex)
            {
                // Use grid line mesh to build columns.
                double  tileStart = (Extent - GridLineWidth) * gridIndex / Board.Size;
                double  x, y, z;

                // Prepare column (file) grid line translation
                x =  tileStart - boardHalfExtent;
                y =  boardHeight;
                z = -boardHalfExtent;

                // Build the model and add it to the board group.
                gridLineModel = new GeometryModel3D(m_gridLineMeshModel.Mesh, gridImageMaterial)
                {
                    Transform = new TranslateTransform3D(x, y, z)
                };
                ModelGroup.Children.Add(gridLineModel);

                // Prepare row (rank) grid line translation.
                x = -boardHalfExtent;
                y =  boardHeight;
                z = -boardHalfExtent + tileStart + GridLineWidth;

                // Build the model and add it to the board group.
                gridLineModel = new GeometryModel3D(m_gridLineMeshModel.Mesh, gridImageMaterial);
                // Rotate 90 degrees to build rows.
                Vector3D axis = new Vector3D(0, 1, 0);
                AxisAngleRotation3D axisAngle = new AxisAngleRotation3D(axis, 90);
                Transform3D rotateTransform = new RotateTransform3D(axisAngle);
                TranslateTransform3D translateTransform = new TranslateTransform3D(x, y, z);
                // Rotate first, and then translate.
                Transform3DGroup transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(rotateTransform);
                transformGroup.Children.Add(translateTransform);
                gridLineModel.Transform = transformGroup;
                ModelGroup.Children.Add(gridLineModel);
            }

            //===== Create BoardCell objects for each of the board cells.

            m_boardCells = new BoardCell[Board.Size, Board.Size];

            double halfCellExtent = CellExtent / 2;
            double left = -boardHalfExtent;
            double front = boardHalfExtent;

            for (int file = 0; file < Board.Size; ++file)
            {
                double fileStart = (Extent - GridLineWidth) * file / Board.Size;
                double x = (left  + fileStart) + halfCellExtent + GridLineWidth;

                for (int rank = 0; rank < Board.Size; ++rank)
                {
                    double rankStart = (Extent - GridLineWidth) * rank / Board.Size;
                    double y = (front - rankStart) - halfCellExtent - GridLineWidth;

                    Point center = new Point(x, y);
                    m_boardCells[file, rank] = new BoardCell(Board, file, rank, center, CellExtent);
                }
            }

            // TODO - Build a Model3D that contains both the board and grid line models,
            //        and use this in the TableView for hit testing.

            ApplyScheme(Scheme.Current);
        }
    }
}
