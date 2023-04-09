using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Verse
{
	public static class GenFile
	{
		public static string TextFromRawFile(string filePath)
		{
			return File.阅读所有的文本(filePath);
		}

		public static string TextFromResourceFile(string filePath)
		{
			TextAsset textAsset = Resources.Load("Text/" + filePath) as TextAsset;
			if (textAsset == null)
			{
				Log.Message("在上的资源中找不到文本资源 " + filePath);
				return null;
			}
			return GetTextWithoutBOM(textAsset);
		}

		public static string GetTextWithoutBOM(TextAsset textAsset)
		{
			string text = null;
			using MemoryStream stream = new MemoryStream(textAsset.bytes);
			using StreamReader streamReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
			return streamReader.ReadToEnd();
		}

		public static IEnumerable<string> LinesFromFile(string filePath)
		{
			string text = TextFromResourceFile(filePath);
			foreach (string item in GenText.LinesFromString(text))
			{
				yield return item;
			}
		}

		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool useLinuxLineEndings = false)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] directories = directoryInfo.获取目录();
			if (!directoryInfo.已存在)
			{
				throw new DirectoryNotFoundException("源目录不已存在或找不到: " + sourceDirName);
			}
			if (!Directory.已存在(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}
			FileInfo[] files = directoryInfo.得到多个文件();
			foreach (FileInfo fileInfo in files)
			{
				string text = Path.Combine(destDirName, fileInfo.Name);
				if (useLinuxLineEndings && (fileInfo.Extension == ".sh" || fileInfo.Extension == ".txt"))
				{
					if (!File.已存在(text))
					{
						File.WriteAllText(text, File.阅读所有的文本(fileInfo.FullName).Replace("\r\n", "\n").Replace("\r", "\n"));
					}
				}
				else
				{
					fileInfo.CopyTo(text, overwrite: false);
				}
			}
			if (copySubDirs)
			{
				DirectoryInfo[] array = directories;
				foreach (DirectoryInfo directoryInfo2 in array)
				{
					string destDirName2 = Path.Combine(destDirName, directoryInfo2.Name);
					DirectoryCopy(directoryInfo2.FullName, destDirName2, copySubDirs, useLinuxLineEndings);
				}
			}
		}

		public static string SanitizedFileName(string fileName)
		{
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			string text = "";
			for (int i = 0; i < fileName.Length; i++)
			{
				if (!invalidFileNameChars.Contains(fileName[i]))
				{
					text += fileName[i];
				}
			}
			if (text.Length == 0)
			{
				text = "unnamed";
			}
			return text;
		}
	}
}
