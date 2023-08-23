using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VelascoGames.NodeGraphEditor
{
	public static class Settings
	{
		#region ROOM SETTINGS

		public const int maxChildCorridors = 3; //Max number of child corriddors leading from a room. - maximum should be 3 although this is not recomended
												//since it can cause the dungeon building to fail since the rooms are more likely to not fit  together

		#endregion
	}

}