// Denis Labrecque
// May 1, 2018
// Project #8: Game Project

// This program contains the code for playing games of Minesweeper
using FoxtrotGame;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace FoxtroutGame
{
   public sealed partial class MainPage : Page
   {
      const string GAME_SIZE = "gameSizeLocal";
      const string GAME_DIFICULTY = "gameDifficultyLocal";
      const int DEFAULT_GAME_SIZE = 10;
      const int DEFAULT_GAME_DIFFICULTY = 1; // Index

      #region Fields
      static MainPage mainPage; // Allows fetching styles from this page's resources
      GameGrid minesweepGame; // The main game played by the player
      DispatcherTimer dispatcherTimer;
      Statistics statistics; // Statistics about all games played while the app is open
      int secondsElapsed;
      int minutesElapsed;
      ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
      #endregion

      #region Properties
      private int LocalGameSize {
         get {
            if(localSettings.Values[GAME_SIZE] == null)
            {
               localSettings.Values[GAME_SIZE] = DEFAULT_GAME_SIZE;
               return DEFAULT_GAME_SIZE;
            }
            else
            {
               return (int)localSettings.Values[GAME_SIZE];
            }
         }
         set {
            localSettings.Values[GAME_SIZE] = value;
         }
      }

      private int LocalGameDifficulty {
         get {
            if(localSettings.Values[GAME_DIFICULTY] == null)
            {
               localSettings.Values[GAME_DIFICULTY] = DEFAULT_GAME_DIFFICULTY;
               return DEFAULT_GAME_DIFFICULTY;
            }
            else
            {
               return (int)localSettings.Values[GAME_DIFICULTY];
            }
         }
         set {
            localSettings.Values[GAME_DIFICULTY] = value;
         }
      }
      #endregion

      #region Constructor
      // Initialize a new game with the default grid upon opening the app
      public MainPage()
      {
         this.InitializeComponent();
         mainPage = this;
         statistics = new Statistics();
         ShowPlayerStatistics();
         difficultySettings.SelectedIndex = LocalGameDifficulty;
         NewGame(LocalGameSize, LocalGameDifficulty, false);
         gridSize.Text = Convert.ToString(LocalGameSize);
      }
      #endregion

      #region Methods
      // Initialize a new game grid, erasing any previous grid if required
      private void NewGame(int gridSize, int difficultyIndex, bool erase = true)
      {
         minesweepGame = new GameGrid(gridSize, difficultyIndex);
         CreateGameBoard(erase);
         ShowIndicators();
         StartTimer();
      }

      // Use upon game activation to initialize the timer and show it to the user back at zero
      private void StartTimer()
      {
         secondsElapsed = 0;
         minutesElapsed = 0;
         dispatcherTimer = new DispatcherTimer {
            Interval = new TimeSpan(0, 0, 1)
         };
         dispatcherTimer.Tick += DispatcherTimer_Tick;
         dispatcherTimer.Start();
         timeIndicator.Text = "00:00";
      }

      // Create as many lists as necessary to fill the board according to the size specified
      void CreateGameBoard(bool erase = true)
      {
         // Erase any previous game
         if(erase)
         {
            for(int i = mainGameBoard.Children.Count; i > 0; i--)
            {
               mainGameBoard.Children.RemoveAt(i - 1);
            }
         }

         //        Main Game Board
         // column 0   column 1   column 2
         // tile 0,0   tile 1,0   tile 2,0
         // tile 0,1   tile 1,1   tile 2,1
         // tile 0,2   tile 1,2   tile 2,2

         // Create a new list view for each game board column
         for(int col = 0; col < minesweepGame.Width; col++)
         {
            ListView column = new ListView {
               Name = col.ToString(),
               SelectionMode = ListViewSelectionMode.None,
               IsItemClickEnabled = true
            };
            column.ItemClick += Column_ItemClick;
            column.IsTabStop = false;
            column.ItemContainerStyleSelector = new ColumnStyleSelector();

            // Fill the column with tile
            Tile[] columnTiles = new Tile[minesweepGame.Width];
            for(int row = 0; row < minesweepGame.Width; row++)
            {
               columnTiles[row] = minesweepGame.TileList[col, row];
            }
            column.ItemsSource = columnTiles;

            // Append the newly created column to the main game board (in order of creation)
            mainGameBoard.Children.Add(column);
         }
      }

      // Check every column in the game, and re-render that column if one of its tiles should be made visible. This method must be called
      // after every turn to empty the game grid of tiles that are revealed but not shown graphically
      private void RevealTilesToBeSeen()
      {
         // Go through the array of game tiles and find which columns must be re-rendered to the user
         for(int c = 0; c < minesweepGame.Width; c++)
         {
            // Go through each tile (ie. row) in a column
            for(int r = 0; r < minesweepGame.Width; r++)
            {
               // For a column that contains a tile that must be revealed, insert the newest data into the list view and re-render the list
               if(minesweepGame.TileList[c, r].IsRevealed == true && minesweepGame.TileList[c, r].IsShownGraphically == false)
               {
                  ListView listView = (ListView)mainGameBoard.Children[c];
                  Tile[] columnTiles = new Tile[minesweepGame.Width];

                  for(r = 0; r < minesweepGame.Width; r++)
                  {
                     if(minesweepGame.TileList[c, r].IsRevealed == true && minesweepGame.TileList[c, r].IsShownGraphically == false)
                     {
                        minesweepGame.TileList[c, r].IsShownGraphically = true;
                     }

                     columnTiles[r] = minesweepGame.TileList[c, r];
                  }

                  // Graphically insert the list view in the window
                  listView.ItemsSource = columnTiles;
               }
            }
         }
      }

      /// <summary>
      /// Check whether the game is won or lost and continue or end the game accordingly.
      /// </summary>
      /// <param name="tilesRevealed">Number of tiles yet revealed in the game</param>
      private void CheckGameCondition(int tilesRevealed)
      {
         if(minesweepGame.IsWon != null)
         {
            dispatcherTimer.Stop();

            // Lock every column from being clicked
            for(int col = 0; col < minesweepGame.Width; col++)
            {
               ListView column = (ListView)mainGameBoard.Children[col];
               column.IsItemClickEnabled = false;
            }

            // Do things based on whether the game was won or lost
            if(minesweepGame.IsWon == true)
            {
               // Play an ending sound
               soundPlayer.Source = new Uri("ms-appx:///Sound/winSound.wav");

               // Update objectives and statistics
               statistics.AddAGameWon(tilesRevealed, minesweepGame.NumberOfClicks, minesweepGame.DifficultyIndex, minesweepGame.Width, secondsElapsed + (minutesElapsed * 60));

               // Update statistics in the statistics panel
               ShowPlayerStatistics();

               // Notify the user when he reaches a new objective
               Objective objectiveReached = new Objective();
               if(statistics.IsNewBadgeEarned(out objectiveReached) == true)
               {
                  // Show a congratulatory message flyout
                  newBadgeIcon.Text = objectiveReached.Icon;
                  newBadgeTitle.Text = objectiveReached.Title;
                  newBadgeDescription.Text = objectiveReached.Description;
                  mainGrid.ContextFlyout.ShowAt(mainGrid);
                  
               }
            }
            else
            {
               // Play an losing sound
               soundPlayer.Source = new Uri("ms-appx:///Sound/loseSound.wav");

               // Update Statistics
               statistics.AddAGameLost(secondsElapsed + (minutesElapsed * 60));

               // Show statistics in the statistics panel
               ShowPlayerStatistics();
            }
         }

         // Statistics.FlushStatisticsAndObjectives(); // Put stats back to zero (for testing -- must play a game)
      }

      /// <summary>
      /// Show the user the average number of games played, won, lost, and time per game.
      /// </summary>
      private void ShowPlayerStatistics()
      {
         gamesPlayedStatistic.Text     = $"{statistics.GamesPlayed}";
         gamesWonStatistic.Text        = $"{statistics.GamesWon}";
         gamesLostStatistic.Text       = $"{statistics.GamesLost}";
         averageGameTimeStatistic.Text = statistics.AverageGameTime;
      }

      // Show the user how many tiles he has revealed and how many mines are in the game
      private void ShowIndicators()
      {
         revealedIndicator.Text = $"{minesweepGame.RevealedTiles}/{minesweepGame.NumberOfTiles}";
         minesIndicator.Text = $"M {minesweepGame.NumberOfMines}";
      }

      // Selects what style applies to a list view item according to that item's properties when it is rendered
      public class ColumnStyleSelector : StyleSelector
      {
         protected override Style SelectStyleCore(object item, DependencyObject container)
         {
            Tile tile = (Tile)item;

            if(tile.Type == (int)TileType.ExplodedMine)
            {
               return mainPage.Resources["TileExploded"] as Style;
            }
            else if(tile.IsRevealed == true && tile.IsShownGraphically == true)
            {
               switch(tile.AdjacentMines)
               {
                  case 0:
                     return mainPage.Resources["Tile0"] as Style;
                  case 1:
                     return mainPage.Resources["Tile1"] as Style;
                  case 2:
                     return mainPage.Resources["Tile2"] as Style;
                  case 3:
                     return mainPage.Resources["Tile3"] as Style;
                  case 4:
                     return mainPage.Resources["Tile4"] as Style;
                  default:
                     return mainPage.Resources["TileM"] as Style;
               }
            }
            else
            {
               return mainPage.Resources["ListViewItem"] as Style;
            }
         }
      }
      #endregion

      #region Events
      // MAIN GAME EVENT
      // Upon clicking a tile, find which tile has been clicked, and perform checks to either detonate a mine, or reveal more area.
      private void Column_ItemClick(object sender, ItemClickEventArgs e)
      {
         int columnNumber; // Column clicked
         int itemNumber; // Row clicked

         // Get the clicked list view and clicked tile
         ListView listView = (ListView)sender;
         Tile clickedTile = (Tile)e.ClickedItem;

         // Obtain the click position by the clicked list view and clicked tile
         columnNumber = int.Parse(listView.Name);
         itemNumber = listView.Items.IndexOf(clickedTile);

         // Perform checks to detonate a mine (loss), or reveal more area (possible win)
         minesweepGame.CheckMove(columnNumber, itemNumber, true);

         // Show the user updated information about winning or losing, and update objectives
         CheckGameCondition(minesweepGame.NumberOfTilesRevealed);

         // Graphically reveal the one clicked tile plus any other revealed tiles, and update game information
         RevealTilesToBeSeen();

         ShowIndicators();
      }

      // Updates the timer display after every second
      private void DispatcherTimer_Tick(object sender, object e)
      {
         secondsElapsed++;
         if(secondsElapsed > 59)
         {
            secondsElapsed = 0;
            minutesElapsed++;
         }
         timeIndicator.Text = String.Format("{0:00}", minutesElapsed) + ":" + string.Format("{0:00}", secondsElapsed);
      }

      // Create a new game based on selected game settings
      private void Reset_Click(object sender, RoutedEventArgs e)
      {
         dispatcherTimer.Stop(); // When the user starts a new game without winning or losing
         NewGame(LocalGameSize, difficultySettings.SelectedIndex);
         newGameSettings.Hide();
      }

      // Increase the size of the game grid by one (used when selecting how big to make the grid)
      private void IncrementGridSize_Click(object sender, RoutedEventArgs e)
      {
         if(LocalGameSize < GameGrid.MAX_GRID_WIDTH)
         {
            LocalGameSize += 1;
            gridSize.Text = Convert.ToString(LocalGameSize);
            if(LocalGameSize == GameGrid.MAX_GRID_WIDTH)
               incrementGridSize.IsEnabled = false;
            if(decrementGridSize.IsEnabled == false)
               decrementGridSize.IsEnabled = true;
         }
         else
         {
            incrementGridSize.IsEnabled = false;
         }
      }

      // Decrease the size of the game grid by one (used when selecting how big to make the grid)
      private void DecrementGridSize_Click(object sender, RoutedEventArgs e)
      {
         if(LocalGameSize > GameGrid.MIN_GRID_WIDTH)
         {
            LocalGameSize--;
            gridSize.Text = Convert.ToString(LocalGameSize);
            if(LocalGameSize == GameGrid.MIN_GRID_WIDTH)
               decrementGridSize.IsEnabled = false;
            if(incrementGridSize.IsEnabled == false)
               incrementGridSize.IsEnabled = true;
         }
         else
         {
            decrementGridSize.IsEnabled = false;
         }
      }
      #endregion

      private void DifficultySettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         LocalGameDifficulty = difficultySettings.SelectedIndex;
      }
   }
}
