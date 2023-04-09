using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld
{
	public class CompAssignableToPawn_MeditationSpot : CompAssignableToPawn
	{
		//指定候选人
		public override IEnumerable<Pawn> AssigningCandidates
		{
			get
			{
				if (!parent.Spawned)
				{
					return Enumerable.Empty<Pawn>();
				}
				return parent.Map.mapPawns.FreeColonists.OrderByDescending((Pawn p) => CanAssignTo(p).Accepted);
			}
		}

		protected override string GetAssignmentGizmoDesc()
		{
			return "CommandMeditationSpotSetOwnerDesc".Translate();
		}

		public override string CompInspectStringExtra()
		{
			if (base.AssignedPawnsForReading.Count == 0)
			{
				return "Owner".Translate() + ": " + "Nobody".Translate();
			}
			if (base.AssignedPawnsForReading.Count == 1)
			{
				return "Owner".Translate() + ": " + base.AssignedPawnsForReading[0].Label;
			}
			return "";
		}

		//指定任何
		public override bool AssignedAnything(Pawn pawn)
		{
			return pawn.ownership.AssignedMeditationSpot != null;
		}

		//尝试指定玩家
		public override void TryAssignPawn(Pawn pawn)
		{
			pawn.ownership.ClaimMeditationSpot((Building)parent);
		}
		//尝试取消指定
		public override void TryUnassignPawn(Pawn pawn, bool sort = true)
		{
			pawn.ownership.UnclaimMeditationSpot();
		}

		//发布公开数据
		public override void PostExposeData()
		{
			base.PostExposeData();
			if (Scribe.mode == LoadSaveMode.PostLoadInit && assignedPawns.RemoveAll((Pawn x) => x.ownership.AssignedMeditationSpot != parent) > 0)
			{
				Log.Warning(parent.ToStringSafe() + " 有指定的卒子没有把它作为指定的冥想地点。 删除  .");
			}
		}
	}
}
