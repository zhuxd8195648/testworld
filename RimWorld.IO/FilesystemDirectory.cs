using System.Collections.Generic;
using System.IO;

namespace RimWorld.IO
{
	internal class 虚拟文件系统_目录 : 虚拟目录
	{
		private DirectoryInfo dirInfo;

		public override string Name => dirInfo.Name;

		public override string 完整路径 => dirInfo.FullName;

		public override bool 已存在 => dirInfo.已存在;

		public 虚拟文件系统_目录(string dir)
		{
			dirInfo = new DirectoryInfo(dir);
		}

		public 虚拟文件系统_目录(DirectoryInfo dir)
		{
			dirInfo = dir;
		}

		public override IEnumerable<虚拟目录> 获取目录(string 搜索模式, 搜索选项 搜索选项)
		{
			DirectoryInfo[] directories = dirInfo.获取目录(搜索模式, 搜索选项);
			foreach (DirectoryInfo dir in directories)
			{
				yield return new 虚拟文件系统_目录(dir);
			}
		}

		public override 虚拟目录 获取目录(string directoryName)
		{
			return new 虚拟文件系统_目录(Path.Combine(完整路径, directoryName));
		}

		public override 虚拟文件 得到单个文件(string filename)
		{
			return new 虚拟文件系统_文件(new FileInfo(Path.Combine(完整路径, filename)));
		}

		public override IEnumerable<虚拟文件> 得到多个文件(string 搜索模式, 搜索选项 搜索选项)
		{
			FileInfo[] files = dirInfo.得到多个文件(搜索模式, 搜索选项);
			foreach (FileInfo fileInfo in files)
			{
				yield return new 虚拟文件系统_文件(fileInfo);
			}
		}
	}
}
