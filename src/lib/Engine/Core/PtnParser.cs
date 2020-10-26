using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace STak.TakEngine
{
    public static class PtnParser
    {
        private static readonly Regex s_moveRegex;
        private static readonly Regex s_turnRegex;
        private static readonly Regex s_resultRegex;

        static PtnParser()
        {
            string moveStr = @"^\s*"
                           + @"(?<stoneCount>\d+)?"       // Optional stone count
                           + @"(?<stoneType1>[FSC])?"     // Optional stone type
                           + @"(?<stoneFile>[a-h])"       // File of stone placement or start of move
                           + @"(?<stoneRank>[0-9])"       // Rank of stone placement or start of move
                           + @"(?<direction>[<>\-\+])?"   // Optional direction, for stone moves only
                           + @"(?<dropCounts>\d+)?"       // Optional stones dropped on one or more squares
                           + @"(?<stoneType2>[FSC])?"     // Optional type of last stone dropped
                           + @"(?<info>[!*?'""]+)?"       // Optional informational marks
                           + @"(\s*(?<comment>\{.*\}))?"; // Optional comment

            string turnStr = @"^\s*"
                           + @"(?<turnNumber>\d+)\."       // Turn number
                           + @"\s+"
                           + @"(?<stoneCount1>\d+)?"       // Optional stone count
                           + @"(?<stoneType11>[FSC])?"     // Optional stone type
                           + @"(?<stoneFile1>[a-h])"       // File of stone placement or start of move
                           + @"(?<stoneRank1>[0-9])"       // Rank of stone placement or start of move
                           + @"(?<direction1>[<>\-\+])?"   // Optional direction, for stone moves only
                           + @"(?<dropCounts1>\d+)?"       // Optional stones dropped on one or more squares
                           + @"(?<stoneType12>[FSC])?"     // Optional type of last stone dropped
                           + @"(?<info1>[!*?'""]+)?"       // Optional informational marks
                           + @"(\s*(?<comment1>\{.*\}))?"  // Optional comment

                           + @"("                          // Optional Player 2 section (if game not ended)
                           + @"\s+"                        // Whitespace
                           + @"(?<stoneCount2>\d+)?"       // Optional stone count
                           + @"(?<stoneType21>[FSC])?"     // Optional stone type
                           + @"(?<stoneFile2>[a-h])"       // File of stone placement or start of move
                           + @"(?<stoneRank2>[0-9])"       // Rank of stone placement or start of move
                           + @"(?<direction2>[<>\-\+])?"   // Optional direction, for stone moves only
                           + @"(?<dropCounts2>\d+)?"       // Optional stones dropped on one or more squares
                           + @"(?<stoneType22>[FSC])?"     // Optional type of last stone dropped
                           + @"(?<info2>[!*?'""]+)?"       // Optional informational marks
                           + @"(\s*(?<comment2>\{.*\}))?"  // Optional comment
                           + @")?"                         // End optional player 2 section

                           + @"("                          // Optional Game end section
                           + @"\s+"                        // Whitespace
                           + @"(?<roadWin>R-0|0-R)?"       // Road win
                           + @"(?<flatWin>F-0|0-F)?"       // Flat win
                           + @"(?<timeWin>1-0|0-1)?"       // Time win (or resignation)
                           + @"(?<draw>1/2-1/2)?"          // Draw
                           + @")?";                        // End game end section

            string resultStr = @"("                        // Result header
                             + @"(?<roadWin>R-0|0-R)?"     // Road win
                             + @"(?<flatWin>F-0|0-F)?"     // Flat win
                             + @"(?<timeWin>1-0|0-1)?"     // Time win (or resignation)
                             + @"(?<draw>1/2-1/2)?"        // Draw
                             + @")";                       // End game end section

            s_moveRegex   = new Regex(moveStr,   RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            s_turnRegex   = new Regex(turnStr,   RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            s_resultRegex = new Regex(resultStr, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        }


        public static GameRecord ParseFile(string fileName)
        {
            return ParseText(File.ReadAllLines(fileName));
        }


        public static GameRecord ParseText(string ptn)
        {
            return ParseText(FormatMultiline(ptn));
        }


        public static GameRecord ParseText(IEnumerable<string> textLines)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            List<IMove>                moves   = new List<IMove>();
            GameResult                 result  = null;

            string blankLineStr  = @"^\s*$";
            string headerLineStr = @"^\[(?<tag>[A-Za-z0-9_]+)\s+""(?<value>.*)""\]\s*$";

            Regex blankLineRegex  = new Regex(blankLineStr,  RegexOptions.ExplicitCapture);
            Regex headerLineRegex = new Regex(headerLineStr, RegexOptions.ExplicitCapture);

            bool parsingHeader = true;
            PlayerReserve[] reserves = new PlayerReserve[2];
            int boardSize;

            foreach (string line in textLines)
            {
                if (parsingHeader || line.Trim().Length > 0)
                {
                    if (parsingHeader)
                    {
                        if (blankLineRegex.IsMatch(line))
                        {
                            parsingHeader = false;
                        }
                        else
                        {
                            Match match = headerLineRegex.Match(line);

                            if (match.Success)
                            {
                                GroupCollection groups = match.Groups;
                                string tag   = groups["tag"].Value;
                                string value = groups["value"].Value;
                                headers.Add(tag, value);

                                if (tag == "Size")
                                {
                                    boardSize = Int32.Parse(value);

                                    var player1 = new Player("Dummy");
                                    var player2 = new Player("Dummy");

                                    // Join a non-existent game to set the player Ids.
                                    player1.Join(null, Player.One);
                                    player2.Join(null, Player.Two);

                                    reserves = new PlayerReserve[2];
                                    reserves[Player.One] = new PlayerReserve(boardSize, player1);
                                    reserves[Player.Two] = new PlayerReserve(boardSize, player2);
                                }
                                else if (tag == "Result")
                                {
                                    WinType winType = WinType.None;
                                    int playerId = Player.None;

                                    if (value[0] == 'R' || value[2] == 'R')
                                    {
                                        winType  = WinType.Road;
                                        playerId = (value[0] == 'R') ? Player.One : Player.Two;
                                    }
                                    else if (value[0] == 'F' || value[2] == 'F')
                                    {
                                        winType  = WinType.Flat;
                                        playerId = (value[0] == 'F') ? Player.One : Player.Two;
                                    }
                                    else if (value[1] == '/')
                                    {
                                        winType  = WinType.Draw;
                                        playerId = Player.None;
                                    }
                                    else if (value[0] == '0' || value[0] == '1')
                                    {
                                        winType  = WinType.Time;
                                        playerId = (value[0] == '0') ? Player.One : Player.Two;
                                    }

                                    if (result != null)
                                    {
                                        throw new Exception("Found multiple game result header lines.");
                                    }

                                    result = new GameResult(playerId, winType);
                                }
                            }
                            else
                            {
                                throw new Exception($"Invalid line in PTN file header: {line}");
                            }
                        }
                    }
                    else
                    {
                        GameResult statedResult  = null;

                        foreach (IMove move in ParseTurn(line, reserves, out statedResult))
                        {
                            moves.Add(move);
                        }

                        if (statedResult != null)
                        {
                            if (result == null)
                            {
                                result = statedResult;
                            }
                            else if (result != null && result.Winner  != statedResult.Winner
                                                    && result.WinType != statedResult.WinType)
                            {
                                throw new Exception("Found multiple game result header lines.");
                            }
                        }
                    }
                }
            }

            return new GameRecord(headers, moves.ToArray(), result);
        }


        //
        // In the case of a StackMove that is in progress, with a stack having been grabbed but no movement
        // or stone dropping yet done, the PTN would not contain the direction character.  We use that character
        // to determine type of of move the PTN represents if moveType is null.  For this reason it's safest to
        // always explicitly pass the type of move when parsing moves in progress (or any time, really).
        //
        public static IMove ParseMove(int playerId, string movePtn, Type moveType = null)
        {
            IMove move = null;

            Match match = s_moveRegex.Match(movePtn);

            if (match.Success)
            {
                int[] dropCounts;
                Stone stone;
                Cell cell;

                GroupCollection groups = match.Groups;

                string dirNotationS = groups["direction"]?.Value;
                string stoneCountS  = groups["stoneCount"]?.Value;
                string stoneFileS   = groups["stoneFile"]?.Value;
                string stoneRankS   = groups["stoneRank"]?.Value;
                string stoneTypeS   = groups["stoneType1"]?.Value;

                int       stoneFile = Cell.ConvertFile(stoneFileS);
                int       stoneRank = Int32.Parse(stoneRankS)-1;
                StoneType stoneType = GetStoneType(stoneTypeS);

                bool isStoneMove = (moveType == null &&   String.IsNullOrEmpty(dirNotationS))
                                                     ||   moveType.Equals(typeof(StoneMove));
                bool isStackMove = (moveType == null && ! String.IsNullOrEmpty(dirNotationS))
                                                     ||   moveType.Equals(typeof(StackMove));

                if (isStoneMove)
                {
                    // A stone is being placed.
                    cell  = new Cell(stoneFile, stoneRank);
                    stone = new Stone(playerId, stoneType);
                    move  = new StoneMove(cell, stone);
                }
                else if (isStackMove)
                {
                    // A stack is being moved.
                    int stoneCount = String.IsNullOrEmpty(stoneCountS) ? 1 : Int32.Parse(stoneCountS);
                    Direction direction = new Direction(dirNotationS[0]);
                    dropCounts = GetDropCountArray(stoneCount, groups["dropCounts"].Value);
                    cell = new Cell(stoneFile, stoneRank);
                    move = new StackMove(cell, stoneCount, direction, dropCounts);
                }
                else
                {
                    throw new Exception("Cannot parse non-move type.");
                }
            }

            return move;
        }


        public static string ToString(IBasicGame game, string site, bool verbose = true, bool includeHeader = true,
                                                                                    bool includeUndoneMoves = true)
        {
            var writer = new StringWriter();
            Save(writer, game, site, verbose, includeHeader, includeUndoneMoves);
            return writer.ToString();
        }


        public static string ToString(GameRecord record)
        {
            var sb = new StringBuilder();

            sb.Append(FormatHeaders(record));
            sb.Append('\n');
            sb.Append(FormatMoves(record.Moves, null, true));

            return sb.ToString();
        }


        public static void Save(TextWriter writer, IBasicGame game, string site, bool verbose = true,
                                           bool includeHeader = true, bool includeUndoneMoves = true)
        {
            // We write the newline after each of player 2's moves explicitly in a string rather than using
            // writer.WriteLine, so we set the writer's newline to match so the header lines don't include
            // a carriage return with each newline.
            writer.NewLine = "\n";

            if (includeHeader)
            {
                foreach (var headerLine in GetHeader(game, site))
                {
                    writer.WriteLine(headerLine);
                }
                writer.WriteLine(""); // Header/Moves separator.
            }

            string result = game.IsCompleted ? FormatGameResult(game) : null;
            var moves = (includeUndoneMoves) ? game.ExecutedMoves.Concat(game.RevertedMoves).ToList()
                                             : game.ExecutedMoves.ToList();
            writer.Write(FormatMoves(moves, result, verbose));
        }


        public static void Save(string pathName, IBasicGame game, string site, bool verbose = true,
                                         bool includeHeader = true, bool includeUndoneMoves = true,
                                                                             bool overwrite = false)
        {
            FileMode fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
            using StreamWriter writer = new StreamWriter(new FileStream(pathName, fileMode, FileAccess.Write));
            Save(writer, game, site, verbose, includeHeader, includeUndoneMoves);
        }


        public static string FormatHeaders(GameRecord record)
        {
            var sb = new StringBuilder();
            int length = record.Headers.Select(h => h.Key.Length).Max();
            string text = String.Empty;

            foreach (var header in record.Headers)
            {
                string separator = new String(' ', (1 + length - header.Key.Length));
                sb.AppendFormat("[{0}{1}\"{2}\"]\n", header.Key, separator, header.Value);
            }

            return sb.ToString();
        }


        public static string FormatMoves(IList<IMove> moves, string formattedResult, bool verbose = false)
        {
            string text = String.Empty;

            for (int ply = 0; ply < moves.Count; ++ply)
            {
                text += (ply % 2 == 0) ? $"{ply / 2 + 1}. {moves[ply].ToString(verbose)}"
                                                     : $" {moves[ply].ToString(verbose)}\n";
            }

            if (formattedResult != null)
            {
                if (text[^1] == '\n')
                {
                    text = text[0..^1];
                }
                text += ' ' + formattedResult;
            }

            return text;
        }


        private static string FormatGameResult(IBasicGame game)
        {
            string result = null;

            if (game.IsCompleted)
            {
                if (game.Result.WinType == WinType.None)
                {
                    throw new Exception("Game is completed but win type is unknown.");
                }

                int winner = game.Result.Winner;

                result = game.Result.WinType switch
                {
                    WinType.Road => (winner == Player.One) ? "R-0" : "0-R",
                    WinType.Flat => (winner == Player.One) ? "F-0" : "0-F",
                    WinType.Time => (winner == Player.One) ? "1-0" : "0-1",
                    WinType.Draw => "1/2-1/2",
                               _ => null
                };
            }

            return result;
        }


        private static List<string> GetHeader(IBasicGame game, string site)
        {
            List<string> headerLines = new List<string>();

            DateTime dateTime = DateTime.UtcNow;
            string date = dateTime.ToString("yyyy.MM.dd");
            string time = dateTime.ToString("HH:mm:ss");
            WinType winType = game.Result.WinType;

            headerLines.Add($"[Date \"{date}\"]");
            headerLines.Add($"[Time \"{time}\"]");
            headerLines.Add($"[Player1 \"{game.PlayerOne.Name}\"]");
            headerLines.Add($"[Player2 \"{game.PlayerTwo.Name}\"]");
            headerLines.Add($"[Size \"{game.Board.Size}\"]");
            headerLines.Add($"[Site \"{site}\"]");

            if (winType != WinType.None)
            {
                int winner = game.Result.Winner;
                string result = (winType == WinType.Road && winner == Player.One) ? "R-0"
                              : (winType == WinType.Road && winner == Player.Two) ? "0-R"
                              : (winType == WinType.Flat && winner == Player.One) ? "F-0"
                              : (winType == WinType.Flat && winner == Player.Two) ? "0-F"
                              : (winType == WinType.Time && winner == Player.One) ? "1-0"
                              : (winType == WinType.Time && winner == Player.Two) ? "0-1"
                              : "1/2-1/2";

                headerLines.Add($"[Result \"{result}\"]");
            }

            return headerLines;
        }


        private static List<string> FormatMultiline(string ptn)
        {
            var formattedPtn = new List<string>();

            var regex1 = @"(\[\S+\s+""[^""]+""\]\s*)+(\b)";     // Header lines and blank separator line.
            var regex2 = @"((\d+\.\s+\S+)(\s+\S+\s*)?)+";       // Game move lines.

            var matches = Regex.Matches(ptn, regex1 + regex2);

            for (int i = 1; i < 4; ++i)
            {
                foreach (Capture capture in matches[0].Groups[i].Captures)
                {
                    formattedPtn.Add(capture.ToString());
                }
            }

            return formattedPtn;
        }


        private static IMove[] ParseTurn(string turnPtn, PlayerReserve[] reserves, out GameResult result)
        {
            result = null;

            IMove[] moves = new IMove[2];

            Match match = s_turnRegex.Match(turnPtn);

            if (match.Success)
            {
                int[] dropCounts;
                Stone stone;
                Cell cell;

                GroupCollection groups = match.Groups;

                string turnNumber   = groups["turnNumber"]?.Value;
                string dirNotationS = groups["direction1"]?.Value;
                string stoneCountS  = groups["stoneCount1"]?.Value;
                string stoneFileS   = groups["stoneFile1"]?.Value;
                string stoneRankS   = groups["stoneRank1"]?.Value;
                string stoneTypeS   = groups["stoneType11"]?.Value;

                int       turn      = Int32.Parse(turnNumber);
                int       stoneFile = Cell.ConvertFile(stoneFileS);
                int       stoneRank = Int32.Parse(stoneRankS)-1;
                StoneType stoneType = GetStoneType(stoneTypeS);

                if (String.IsNullOrEmpty(dirNotationS))
                {
                    // A stone is being placed.
                    cell = new Cell(stoneFile, stoneRank);
                    int playerId = (turn == 1) ? Player.Two : Player.One;
                    stone = reserves[playerId].DrawStone(stoneType);
                    moves[0] = new StoneMove(cell, stone);
                }
                else
                {
                    // A stack is being moved.
                    int stoneCount = String.IsNullOrEmpty(stoneCountS) ? 1 : Int32.Parse(stoneCountS);
                    Direction direction = new Direction(dirNotationS[0]);
                    dropCounts = GetDropCountArray(stoneCount, groups["dropCounts1"].Value);
                    cell = new Cell(stoneFile, stoneRank);
                    moves[0] = new StackMove(cell, stoneCount, direction, dropCounts);
                }

                // Process player two's move, if it's present.

                if (! String.IsNullOrEmpty(groups["stoneFile2"].Value))
                {
                    dirNotationS = groups["direction2"]?.Value;
                    stoneCountS  = groups["stoneCount2"]?.Value;
                    stoneFileS   = groups["stoneFile2"]?.Value;
                    stoneRankS   = groups["stoneRank2"]?.Value;
                    stoneTypeS   = groups["stoneType21"]?.Value;

                    stoneFile = Cell.ConvertFile(stoneFileS);
                    stoneRank = Int32.Parse(stoneRankS)-1;
                    stoneType = GetStoneType(stoneTypeS);

                    if (String.IsNullOrEmpty(dirNotationS))
                    {
                        // A stone is being placed.
                        cell = new Cell(stoneFile, stoneRank);
                        int playerId = (turn == 1) ? Player.One : Player.Two;
                        stone = reserves[playerId].DrawStone(stoneType);
                        moves[1] = new StoneMove(cell, stone);
                    }
                    else
                    {
                        // A stack is being moved.
                        int stoneCount = (stoneCountS != null && stoneCountS != "") ? Int32.Parse(stoneCountS) : 1;
                        Direction direction = new Direction(dirNotationS[0]);
                        dropCounts = GetDropCountArray(stoneCount, groups["dropCounts2"].Value);
                        cell = new Cell(stoneFile, stoneRank);
                        moves[1] = new StackMove(cell, stoneCount, direction, dropCounts);
                    }
                }

                string roadWinStr = groups["roadWin"].Value;
                string flatWinStr = groups["flatWin"].Value;
                string timeWinStr = groups["timeWin"].Value;
                string drawStr    = groups["draw"].Value;

                if (! (String.IsNullOrEmpty(roadWinStr) && String.IsNullOrEmpty(flatWinStr)
                    && String.IsNullOrEmpty(timeWinStr) && String.IsNullOrEmpty(drawStr)))
                {
                    // Default to the case of a draw, and update below.
                    int playerId = Player.None;
                    WinType winType = WinType.None;

                    if (! String.IsNullOrEmpty(roadWinStr))
                    {
                        playerId = (roadWinStr[0] == 'R') ? Player.One : Player.Two;
                        winType = WinType.Road;
                    }
                    else if (! String.IsNullOrEmpty(flatWinStr))
                    {
                        playerId = (flatWinStr[0] == 'F') ? Player.One : Player.Two;
                        winType = WinType.Flat;
                    }
                    else if (! String.IsNullOrEmpty(timeWinStr))
                    {
                        playerId = (timeWinStr[0] == '1') ? Player.One : Player.Two;
                        winType = WinType.Time;
                    }

                    result = new GameResult(playerId, winType);
                }
            }

            if (moves[1] == null)
            {
                moves = new IMove[] { moves[0] };
            }

            return moves;
        }


        private static int[] GetDropCountArray(int stoneCount, string dropCountStr)
        {
            int[] dropCounts = new int[] { stoneCount };

            if (! String.IsNullOrEmpty(dropCountStr))
            {
                int dropCount = Int32.Parse(dropCountStr);
                List<int> dropCountList = new List<int>();

                while (dropCount > 0)
                {
                    dropCountList.Insert(0, dropCount % 10);
                    dropCount /= 10;
                }

                dropCounts = dropCountList.ToArray();
            }

            return dropCounts;
        }


        private static StoneType GetStoneType(string stoneTypeStr)
        {
            return stoneTypeStr == "C" ? StoneType.Cap
                 : stoneTypeStr == "S" ? StoneType.Standing
                                       : StoneType.Flat;
        }
    }
}
