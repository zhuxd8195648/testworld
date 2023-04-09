using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using RimWorld.IO;

namespace Verse
{
	public static class DirectXmlLoaderSimple
	{
		public struct XmlKeyValuePair
		{
			public string key;

			public string value;

			public int lineNumber;
		}

		public static IEnumerable<XmlKeyValuePair> ValuesFromXmlFile(虚拟文件 file)
		{
			XDocument xDocument = file.加载为XDocument();
			foreach (XElement item in xDocument.Root.Elements())
			{
				string key = item.Name.ToString();
				string value = item.Value;
				value = value.Replace("\\n", "\n");
				XmlKeyValuePair xmlKeyValuePair = default(XmlKeyValuePair);
				xmlKeyValuePair.key = key;
				xmlKeyValuePair.value = value;
				xmlKeyValuePair.lineNumber = ((IXmlLineInfo)item).LineNumber;
				yield return xmlKeyValuePair;
			}
		}
	}
}
