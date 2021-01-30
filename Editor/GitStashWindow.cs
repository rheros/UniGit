﻿using System.Linq;
using LibGit2Sharp;
using UniGit.Utils;
using UnityEditor;
using UnityEngine;

namespace UniGit
{
	public class GitStashWindow : PopupWindowContent
	{
		private readonly GitManager gitManager;
		private readonly GitOverlay gitOverlay;
		private readonly GitCallbacks gitCallbacks;

		private StashCollection stashCollection;
		private Vector2 stashScroll;
		private int selectedStash;
		private GUIStyle stashStyle;

		[UniGitInject]
		public GitStashWindow(GitManager gitManager,GitOverlay gitOverlay,GitCallbacks gitCallbacks)
		{
			this.gitManager = gitManager;
			this.gitOverlay = gitOverlay;
			this.gitCallbacks = gitCallbacks;
		}

		public override void OnOpen()
		{
			base.OnOpen();
			stashCollection = gitManager.Repository.Stashes;
			stashStyle = new GUIStyle("MenuItem") {wordWrap = true,fixedHeight = 0,normal = {background = ((GUIStyle)"ProjectBrowserHeaderBgTop").normal.background }};
		}

		public override void OnGUI(Rect rect)
		{
			if(Event.current.type == EventType.MouseMove) editorWindow.Repaint();
			var stashCount = stashCollection.Count();
			EditorGUILayout.BeginHorizontal("IN BigTitle");
			if (GUILayout.Button(GitGUI.GetTempContent("Stash Save",gitOverlay.icons.stashIcon.image,"Save changes in working directory to stash.")))
			{
				UniGitLoader.GetWindow<GitStashSaveWizard>(true);
			}
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;
			stashScroll = EditorGUILayout.BeginScrollView(stashScroll, GUILayout.ExpandHeight(true));
			var stashId = 0;
			foreach (var stash in stashCollection)
			{
				var msg = stash.Message;
				var stashContent = GitGUI.GetTempContent(msg);
				var stastRect = GUILayoutUtility.GetRect(stashContent, stashStyle);
				if (Event.current.type == EventType.Repaint)
				{
					stashStyle.Draw(stastRect, stashContent, stastRect.Contains(Event.current.mousePosition) || stashId == selectedStash, false, false, false);
				}
				else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && stastRect.Contains(Event.current.mousePosition))
				{
					selectedStash = stashId;
				}
				stashId ++;
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal("ProjectBrowserBottomBarBg");
			GUI.enabled = stashCount > 0;
			if (GUILayout.Button(GitGUI.GetTempContent("Apply","Apply stash to working directory."), EditorStyles.miniButtonLeft))
			{
				if (EditorUtility.DisplayDialog("Apply Stash: " + selectedStash,"Are you sure you want to apply stash ? This will override your current working directory!","Apply","Cancel"))
				{
					stashCollection.Apply(selectedStash);
					gitManager.MarkDirty(true);
					gitCallbacks.IssueAssetDatabaseRefresh();
				}
			}
			if (GUILayout.Button(GitGUI.GetTempContent("Pop","Remove and apply stash to working directory."), EditorStyles.miniButtonMid))
			{
				if (EditorUtility.DisplayDialog("Pop Stash: " + selectedStash, "Are you sure you want to pop the stash ? This will override your current working directory and remove the stash from the list.", "Pop and Apply", "Cancel"))
				{
					stashCollection.Pop(selectedStash);
					gitManager.MarkDirty(true);
					gitCallbacks.IssueAssetDatabaseRefresh();
				}
			}
			if (GUILayout.Button(GitGUI.GetTempContent("Remove","Remove stash from list"), EditorStyles.miniButtonRight))
			{
				if (EditorUtility.DisplayDialog("Remove Stash: " + selectedStash, "Are you sure you want to remove the stash ? This action cannot be undone!", "Remove", "Cancel"))
				{
					stashCollection.Remove(selectedStash);
				}
			}
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
		}
	}
}