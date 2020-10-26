using System;
using System.Runtime.Serialization;

namespace STak.TakHub.Interop
{
    public class HubGameType
    {
        public const int None     = -0x0001;
        public const int Any      =  0x0000;
        public const int Public   =  0x0001;
        public const int Private  =  0x0002;
        public const int Ranked   =  0x0004;
        public const int Unranked =  0x0008;
        public const int Default  =  Public | Unranked;

        public int  Mask       { get; private set; }

        public bool IsNone     { get =>  (Mask             == -1);          }
        public bool IsAny      { get =>  (Mask             ==  0);          }
        public bool IsPublic   { get => ((Mask & Public  ) !=  0) || IsAny; }
        public bool IsPrivate  { get => ((Mask & Private ) !=  0) || IsAny; }
        public bool IsRanked   { get => ((Mask & Ranked  ) !=  0) || IsAny; }
        public bool IsUnranked { get => ((Mask & Unranked) !=  0) || IsAny; }

        public void MakeNone()     => Mask  = None;
        public void MakeAny()      => Mask  = Any;
        public void MakePublic()   => Mask |= (Public   & ~Private );
        public void MakePrivate()  => Mask |= (Private  & ~Public  );
        public void MakeRanked()   => Mask |= (Ranked   & ~Unranked);
        public void MakeUnranked() => Mask |= (Unranked & ~Ranked  );


        public HubGameType()
        {
        }


        public HubGameType(int mask)
        {
            Validate();
            Mask = mask;
        }


        public static bool IsMatch(HubGameType type1, HubGameType type2)
        {
            type1.Validate();
            type2.Validate();

            return (type1.IsAny || type2.IsAny ||
                 (((type1.IsPublic && type2.IsPublic) || (type1.IsPrivate  && type2.IsPrivate)) &&
                  ((type1.IsRanked && type2.IsRanked) || (type1.IsUnranked && type2.IsUnranked))));
        }


        public static HubGameType GetBestMatch(HubGameType type1, HubGameType type2)
        {
            type1.Validate();
            type2.Validate();

            HubGameType type = new HubGameType(HubGameType.None);

            if (IsMatch(type1, type2))
            {
                type.MakeAny();

                if (type1.IsAny)
                {
                    if (type2.IsPublic) { type.MakePublic(); } else { type.MakePrivate();  }
                    if (type2.IsRanked) { type.MakeRanked(); } else { type.MakeUnranked(); }
                }
                if (type2.IsAny)
                {
                    if (type1.IsPublic) { type.MakePublic(); } else { type.MakePrivate();  }
                    if (type1.IsRanked) { type.MakeRanked(); } else { type.MakeUnranked(); }
                }
                else
                {
                    if (type1.IsPublic && type2.IsPublic) { type.MakePublic(); } else { type.MakePrivate();  }
                    if (type1.IsRanked && type2.IsRanked) { type.MakeRanked(); } else { type.MakeUnranked(); }
                }
            }

            return type;
        }



        public static HubGameType Resolve(HubGameType gameType1, HubGameType gameType2)
        {
            int mask = 0;

            mask |= (gameType1.IsPrivate  || gameType2.IsPrivate)  ? HubGameType.Private  : HubGameType.Public;
            mask |= (gameType1.IsUnranked || gameType2.IsUnranked) ? HubGameType.Unranked : HubGameType.Ranked;

            return new HubGameType(mask);
        }


        private void Validate()
        {
            if ((Mask != Any) && (((Mask & (Public | Private)) == 0) || ((Mask & (Ranked | Unranked)) == 0)))
            {
                throw new Exception("Game type must be Public and/or Private, and Ranked and/or Unranked.");
            }
        }
    }
}
