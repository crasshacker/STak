using System;
using System.Windows.Media.Media3D;

namespace STak.WinTak
{
    public interface IPositionable
    {
        void    SetPosition(Point3D point);
        Point3D GetPosition();
    }
}

