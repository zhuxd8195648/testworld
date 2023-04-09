using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RimWorld.IO;

namespace Verse
{
	public class LanguageWordInfo
	{
		private Dictionary<string, Gender> genders = new Dictionary<string, Gender>();

		private const string FolderName = "WordInfo";

		private const string GendersFolderName = "Gender";

		private const string MaleFileName = "Male.txt";

		private const string FemaleFileName = "Female.txt";

		private const string NeuterFileName = "Neuter.txt";

		private static StringBuilder tmpLowercase = new StringBuilder();

		public void LoadFrom(Tuple<虚拟目录, ModContentPack, string> dir, LoadedLanguage lang)
		{
			虚拟目录 directory = dir.Item1.获取目录("WordInfo").获取目录("Gender");
			TryLoadFromFile(directory.得到单个文件("Male.txt"), Gender.Male, dir, lang);
			TryLoadFromFile(directory.得到单个文件("Female.txt"), Gender.Female, dir, lang);
			TryLoadFromFile(directory.得到单个文件("Neuter.txt"), Gender.None, dir, lang);
		}

		public Gender ResolveGender(string str, string fallback = null)
		{
			if (!TryResolveGender(str, out var gender) && fallback != null)
			{
				TryResolveGender(str, out gender);
			}
			return gender;
		}

		private bool TryResolveGender(string str, out Gender gender)
		{
			tmpLowercase.Length = 0;
			for (int i = 0; i < str.Length; i++)
			{
				tmpLowercase.Append(char.ToLower(str[i]));
			}
			string key = tmpLowercase.ToString();
			if (genders.TryGetValue(key, out gender))
			{
				return true;
			}
			gender = Gender.Male;
			return false;
		}

		private void TryLoadFromFile(虚拟文件 file, Gender gender, Tuple<虚拟目录, ModContentPack, string> dir, LoadedLanguage lang)
		{
			string[] array;
			try
			{
				array = file.ReadAllLines();
			}
			catch (DirectoryNotFoundException)
			{
				return;
			}
			catch (FileNotFoundException)
			{
				return;
			}
			if (!lang.TryRegisterFileIfNew(dir, file.完整路径))
			{
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].NullOrEmpty() && !genders.ContainsKey(array[i]))
				{
					genders.Add(array[i], gender);
				}
			}
		}
	}
}
