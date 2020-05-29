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
                        case GhostBehavior.Inky: InkyBehavior( ghost ); break;
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
            var direction = ghost.Direction;
            var (r, c) = NextPosition( ghost.CurrentRow, ghost.CurrentCol, direction );
            while( !IsValidMove( r, c ) || IsGhostGate( r, c ) ) {
                direction = ( Direction )( ( ( int )direction + 1 ) % 4 );
                (r, c) = NextPosition( ghost.CurrentRow, ghost.CurrentCol, direction );
            }
            ghost.PreviousCol = ghost.CurrentCol;
            ghost.PreviousRow = ghost.CurrentRow;
            ghost.CurrentRow = r;
            ghost.CurrentCol = c;
            ghost.Direction = direction;
        }

        static void BlinkyBehavior( Ghost ghost ) {
            //only execute every other frame
            if( FrameCount % 2 != 0 )
                return;

            //chase behavior
            //calc current position deltas
            int rowDelta = Math.Abs(ghost.CurrentRow - PacMan.CurrentRow);
            int colDelta = Math.Abs(ghost.CurrentCol - PacMan.CurrentCol);

            Direction direction = ghost.Direction;

            //not allowed to change directions North to South or East to West

            if( rowDelta > colDelta ) {
                //attempt to move north or south as required but only if i'm not already traveling North/South
                if( !( ghost.Direction == Direction.North || ghost.Direction == Direction.South ) )
                    direction = ghost.CurrentRow > PacMan.CurrentRow ? Direction.North : Direction.South;
            } else {
                if( !( ghost.Direction == Direction.East || ghost.Direction == Direction.West ) )
                    direction = ghost.CurrentCol > PacMan.CurrentCol ? Direction.West : Direction.East;
            }

            //go north if possible
            var (r, c) = NextPosition( ghost.CurrentRow, ghost.CurrentCol, direction );
            if( !IsValidMove( r, c ) || IsGhostGate( r, c ) ) {
                direction = ghost.Direction;
                while( true ) {
                    //try moving in the current direction
                    (r, c) = NextPosition( ghost.CurrentRow, ghost.CurrentCol, direction );
                    if( IsValidMove( r, c ) && !IsGhostGate( r, c ) )
                        break;
                    else {
                        //pick a new direction
                        switch( direction ) {
                            case Direction.North: direction = Direction.West; break;
                            case Direction.South: direction = Direction.East; break;
                            case Direction.East: direction = Direction.North; break;
                            case Direction.West: direction = Direction.South; break;
                        }
                    }
                }
            }

            ghost.PreviousCol = ghost.CurrentCol;
            ghost.PreviousRow = ghost.CurrentRow;
            ghost.Direction = direction;
            ghost.CurrentCol = c;
            ghost.CurrentRow = r;
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
            var c = Board[ row, col ];
            return !IsWall( c );
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
