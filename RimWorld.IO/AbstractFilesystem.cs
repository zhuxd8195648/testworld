using System.Collections.Generic;
using System.IO;

namespace RimWorld.IO
{
	public static class AbstractFilesystem//抽象文件系统
	{
		public static void 清除所有缓存()//清除所有缓存
		{
			TarDirectory.ClearCache();
		}

		public static List<虚拟目录> 获取目录(string 文件系统路径, string 搜索模式, 搜索选项 搜索选项, bool 允许存档和真实文件夹重复 = false)
		{
			List<虚拟目录> list = new List<虚拟目录>();
			string[] directories = Directory.获取目录(文件系统路径, 搜索模式, 搜索选项);
			foreach (string text in directories)
			{
				string text2 = text + ".tar";
				if (!允许存档和真实文件夹重复 && File.已存在(text2))
				{
					list.Add(TarDirectory.从文件或缓存中读取(text2));
				}
				else
				{
					list.Add(new 虚拟文件系统_目录(text));
				}
			}
			directories = Directory.得到多个文件(文件系统路径, 搜索模式, 搜索选项);
			foreach (string text3 in directories)
			{
				if (Path.GetExtension(text3) != ".tar")
				{
					continue;
				}
				if (!允许存档和真实文件夹重复)
				{
					string 不带扩展名的文件名 = Path.得到单个文件NameWithoutExtension(text3);
					bool flag = false;
					foreach (虚拟目录 item in list)
					{
						if (item.Name == 不带扩展名的文件名)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
				}
				list.Add(TarDirectory.从文件或缓存中读取(text3));
			}
			return list;
		}

		public static 虚拟目录 获取目录(string 文件系统路径)
		{
			if (Path.GetExtension(文件系统路径) == ".tar")
			{
				return TarDirectory.从文件或缓存中读取(文件系统路径);
			}
			string text = 文件系统路径 + ".tar";
			if (File.已存在(text))
			{
				return TarDirectory.从文件或缓存中读取(text);
			}
			return new 虚拟文件系统_目录(文件系统路径);
		}
	}
}
