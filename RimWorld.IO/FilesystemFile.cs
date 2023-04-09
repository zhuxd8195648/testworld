using System.IO;

namespace RimWorld.IO
{
	internal class 虚拟文件系统_文件 : 虚拟文件//文件系统文件
	{
		private FileInfo fileInfo;

		public override string Name => fileInfo.Name;

		public override string 完整路径 => fileInfo.FullName;

		public override bool 已存在 => fileInfo.已存在;

		public override long Length => fileInfo.Length;

		public 虚拟文件系统_文件(FileInfo fileInfo)
		{
			this.fileInfo = fileInfo;
		}

		public override Stream CreateReadStream()
		{
			return fileInfo.OpenRead();
		}

		public override byte[] ReadAllBytes()
		{
			return File.ReadAllBytes(fileInfo.FullName);
		}

		public override string[] ReadAllLines()
		{
			return File.ReadAllLines(fileInfo.FullName);
		}

		public override string 阅读所有的文本()
		{
			return File.阅读所有的文本(fileInfo.FullName);
		}

		public static implicit operator 虚拟文件系统_文件(FileInfo fileInfo)
		{
			return new 虚拟文件系统_文件(fileInfo);
		}

		public override string ToString()
		{
			return $"虚拟文件系统_文件 [{完整路径}], Length {fileInfo.Length.ToString()}";
		}
	}
}
