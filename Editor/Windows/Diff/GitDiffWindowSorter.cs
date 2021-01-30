﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using UniGit.Utils;
using UniGit.Windows.Diff;

namespace UniGit
{
	public class GitDiffWindowSorter : IComparer<StatusListEntry>
	{
		private readonly GitDiffWindow window;
		private readonly GitManager gitManager;

		public GitDiffWindowSorter(GitDiffWindow window,GitManager gitManager)
		{
			this.window = window;
			this.gitManager = gitManager;
		}

		public int Compare(StatusListEntry x, StatusListEntry y)
		{
			var stateCompare = window.IsGrouping() ? GetPriority(x.State).CompareTo(GetPriority(y.State)) : 0;
			if (stateCompare == 0)
			{
				var settings = window.GitDiffSettings;

				if (settings.sortDir == GitDiffWindow.SortDir.Descending)
				{
					var oldLeft = x;
					x = y;
					y = oldLeft;
				}

				if (settings.unstagedChangesPriority)
				{
					var canStageX = GitManager.CanStage(x.State);
					var canUnstageX = GitManager.CanUnstage(x.State);
					var canStageY = GitManager.CanStage(y.State);
					var canUnstageY = GitManager.CanUnstage(y.State);

					//prioritize upsaged changes that are pending
					if ((canStageX && canUnstageX) && !(canStageY && canUnstageY))
					{
						return -1;
					}
					if (!(canStageX && canUnstageX) && (canStageY && canUnstageY))
					{
						return 1;
					}
				}

				switch (settings.sortType)
				{
					case GitDiffWindow.SortType.Name:
						stateCompare = string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase);
						break;
					case GitDiffWindow.SortType.Path:
						stateCompare = string.Compare(x.LocalPath, y.LocalPath, StringComparison.InvariantCultureIgnoreCase);
						break;
					case GitDiffWindow.SortType.ModificationDate:
						//todo cache modification dates
						var modifedTimeLeft = GetClosest(gitManager.GetPathWithMeta(x.LocalPath).Select(p => File.GetLastWriteTime(UniGitPathHelper.Combine(gitManager.GetCurrentRepoPath(), p))));
						var modifedRightTime = GetClosest(gitManager.GetPathWithMeta(x.LocalPath).Select(p => File.GetLastWriteTime(UniGitPathHelper.Combine(gitManager.GetCurrentRepoPath(),p))));
						stateCompare = DateTime.Compare(modifedRightTime,modifedTimeLeft);
						break;
					case GitDiffWindow.SortType.CreationDate:
						//todo cache creation dates
						var createdTimeLeft = GetClosest(gitManager.GetPathWithMeta(x.LocalPath).Select(p => File.GetCreationTime(UniGitPathHelper.Combine(gitManager.GetCurrentRepoPath(),p))));
						var createdRightTime = GetClosest(gitManager.GetPathWithMeta(y.LocalPath).Select(p => File.GetCreationTime(UniGitPathHelper.Combine(gitManager.GetCurrentRepoPath(),p))));
						stateCompare = DateTime.Compare(createdRightTime,createdTimeLeft);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			if (stateCompare == 0)
			{
				stateCompare = string.Compare(x.LocalPath, y.LocalPath, StringComparison.Ordinal);
			}
			return stateCompare;
		}

		private DateTime GetClosest(IEnumerable<DateTime> dates)
		{
			var now = DateTime.MaxValue;
			var closest = DateTime.Now;
			var min = long.MaxValue;

			foreach (var date in dates)
				if (Math.Abs(date.Ticks - now.Ticks) < min)
				{
					min = date.Ticks - now.Ticks;
					closest = date;
				}

			return closest;
		}

		private static int GetPriority(FileStatus status)
		{
			if (status.IsFlagSet(FileStatus.Conflicted))
			{
				return -1;
			}
			if (status.IsFlagSet(FileStatus.NewInIndex | FileStatus.NewInWorkdir))
			{
				return 1;
			}
			if (status.IsFlagSet(FileStatus.ModifiedInIndex | FileStatus.ModifiedInWorkdir))
			{
				return 2;
			}
			if (status.IsFlagSet(FileStatus.DeletedFromIndex | FileStatus.DeletedFromWorkdir))
			{
				return 3;
			}
			if (status.IsFlagSet(FileStatus.RenamedInIndex | FileStatus.RenamedInWorkdir))
			{
				return 3;
			}
			return 4;
		}
	}
}
