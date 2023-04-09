using System.IO;

namespace RimWorld.IO
{
	public abstract class 虚拟文件
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

		public abstract long Length
		{
			get;
		}

		public abstract Stream CreateReadStream();

		public abstract string 阅读所有的文本();

		public abstract string[] ReadAllLines();

		public abstract byte[] ReadAllBytes();
	}
}
