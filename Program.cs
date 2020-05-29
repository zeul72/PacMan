using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace PacMan {

    public enum Direction {
        North,
        South,
        East,
        West
    }

    class Character {
        public int CurrentRow;
        public int CurrentCol;
        public int PreviousRow;
        public int PreviousCol;
        public Direction Direction;
    }

    class PacMan : Character { }

    public enum GhostBehavior {
        Inky,
        Blinky,
        Pinky,
        Clyde
    }

    class Ghost : Character {
        public GhostBehavior Behavior;
        public char Glyph;                  //glyph that represents the Ghost
        public ConsoleColor Color;
        public bool InThePen = true;
        public int EscapeSeconds = 0;
    }

    //A* Node
    class Location {
        public int Row;
        public int Col;
        public int F;
        public int G;
        public int H;
        public Location Parent;
    }

    class Program {

        static readonly char GhostGate = '─';
        static readonly List<char> WallChars = new List<char> {'═','║','╔','╚','╗','╝','╦','╩'};
        static readonly List<char> PacManGlyphs = new List<char> { 'U', '∩', '≤', '≥' };
        static readonly Dictionary<char,ConsoleColor> Palette = new Dictionary<char, ConsoleColor> {
            { ' ', ConsoleColor.White },
            { '═', ConsoleColor.Blue },
            { '║', ConsoleColor.Blue },
            { '╔', ConsoleColor.Blue },
            { '╚', ConsoleColor.Blue },
            { '╗', ConsoleColor.Blue },
            { '╝', ConsoleColor.Blue },
            { '╦', ConsoleColor.Blue },
            { '╩', ConsoleColor.Blue },
            { '─', ConsoleColor.Green },
            { '*', ConsoleColor.White },
            { '■', ConsoleColor.DarkYellow },
            { '░', ConsoleColor.Magenta },
            { '▒', ConsoleColor.Cyan },
            { '▓', ConsoleColor.DarkCyan },
            { '█', ConsoleColor.DarkYellow },
        };


        static int FrameCount   = 0;
        static int GameSeconds  = 0;

        //Points Info
        static int Points = 0;
        //Board Info
        static char[,] Board;
        static int BoardRows;
        static int BoardCols;
        static int BoardTop = 5;
        static int BoardLeft = 5;
        static bool GameOver = false;

        static Character PacMan = new Character {
            CurrentCol = 14,
            CurrentRow = 21,
            PreviousCol = 14,
            PreviousRow = 21,
            Direction = Direction.West
        };

        static Ghost[] Ghosts  = new Ghost[] {
            new Ghost {
               CurrentCol = 11,
               PreviousCol = 11,
               CurrentRow = 13,
               PreviousRow = 13,
               Direction = Direction.West,
               Behavior = GhostBehavior.Blinky,
               Color = ConsoleColor.Magenta,
               Glyph = '▓',
               EscapeSeconds = 2
            },
             new Ghost {
               CurrentCol = 13,
               PreviousCol = 13,
               CurrentRow = 13,
               PreviousRow = 13,
               Direction = Direction.East,
               Behavior = GhostBehavior.Inky,
               Color = ConsoleColor.Red,
               Glyph = '▒',
               EscapeSeconds = 4
            }
        };

        static ConsoleKeyInfo? LastKey = null;
        static int PacManLogicLoops = 0;

        static void Main( ) {
            LoadBoard( "level01.board" );

            InitDisplay( );
            RunCountDown( );
            GameLoop( );
            Console.ReadLine( );
        }
        static void InitDisplay( ) {
            Console.SetWindowSize( 50, 50 );
            Console.BufferHeight = 50;
            Console.BufferWidth = 50;

            DrawBoard( );
            DrawPacMan( );
            DrawGhosts( );
        }

        static void RunCountDown( ) {
            for( int i = 5; i > 0; i-- ) {
                DrawAt( BoardTop + 15, BoardLeft + 14, ConsoleColor.Yellow, i.ToString( ) );
                Thread.Sleep( 1000 );
            }
            DrawAt( BoardTop + 15, BoardLeft + 14, ConsoleColor.Black, ' ' );
        }

        static void GameLoop( ) {

            while( true ) {
                PacManLogic( );
                GhostLogic( );
                DrawDots( );
                DrawGhosts( );
                DrawPacMan( );
                DrawPoints( );
                if( GameOver ) {
                    DrawAt( 4, 5, ConsoleColor.DarkBlue, "     G A M E    O V E R     " );
                    DrawAt( BoardTop + PacMan.CurrentRow, BoardLeft + PacMan.CurrentCol, ConsoleColor.DarkRed, 'X' );
                    break;
                }

                Thread.Sleep( 1000 / 10 );
                FrameCount++;
                GameSeconds = GameSeconds + ( FrameCount % 10 == 0 ? 1 : 0 );
                DrawFrameCount( );
                DrawGameSeconds( );
            };
        }

        static bool TryGetKey( out ConsoleKeyInfo? keyInfo ) {
            keyInfo = null;
            while( Console.KeyAvailable )
                keyInfo = Console.ReadKey( );
            return keyInfo != null;
        }

        static void PacManLogic( ) {

            int nextRow = PacMan.CurrentRow, nextCol = PacMan.CurrentCol;
            var nextDirection = PacMan.Direction;

            PacManLogicLoops += ( PacManLogicLoops + 1 ) % 3;
            if( PacManLogicLoops == 0 )
                LastKey = null;

            if( TryGetKey( out var consoleKeyInfo ) )
                LastKey = consoleKeyInfo;

            if( LastKey.HasValue ) {
                switch( LastKey.Value.Key ) {
                    case ConsoleKey.UpArrow:
                        (nextRow, nextCol) = NextPosition( PacMan.CurrentRow, PacMan.CurrentCol, Direction.North );
                        if( IsValidMove( nextRow, nextCol ) ) {
                            nextDirection = Direction.North;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        (nextRow, nextCol) = NextPosition( PacMan.CurrentRow, PacMan.CurrentCol, Direction.South );
                        if( IsValidMove( nextRow, nextCol ) ) {
                            nextDirection = Direction.South;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        (nextRow, nextCol) = NextPosition( PacMan.CurrentRow, PacMan.CurrentCol, Direction.East );
                        if( IsValidMove( nextRow, nextCol ) ) {
                            nextDirection = Direction.East;
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        (nextRow, nextCol) = NextPosition( PacMan.CurrentRow, PacMan.CurrentCol, Direction.West );
                        if( IsValidMove( nextRow, nextCol ) ) {
                            nextDirection = Direction.West;
                        }
                        break;
                }
            }

            PacMan.Direction = nextDirection;
            (nextRow, nextCol) = NextPosition( PacMan.CurrentRow, PacMan.CurrentCol, PacMan.Direction );
            if( IsValidMove( nextRow, nextCol ) && !IsGhostGate( nextRow, nextCol ) ) {
                PacMan.PreviousRow = PacMan.CurrentRow;
                PacMan.PreviousCol = PacMan.CurrentCol;
                PacMan.CurrentRow = nextRow;
                PacMan.CurrentCol = nextCol;
                if( IsPointPellet( PacMan.CurrentRow, PacMan.CurrentCol ) ) {
                    Points += 5;
                    Board[ PacMan.CurrentRow, PacMan.CurrentCol ] = ' ';
                }
            }
        }

        static void DrawBoard( ) {
            Console.SetWindowPosition( 0, 0 );
            for( int i = 0; i < BoardRows; i++ ) {
                for( int j = 0; j < BoardCols; j++ ) {
                    var c = Board[i,j];
                    DrawAt( BoardTop + i, BoardLeft + j, GetColor( c ), c );
                }
            }
        }

        static void DrawDots( ) {
            Console.SetWindowPosition( 0, 0 );
            for( int i = 0; i < BoardRows; i++ ) {
                for( int j = 0; j < BoardCols; j++ ) {
                    var c = Board[i,j];
                    if( c == '*' )
                        DrawAt( BoardTop + i, BoardLeft + j, GetColor( c ), c );
                }
            }
        }


        static void DrawPacMan( ) {
            DrawAt( BoardTop + PacMan.PreviousRow, BoardLeft + PacMan.PreviousCol, ConsoleColor.Black, ' ' );
            DrawAt( BoardTop + PacMan.CurrentRow, BoardLeft + PacMan.CurrentCol, ConsoleColor.Yellow, PacManGlyphs[ ( int )PacMan.Direction ] );
        }

        static void GhostLogic( ) {
            foreach( var ghost in Ghosts ) {
                if( ghost.InThePen ) {
                    if( ghost.EscapeSeconds < GameSeconds ) {
                        //time to try and get out!
                        var (r, c) = NextPosition( ghost.CurrentRow, ghost.CurrentCol, Direction.North );
                        if( IsValidMove( r, c ) ) {
                            ghost.PreviousCol = ghost.CurrentCol;
                            ghost.PreviousRow = ghost.CurrentRow;
                            ghost.CurrentRow = r - 1;
                            ghost.CurrentCol = c;
                            ghost.Direction = Direction.North;
                            ghost.InThePen = false;
                        } else {
                            Oscillate( ghost );
                        }
                    } else {
                        Oscillate( ghost );
                    }
                } else {
                    switch( ghost.Behavior ) {
                        case GhostBehavior.Inky:
                            InkyBehavior( ghost );
                            break;
                        case GhostBehavior.Blinky:
                            BlinkyBehavior( ghost );
                            break;
                        case GhostBehavior.Pinky: break;
                        case GhostBehavior.Clyde: break;
                    }
                }
                if( ghost.CurrentCol == PacMan.CurrentCol && ghost.CurrentRow == PacMan.CurrentRow ) {
                    GameOver = true;
                    break;
                }
            }
        }

        static void Oscillate( Ghost ghost ) {
            var (r, c) = NextPosition( ghost.CurrentRow, ghost.CurrentCol, ghost.Direction );
            if( !IsValidMove( r, c ) ) {
                ghost.Direction = ghost.Direction == Direction.East ? Direction.West : Direction.East;
                (r, c) = NextPosition( ghost.CurrentRow, ghost.CurrentCol, ghost.Direction );
            }
            ghost.PreviousCol = ghost.CurrentCol;
            ghost.PreviousRow = ghost.CurrentRow;
            ghost.CurrentRow = r;
            ghost.CurrentCol = c;
        }

        static void InkyBehavior( Ghost ghost ) {
            //only execute every other frame
            if( FrameCount % 2 != 0 )
                return;

            //target 4 tiles ahead of pac-man, unless ghost is within 4 tiles
            var targetRow = PacMan.CurrentRow;
            var targetCol = PacMan.CurrentCol;

            if( Math.Abs( targetRow - ghost.CurrentRow ) > 4 && Math.Abs( targetCol - ghost.CurrentCol ) > 4 ) {
                switch( PacMan.Direction ) {
                    case Direction.North:
                        targetRow -= 4;
                        if( targetRow < 0 )
                            targetRow = 0;
                        break;

                    case Direction.South:
                        targetRow += 4;
                        if( targetRow >= BoardRows )
                            targetRow = BoardRows - 1;
                        break;

                    case Direction.East:
                        targetCol += 4;
                        if( targetCol >= BoardCols )
                            targetCol = BoardCols - 1;
                        break;

                    case Direction.West:
                        targetCol -= 4;
                        if( targetCol < 0 )
                            targetCol = 0;
                        break;
                }
            }

            var (row, col, direction) = ComputeAStar( ghost.CurrentRow, ghost.CurrentCol, targetRow, targetCol );

            ghost.PreviousCol = ghost.CurrentCol;
            ghost.PreviousRow = ghost.CurrentRow;
            ghost.Direction = direction;
            ghost.CurrentRow = row;
            ghost.CurrentCol = col;
        }

        static void BlinkyBehavior( Ghost ghost ) {
            //only execute every other frame
            if( FrameCount % 2 != 0 )
                return;

            var (row, col, direction) = ComputeAStar( ghost.CurrentRow, ghost.CurrentCol, PacMan.CurrentRow, PacMan.CurrentCol );

            ghost.PreviousCol = ghost.CurrentCol;
            ghost.PreviousRow = ghost.CurrentRow;
            ghost.Direction = direction;
            ghost.CurrentRow = row;
            ghost.CurrentCol = col;
        }

        //A* Pathing
        static int ComputeHScore( int currentRow, int currentCol, int targetRow, int targetCol )
            => Math.Abs( targetRow - currentRow ) + Math.Abs( targetCol - currentCol );

        static (int row, int col, Direction direction) ComputeAStar( int currentRow, int currentCol, int targetRow, int targetCol ) {
            Location current = null;
            var start = new Location { Row = currentRow, Col = currentCol};
            var target = new Location{ Row = targetRow, Col = targetCol};
            var openList = new List<Location>();
            var closedList = new List<Location>();
            int g = 0;
            openList.Add( start );
            while( openList.Count > 0 ) {
                var lowest = openList.Min( k => k.F);
                current = openList.First( k => k.F == lowest );
                closedList.Add( current );
                openList.Remove( current );
                if( closedList.Any( k => k.Col == targetCol && k.Row == targetRow ) )
                    break;

                var adjacentSquares = GetWalkableAdjacentSquares(current.Row,current.Col);
                g++;
                foreach( var adjacentSquare in adjacentSquares ) {
                    if( closedList.Any( k => k.Row == adjacentSquare.Row && k.Col == adjacentSquare.Col ) )
                        continue;
                    if( !openList.Any( k => k.Row == adjacentSquare.Row && k.Col == adjacentSquare.Col ) ) {
                        adjacentSquare.G = g;
                        adjacentSquare.H = ComputeHScore( adjacentSquare.Row, adjacentSquare.Col, target.Row, target.Col );
                        adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                        adjacentSquare.Parent = current;
                        openList.Insert( 0, adjacentSquare );
                    } else {
                        if( g + adjacentSquare.H < adjacentSquare.F ) {
                            adjacentSquare.G = g;
                            adjacentSquare.F = adjacentSquare.G + adjacentSquare.H;
                            adjacentSquare.Parent = current;
                        }
                    }
                }
            }

            //walk it back to the child of the head of the list
            var node = current;
            while( node.Parent?.Parent != null )
                node = node.Parent;

            //get relative direction to current
            var direction = Direction.North;
            if( node.Row != currentRow )
                direction = node.Row > currentRow ? Direction.South : Direction.North;
            if( node.Col != currentCol )
                direction = node.Col > currentCol ? Direction.East : Direction.West;

            return (node.Row, node.Col, direction);
        }

        static List<Location> GetWalkableAdjacentSquares( int row, int col ) {
            var proposedLocations = new List<Location>() {
                new Location { Row = row, Col = col - 1, },
                new Location { Row = row, Col = col + 1  },
                new Location { Row = row - 1, Col = col },
                new Location { Row = row + 1, Col = col },
            };
            return proposedLocations.Where( k => IsValidMove( k.Row, k.Col ) && !IsGhostGate( k.Row, k.Col ) ).ToList( );
        }

        static void DrawGhosts( ) {
            foreach( var ghost in Ghosts ) {
                DrawAt( BoardTop + ghost.PreviousRow, BoardLeft + ghost.PreviousCol, ConsoleColor.Black, ' ' );
                DrawAt( BoardTop + ghost.CurrentRow, BoardLeft + ghost.CurrentCol, ghost.Color, ghost.Glyph );
            }
        }
        static void DrawPoints( ) => DrawAt( 4, 5, ConsoleColor.White, $"Points {Points}" );
        static void DrawFrameCount( ) => DrawAt( 0, 0, ConsoleColor.Green, $"Frame Count: {FrameCount}" );
        static void DrawGameSeconds( ) => DrawAt( 1, 0, ConsoleColor.Green, $"Game Seconds: {GameSeconds}" );
        static void DrawAt( int top, int left, ConsoleColor color, string text ) {
            Console.SetCursorPosition( left, top );
            Console.ForegroundColor = color;
            Console.Write( text );
        }
        static void DrawAt( int top, int left, ConsoleColor color, char c ) {
            Console.SetCursorPosition( left, top );
            Console.ForegroundColor = color;
            Console.Write( c );
        }

        static bool IsValidMove( int row, int col ) {
            if( row < 0 || row >= BoardRows ) return false;
            if( col < 0 || col >= BoardCols ) return false;
            return !IsWall( Board[ row, col ] );
        }

        static bool IsGhostGate( int row, int col ) => GhostGate == Board[ row, col ];

        static (int row, int col) NextPosition( int row, int col, Direction direction ) {
            switch( direction ) {
                case Direction.North:
                    row--;
                    if( row < 0 )
                        row = ( BoardRows - 1 );
                    break;
                case Direction.South:
                    row++;
                    if( row >= BoardRows )
                        row = 0;
                    break;
                case Direction.East:
                    col++;
                    if( col >= BoardCols )
                        col = 0;
                    break;
                case Direction.West:
                    col--;
                    if( col < 0 )
                        col = BoardCols - 1;
                    break;
            }
            return (row, col);
        }

        static bool IsPointPellet( int row, int col ) => Board[ row, col ] == '*';

        static bool IsWall( char c ) => WallChars.Any( k => k == c );



        static ConsoleColor GetColor( char c ) => Palette.ContainsKey( c ) ? Palette[ c ] : ConsoleColor.White;
        static void LoadBoard( string file ) {
            using var fs = File.OpenRead(file);
            using var sr = new StreamReader(fs);

            //1st line has board size
            (BoardRows, BoardCols) = ParseRowColData( sr.ReadLine( ) );

            Board = new char[ BoardRows, BoardCols ];

            for( int i = 0; i < BoardRows; i++ ) {
                var line = sr.ReadLine();
                for( int j = 0; j < line.Length; j++ ) {
                    Board[ i, j ] = line[ j ];
                }
            }
        }
        static (int rows, int cols) ParseRowColData( string data ) {
            var regEx = new Regex(@"cols=(?<cols>\d+),rows=(?<rows>\d+)");
            var match = regEx.Match(data);
            return (int.Parse( match.Groups[ "rows" ].Value ), int.Parse( match.Groups[ "cols" ].Value ));
        }
    }
}
