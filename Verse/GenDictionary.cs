using System.Collections.Generic;
using System.Text;

namespace Verse
{
	public static class GenDictionary
	{
		public static string 字符串完整内容<K, V>(this Dictionary<K, V> dict)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<K, V> item in dict)
			{
				stringBuilder.AppendLine(item.Key.ToString() + ": " + item.Value.ToString());
			}
			return stringBuilder.ToString();
		}
	}
}
