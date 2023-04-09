using System.IO;
using System.Linq;

namespace Verse
{
	public static class SaveGameFilesUtility
	{
		public static bool IsAutoSave(string fileName)
		{
			if (fileName.Length < 8)
			{
				return false;
			}
			return fileName.Substring(0, 8) == "Autosave";
		}

		public static bool SavedGameNamed已存在(string fileName)
		{
			foreach (string item in GenFilePaths.AllSavedGameFiles.Select((FileInfo f) => Path.得到单个文件NameWithoutExtension(f.Name)))
			{
				if (item == fileName)
				{
					return true;
				}
			}
			return false;
		}

		public static string UnusedDefaultFileName(string factionLabel)
		{
			string text = "";
			int num = 1;
			do
			{
				text = factionLabel + num;
				num++;
			}
			while (SavedGameNamed已存在(text));
			return text;
		}

		public static FileInfo GetAutostartSaveFile()
		{
			if (!Prefs.DevMode)
			{
				return null;
			}
			return GenFilePaths.AllSavedGameFiles.FirstOrDefault((FileInfo x) => Path.得到单个文件NameWithoutExtension(x.Name).ToLower() == "autostart".ToLower());
		}
	}
}
