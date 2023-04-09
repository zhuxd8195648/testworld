using Verse;

namespace RimWorld
{
	//定义一个类PawnRecentMemory，实现了IExposable接口
	public class PawnRecentMemory : IExposable
	{
		//定义私有变量pawn和lastLightTick，lastOutdoorTick的默认值为999999
		private Pawn pawn;
		private int lastLightTick = 999999;
		private int lastOutdoorTick = 999999;

		//TicksSinceLastLight和TicksSinceOutdoors是只读属性，返回自上一次经过多少时间
		public int TicksSinceLastLight => Find.TickManager.TicksGame - lastLightTick;
		public int TicksSinceOutdoors => Find.TickManager.TicksGame - lastOutdoorTick;

		//构造函数PawnRecentMemory，需要一个参数Pawn类型的pawn
		public PawnRecentMemory(Pawn pawn)
		{
			this.pawn = pawn;
		}

		//实现接口IExposable的方法
		public void ExposeData()
		{
			Scribe_Values.Look(ref lastLightTick, "lastLightTick", 999999); //读取/存储lastLightTick值
			Scribe_Values.Look(ref lastOutdoorTick, "lastOutdoorTick", 999999); //读取/存储lastOutdoorTick值
		}

		//RecentMemoryInterval方法记录生物的运动信息
		public void RecentMemoryInterval()
		{
			if (pawn.Spawned) //pawn是否存在世界中
			{
				//pawn所在位置是否有光芒
				if (pawn.Map.glowGrid.PsychGlowAt(pawn.Position) != 0) 
				{
					lastLightTick = Find.TickManager.TicksGame; //记录上一个亮光时刻
				}
				if (Outdoors()) //pawn是否在室外
				{
					lastOutdoorTick = Find.TickManager.TicksGame; //记录pawn进入室外的时间
				}
			}
		}

		//Outdoors方法返回pawn是否在户外
		private bool Outdoors()
		{
			return pawn.GetRoom()?.PsychologicallyOutdoors ?? false; //获取屋子（Room）对象，如果是户外则返回真，否则返回假
		}

		//Notify_Spawned方法通知pawn被重生或是刚刚生成
		public void Notify_Spawned(bool respawningAfterLoad)
		{
			lastLightTick = Find.TickManager.TicksGame; //记录上一个有光芒时刻
			if (!respawningAfterLoad && Outdoors()) //pawn不是重生并且在户外
			{
				lastOutdoorTick = Find.TickManager.TicksGame; //记录pawn进入室外的时间
			}
		}
	}
}
