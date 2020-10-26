using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using STak.TakEngine;

namespace STak.WinTak
{
    public class BoardCell
    {
        public Cell    Cell   { get; private set; }
        public Board   Board  { get; private set; }
        public Point   Center { get; private set; }
        public double  Extent { get; private set; }
        public Stack   Stack  { get; private set; }

        public Rect    Rect   => new Rect(Center.X - (Extent/2), Center.Y - (Extent/2), Extent, Extent);


        public BoardCell(Board board, int file, int rank, Point center, double extent)
        {
            Cell   = new Cell(file, rank);
            Board  = board;
            Center = center;
            Extent = extent;
            Stack  = board[file, rank];
        }


        public bool Contains(Point point, double factor = 1.0)
        {
            Rect   rect  = Rect;
            double width = rect.Width;

            rect.Width  *= factor;
            rect.Height *= factor;
            rect.X      += (width-rect.Width)  / 2;
            rect.Y      += (width-rect.Height) / 2;

            return rect.Contains(point);
        }


        public bool Intersects(Rect rect)
        {
            return Rect.IntersectsWith(rect);
        }
    }
}
