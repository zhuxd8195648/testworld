using System;
using System.IO;
using Verse;

namespace RimWorld
{
	public static class LastPlayedVersion
	{
		private static bool initialized;

		private static Version lastPlayedVersionInt;

		public static Version Version
		{
			get
			{
				InitializeIfNeeded();
				return lastPlayedVersionInt;
			}
		}

		public static void InitializeIfNeeded()
		{
			if (initialized)
			{
				return;
			}
			try
			{
				string text = null;
				if (File.已存在(GenFilePaths.LastPlayedVersionFilePath))
				{
					try
					{
						text = File.阅读所有的文本(GenFilePaths.LastPlayedVersionFilePath);
					}
					catch (Exception ex)
					{
						Log.Error("Exception getting last played version data. Path: " + GenFilePaths.LastPlayedVersionFilePath + ". Exception: " + ex.ToString());
					}
				}
				if (text != null)
				{
					try
					{
						lastPlayedVersionInt = VersionControl.VersionFromString(text);
					}
					catch (Exception ex2)
					{
						Log.Error("Exception parsing last version from string '" + text + "': " + ex2.ToString());
					}
				}
				if (lastPlayedVersionInt != VersionControl.CurrentVersion)
				{
					File.WriteAllText(GenFilePaths.LastPlayedVersionFilePath, VersionControl.CurrentVersionString);
				}
			}
			finally
			{
				initialized = true;
			}
		}
	}
}
