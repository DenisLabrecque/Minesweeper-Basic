// Denis Labrecque, Kara Funda
// April 30, 2018

using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace FoxtroutGame
{
   // This class contains the logic for a Minesweeper game. It expects a call to its move-checking method every time
   // the user clicks a new tile. This updates the class' information, and maintains the tile list, properties, and
   // game condition fields, to be then referenced by the caller for showing the turn's results.
   class GameGrid
   {
      #region Constants
      public static float[] DIFFICULTY_LEVELS = { 0.09f, 0.13f, 0.165f, 0.20f, 0.233f }; // Mines per number of tiles
      public const int MIN_GRID_WIDTH = 3;
      public const int MAX_GRID_WIDTH = 25;
      #endregion

      #region Fields
      private bool? isWon = null;
      private bool isFirstClick; // Mines must be initialized after the first click, because the first click is safe
      private bool newTurn = true; // Control indicating when passes to reveal side-touching tiles are complete
      private int gridWidth;
      private int numberOfTiles;
      private int numberOfMines;
      private int tilesRevealed; // X number of tiles revealed badges
      private int userClicks; // First-click badge
      private int difficultyIndex; // Win games by difficulty and game size
      private Tile[,] tiles; // List of tiles by column and row

      // Tile coordinates that have not been verified for touching blanks
      private HashSet<Coordinate> revealUnchecked = new HashSet<Coordinate>();
      // Tile coordinates that have been verified for touching blanks
      private HashSet<Coordinate> revealChecked = new HashSet<Coordinate>();

      // Series of tile coordinates around any given tile
      int[,] allTouchingTiles = new int[,] {
            { -1, +1 }, { 0, +1 }, {+1, +1 },
            { -1,  0 },            {+1,  0 },
            { -1, -1 }, { 0, -1 }, {+1, -1 }
         };

      // Series of side-touching tile coordinates around any given tile
      int[,] sideTouchingTiles = new int[,] {
                        { 0, +1 },
            { -1,  0 },            {+1,  0 },
                        { 0, -1 }
         };
      #endregion

      #region Properties
      public int Width {
         get => gridWidth;
      }

      public Tile[,] TileList {
         get => tiles;
      }

      public bool IsFirstClick {
         get => isFirstClick;
      }

      public HashSet<Coordinate> RevealChecked {
         get => revealChecked;
      }

      public bool? IsWon {
         get => isWon;
      }

      public int RevealedTiles {
         get => CheckWin();
      }

      public int NumberOfTiles {
         get => numberOfTiles;
      }

      public int NumberOfMines {
         get => numberOfMines;
      }

      public int NumberOfTilesRevealed {
         get => tilesRevealed;
      }

      public int NumberOfClicks {
         get => userClicks;
      }

      public int DifficultyIndex {
         get => difficultyIndex;
      }
      #endregion

      #region Constructor
      public GameGrid(int gridSize, int difficultyIndex)
      {
         if(gridSize < MIN_GRID_WIDTH)
         {
            throw new ArgumentOutOfRangeException("A game grid cannot be less than " + MIN_GRID_WIDTH + " tiles wide.");
         }
         else if(gridSize > MAX_GRID_WIDTH)
         {
            throw new ArgumentOutOfRangeException("A game grid cannot be more than " + MAX_GRID_WIDTH + " tiles wide.");
         }
         else
         {
            gridWidth = gridSize;
         }

         tiles = new Tile[gridWidth, gridWidth];
         numberOfTiles = gridWidth * gridWidth;
         InitializeBlankGame(); // Initialize all game tiles as blank

         if(difficultyIndex >= 0 && difficultyIndex <= DIFFICULTY_LEVELS.Length)
         {
            numberOfMines = (int)(numberOfTiles * DIFFICULTY_LEVELS[difficultyIndex]);
         }
         else
         {
            throw new IndexOutOfRangeException($"There are {DIFFICULTY_LEVELS.Length} levels of difficulty.");
         }

         // There must be at least 1 mine (for smaller grid sizes)
         if(numberOfMines == 0)
         {
            numberOfMines = 1;
         }
         
         isFirstClick = true;
         tilesRevealed = 0;
         userClicks = 0;
         this.difficultyIndex = difficultyIndex;
      }
      #endregion

      #region Constructor Helper Methods
      // Initialize all game tiles as blank
      private void InitializeBlankGame()
      {
         // Fill the tile array
         for(int col = 0; col < gridWidth; col++)
         {
            for(int row = 0; row < gridWidth; row++)
            {
               tiles[col, row] = new Tile();
            }
         }
      }
      #endregion

      #region Methods
      // MAIN GAME METHOD
      // Initialize all tiles on a first click, and check whether the game is won or lost on every click;
      // must be called at every tile click.
      public void CheckMove(int clickColumn, int clickRow, bool addClick = false)
      {
         if(addClick == true)
         {
            userClicks++;
         }

         if(tiles[clickColumn, clickRow].IsRevealed == false) {
            if(isFirstClick)
            {
               isFirstClick = false;
               InitializeMineTiles(clickColumn, clickRow);
               InitializeNumberedTiles(); // Number tiles by the number of mines they touch
               CheckMove(clickColumn, clickRow);
            }
            else
            {
               // Flag this tile as one which needs to be revealed graphically
               tiles[clickColumn, clickRow].IsRevealed = true;

               // Find what type this tile is
               switch(tiles[clickColumn, clickRow].Type)
               {
                  // Click a mine
                  case (int)TileType.Mine:
                     // Game lost
                     tiles[clickColumn, clickRow].Type = (int)TileType.ExplodedMine;
                     isWon = false;
                     RevealAllMines();
                     break;

                  // Click a numbered tile
                  case (int)TileType.Numbered:
                     tilesRevealed++;
                     CheckWin();
                     break;

                  // Click a blank tile
                  case (int)TileType.Blank:
                     tilesRevealed++;
                     // Reveal adjacent tiles
                     newTurn = true;
                     FindTouchingBlankTiles(clickColumn, clickRow);
                     CheckWin();
                     break;

                  default:
                     throw new Exception("Unknown tile type clicked.");
               }
            }
         }
      }

      // Find and return the number of tiles that have been shown to the user
      private int CheckWin()
      {
         int revealedTiles = 0;

         // Go through all tiles to find the number of tiles revealed
         for(int col = 0; col < gridWidth; col++)
         {
            for(int row = 0; row < gridWidth; row++)
            {
               if(tiles[col, row].IsRevealed == true && (tiles[col, row].Type == (int)TileType.Blank || tiles[col, row].Type == (int)TileType.Numbered))
               {
                  revealedTiles++;
               }
            }
         }

         if(revealedTiles + numberOfMines == numberOfTiles)
         {
            isWon = true;
            RevealAllMines();
         }

         return revealedTiles;
      }

      // Randomly choose tiles that are mines, except the first clicked tile, which is always safe.
      private void InitializeMineTiles(int firstClickColumn, int firstClickRow)
      {
         Random random = new Random();
         int column;
         int row;

         // For as many mines as there should be, add mines in random grid locations
         for(int i = 1; i <= numberOfMines; i++)
         {
            do
            {
               column = random.Next(0, gridWidth);
               row = random.Next(0, gridWidth);
            }
            while((column == firstClickColumn && row == firstClickRow) || tiles[column, row].Type == (int)TileType.Mine);

            tiles[column, row].Type = (int)TileType.Mine;
         }
      }

      // Number tiles adjacent to mines with the number of adjacent mines
      private void InitializeNumberedTiles()
      {
         // Go through each game board tile
         for(int col = 0; col < gridWidth; col++)
         {
            for(int row = 0; row < gridWidth; row++)
            {
               // Change the tiles that are adjacent to mines to have numbers
               for(int adj = 0; adj < allTouchingTiles.Length; adj++)
               {
                  try
                  {
                     if(tiles[col + allTouchingTiles[adj, 0], row + allTouchingTiles[adj, 1]].Type == (int)TileType.Mine
                        && tiles[col, row].Type != (int)TileType.Mine)
                     {
                        tiles[col, row].Type = (int)TileType.Numbered;
                        tiles[col, row].AdjacentMines++;
                     }
                  }
                  catch(IndexOutOfRangeException) {
                     // Ignore out of range
                  }
               }
            }
         }
      }

      // Find all connected side-touching blank tiles, from a certain coordinate, recursively, and list them.
      // This method requires a boolean control variable that delimits each new turn.
      private void FindTouchingBlankTiles(int columnPosition, int rowPosition)
      {
         // Create a new coordinate according to the passed information
         Coordinate coordinate = new Coordinate(columnPosition, rowPosition);

         // Stop recursion when all side-touching blank tiles have been found
         if(!newTurn)
         {
            return;
         }
         // If the coordinate was already checked, go recursively through the list of tiles to be revealed to see whether more
         // tiles can be added. If no more tiles can be found, all connected tiles are found: reveal them.
         else if(revealChecked.Contains(coordinate))
         {
            // Be sure this item was removed from the other list
            revealUnchecked.Remove(coordinate);

            // All tiles were found; end method calls
            if(revealUnchecked.Count == 0)
            {
               Coordinate[] coordinates = new Coordinate[revealChecked.Count];
               revealChecked.CopyTo(coordinates);

               // Reveal all connected tiles that were found
               for(int i = 0; i < coordinates.Length; i++)
               {
                  tiles[coordinates[i].Column, coordinates[i].Row].IsRevealed = true;
               }

               // Show all numbered tiles near the list of tiles to reveal
               ShowNumberedTilesFromTouchingTiles();

               // Flush list data in preparation for the next turn
               revealChecked = new HashSet<Coordinate>();
               revealUnchecked = new HashSet<Coordinate>();

               // Stop execution of this method
               newTurn = false;
               return;
            }
            else
            {
               // Go recursively through the list of tiles to be revealed
               Coordinate[] revealUncheckedArray = new Coordinate[revealUnchecked.Count]; // Copy the hash set to a temporary array for enumeration
               revealUnchecked.CopyTo(revealUncheckedArray);
               for(int i = 0; i < revealUncheckedArray.Length; i++)
               {
                  FindTouchingBlankTiles(revealUncheckedArray[i].Column, revealUncheckedArray[i].Row);
               }
            }
         }

         // If the coordinate was not already checked, add blank tiles touching it in the list to be revealed
         else
         {
            revealChecked.Add(coordinate);
            tilesRevealed++;

            // Go through the list of tiles with sides touching and add them to the list of tiles to be revealed
            for(int i = 0; i < sideTouchingTiles.Length; i++)
            {
               try
               {
                  // Only add a tile to the reveal list if it is blank
                  if(tiles[columnPosition + sideTouchingTiles[i, 0], rowPosition + sideTouchingTiles[i, 1]].Type == (int)TileType.Blank)
                  {
                     revealUnchecked.Add(new Coordinate(columnPosition + sideTouchingTiles[i, 0], rowPosition + sideTouchingTiles[i, 1]));
                  }
               }
               catch(IndexOutOfRangeException) { }
            }

            // Make the first part execute
            FindTouchingBlankTiles(columnPosition, rowPosition);
         }
      }
      
      // Once all connected blank tiles have been revealed, this method will show their peripheral numbered tiles
      private void ShowNumberedTilesFromTouchingTiles()
      {
         // Go through the list of connected blank tiles with touching sides
         foreach(Coordinate coord in revealChecked)
         {
            // Go around the eight positions about a tile
            for(int i = 0; i < allTouchingTiles.Length; i++)
            {
               try
               {
                  if(tiles[coord.Column + allTouchingTiles[i, 0], coord.Row + allTouchingTiles[i, 1]].Type == (int)TileType.Numbered)
                  {
                     tiles[coord.Column + allTouchingTiles[i, 0], coord.Row + allTouchingTiles[i, 1]].IsRevealed = true;
                  }
               }
               catch(IndexOutOfRangeException) { }
            }
         }
      }

      // List all tiles that should be shown
      private void RevealAllTiles()
      {
         // Make every game tile visible
         for(int col = 0; col < Width; col++)
         {
            for(int row = 0; row < Width; row++)
            {
               tiles[col, row].IsRevealed = true;
            }
         }
      }

      // Show all mine game tiles
      private void RevealAllMines()
      {
         // Go through every game tile
         for(int col = 0; col < Width; col++)
         {
            for(int row = 0; row < Width; row++)
            {
               if(tiles[col, row].Type == (int)TileType.Mine || tiles[col, row].Type == (int)TileType.ExplodedMine)
               {
                  tiles[col, row].IsRevealed = true;
               }
            }
         }
      }

      // Return whether indices are suitable for a column-row grid
      private bool IsValidCoordinate(int columnIndex, int rowIndex)
      {
         if(columnIndex >= 0 && rowIndex >= 0 && columnIndex < gridWidth && rowIndex < gridWidth)
         {
            return true;
         }
         else
         {
            return false;
         }
      }

      // Overridden string method that returns basic information about a minesweeper game
      public override string ToString()
      {
         string gameInformation = string.Empty;

         gameInformation +=   $"Game Size: {gridWidth}x{gridWidth}";
         gameInformation += $"\nMines:     {numberOfMines}";
         if(isWon == true)
         {
            gameInformation += $"\nGame Won?: Yes";
         }
         else if(isWon == false)
         {
            gameInformation += $"\nGame Won?: Lost";
         }
         else
         {
            gameInformation += $"\nGame Won: Undetermined";
         }

         return gameInformation;
      }
      #endregion
   }

   // Contains a column and row coordinate for storing tile coordinates on a column-row grid.
   struct Coordinate
   {
      #region Fields
      int column;
      int row;
      #endregion

      #region Properties
      public int Column {
         get => column;
      }

      public int Row {
         get => row;
      }
      #endregion

      #region Constructor
      public Coordinate(int columnIndex, int rowIndex)
      {
         if(columnIndex >= 0 && rowIndex >= 0)
         {
            column = columnIndex;
            row = rowIndex;
         }
         else
         {
            throw new IndexOutOfRangeException("Column and row indices for a coordinate cannot be negative.");
         }
      }
      #endregion
   }
}
