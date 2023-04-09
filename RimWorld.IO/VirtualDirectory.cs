using System.Collections.Generic;
using System.IO;

namespace RimWorld.IO
{
	public abstract class 虚拟目录
	{
		public abstract string Name
		{
			get;
		}

		public abstract string 完整路径
		{
			get;
		}

		public abstract bool 已存在
		{
			get;
		}

		public abstract 虚拟目录 获取目录(string directoryName);

		public abstract 虚拟文件 得到单个文件(string filename);

		public abstract IEnumerable<虚拟文件> 得到多个文件(string 搜索模式, 搜索选项 搜索选项);

		public abstract IEnumerable<虚拟目录> 获取目录(string 搜索模式, 搜索选项 搜索选项);

		public string 阅读所有的文本(string filename)
		{
			return 得到单个文件(filename).阅读所有的文本();
		}

		public bool 文件已经已存在(string filename)
		{
			return 得到单个文件(filename).已存在;
		}

		public override string ToString()
		{
			return 完整路径;
		}
	}
}
