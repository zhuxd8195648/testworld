using System;
using RimWorld.IO;

namespace Verse
{
	public class LoadedContentItem<T> where T : class
	{
		public 虚拟文件 internalFile;

		public T contentItem;

		public IDisposable extraDisposable;

		public LoadedContentItem(虚拟文件 internalFile, T contentItem, IDisposable extraDisposable = null)
		{
			this.internalFile = internalFile;
			this.contentItem = contentItem;
			this.extraDisposable = extraDisposable;
		}
	}
}
