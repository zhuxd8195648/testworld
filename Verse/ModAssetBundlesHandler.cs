using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Verse
{
	public class ModAssetBundlesHandler
	{
		private ModContentPack mod;

		public List<AssetBundle> loadedAssetBundles = new List<AssetBundle>();

		public static readonly string[] TextureExtensions = new string[4]
		{
			".png",
			".jpg",
			".jpeg",
			".psd"
		};

		public static readonly string[] AudioClipExtensions = new string[2]
		{
			".wav",
			".mp3"
		};

		public ModAssetBundlesHandler(ModContentPack mod)
		{
			this.mod = mod;
		}

		public void ReloadAll()
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(mod.RootDir, "AssetBundles"));
			if (!directoryInfo.已存在)
			{
				return;
			}
			FileInfo[] files = directoryInfo.得到多个文件("*", 搜索选项.AllDirectories);
			foreach (FileInfo fileInfo in files)
			{
				if (fileInfo.Extension.NullOrEmpty())
				{
					AssetBundle assetBundle = AssetBundle.LoadFromFile(fileInfo.FullName);
					if (assetBundle != null)
					{
						loadedAssetBundles.Add(assetBundle);
					}
					else
					{
						Log.Error("Could not load asset bundle at " + fileInfo.FullName);
					}
				}
			}
		}

		public void ClearDestroy()
		{
			LongEventHandler.ExecuteWhenFinished(delegate
			{
				for (int i = 0; i < loadedAssetBundles.Count; i++)
				{
					loadedAssetBundles[i].Unload(unloadAllLoadedObjects: true);
				}
				loadedAssetBundles.Clear();
			});
		}
	}
}
