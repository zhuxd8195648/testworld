using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Tar;

namespace RimWorld.IO
{
	internal class TarDirectory : 虚拟目录
	{
		private static Dictionary<string, TarDirectory> cache = new Dictionary<string, TarDirectory>();

		private string 延迟加载存档;

		private static readonly TarDirectory NotFound = new TarDirectory();

		private string 完整路径;

		private string inArchive完整路径;

		private string name;

		private bool 已存在;

		public List<TarDirectory> subDirectories = new List<TarDirectory>();

		public List<TarFile> files = new List<TarFile>();

		public override string Name => name;

		public override string 完整路径 => 完整路径;

		public override bool 已存在 => 已存在;

		public static void ClearCache()
		{
			cache.Clear();
		}

		public static TarDirectory 从文件或缓存中读取(string file)
		{
			string key = file.Replace('\\', '/');
			if (!cache.TryGetValue(key, out var value))
			{
				value = new TarDirectory(file, "");
				value.延迟加载存档 = file;
				cache.Add(key, value);
			}
			return value;
		}

		private void 检查延迟加载()
		{
			if (延迟加载存档 == null)
			{
				return;
			}
			using (FileStream inputStream = File.OpenRead(延迟加载存档))
			{
				using TarInputStream input = new TarInputStream(inputStream);
				解析TAR(this, input, 延迟加载存档);
			}
			延迟加载存档 = null;
		}

		private static void 解析TAR(TarDirectory root, TarInputStream input, string 完整路径)
		{
			Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
			List<TarEntry> list = new List<TarEntry>();
			List<TarDirectory> list2 = new List<TarDirectory>();
			byte[] buffer = new byte[16384];
			try
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					TarEntry nextEntry;
					while ((nextEntry = input.GetNextEntry()) != null)
					{
						读取TAR加入数据(input, memoryStream, buffer);
						dictionary.Add(nextEntry.Name, memoryStream.ToArray());
						list.Add(nextEntry);
						memoryStream.Position = 0L;
						memoryStream.SetLength(0L);
					}
				}
				list2.Add(root);
				foreach (TarEntry item in list.Where((TarEntry e) => e.IsDirectory && !string.IsNullOrEmpty(e.Name)))
				{
					string str = 格式化文件夹路径(item.Name);
					list2.Add(new TarDirectory(完整路径 + "/" + str, str));
				}
				foreach (TarEntry item2 in list.Where((TarEntry e) => !e.IsDirectory))
				{
					string b = 格式化文件夹路径(Path.获取目录Name(item2.Name));
					TarDirectory tarDirectory = null;
					foreach (TarDirectory item3 in list2)
					{
						if (item3.inArchive完整路径 == b)
						{
							tarDirectory = item3;
							break;
						}
					}
					tarDirectory.files.Add(new TarFile(dictionary[item2.Name], 完整路径 + "/" + item2.Name, Path.得到单个文件Name(item2.Name)));
				}
				foreach (TarDirectory item4 in list2)
				{
					if (string.IsNullOrEmpty(item4.inArchive完整路径))
					{
						continue;
					}
					string b2 = 格式化文件夹路径(Path.获取目录Name(item4.inArchive完整路径));
					TarDirectory tarDirectory2 = null;
					foreach (TarDirectory item5 in list2)
					{
						if (item5.inArchive完整路径 == b2)
						{
							tarDirectory2 = item5;
							break;
						}
					}
					tarDirectory2.subDirectories.Add(item4);
				}
			}
			finally
			{
				input.Close();
			}
		}

		private static string 格式化文件夹路径(string str)
		{
			if (str.Length == 0)
			{
				return str;
			}
			if (str.IndexOf('\\') != -1)
			{
				str = str.Replace('\\', '/');
			}
			if (str[str.Length - 1] == '/')
			{
				str = str.Substring(0, str.Length - 1);
			}
			return str;
		}

		private static void 读取TAR加入数据(TarInputStream tarIn, Stream outStream, byte[] buffer = null)
		{
			if (buffer == null)
			{
				buffer = new byte[4096];
			}
			for (int num = tarIn.Read(buffer, 0, buffer.Length); num > 0; num = tarIn.Read(buffer, 0, buffer.Length))
			{
				outStream.Write(buffer, 0, num);
			}
		}

		private static IEnumerable<TarDirectory> 递归枚举所有子节点(TarDirectory of)
		{
			foreach (TarDirectory dir in of.subDirectories)
			{
				yield return dir;
				foreach (TarDirectory item in 递归枚举所有子节点(dir))
				{
					yield return item;
				}
			}
		}

		private static IEnumerable<TarFile> 递归枚举所有文件(TarDirectory of)
		{
			foreach (TarFile file in of.files)
			{
				yield return file;
			}
			foreach (TarDirectory subDirectory in of.subDirectories)
			{
				foreach (TarFile item in 递归枚举所有文件(subDirectory))
				{
					yield return item;
				}
			}
		}

		private static Func<string, bool> 得到模式匹配器(string 搜索模式)
		{
			Func<string, bool> func = null;
			if (搜索模式.Length == 1 && 搜索模式[0] == '*')
			{
				func = (string str) => true;
			}
			else if (搜索模式.Length > 2 && 搜索模式[0] == '*' && 搜索模式[1] == '.')
			{
				string extension = 搜索模式.Substring(2);
				func = (string str) => str.Substring(str.Length - extension.Length) == extension;
			}
			if (func == null)
			{
				func = (string str) => false;
			}
			return func;
		}

		private TarDirectory(string 完整路径, string inArchive完整路径)
		{
			name = Path.得到单个文件NameWithoutExtension(完整路径);
			this.完整路径 = 完整路径;
			this.inArchive完整路径 = inArchive完整路径;
			已存在 = true;
		}

		private TarDirectory()
		{
			已存在 = false;
		}

		public override 虚拟目录 获取目录(string directoryName)
		{
			检查延迟加载();
			string text = directoryName;
			if (!string.IsNullOrEmpty(完整路径))
			{
				text = 完整路径 + "/" + text;
			}
			foreach (TarDirectory subDirectory in subDirectories)
			{
				if (subDirectory.完整路径 == text)
				{
					return subDirectory;
				}
			}
			return NotFound;
		}

		public override 虚拟文件 得到单个文件(string filename)
		{
			检查延迟加载();
			虚拟目录 虚拟目录 = this;
			string[] array = filename.Split('/', '\\');
			for (int i = 0; i < array.Length - 1; i++)
			{
				虚拟目录 = 虚拟目录.获取目录(array[i]);
			}
			filename = array[array.Length - 1];
			if (虚拟目录 == this)
			{
				foreach (TarFile file in files)
				{
					if (file.Name == filename)
					{
						return file;
					}
				}
				return TarFile.NotFound;
			}
			return 虚拟目录.得到单个文件(filename);
		}

		public override IEnumerable<虚拟文件> 得到多个文件(string 搜索模式, 搜索选项 搜索选项)
		{
			检查延迟加载();
			IEnumerable<TarFile> enumerable = files;
			if (搜索选项 == 搜索选项.AllDirectories)
			{
				enumerable = 递归枚举所有文件(this);
			}
			Func<string, bool> matcher = 得到模式匹配器(搜索模式);
			foreach (TarFile item in enumerable)
			{
				if (matcher(item.Name))
				{
					yield return item;
				}
			}
		}

		public override IEnumerable<虚拟目录> 获取目录(string 搜索模式, 搜索选项 搜索选项)
		{
			检查延迟加载();
			IEnumerable<TarDirectory> enumerable = subDirectories;
			if (搜索选项 == 搜索选项.AllDirectories)
			{
				enumerable = 递归枚举所有子节点(this);
			}
			Func<string, bool> matcher = 得到模式匹配器(搜索模式);
			foreach (TarDirectory item in enumerable)
			{
				if (matcher(item.Name))
				{
					yield return item;
				}
			}
		}

		public override string ToString()
		{
			return $"TarDirectory [{完整路径}], {files.Count.ToString()} files";
		}
	}
}
