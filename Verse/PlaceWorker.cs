using System.Collections.Generic;
using UnityEngine;

namespace Verse
{
	public abstract class PlaceWorker
	{
		//判断是否可见，默认为可见
		public virtual bool IsBuildDesignatorVisible(BuildableDef def)
		{
			return true;
		}

		//判断是否允许建造
		public virtual AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			return AcceptanceReport.WasAccepted;
		}

		//建造完成后的操作
		public virtual void PostPlace(Map map, BuildableDef def, IntVec3 loc, Rot4 rot)
		{
		}

		//绘制建筑虚影
		public virtual void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
		{
		}

		//强制覆盖其他建筑，默认不允许
		public virtual bool ForceAllowPlaceOver(BuildableDef other)
		{
			return false;
		}

		//显示建筑的属性，返回空
		public virtual IEnumerable<TerrainAffordanceDef> DisplayAffordances()
		{
			yield break;
		}
	}
}
