using Verse;

namespace RimWorld
{
	//定义一个名为PlaceWorker_NotUnderRoof的类，继承PlaceWorker类
	public class PlaceWorker_NotUnderRoof : PlaceWorker
	{
		//重写AllowsPlacing函数，函数参数包括：检查的BuildableDef，位置、旋转、地图、要忽略的Thing和要检查的Thing
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			//如果该位置处于有屋顶的区域内，返回一个AcceptanceReport类型的对象，其中包含"MustPlaceUnroofed"翻译后的字符串
			if (map.roofGrid.Roofed(loc))
			{
				return new AcceptanceReport("MustPlaceUnroofed".Translate());
			}
			//否则，返回true
			return true;
		}
	}
}
