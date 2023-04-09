using UnityEngine.SceneManagement;
using Verse.Profile;

namespace Verse
{
	public static class GenScene
	{
		public const string EntrySceneName = "Entry";

		public const string PlaySceneName = "Play";

		public static bool InEntryScene => SceneManager.GetActiveScene().name == "Entry";

		public static bool InPlayScene => SceneManager.GetActiveScene().name == "Play";

		public static void GoToMainMenu()
		{
			LongEventHandler.ClearQueuedEvents();
			LongEventHandler.QueueLongEvent(delegate
			{
				MemoryUtility.清除所有地图和世界();
				Current.Game = null;
			}, "Entry", "LoadingLongEvent", doAsynchronously: true, null, showExtraUIInfo: false);
		}
	}
}
