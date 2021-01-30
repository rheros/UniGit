﻿using System.IO;
using System.Linq;
using LibGit2Sharp;
using UniGit.Utils;
using UnityEditor;
using UnityEngine;
using Tree = LibGit2Sharp.Tree;

namespace UniGit
{
	public class GitCommitDetailsWindow : PopupWindowContent
	{
		private readonly Commit commit;
		private readonly GUIStyle commitMessageStyle;
		private readonly TreeChanges changes;
		private readonly Tree commitTree;
		private Vector2 scroll;
		private readonly GitManager gitManager;
		private readonly GitExternalManager externalManager;
		private readonly GitOverlay gitOverlay;

		[UniGitInject]
		public GitCommitDetailsWindow(GitManager gitManager,GitExternalManager externalManager,Commit commit,GitOverlay gitOverlay)
		{
			this.gitManager = gitManager;
			this.commit = commit;
			this.externalManager = externalManager;
			this.gitOverlay = gitOverlay;
			commitTree = commit.Tree;
			var parentCommit = commit.Parents.FirstOrDefault();

			if (parentCommit != null)
			{
				changes = gitManager.Repository.Diff.Compare<TreeChanges>(parentCommit.Tree, commitTree);
			}

			commitMessageStyle = new GUIStyle(GitGUI.Styles.ObjectField) {alignment = TextAnchor.UpperLeft,padding = new RectOffset(4,4,4,4),wordWrap = true};
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(640, 256);
		}

		public override void OnGUI(Rect rect)
		{
			EditorGUILayout.Space();
			var msgHeight = commitMessageStyle.CalcHeight(GitGUI.GetTempContent(commit.Message), rect.width);
			scroll = EditorGUILayout.BeginScrollView(scroll);
			EditorGUILayout.LabelField(GitGUI.GetTempContent(commit.Message), commitMessageStyle, GUILayout.Height(msgHeight));
			if (changes != null)
			{
				foreach (var change in changes)
				{
					//EditorGUILayout.BeginHorizontal();
					//GUILayout.Label(change.Status.ToString(), "AssetLabel");
					EditorGUILayout.BeginHorizontal("ProjectBrowserHeaderBgTop");
					GUILayout.Label(new GUIContent(gitOverlay.GetDiffTypeIcon(change.Status, true)) {tooltip = change.Status.ToString()}, GUILayout.Width(16));
					GUILayout.Space(8);
					var pathChunks = change.Path.Split(Path.DirectorySeparatorChar);
					for (var i = 0; i < pathChunks.Length; i++)
					{
						var chunk = pathChunks[i];
						if (GUILayout.Button(GitGUI.GetTempContent(chunk), GitGUI.Styles.BreadcrumMid))
						{
							var assetPath = string.Join("/", pathChunks,0,i+1);
							if (UniGitPathHelper.IsMetaPath(assetPath))
							{
								assetPath = GitManager.AssetPathFromMeta(assetPath);
							}
							ShowContextMenuForElement(change.Path, assetPath);
						}
						
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				DrawTreeEntry(commitTree, 0);
			}
			EditorGUILayout.Space();
			EditorGUILayout.EndScrollView();
		}

		private void ShowContextMenuForElement(string changePath,string assetPath)
		{
			var menu = new GenericMenu();
			if (commit.Parents.Count() == 1)
			{

				menu.AddItem(new GUIContent("Difference with previous commit"), false, () =>
				{
					var parent = commit.Parents.Single();
					gitManager.ShowDiff(changePath, parent,commit, externalManager);
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(new GUIContent("Difference with previous commit")));
			}
			menu.AddItem(new GUIContent("Difference with HEAD"), false, () =>
			{
				gitManager.ShowDiff(changePath, commit, gitManager.Repository.Head.Tip, externalManager);
			});
			menu.AddItem(new GUIContent("Select In Project"), false, () =>
			{
				var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
				if (asset != null)
				{
					Selection.activeObject = asset;
				}
			});
			menu.ShowAsContext();
		}

		private void DrawTreeEntry(Tree tree, int depth)
		{
			foreach (var file in tree)
			{
				if (file.TargetType == TreeEntryTargetType.Tree)
				{
					EditorGUI.indentLevel = depth;
					EditorGUILayout.LabelField(Path.GetFileName(file.Path));
					DrawTreeEntry(file.Target as Tree, depth + 1);
				}
				else if (!UniGitPathHelper.IsMetaPath(file.Path))
				{
					EditorGUI.indentLevel = depth;
					EditorGUILayout.LabelField(file.Path);
				}
			}
		}
	}
}