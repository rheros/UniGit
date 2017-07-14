﻿using System.IO;
using UniGit.Settings;
using UniGit.Utils;
using UnityEditor;
using UnityEngine;

namespace UniGit
{
	[InitializeOnLoad]
	internal static class UniGitLoader
	{
		private static readonly GitCallbacks callbacks;

		static UniGitLoader()
		{
			var recompileChecker = ScriptableObject.CreateInstance<AssemblyReloadScriptableChecker>();
			recompileChecker.OnBeforeReloadAction = OnBeforeAssemblyReload;

			string repoPath = Application.dataPath.Replace("/Assets", "").Replace("/", "\\");
			string settingsPath = Path.Combine(repoPath, Path.Combine(".git",Path.Combine("UniGit", "Settings.json")));

			callbacks = new GitCallbacks();
			EditorApplication.update += callbacks.IssueEditorUpdate;
			callbacks.RefreshAssetDatabase += AssetDatabase.Refresh;
			callbacks.SaveAssetDatabase += AssetDatabase.SaveAssets;
			var settings = new GitSettingsJson();
			var settingsManager = new GitSettingsManager(settings, settingsPath, callbacks);
			settingsManager.LoadGitSettings();
			var editorPrefs = new UnityEditorGitPrefs();

			GitManager gitManager = new GitManager(repoPath, callbacks, settings, editorPrefs);
			GitManager.Instance = gitManager;

			//delayed called must be used for serialized properties to be loaded
			EditorApplication.delayCall += () =>
			{
				settingsManager.LoadOldSettingsFile();
				gitManager.MarkDirty(true);
			};

			if (!gitManager.IsValidRepo)
			{
				return;
			}

			GitUnityMenu.Init(gitManager);
			GitProjectContextMenus.Init(gitManager);
			GitResourceManager.Initilize();
			GitCredentialsManager.Load(gitManager);
			GitOverlay.Initlize(gitManager);
			GitLfsManager.Load(gitManager);
			GitHookManager.Load(gitManager);
			GitExternalManager.Load(gitManager);
		}

		private static void OnBeforeAssemblyReload()
		{
			if(GitManager.Instance != null) GitManager.Instance.Dispose();
		}
	}
}