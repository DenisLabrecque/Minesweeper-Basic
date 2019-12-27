using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FoxtrotGame
{
   // Statistics about all games a user has played in one app session
   public class Statistics
   {
      const string GAMES_PLAYED = "gamesPlayed";
      const string GAMES_WON = "gamesWon";
      const string GAMES_LOST = "gamesLost";
      const string AVERAGE_SECONDS = "averageSeconds";
      const string NUMBER_TILES_REVEALED = "numberOfTilesRevealed";

      static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

      TrulyObservableCollection<Objective> objectives;
      ObservableCollection<Objective> badgesEarned = new ObservableCollection<Objective>();

      #region Constructor
      public Statistics()
      {
         InitializeAllObjectives();

         // Add all the badges that were already earned into a second list
         for(int i = 0; i < objectives.Count; i++)
         {
            if(objectives[i].AccomplishedNumberOfTimes >= 1.0f)
            {
               badgesEarned.Add(objectives[i]);
            }
         }
      }
      #endregion

      /// <summary>
      /// Create a new objective object for each objective that exists, according to each unique objective identifier.
      /// </summary>
      public void InitializeAllObjectives()
      {
         objectives = new TrulyObservableCollection<Objective> {
            new Objective(Objective.WIN_SINGLE_CLICK_GAME_OBJECTIVE),
            new Objective(Objective.WIN_DIFFICULT_GAME_OBJECTIVE),
            new Objective(Objective.WIN_DOCTORATE_GAME_OBJECTIVE),
            new Objective(Objective.WIN_SATURATION_GAME_OBJECTIVE),
            new Objective(Objective.REVEAL_100_GAME_OBJECTIVE),
            new Objective(Objective.REVEAL_1000_GAME_OBJECTIVE),
            new Objective(Objective.REVEAL_10000_GAME_OBJECTIVE)
         };
      }

      #region Properties
      public int GamesPlayed {
         get {
            // Update the number of games from local memory
            if(localSettings.Values[GAMES_PLAYED] == null)
            {
               return 0;
            }
            else
            {
               return (int)localSettings.Values[GAMES_PLAYED];
            }
         }

         private set {
            localSettings.Values[GAMES_PLAYED] = value;
         }
      }

      public int GamesWon {
         get {
            // Update the number of games from local memory
            if(localSettings.Values[GAMES_WON] == null)
            {
               return 0;
            }
            else
            {
               return (int)localSettings.Values[GAMES_WON];
            }
         }

         private set {
            localSettings.Values[GAMES_WON] = value;
         }
      }

      public int GamesLost {
         get {
            // Update the number of games from local memory
            if(localSettings.Values[GAMES_LOST] == null)
            {
               return 0;
            }
            else
            {
               return (int)localSettings.Values[GAMES_LOST];
            }
         }
         private set {
            localSettings.Values[GAMES_LOST] = value;
         }
      }

      /// <summary>
      /// Straight average seconds as an integer.
      /// </summary>
      public int AverageSeconds {
         get {
            if(localSettings.Values[AVERAGE_SECONDS] == null)
            {
               return 0;
            }
            else
            {
               return (int)localSettings.Values[AVERAGE_SECONDS];
            }
         }

         private set {
            localSettings.Values[AVERAGE_SECONDS] = value;
         }
      }

      /// <summary>
      /// Read average game time formatted as a string.
      /// </summary>
      public string AverageGameTime {
         get {
            int seconds = AverageSeconds;
            int minutes = 0;

            while(seconds >= 60)
            {
               minutes++;
               seconds -= 60;
            }

            return String.Format("{0:00}", minutes) + ":" + string.Format("{0:00}", seconds);
         }
      }

      /// <summary>
      /// Read the list of game objectives.
      /// </summary>
      public ObservableCollection<Objective> Objectives {
         get => objectives;
      }

      /// <summary>
      /// Test whether a new badge was earned after a game. This should be called after each game that is won. Updates the number of
      /// badges earned. TODO: will not show more than one objective won at once.
      /// </summary>
      public bool IsNewBadgeEarned(out Objective badgeMaybeEarned)
      {
         for(int i = 0; i < objectives.Count; i++)
         {
            if(objectives[i].AccomplishedNumberOfTimes >= 1.0f)
            {
               if(badgesEarned.Contains(objectives[i]) == false)
               {
                  badgeMaybeEarned = objectives[i];
                  badgesEarned.Add(objectives[i]);
                  return true;
               }
            }
         }

         // Else
         badgeMaybeEarned = null;
         return false;
      }
      #endregion

      /// <summary>
      /// Update the number of games played, lost, and average seconds after a game is won.
      /// </summary>
      /// <param name="tilesRevealed">The number of tiles revealed in the latest game</param>
      /// <param name="userClicks">The number of clicks used to play the latest game</param>
      /// <param name="difficultyIndex">The difficulty index used to play the latest game</param>
      /// <param name="gameSize">The width of the latest game</param>
      /// <param name="gameSeconds">The number of seconds the latest game took to play</param>
      public void AddAGameWon(int tilesRevealed, int userClicks, int difficultyIndex, int gameSize, int gameSeconds)
      {
         GamesPlayed += 1;
         GamesWon += 1;
         UpdateAverageSecondsFromNewGame(gameSeconds);

         // Update objectives
         foreach(Objective objective in objectives)
         {
            objective.UpdateNumberOfTimesAccomplished(this, tilesRevealed, userClicks, difficultyIndex, gameSize);
         }
      }

      /// <summary>
      /// Update the number of games played, lost, and average seconds after a game is lost.
      /// </summary>
      /// <param name="gameSeconds"></param>
      public void AddAGameLost(int gameSeconds)
      {
         GamesPlayed += 1;
         GamesLost += 1;
         UpdateAverageSecondsFromNewGame(gameSeconds);
      }

      /// <summary>
      /// Add to the number of average seconds by averaging this new game over the other new games.
      /// </summary>
      /// <param name="gameSeconds"></param>
      private void UpdateAverageSecondsFromNewGame(int gameSeconds)
      {
         if(GamesPlayed == 0 || AverageSeconds == 0)
         {
            AverageSeconds = gameSeconds;
         }
         else
         {
            AverageSeconds = ((AverageSeconds * (GamesPlayed - 1)) + gameSeconds) / GamesPlayed;
         }
      }

      /// <summary>
      /// For testing, add a new unknown objective to the list of objectives.
      /// </summary>
      public void AddNullObjective()
      {
         objectives.Add(new Objective());
      }

      /// <summary>
      /// For testing and cleaning, flush all statistical data from local memory by null assignment. Flush objectives from roaming, too.
      /// </summary>
      public static void FlushStatisticsAndObjectives()
      {
         localSettings.Values[GAMES_PLAYED] = null;
         localSettings.Values[GAMES_WON] = null;
         localSettings.Values[GAMES_LOST] = null;
         localSettings.Values[AVERAGE_SECONDS] = null;
         localSettings.Values[NUMBER_TILES_REVEALED] = null;

         Objective.FlushAllObjectivesFromRoaming();
      }

      /// <summary>
      /// Overridden string method returning games played, games won, games lost, and average time game statistics.
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         return $"Games played: {GamesPlayed}\nGames won: {GamesWon}\nGames lost: {GamesLost}";
      }
   }
}
