using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace RimWorld.IO
{
	public static class 虚拟文件信息载入
	{
		public static XDocument 加载为XDocument(this 虚拟文件 file)
		{
			using Stream input = file.CreateReadStream();
			return XDocument.Load(XmlReader.Create(input), LoadOptions.SetLineInfo);
		}
	}
}
