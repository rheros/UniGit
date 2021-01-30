﻿using System;
using System.Linq;
using LibGit2Sharp;
using UniGit.Utils;
using UnityEditor;
using UnityEngine;

namespace UniGit
{
	public class GitAutoFetcher : IDisposable
	{
		private readonly GitCredentialsManager credentialsManager;
		private readonly GitManager gitManager;
		private readonly GitCallbacks gitCallbacks;
		private readonly GitSettingsJson gitSettings;
		private readonly ILogger logger;
		private readonly GitInitializer initializer;
		private bool needsFetch;

		[UniGitInject]
		public GitAutoFetcher(GitManager gitManager,
			GitCredentialsManager credentialsManager,
			GitCallbacks gitCallbacks,
			GitSettingsJson gitSettings,
			ILogger logger,
			GitInitializer initializer)
		{
			this.gitManager = gitManager;
			this.credentialsManager = credentialsManager;
			this.gitCallbacks = gitCallbacks;
			this.logger = logger;
			this.gitSettings = gitSettings;
			this.initializer = initializer;
			gitCallbacks.EditorUpdate += OnEditorUpdate;
			needsFetch = !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
		}

		private void OnEditorUpdate()
		{
			if (needsFetch)
			{
				try
				{
					needsFetch = AutoFetchChanges();
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					needsFetch = false;
				}
			}
		}

		private bool AutoFetchChanges()
		{
			if (gitManager.Repository == null || !initializer.IsValidRepo || !gitSettings.AutoFetch) return false;
			var remote = gitManager.Repository.Network.Remotes.FirstOrDefault();
			if (remote == null) return false;
			GitProfilerProxy.BeginSample("Git automatic fetching");
			try
			{
                GitCommands.Fetch(gitManager.Repository, remote, new FetchOptions()
				{
					CredentialsProvider = credentialsManager.FetchChangesAutoCredentialHandler,
					OnTransferProgress = gitManager.FetchTransferProgressHandler,
					RepositoryOperationStarting = (context) =>
					{
						logger.Log(LogType.Log,"Repository Operation Starting");
						return true;
					}
				});
				//Debug.LogFormat("Auto Fetch From remote: {0} - ({1}) successful.", remote.Name, remote.Url);
			}
			catch (Exception e)
			{
				logger.LogFormat(LogType.Error,"Automatic Fetching from remote: {0} with URL: {1} Failed!",remote.Name, remote.Url);
				logger.LogException(e);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				GitProfilerProxy.EndSample();
			}
			return false;
		}

		public void Dispose()
		{
			if(gitCallbacks != null) gitCallbacks.EditorUpdate -= OnEditorUpdate;
		}
	}
}