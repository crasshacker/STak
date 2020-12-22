using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace STak.TakEngine
{
    public enum StoneType
    {
        None,
        Flat,
        Standing,
        Cap
    };


    [Serializable]
    public class Stone
    {
        public int       Id       { get; private set; }
        public int       PlayerId { get; init; }
        public StoneType Type     { get; set; }


        public Stone()
        {
        }


        public Stone(int playerId, StoneType stoneType, int stoneId = -1)
        {
            PlayerId = playerId;
            Type     = stoneType;
            Id       = stoneId;
        }


        public Stone Clone()
        {
            return new Stone(PlayerId, Type, Id);
        }


        public string GetNotation(bool verbose = false)
        {
            return (Type == StoneType.Standing) ? "S"
                 : (Type == StoneType.Cap)      ? "C"
                 : verbose                      ? "F"
                 : String.Empty;
        }


        //
        // Stones are maybe equal if they have the same player Id and stone type, and either have the same
        // Id or at least one of them has an Id of -1.  This allows a partial equality check in the case that
        // on of the stones is from a BitBoard, and thus is just comprised of a bit.  (The BitBoard doesn't
        // need stone Ids in its view of the board.)
        //
        public bool MaybeEquals(Stone stone)
        {
            bool maybeEquals = false;

            if (stone != null)
            {
                maybeEquals = ((PlayerId == stone.PlayerId && Type == stone.Type)
                               && (Id == stone.Id || Id == -1 || stone.Id == -1));
            }

            return maybeEquals;
        }


	public override bool Equals(object obj)
        {
            if      (ReferenceEquals(null, obj)) return false;
            else if (ReferenceEquals(this, obj)) return true;
            else if (obj.GetType() != GetType()) return false;
            else                                 return Equals(obj as Stone);
	}


        public bool Equals(Stone stone)
        {
            return stone != null
                && Id       == stone.Id
                && PlayerId == stone.PlayerId
                && Type     == stone.Type;
        }


        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap.
            {
                int hash = 17;
                hash = hash * 23 + Id;
                hash = hash * 23 + PlayerId;
                hash = hash * 23 + (int) Type;
                return hash;
            }
        }


        public override string ToString()
        {
            return String.Format("{0}/{1}/{2}", Id, PlayerId, Enum.GetName(typeof(StoneType), this.Type));
        }


        public static Stone FromString(string str)
        {
            string[] values = str.Split('/');

            int       stoneId   = Int32.Parse(values[0]);
            int       playerId  = Int32.Parse(values[1]);
            StoneType stoneType = (StoneType) Enum.Parse(typeof(StoneType), values[2]);

            return new Stone(playerId, stoneType, stoneId);
        }
    }
}
