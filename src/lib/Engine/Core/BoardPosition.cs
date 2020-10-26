using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    /// <summary>
    /// Represents a position in 2D "board space."
    /// </summary>
    /// <remarks>
    /// A BoardPosition represents a position in the 2D space formed by the X and Z axes, with height
    /// (the Y axis) ignored.  The board is considered to be a 1x1 square, with (0, 0) at the center
    /// of the board.  So for example, cell b3 of a 5x5 board would be centered at (-0.25, 0.0).
    /// </remarks>
    [Serializable]
    public record BoardPosition(double File, double Rank);
}
