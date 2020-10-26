using System;
using System.Collections.Generic;

namespace STak.WinTak
{
    public class RawMeshModel
    {
        public Dictionary<string, double> Constants  { get; set; }
        public List<double[]>             Vertices   { get; set; }
        public List<int[]>                Triangles  { get; set; }
        public List<double[]>             TextureMap { get; set; }
    }
}
