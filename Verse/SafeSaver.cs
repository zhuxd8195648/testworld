using System;
using System.IO;
using System.Threading;

namespace Verse
{
	public static class SafeSaver
	{
		private static readonly string NewFileSuffix = ".new";

		private static readonly string OldFileSuffix = ".old";

		private static string 得到单个文件完整路径(string path)
		{
			return Path.Get完整路径(path);
		}

		private static string GetNewFile完整路径(string path)
		{
			return Path.Get完整路径(path + NewFileSuffix);
		}

		private static string GetOldFile完整路径(string path)
		{
			return Path.Get完整路径(path + OldFileSuffix);
		}

		public static void Save(string path, string documentElementName, Action saveAction, bool leaveOldFile = false)
		{
			try
			{
				CleanSafeSaverFiles(path);
				if (!File.已存在(得到单个文件完整路径(path)))
				{
					DoSave(得到单个文件完整路径(path), documentElementName, saveAction);
					return;
				}
				DoSave(GetNewFile完整路径(path), documentElementName, saveAction);
				try
				{
					SafeMove(得到单个文件完整路径(path), GetOldFile完整路径(path));
				}
				catch (Exception ex)
				{
					Log.Warning("Could not move file from \"" + 得到单个文件完整路径(path) + "\" to \"" + GetOldFile完整路径(path) + "\": " + ex);
					throw;
				}
				try
				{
					SafeMove(GetNewFile完整路径(path), 得到单个文件完整路径(path));
				}
				catch (Exception ex2)
				{
					Log.Warning("Could not move file from \"" + GetNewFile完整路径(path) + "\" to \"" + 得到单个文件完整路径(path) + "\": " + ex2);
					RemoveFileIf已存在(得到单个文件完整路径(path), rethrow: false);
					RemoveFileIf已存在(GetNewFile完整路径(path), rethrow: false);
					try
					{
						SafeMove(GetOldFile完整路径(path), 得到单个文件完整路径(path));
					}
					catch (Exception ex3)
					{
						Log.Warning("Could not move file from \"" + GetOldFile完整路径(path) + "\" back to \"" + 得到单个文件完整路径(path) + "\": " + ex3);
					}
					throw;
				}
				if (!leaveOldFile)
				{
					RemoveFileIf已存在(GetOldFile完整路径(path), rethrow: true);
				}
			}
			catch (Exception ex4)
			{
				GenUI.ErrorDialog("ProblemSavingFile".Translate(得到单个文件完整路径(path), ex4.ToString()));
				throw;
			}
		}

		private static void CleanSafeSaverFiles(string path)
		{
			RemoveFileIf已存在(GetOldFile完整路径(path), rethrow: true);
			RemoveFileIf已存在(GetNewFile完整路径(path), rethrow: true);
		}

		private static void DoSave(string 完整路径, string documentElementName, Action saveAction)
		{
			try
			{
				Scribe.saver.InitSaving(完整路径, documentElementName);
				saveAction();
				Scribe.saver.FinalizeSaving();
			}
			catch (Exception ex)
			{
				Log.Warning("An exception was thrown during saving to \"" + 完整路径 + "\": " + ex);
				Scribe.saver.ForceStop();
				RemoveFileIf已存在(完整路径, rethrow: false);
				throw;
			}
		}

		private static void RemoveFileIf已存在(string path, bool rethrow)
		{
			try
			{
				if (File.已存在(path))
				{
					File.Delete(path);
				}
			}
			catch (Exception ex)
			{
				Log.Warning("Could not remove file \"" + path + "\": " + ex);
				if (rethrow)
				{
					throw;
				}
			}
		}

		private static void SafeMove(string from, string to)
		{
			Exception ex = null;
			for (int i = 0; i < 50; i++)
			{
				try
				{
					File.Move(from, to);
					return;
				}
				catch (Exception ex2)
				{
					if (ex == null)
					{
						ex = ex2;
					}
				}
				Thread.Sleep(1);
			}
			throw ex;
		}
	}
}
