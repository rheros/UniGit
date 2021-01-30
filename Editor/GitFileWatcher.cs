﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using UniGit.Utils;

namespace UniGit
{
	public class GitFileWatcher : IDisposable
	{
		private readonly List<FileSystemWatcher> fileWatchers;
		private readonly GitManager gitManager;
		private readonly GitSettingsJson gitSettings;
		private readonly GitCallbacks gitCallbacks;
		private readonly Regex ignoreFoldersRegex;
        private UniGitPaths paths;

		[UniGitInject]
		public GitFileWatcher(GitManager gitManager,
			GitCallbacks gitCallbacks,
			GitSettingsJson gitSettings,
            UniGitPaths paths,
			[UniGitInjectOptional] bool trackAssetsPath)
        {
            this.paths = paths;
			this.gitManager = gitManager;
			this.gitSettings = gitSettings;
			this.gitCallbacks = gitCallbacks;
			fileWatchers = new List<FileSystemWatcher>();

			var regexPattern = @".*.git$";
			if (!trackAssetsPath) regexPattern += "|.*Assets$";
			ignoreFoldersRegex = new Regex(regexPattern);

			gitCallbacks.OnSettingsChange += OnSettingsChange;
			gitCallbacks.OnRepositoryLoad += OnRepositoryLoad;
		}

		private void ClearWatchers()
		{
			foreach (var fileWatcher in fileWatchers)
			{
				Unsubscribe(fileWatcher);
				fileWatcher.Dispose();
			}

			fileWatchers.Clear();
		}

		private void CreateWatchers()
		{
			var rootedPath = gitManager.GetCurrentRepoPath();
			var projectPath = rootedPath.Replace(paths.ProjectPath, "");
            if (UniGitPathHelper.IsPathInAssetFolder(projectPath)) return;
            var mainFileWatcher = new FileSystemWatcher(rootedPath)
            {
                InternalBufferSize = 4,
                EnableRaisingEvents = gitSettings.TrackSystemFiles,
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName
            };
            fileWatchers.Add(mainFileWatcher);
            Subscribe(mainFileWatcher);

            var repoDirectoryInfo = new DirectoryInfo(rootedPath);
            foreach (var directory in repoDirectoryInfo.GetDirectories())
            {
                if (gitManager.Repository.Ignore.IsPathIgnored(directory.FullName) ||
                    !ShouldTrackDirectory(directory)) continue;
                var fileWatcher = new FileSystemWatcher(directory.FullName)
                {
                    InternalBufferSize = 4,
                    EnableRaisingEvents = gitSettings.TrackSystemFiles,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName
                };

                fileWatchers.Add(fileWatcher);
                Subscribe(fileWatcher);
            }
        }

		private void OnSettingsChange()
		{
			foreach (var fileWatcher in fileWatchers)
			{
				fileWatcher.EnableRaisingEvents = gitSettings.TrackSystemFiles;
			}
		}

		private void OnRepositoryLoad(Repository repository)
		{
			ClearWatchers();
			CreateWatchers();
		}

		private bool ShouldTrackDirectory(DirectoryInfo directory)
		{
			return !directory.Attributes.HasFlag(FileAttributes.Hidden) && !ignoreFoldersRegex.IsMatch(directory.FullName);
		}

		private void Subscribe(FileSystemWatcher watcher)
		{
			watcher.Created += WatcherActivity;
			watcher.Deleted += WatcherActivity;
			watcher.Changed += WatcherActivity;
			watcher.Renamed += WatcherActivity;
		}

		private void Unsubscribe(FileSystemWatcher watcher)
		{
			watcher.Created -= WatcherActivity;
			watcher.Deleted -= WatcherActivity;
			watcher.Changed -= WatcherActivity;
			watcher.Renamed -= WatcherActivity;
		}

		private void WatcherActivity(object sender, FileSystemEventArgs e)
		{
			var relativePath = gitManager.GetRelativePath(e.FullPath);
			if (!gitManager.Repository.Ignore.IsPathIgnored(relativePath) && !gitManager.IsDirectory(relativePath))
			{
				if (e.ChangeType == WatcherChangeTypes.Renamed)
				{
					var relativeOldPath = ((RenamedEventArgs) e).OldFullPath;
					gitManager.MarkDirtyAuto(relativePath);
					gitManager.MarkDirtyAuto(relativeOldPath);
				}
				else
				{
					gitManager.MarkDirtyAuto(relativePath);
				}
			}
		}

		public bool WaitForChange(out WaitForChangedResult result,WatcherChangeTypes types,int timeout)
		{
			foreach (var fileWatcher in fileWatchers)
			{
				var newResult = fileWatcher.WaitForChanged(types, timeout);
                if (newResult.ChangeType == 0 && !newResult.TimedOut) continue;
                result = newResult;
                return false;
            }

			result = new WaitForChangedResult();
			return true;
		}

		public void Dispose()
		{
			gitCallbacks.OnSettingsChange -= OnSettingsChange;
			gitCallbacks.OnRepositoryLoad -= OnRepositoryLoad;

			foreach (var fileWatcher in fileWatchers)
			{
				Unsubscribe(fileWatcher);
				fileWatcher.Dispose();
			}
			fileWatchers.Clear();
		}
	}
}
