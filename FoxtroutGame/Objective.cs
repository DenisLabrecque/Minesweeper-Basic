using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FoxtrotGame
{
   public class Objective : IEquatable<Objective>, INotifyPropertyChanged
   {
      #region Roaming ID Constants
      private const string THIS_DONE_NUMBER_OF_TIMES      = "numberOfTimes"; // Combine to this objective's ID to create a unique roaming tag
      public const string WIN_DIFFICULT_GAME_OBJECTIVE    = "winDifficultGameChallenge";
      public const string WIN_DOCTORATE_GAME_OBJECTIVE    = "winDoctorateGameChallenge";
      public const string WIN_SATURATION_GAME_OBJECTIVE   = "winSaturationGameChallenge";
      public const string REVEAL_1000_GAME_OBJECTIVE      = "reveal1000Challenge";
      public const string REVEAL_100_GAME_OBJECTIVE       = "reveal100gameChallenge";
      public const string REVEAL_10000_GAME_OBJECTIVE     = "reveal10000gameChallenge";
      public const string WIN_SINGLE_CLICK_GAME_OBJECTIVE = "winSingleClickChallenge";
      #endregion

      #region Fields
      string icon;
      string title;
      string description;
      string identifier;
      static ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

      public event PropertyChangedEventHandler PropertyChanged;
      #endregion

      #region Constructors
      /// <summary>
      /// Constructor without any information. Every field is set to unknown or zero, and a random identifier is created.
      /// </summary>
      public Objective()
      {
         icon        = "!";
         title       = "Unknown";
         description = "No description";
         identifier  = new Guid().ToString();
      }

      /// <summary>
      /// Standard constructor for an objective; it generates the objective icon, title, and description according to the constant
      /// identifier passed as an argument, and retrieves the number of times this objective was achieved from roaming.
      /// </summary>
      /// <param name="objectiveIdentifier">Constant program objective identifier which constructs an objective based on a switch.</param>
      public Objective(string objectiveIdentifier)
      {
         // String resources
         var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

         // Flip between the objective being created
         switch(objectiveIdentifier)
         {
            case WIN_SINGLE_CLICK_GAME_OBJECTIVE:
               icon = "🍀";
               title = resourceLoader.GetString("winSingleClickObjectiveTitle");
               description = resourceLoader.GetString("winSingleClickObjectiveDescription");
               identifier = WIN_SINGLE_CLICK_GAME_OBJECTIVE;
               break;

            case WIN_DIFFICULT_GAME_OBJECTIVE:
               icon = "👨‍💼";
               title = resourceLoader.GetString("winDifficultObjectiveTitle");
               description = resourceLoader.GetString("winDifficultObjectiveDescription");
               identifier = WIN_DIFFICULT_GAME_OBJECTIVE;
               break;

            case WIN_DOCTORATE_GAME_OBJECTIVE:
               icon = "👨‍🎓";
               title = resourceLoader.GetString("winDoctorateObjectiveTitle");
               description = resourceLoader.GetString("winDoctorateObjectiveDescription");
               identifier = WIN_DOCTORATE_GAME_OBJECTIVE;
               break;

            case WIN_SATURATION_GAME_OBJECTIVE:
               icon = "🕵️";
               title = resourceLoader.GetString("winSaturationObjectiveTitle");
               description = resourceLoader.GetString("winSaturationObjectiveDescription");
               identifier = WIN_SATURATION_GAME_OBJECTIVE;
               break;

            case REVEAL_100_GAME_OBJECTIVE:
               icon = "🌱";
               title = resourceLoader.GetString("reveal100ObjectiveTitle");
               description = resourceLoader.GetString("reveal100ObjectiveDescription");
               identifier = REVEAL_100_GAME_OBJECTIVE;
               break;

            case REVEAL_1000_GAME_OBJECTIVE:
               icon = "🐣";
               title = resourceLoader.GetString("reveal1000ObjectiveTitle");
               description = resourceLoader.GetString("reveal1000ObjectiveDescription");
               identifier = REVEAL_1000_GAME_OBJECTIVE;
               break;

            case REVEAL_10000_GAME_OBJECTIVE:
               icon = "🌈";
               title = resourceLoader.GetString("reveal10000ObjectiveTitle");
               description = resourceLoader.GetString("reveal10000ObjectiveDescription");
               identifier = REVEAL_10000_GAME_OBJECTIVE;
               break;

            default:
               icon = "!";
               title = "Unknown";
               description = "Error constructing objective.";
               identifier = new Guid().ToString();
               break;
         }
      }
      #endregion

      #region Properties
      /// <summary>
      /// Gets the objective title; bound to XAML.
      /// </summary>
      public string Title {
         get => title;
      }

      /// <summary>
      /// The unique constant identifier of a game objective.
      /// </summary>
      public string Identifier {
         get => identifier;
      }

      /// <summary>
      /// Gets the objective description; bound to XAML.
      /// </summary>
      public string Description {
         get => description;
      }

      /// <summary>
      /// Gets the emoji associated with an objective. This binds to the XAML.
      /// </summary>
      public string Icon {
         get => icon;
      }

      /// <summary>
      /// Property bound to the XAML that shows the objective as a hard colour if complete, or faded if not.
      /// Updates the number of times an objective has been achieved.
      /// </summary>
      public float Opacity {
         get {
            float timesAchieved = AccomplishedNumberOfTimes;
            if(timesAchieved < 1.0f)
            {
               return 0.40f;
            }
            else
            {
               return 1.0f;
            }
         }
      }

      /// <summary>
      /// Updates and gets or sets and updates the number of times this objective was reached, based on roaming memory.
      /// </summary>
      public float AccomplishedNumberOfTimes {
         get {
            if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
            {
               return 0.0f;
            }
            else
            {
               return (float)(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES]);
            }
         }

         private set {
            roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = value;
         }
      }
      #endregion

      #region Methods
      /// <summary>
      /// Updates the object property containing how many times a certain achievement was reached. Must be called at the end of each game.
      /// Contains logic specific to each objective.
      /// </summary>
      /// <param name="sender">The calling class.</param>
      /// <param name="revealedTiles">The number of tiles revealed in a completed game.</param>
      /// <param name="userClicks">The number of clicks the user took to complete a game.</param>
      /// <param name="difficultyIndex">The difficulty index of a completed game.</param>
      /// <param name="gameSize">The width of a completed game board.</param>
      /// <param name="objectives">A reference to the list of objectives being affected by this operation.</param>
      public void UpdateNumberOfTimesAccomplished(Statistics sender, int revealedTiles, int userClicks, int difficultyIndex, int gameSize)
      {
         // Find which objective is being checked for completion and set it as complete if so.
         switch(identifier)
         {
            case WIN_SINGLE_CLICK_GAME_OBJECTIVE:
               if(userClicks == 1)
               {
                  if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
                  {
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = 1.0f;
                  }
                  else
                  {
                     float temp = (float)roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES];
                     temp += 1.0f;
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = temp;
                  }
               }
               break;

            case WIN_DIFFICULT_GAME_OBJECTIVE:
               if(difficultyIndex == 2 && gameSize >= 10)
               {
                  if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
                  {
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = 1.0f;
                  }
                  else
                  {
                     float temp = (float)roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES];
                     temp += 1.0f;
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = temp;
                  }
               }
               break;

            case WIN_DOCTORATE_GAME_OBJECTIVE:
               if(difficultyIndex == 3 && gameSize >= 10)
               {
                  if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
                  {
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = 1.0f;
                  }
                  else
                  {
                     float temp = (float)roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES];
                     temp += 1.0f;
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = temp;
                  }
               }
               break;

            case WIN_SATURATION_GAME_OBJECTIVE:
               if(difficultyIndex == 4 && gameSize >= 10)
               {
                  if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
                  {
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = 1.0f;
                  }
                  else
                  {
                     float temp = (float)roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES];
                     temp += 1.0f;
                     roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = temp;
                  }
               }
               break;

            case REVEAL_100_GAME_OBJECTIVE:
               if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
               {
                  roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = (float)(revealedTiles / 100.0f);
               }
               else
               {
                  float temp = (float)roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES];
                  temp += (revealedTiles / 100.0f);
                  roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = temp;
               }
               break;

            case REVEAL_1000_GAME_OBJECTIVE:
               if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
               {
                  roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = (float)(revealedTiles / 1000.0f);
               }
               else
               {
                  float temp = (float)roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES];
                  temp += (revealedTiles / 1000.0f);
                  roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = temp;
               }
               break;

            case REVEAL_10000_GAME_OBJECTIVE:
               if(roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] == null)
               {
                  roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = (float)(revealedTiles / 10000.0f);
               }
               else
               {
                  float temp = (float)roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES];
                  temp += (revealedTiles / 10000.0f);
                  roamingSettings.Values[identifier + THIS_DONE_NUMBER_OF_TIMES] = temp;
               }
               break;

            default:
               break;
         }

         // Force the list of objectives to update
         //for(int i = 0; i < objectives.Count; i++)
         //{
         //   if(objectives[i].AccomplishedNumberOfTimes >= 1.0f)
         //   {
         //      string swapObjectiveId = objectives[i].Identifier;
         //      //objectives[i] = new Objective(); // can't be done; needs insert

         //      //objectives.Insert(i, new Objective()); // inserts forever; does not replace the item

         //      //objectives.RemoveAt(i); // Crashes the app
         //      //objectives.Insert(i, new Objective());

         //      objectives.Add(new Objective());
         //   }
         //}

         //objectives.Add(new Objective());

         PropertyChanged(this, new PropertyChangedEventArgs("Opacity"));
      }

      /// <summary>
      /// Remove the number of times all objectives were accomplished from roaming.
      /// </summary>
      public static void FlushAllObjectivesFromRoaming()
      {
         roamingSettings.Values[WIN_DIFFICULT_GAME_OBJECTIVE + THIS_DONE_NUMBER_OF_TIMES] = null;
         roamingSettings.Values[WIN_DOCTORATE_GAME_OBJECTIVE + THIS_DONE_NUMBER_OF_TIMES] = null;
         roamingSettings.Values[WIN_SATURATION_GAME_OBJECTIVE + THIS_DONE_NUMBER_OF_TIMES] = null;
         roamingSettings.Values[WIN_SINGLE_CLICK_GAME_OBJECTIVE + THIS_DONE_NUMBER_OF_TIMES] = null;
         roamingSettings.Values[REVEAL_100_GAME_OBJECTIVE + THIS_DONE_NUMBER_OF_TIMES] = null;
         roamingSettings.Values[REVEAL_1000_GAME_OBJECTIVE + THIS_DONE_NUMBER_OF_TIMES] = null;
         roamingSettings.Values[REVEAL_10000_GAME_OBJECTIVE + THIS_DONE_NUMBER_OF_TIMES] = null;
      }

      /// <summary>
      /// Equality operator implementation.
      /// </summary>
      /// <param name="other">Other argument of the equality operand.</param>
      /// <returns></returns>
      public bool Equals(Objective other)
      {
         if(this.identifier == other.Identifier)
         {
            return true;
         }
         else
         {
            return false;
         }
      }
      #endregion

      #region Overrides
      /// <summary>
      /// Overridden string method returning the title of an objective.
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
         return title;
      }
      #endregion
   }
}
