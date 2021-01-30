﻿using System;
using System.IO;
using LibGit2Sharp;
using UniGit.Settings;
using UniGit.Utils;
using UniGit.Windows.Diff;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UniGit
{
	public class GitDiffWindowToolbarRenderer : IDiffWindowRenderer
	{
		private readonly GitManager gitManager;
		private readonly GitDiffElementContextFactory contextFactory;
		private readonly UniGitData data;
		private readonly InjectionHelper injectionHelper;
		private readonly GitSettingsJson gitSettings;
		private readonly GitOverlay gitOverlay;
		private readonly SearchField searchField;
		private readonly IGitPrefs prefs;

		[UniGitInject]
		public GitDiffWindowToolbarRenderer(GitManager gitManager,GitDiffElementContextFactory contextFactory,
			UniGitData data, InjectionHelper injectionHelper,GitSettingsJson gitSettings,GitOverlay gitOverlay,IGitPrefs prefs)
		{
			this.gitManager = gitManager;
			this.contextFactory = contextFactory;
			this.data = data;
			this.injectionHelper = injectionHelper;
			this.gitSettings = gitSettings;
			this.gitOverlay = gitOverlay;
			this.prefs = prefs;
			searchField = new SearchField();
		}

		public void LoadStyles()
		{
		}

		internal void DoDiffToolbar(Rect rect,GitDiffWindow window,ref string filter)
		{
			var settings = window.GitDiffSettings;
			GUILayout.BeginArea(rect, GUIContent.none, EditorStyles.toolbar);
			EditorGUILayout.BeginHorizontal();
			var btRect = GUILayoutUtility.GetRect(GitGUI.GetTempContent("Edit"), EditorStyles.toolbarDropDown, GUILayout.MinWidth(64));
			if (GUI.Button(btRect,GitGUI.GetTempContent("Edit"), EditorStyles.toolbarDropDown))
			{
				var editMenu = new GenericMenuWrapper(new GenericMenu());
				contextFactory.Build(editMenu,window);
				editMenu.GenericMenu.DropDown(btRect);
			}
			btRect = GUILayoutUtility.GetRect(GitGUI.GetTempContent("View"), EditorStyles.toolbarDropDown, GUILayout.MinWidth(64));
			if (GUI.Button(btRect,GitGUI.GetTempContent("View"), EditorStyles.toolbarDropDown))
			{
				var viewMenu = new GenericMenuWrapper(new GenericMenu());
				viewMenu.AddItem(new GUIContent("Small Elements"), prefs.GetBool(GitDiffWindowDiffElementRenderer.SmallElementsKey,false), () => { prefs.SetBool(GitDiffWindowDiffElementRenderer.SmallElementsKey,!prefs.GetBool(GitDiffWindowDiffElementRenderer.SmallElementsKey,false)); });
				viewMenu.GenericMenu.DropDown(btRect);
			}
			btRect = GUILayoutUtility.GetRect(GitGUI.GetTempContent("Filter"), EditorStyles.toolbarDropDown, GUILayout.MinWidth(64));
			if (GUI.Button(btRect,GitGUI.GetTempContent("Filter"), EditorStyles.toolbarDropDown))
			{
				var genericMenu = new GenericMenu();
				var fileStatuses = (FileStatus[])Enum.GetValues(typeof(FileStatus));
				genericMenu.AddItem(new GUIContent("Show All"), settings.showFileStatusTypeFilter == (FileStatus)(-1), () =>
				{
					settings.showFileStatusTypeFilter = (FileStatus)(-1);
					window.UpdateStatusList();
				});
				genericMenu.AddItem(new GUIContent("Show None"), settings.showFileStatusTypeFilter == 0, () =>
				{
					settings.showFileStatusTypeFilter = 0;
					window.UpdateStatusList();
				});
				for (var i = 0; i < fileStatuses.Length; i++)
				{
					var flag = fileStatuses[i];
					genericMenu.AddItem(new GUIContent(flag.ToString()), settings.showFileStatusTypeFilter != (FileStatus)(-1) && settings.showFileStatusTypeFilter.IsFlagSet(flag), () =>
					{
						settings.showFileStatusTypeFilter = settings.showFileStatusTypeFilter.SetFlags(flag, !settings.showFileStatusTypeFilter.IsFlagSet(flag));
						window.UpdateStatusList();
					});
				}
				genericMenu.DropDown(btRect);
			}
			btRect = GUILayoutUtility.GetRect(GitGUI.GetTempContent("Sort"), EditorStyles.toolbarDropDown, GUILayout.MinWidth(64));
			if (GUI.Button(btRect,GitGUI.GetTempContent("Sort"), EditorStyles.toolbarDropDown))
			{
				var genericMenu = new GenericMenu();
				foreach (GitDiffWindow.SortType type in Enum.GetValues(typeof(GitDiffWindow.SortType)))
				{
					var t = type;
					genericMenu.AddItem(new GUIContent(type.GetDescription()), type == settings.sortType, () =>
					{
						settings.sortType = t;
						window.UpdateStatusList();
					});
				}
				genericMenu.AddSeparator("");
				foreach (GitDiffWindow.SortDir dir in Enum.GetValues(typeof(GitDiffWindow.SortDir)))
				{
					var d = dir;
					genericMenu.AddItem(new GUIContent(dir.GetDescription()), dir == settings.sortDir, () =>
					{
						settings.sortDir = d;
						window.UpdateStatusList();
					});
				}
				genericMenu.AddSeparator("");
				genericMenu.AddItem(new GUIContent("Group"), settings.merge, () =>
				{
					settings.merge = !settings.merge;
					window.UpdateStatusList();
				});
				genericMenu.AddItem(new GUIContent("Prioritize Unstaged Changes"),settings.unstagedChangesPriority, () =>
				{
					settings.unstagedChangesPriority = !settings.unstagedChangesPriority;
					window.UpdateStatusList();
				});
				genericMenu.DropDown(btRect);
			}

			var modulesContent = GitGUI.GetTempContent("Modules");
			foreach (var subModule in data.RepositoryStatus.SubModuleEntries)
			{
				if (subModule.Status == SubmoduleStatus.InConfig)
				{
					modulesContent.image = GitGUI.Textures.WarrningIconSmall;
					modulesContent.tooltip = "Some modules are in config only";
					break;
				}
				if (subModule.Status.HasFlag(SubmoduleStatus.WorkDirUninitialized))
				{
					modulesContent.image = GitGUI.Textures.WarrningIconSmall;
					modulesContent.tooltip = "Uninitialized modules";
					break;
				}
				if (subModule.Status.HasFlag(SubmoduleStatus.WorkDirModified))
				{
					modulesContent.image = GitGUI.Textures.CollabPush;
					break;
				}
				if (subModule.Status.HasFlag(SubmoduleStatus.WorkDirModified))
				{
					modulesContent.image = gitOverlay.icons.modifiedIconSmall.image;
					break;
				}
				if (subModule.Status.HasFlag(SubmoduleStatus.WorkDirFilesUntracked))
				{
					modulesContent.image = gitOverlay.icons.untrackedIconSmall.image;
					break;
				}
			}
			
			btRect = GUILayoutUtility.GetRect(modulesContent, EditorStyles.toolbarDropDown, GUILayout.MinWidth(86));
			if (GUI.Button(btRect,modulesContent, EditorStyles.toolbarDropDown))
			{
				PopupWindow.Show(btRect,injectionHelper.CreateInstance<GitSubModulesPopup>());
			}

			EditorGUILayout.Space();

			if (!gitManager.InSubModule)
			{
				GUILayout.Toggle(true, GitGUI.GetTempContent("Main"), "GUIEditor.BreadcrumbLeft",GUILayout.MinWidth(86));
			}
			else
			{
				if(GUILayout.Button(GitGUI.GetTempContent("Main"), "GUIEditor.BreadcrumbLeft",GUILayout.MinWidth(86)))
				{
					gitManager.SwitchToMainRepository();
				}

				GUILayout.Toggle(true, GitGUI.GetTempContent(Path.GetFileName(gitSettings.ActiveSubModule),gitOverlay.icons.submoduleIconSmall.image), "GUIEditor.BreadcrumbMid",GUILayout.MinWidth(86));
			}

			var isUpdating = gitManager.IsUpdating;
			var isStaging = gitManager.IsAsyncStaging;
			var isDirty = gitManager.IsDirty;
			var statusListUpdate = window.GetStatusListUpdateOperation() != null && !window.GetStatusListUpdateOperation().IsDone;
			GUIContent statusContent = null;

			if (isUpdating)
			{
				statusContent = GitGUI.GetTempContent("Updating...",GitGUI.GetTempSpinAnimatedTexture());
			}
			else if (isStaging)
			{
				statusContent = GitGUI.GetTempContent("Staging...",GitGUI.GetTempSpinAnimatedTexture());
			}
			else if (isDirty)
			{
				var updateStatus = GetUpdateStatusMessage(gitManager.GetUpdateStatus());
				statusContent =  GitGUI.GetTempContent(updateStatus + "... ",GitGUI.GetTempSpinAnimatedTexture());
			}
			else if (statusListUpdate)
			{
				statusContent = GitGUI.GetTempContent(window.GetStatusBuildingState(),GitGUI.GetTempSpinAnimatedTexture());
			}

			GUILayout.FlexibleSpace();

			if (statusContent != null)
			{
				GUILayout.Label(statusContent, EditorStyles.toolbarButton);
				if(gitSettings.AnimationType.HasFlag(GitSettingsJson.AnimationTypeEnum.Loading)) window.Repaint();
			}

			filter = searchField.OnToolbarGUI(filter);
			EditorGUILayout.EndHorizontal();
			GUILayout.EndArea();

		}

		private string GetUpdateStatusMessage(GitManager.UpdateStatusEnum status)
        {
            return status switch
            {
                GitManager.UpdateStatusEnum.InvalidRepo => "Invalid Repository",
                GitManager.UpdateStatusEnum.SwitchingToPlayMode => "Switching to play mode",
                GitManager.UpdateStatusEnum.Compiling => "Compiling",
                GitManager.UpdateStatusEnum.UpdatingAssetDatabase => "Updating Asset Database",
                GitManager.UpdateStatusEnum.Updating => "Updating in progress",
                _ => "Waiting to update"
            };
        }
	}
}
