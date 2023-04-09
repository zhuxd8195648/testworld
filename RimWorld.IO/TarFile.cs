using System;
using System.Collections.Generic;
using System.IO;

namespace RimWorld.IO
{
	internal class TarFile : 虚拟文件
	{
		public static readonly TarFile NotFound = new TarFile();

		public byte[] data;

		public string 完整路径;

		public string name;

		public override string Name => name;

		public override string 完整路径 => 完整路径;

		public override bool 已存在 => data != null;

		public override long Length => data.Length;

		public TarFile(byte[] data, string 完整路径, string name)
		{
			this.data = data;
			this.完整路径 = 完整路径;
			this.name = name;
		}

		private TarFile()
		{
		}

		private void CheckAccess()
		{
			if (data == null)
			{
				throw new FileNotFoundException();
			}
		}

		public override Stream CreateReadStream()
		{
			CheckAccess();
			return new MemoryStream(ReadAllBytes());
		}

		public override byte[] ReadAllBytes()
		{
			CheckAccess();
			byte[] array = new byte[data.Length];
			Buffer.BlockCopy(data, 0, array, 0, data.Length);
			return array;
		}

		public override string[] ReadAllLines()
		{
			CheckAccess();
			List<string> list = new List<string>();
			using (MemoryStream stream = new MemoryStream(data))
			{
				using StreamReader streamReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
				while (!streamReader.EndOfStream)
				{
					list.Add(streamReader.ReadLine());
				}
			}
			return list.ToArray();
		}

		public override string 阅读所有的文本()
		{
			CheckAccess();
			using MemoryStream stream = new MemoryStream(data);
			using StreamReader streamReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
			return streamReader.ReadToEnd();
		}

		public override string ToString()
		{
			return $"TarFile [{完整路径}], Length {data.Length.ToString()}";
		}
	}
}
