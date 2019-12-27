// Denis Labrecque
// April 30, 2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxtroutGame
{
   enum TileType { Blank, Numbered, Mine, ExplodedMine } // TODO check whether enumerations are defaulted to static/constant

   class Tile
   {
      #region Constants
      const string MINE = "💣";
      const string FLAG = "🚩";
      const string EXPLOSION = "💥";
      #endregion

      #region Fields
      TileType type;
      bool isRevealed; // Whether a tile should be unhidden to the user
      bool isShownGraphically; // Whether a tile has already been graphically unhidden
      bool isFlagged;
      uint adjacentMines;
      #endregion

      #region Properties
      public uint AdjacentMines {
         get => adjacentMines;
         set {
            if(value <= 8)
            {
               adjacentMines = value; // TODO ++ won't work here
            }
            else
            {
               throw new ArgumentOutOfRangeException("There cannot be more than eight mines adjacent to one tile.");
            }
         }
      }

      public bool IsRevealed {
         get => isRevealed;
         set => isRevealed = value;
      }

      public bool IsShownGraphically {
         get => isShownGraphically;
         set => isShownGraphically = value;
      }

      public bool IsFlagged {
         get => isFlagged;
      }

      public int Type {
         get => (int)type;
         set {
            type = (TileType)value; // TODO error check
         }
      }
      #endregion

      #region Constructor
      public Tile()
      {
         type = TileType.Blank;
         isFlagged = false;
         isRevealed = false;
         isShownGraphically = false;
         adjacentMines = 0;
      }
      #endregion

      #region Methods
      // Overridden string method for displaying tiles to the user.
      public override string ToString()
      {
         if(isRevealed == false)
         {
            return string.Empty;
         }
         else
         {
            switch(type)
            {
               case TileType.Blank:
                  return string.Empty;
               case TileType.Numbered:
                  return adjacentMines.ToString();
               case TileType.Mine:
                  return MINE;
               case TileType.ExplodedMine:
                  return EXPLOSION;
               default:
                  return string.Empty;
            }
         }
      }
      #endregion
   }
}
