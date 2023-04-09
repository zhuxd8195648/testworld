using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld.IO;
using UnityEngine;

namespace Verse
{
	public class LoadedLanguage
	{
		public class KeyedReplacement
		{
			public string key;

			public string value;

			public string fileSource;

			public int fileSourceLine;

			public string fileSource完整路径;

			public bool isPlaceholder;
		}

		public string folderName;

		public LanguageInfo info;

		private LanguageWorker workerInt;

		private LanguageWordInfo wordInfo = new LanguageWordInfo();

		private bool dataIsLoaded;

		public List<string> loadErrors = new List<string>();

		public List<string> backstoriesLoadErrors = new List<string>();

		public bool anyKeyedReplacementsXmlParseError;

		public string lastKeyedReplacementsXmlParseErrorInFile;

		public bool anyDefInjectionsXmlParseError;

		public string lastDefInjectionsXmlParseErrorInFile;

		public bool anyError;

		private string legacyFolderName;

		private Dictionary<ModContentPack, HashSet<string>> tmpAlreadyLoadedFiles = new Dictionary<ModContentPack, HashSet<string>>();

		public Texture2D icon = BaseContent.BadTex;

		public Dictionary<string, KeyedReplacement> keyedReplacements = new Dictionary<string, KeyedReplacement>();

		public List<DefInjectionPackage> defInjections = new List<DefInjectionPackage>();

		public Dictionary<string, List<string>> stringFiles = new Dictionary<string, List<string>>();

		public const string OldKeyedTranslationsFolderName = "CodeLinked";

		public const string KeyedTranslationsFolderName = "Keyed";

		public const string OldDefInjectionsFolderName = "DefLinked";

		public const string DefInjectionsFolderName = "DefInjected";

		public const string LanguagesFolderName = "Languages";

		public const string PlaceholderText = "TODO";

		private bool infoIsRealMetadata;

		public string DisplayName => GenText.SplitCamelCase(folderName);

		public string FriendlyNameNative
		{
			get
			{
				if (info == null || info.friendlyNameNative.NullOrEmpty())
				{
					return folderName;
				}
				return info.friendlyNameNative;
			}
		}

		public string FriendlyNameEnglish
		{
			get
			{
				if (info == null || info.friendlyNameEnglish.NullOrEmpty())
				{
					return folderName;
				}
				return info.friendlyNameEnglish;
			}
		}

		public IEnumerable<Tuple<虚拟目录, ModContentPack, string>> AllDirectories
		{
			get
			{
				foreach (ModContentPack mod in LoadedModManager.RunningMods)
				{
					foreach (string item in mod.foldersToLoadDescendingOrder)
					{
						string path = Path.Combine(item, "Languages");
						虚拟目录 directory = AbstractFilesystem.获取目录(Path.Combine(path, folderName));
						if (directory.已存在)
						{
							yield return new Tuple<虚拟目录, ModContentPack, string>(directory, mod, item);
							continue;
						}
						directory = AbstractFilesystem.获取目录(Path.Combine(path, legacyFolderName));
						if (directory.已存在)
						{
							yield return new Tuple<虚拟目录, ModContentPack, string>(directory, mod, item);
						}
					}
				}
			}
		}

		public LanguageWorker Worker
		{
			get
			{
				if (workerInt == null)
				{
					workerInt = (LanguageWorker)Activator.CreateInstance(info.languageWorkerClass);
				}
				return workerInt;
			}
		}

		public string LegacyFolderName => legacyFolderName;

		public LoadedLanguage(string folderName)
		{
			this.folderName = folderName;
			legacyFolderName = (folderName.Contains("(") ? folderName.Substring(0, folderName.IndexOf("(") - 1) : folderName).Trim();
		}

		public void LoadMetadata()
		{
			if (info != null && infoIsRealMetadata)
			{
				return;
			}
			infoIsRealMetadata = true;
			foreach (ModContentPack runningMod in LoadedModManager.RunningMods)
			{
				foreach (string item in runningMod.foldersToLoadDescendingOrder)
				{
					string text = Path.Combine(item, "Languages");
					if (!new DirectoryInfo(text).已存在)
					{
						continue;
					}
					foreach (虚拟目录 directory in AbstractFilesystem.获取目录(text, "*", 搜索选项.TopDirectoryOnly))
					{
						if (directory.Name == folderName || directory.Name == legacyFolderName)
						{
							info = DirectXmlLoader.ItemFromXmlFile<LanguageInfo>(directory, "LanguageInfo.xml", resolveCrossRefs: false);
							if (info.friendlyNameNative.NullOrEmpty() && directory.文件已经已存在("FriendlyName.txt"))
							{
								info.friendlyNameNative = directory.阅读所有的文本("FriendlyName.txt");
							}
							if (info.friendlyNameNative.NullOrEmpty())
							{
								info.friendlyNameNative = folderName;
							}
							if (info.friendlyNameEnglish.NullOrEmpty())
							{
								info.friendlyNameEnglish = folderName;
							}
							return;
						}
					}
				}
			}
		}

		public void InitMetadata(虚拟目录 directory)
		{
			infoIsRealMetadata = false;
			info = new LanguageInfo();
			string text = Regex.Replace(directory.Name, "(\\B[A-Z]+?(?=[A-Z][^A-Z])|\\B[A-Z]+?(?=[^A-Z]))", " $1");
			string friendlyNameEnglish = text;
			string friendlyNameNative = text;
			int num = text.FirstIndexOf((char c) => c == '(');
			int num2 = text.LastIndexOf(")");
			if (num2 > num)
			{
				friendlyNameEnglish = text.Substring(0, num - 1);
				friendlyNameNative = text.Substring(num + 1, num2 - num - 1);
			}
			info.friendlyNameEnglish = friendlyNameEnglish;
			info.friendlyNameNative = friendlyNameNative;
		}

		public void LoadData()
		{
			if (dataIsLoaded)
			{
				return;
			}
			dataIsLoaded = true;
			DeepProfiler.Start("Loading language data: " + folderName);
			try
			{
				tmpAlreadyLoadedFiles.Clear();
				foreach (Tuple<虚拟目录, ModContentPack, string> allDirectory in AllDirectories)
				{
					Tuple<虚拟目录, ModContentPack, string> localDirectory = allDirectory;
					if (!tmpAlreadyLoadedFiles.ContainsKey(localDirectory.Item2))
					{
						tmpAlreadyLoadedFiles[localDirectory.Item2] = new HashSet<string>();
					}
					LongEventHandler.ExecuteWhenFinished(delegate
					{
						if (icon == BaseContent.BadTex)
						{
							虚拟文件 file = localDirectory.Item1.得到单个文件("LangIcon.png");
							if (file.已存在)
							{
								icon = ModContentLoader<Texture2D>.LoadItem(file).contentItem;
							}
						}
					});
					虚拟目录 directory = localDirectory.Item1.获取目录("CodeLinked");
					if (directory.已存在)
					{
						loadErrors.Add("Translations aren't called CodeLinked any more. Please rename to Keyed: " + directory);
					}
					else
					{
						directory = localDirectory.Item1.获取目录("Keyed");
					}
					if (directory.已存在)
					{
						foreach (虚拟文件 file2 in directory.得到多个文件("*.xml", 搜索选项.AllDirectories))
						{
							if (TryRegisterFileIfNew(localDirectory, file2.完整路径))
							{
								LoadFromFile_Keyed(file2);
							}
						}
					}
					虚拟目录 directory2 = localDirectory.Item1.获取目录("DefLinked");
					if (directory2.已存在)
					{
						loadErrors.Add("Translations aren't called DefLinked any more. Please rename to DefInjected: " + directory2);
					}
					else
					{
						directory2 = localDirectory.Item1.获取目录("DefInjected");
					}
					if (directory2.已存在)
					{
						foreach (虚拟目录 directory4 in directory2.获取目录("*", 搜索选项.TopDirectoryOnly))
						{
							string name = directory4.Name;
							Type typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(name);
							if (typeInAnyAssembly == null && name.Length > 3)
							{
								typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(name.Substring(0, name.Length - 1));
							}
							if (typeInAnyAssembly == null)
							{
								loadErrors.Add(string.Concat("Error loading language from ", allDirectory, ": dir ", directory4.Name, " doesn't correspond to any def type. Skipping..."));
								continue;
							}
							foreach (虚拟文件 file3 in directory4.得到多个文件("*.xml", 搜索选项.AllDirectories))
							{
								if (TryRegisterFileIfNew(localDirectory, file3.完整路径))
								{
									LoadFromFile_DefInject(file3, typeInAnyAssembly);
								}
							}
						}
					}
					EnsureAllDefTypesHaveDefInjectionPackage();
					虚拟目录 directory3 = localDirectory.Item1.获取目录("Strings");
					if (directory3.已存在)
					{
						foreach (虚拟目录 directory5 in directory3.获取目录("*", 搜索选项.TopDirectoryOnly))
						{
							foreach (虚拟文件 file4 in directory5.得到多个文件("*.txt", 搜索选项.AllDirectories))
							{
								if (TryRegisterFileIfNew(localDirectory, file4.完整路径))
								{
									LoadFromFile_Strings(file4, directory3);
								}
							}
						}
					}
					wordInfo.LoadFrom(localDirectory, this);
				}
			}
			catch (Exception arg)
			{
				Log.Error("Exception loading language data. Rethrowing. Exception: " + arg);
				throw;
			}
			finally
			{
				DeepProfiler.End();
			}
		}

		public bool TryRegisterFileIfNew(Tuple<虚拟目录, ModContentPack, string> dir, string filePath)
		{
			if (!filePath.StartsWith(dir.Item3))
			{
				Log.Error("Failed to get a relative path for a file: " + filePath + ", located in " + dir.Item3);
				return false;
			}
			string item = filePath.Substring(dir.Item3.Length);
			if (!tmpAlreadyLoadedFiles.ContainsKey(dir.Item2))
			{
				tmpAlreadyLoadedFiles[dir.Item2] = new HashSet<string>();
			}
			else if (tmpAlreadyLoadedFiles[dir.Item2].Contains(item))
			{
				return false;
			}
			tmpAlreadyLoadedFiles[dir.Item2].Add(item);
			return true;
		}

		private void LoadFromFile_Strings(虚拟文件 file, 虚拟目录 stringsTopDir)
		{
			string text;
			try
			{
				text = file.阅读所有的文本();
			}
			catch (Exception ex)
			{
				loadErrors.Add(string.Concat("Exception loading from strings file ", file, ": ", ex));
				return;
			}
			string text2 = file.完整路径;
			if (stringsTopDir != null)
			{
				text2 = text2.Substring(stringsTopDir.完整路径.Length + 1);
			}
			text2 = text2.Substring(0, text2.Length - Path.GetExtension(text2).Length);
			text2 = text2.Replace('\\', '/');
			List<string> list = new List<string>();
			foreach (string item in GenText.LinesFromString(text))
			{
				list.Add(item);
			}
			if (stringFiles.TryGetValue(text2, out var value))
			{
				foreach (string item2 in list)
				{
					value.Add(item2);
				}
			}
			else
			{
				stringFiles.Add(text2, list);
			}
		}

		private void LoadFromFile_Keyed(虚拟文件 file)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
			try
			{
				foreach (DirectXmlLoaderSimple.XmlKeyValuePair item in DirectXmlLoaderSimple.ValuesFromXmlFile(file))
				{
					if (dictionary.ContainsKey(item.key))
					{
						loadErrors.Add("Duplicate keyed translation key: " + item.key + " in language " + folderName);
						continue;
					}
					dictionary.Add(item.key, item.value);
					dictionary2.Add(item.key, item.lineNumber);
				}
			}
			catch (Exception ex)
			{
				loadErrors.Add(string.Concat("Exception loading from translation file ", file, ": ", ex));
				dictionary.Clear();
				dictionary2.Clear();
				anyKeyedReplacementsXmlParseError = true;
				lastKeyedReplacementsXmlParseErrorInFile = file.Name;
			}
			foreach (KeyValuePair<string, string> item2 in dictionary)
			{
				string text = item2.Value;
				KeyedReplacement keyedReplacement = new KeyedReplacement();
				if (text == "TODO")
				{
					keyedReplacement.isPlaceholder = true;
					text = "";
				}
				keyedReplacement.key = item2.Key;
				keyedReplacement.value = text;
				keyedReplacement.fileSource = file.Name;
				keyedReplacement.fileSourceLine = dictionary2[item2.Key];
				keyedReplacement.fileSource完整路径 = file.完整路径;
				keyedReplacements.SetOrAdd(item2.Key, keyedReplacement);
			}
		}

		public void LoadFromFile_DefInject(虚拟文件 file, Type defType)
		{
			DefInjectionPackage defInjectionPackage = defInjections.Where((DefInjectionPackage di) => di.defType == defType).FirstOrDefault();
			if (defInjectionPackage == null)
			{
				defInjectionPackage = new DefInjectionPackage(defType);
				defInjections.Add(defInjectionPackage);
			}
			defInjectionPackage.AddDataFromFile(file, out var xmlParseError);
			if (xmlParseError)
			{
				anyDefInjectionsXmlParseError = true;
				lastDefInjectionsXmlParseErrorInFile = file.Name;
			}
		}

		private void EnsureAllDefTypesHaveDefInjectionPackage()
		{
			foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases())
			{
				if (!defInjections.Any((DefInjectionPackage x) => x.defType == defType))
				{
					defInjections.Add(new DefInjectionPackage(defType));
				}
			}
		}

		public bool HaveTextForKey(string key, bool allowPlaceholders = false)
		{
			if (!dataIsLoaded)
			{
				LoadData();
			}
			if (key == null)
			{
				return false;
			}
			if (!keyedReplacements.TryGetValue(key, out var value))
			{
				return false;
			}
			if (!allowPlaceholders)
			{
				return !value.isPlaceholder;
			}
			return true;
		}

		public bool TryGetTextFromKey(string key, out TaggedString translated)
		{
			if (!dataIsLoaded)
			{
				LoadData();
			}
			if (key == null)
			{
				translated = key;
				return false;
			}
			if (!keyedReplacements.TryGetValue(key, out var value) || value.isPlaceholder)
			{
				translated = key;
				return false;
			}
			translated = value.value;
			return true;
		}

		public bool TryGetStringsFromFile(string fileName, out List<string> stringsList)
		{
			if (!dataIsLoaded)
			{
				LoadData();
			}
			if (!stringFiles.TryGetValue(fileName, out stringsList))
			{
				stringsList = null;
				return false;
			}
			return true;
		}

		public string GetKeySourceFileAndLine(string key)
		{
			if (!keyedReplacements.TryGetValue(key, out var value))
			{
				return "unknown";
			}
			return value.fileSource + ":" + value.fileSourceLine;
		}

		public Gender ResolveGender(string str, string fallback = null)
		{
			return wordInfo.ResolveGender(str, fallback);
		}

		public void InjectIntoData_BeforeImpliedDefs()
		{
			if (!dataIsLoaded)
			{
				LoadData();
			}
			foreach (DefInjectionPackage defInjection in defInjections)
			{
				try
				{
					defInjection.InjectIntoDefs(errorOnDefNotFound: false);
				}
				catch (Exception arg)
				{
					Log.Error("Critical error while injecting translations into defs: " + arg);
				}
			}
		}

		public void InjectIntoData_AfterImpliedDefs()
		{
			if (!dataIsLoaded)
			{
				LoadData();
			}
			int num = loadErrors.Count;
			foreach (DefInjectionPackage defInjection in defInjections)
			{
				try
				{
					defInjection.InjectIntoDefs(errorOnDefNotFound: true);
					num += defInjection.loadErrors.Count;
				}
				catch (Exception arg)
				{
					Log.Error("Critical error while injecting translations into defs: " + arg);
				}
			}
			BackstoryTranslationUtility.LoadAndInjectBackstoryData(AllDirectories, backstoriesLoadErrors);
			num += backstoriesLoadErrors.Count;
			if (num != 0)
			{
				anyError = true;
				Log.Warning("Translation data for language " + LanguageDatabase.activeLanguage.FriendlyNameEnglish + " has " + num + " errors. Generate translation report for more info.");
			}
		}

		public override string ToString()
		{
			return info.friendlyNameEnglish;
		}
	}
}
