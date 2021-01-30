﻿using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using UniGit.Utils;
using UnityEditor;
using UnityEngine;

namespace UniGit
{
	public class GitBlameWizard : EditorWindow
	{
		private const float CommitLineHeight = 21;

		[SerializeField] private string blameLocalPath;

		private BlameHunkCollection blameHunk;
		private string[] lines;
		private Vector2 linesScroll;
		private Vector2 hunksScroll;
		private string selectedCommit;
		private LogEntry[] commitLog;
		private bool isResizingCommits;
		private float commitsWindowHeight = 180;
		private string invalidMessage;
		private GitManager manager;

		private class Styles
		{
			public GUIStyle lineStyle;
			public GUIStyle lineStyleSelected;
			public GUIStyle lineNumStyle;
			public GUIStyle hunkStyle;
		}
		private Styles styles;

		internal void SetBlamePath(string blameLocalPath)
		{
			this.blameLocalPath = blameLocalPath;
			CheckBlame();
			LoadFileLines();

			titleContent = new GUIContent("Git Blame: " + blameLocalPath);
		}

        [UniGitInject]
		private void Construct(GitManager gitManager)
		{
			manager = gitManager;
		}

	    private void OnEnable()
	    {
	        GitWindows.AddWindow(this);
	    }

	    private void OnDisable()
	    {
	        GitWindows.RemoveWindow(this);
	    }

        private void CheckBlame()
		{
			try
			{
				blameHunk = manager.Repository.Blame(blameLocalPath);
				invalidMessage = null;
			}
			catch (Exception e)
			{
				invalidMessage = e.Message;
			}
		}

		private void LoadFileLines()
		{
			var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(manager.ToProjectPath(blameLocalPath));
			if (asset != null)
			{
				lines = asset.text.Split(new[] { UniGitPathHelper.NewLineChar }, StringSplitOptions.None);
			}
			else
			{
				lines = File.ReadAllLines(UniGitPathHelper.Combine(manager.GetCurrentRepoPath(),blameLocalPath));
			}
			
			commitLog = manager.Repository.Commits.QueryBy(blameLocalPath).Where(e => blameHunk.Any(h => h.FinalCommit.Sha == e.Commit.Sha)).ToArray();
		}

		private void InitGUI()
		{
			styles = new Styles()
			{
				lineStyle = new GUIStyle("CN StatusInfo") { padding = { left = 4 } },
				lineStyleSelected = new GUIStyle(EditorStyles.label) { padding = { left = 4 }, normal = { background = ((GUIStyle)"ChannelStripAttenuationBar").normal.background } },
				lineNumStyle = new GUIStyle(EditorStyles.label) { normal = { background = ((GUIStyle)"OL EntryBackEven").normal.background }, padding = { left = 4 } },
				hunkStyle = new GUIStyle("CN EntryBackOdd") {alignment = TextAnchor.MiddleLeft}
			};
		}

		private void Update()
		{
			if (!string.IsNullOrEmpty(blameLocalPath) && blameHunk == null && manager.Repository != null)
			{
				CheckBlame();
				LoadFileLines();
				titleContent = new GUIContent("Git Blame: " + blameLocalPath);
			}
		}

		protected void OnGUI()
		{
			if (styles == null) InitGUI();

			if (!string.IsNullOrEmpty(invalidMessage))
			{
				EditorGUILayout.HelpBox(invalidMessage,MessageType.Error,true);
				if (GUILayout.Button(GitGUI.GetTempContent("Close")))
				{
					Close();
				}
				return;
			}

			if(Event.current.type == EventType.MouseMove) Repaint();

			if (blameHunk == null)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(GitGUI.IconContent("WaitSpin00"));
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.LabelField(GitGUI.GetTempContent("Checking Blame..."),EditorStyles.centeredGreyMiniLabel);
			}
			else
			{
				var scrollRect = new Rect(0,0,position.width,position.height- commitsWindowHeight);
				var viewRect = new Rect(0,0,0,lines.Length * EditorGUIUtility.singleLineHeight);
				foreach (var line in lines)
                {
                    viewRect.width = Mathf.Max(viewRect.width, styles.lineStyle.CalcSize(GitGUI.GetTempContent(line)).x);
                }
				viewRect.width += 32;
				linesScroll = GUI.BeginScrollView(scrollRect, linesScroll, viewRect);
				for (var i = 0; i < lines.Length; i++)
				{
					var lineContent = GitGUI.GetTempContent(lines[i]);
					var lineRect = new Rect(32, i * EditorGUIUtility.singleLineHeight, viewRect.width - 32, EditorGUIUtility.singleLineHeight);
					if (lineRect.y < linesScroll.y + scrollRect.height && lineRect.y + lineRect.height > linesScroll.y)
                    {
                        var isFromHunk = blameHunk.Any(hunk => hunk.ContainsLine(i) && hunk.FinalCommit.Sha == selectedCommit);
                        switch (Event.current.type)
                        {
                            case EventType.Repaint:
                                styles.lineNumStyle.Draw(new Rect(0, i * EditorGUIUtility.singleLineHeight, 32, EditorGUIUtility.singleLineHeight), i.ToString(),false,false,false,false);
                                styles.lineStyle.Draw(lineRect,lineContent,false,false,isFromHunk,false);
                                break;
                            case EventType.MouseDown when Event.current.button == 0 && lineRect.Contains(Event.current.mousePosition):
                            {
                                foreach (var hunk in blameHunk)
                                {
                                    if (hunk.ContainsLine(i))
                                    {
                                        selectedCommit = hunk.FinalCommit.Sha;
                                        MoveToCommit(selectedCommit);
                                        Repaint();
                                        break;
                                    }
                                }

                                break;
                            }
                        }
                    }
				}
				GUI.EndScrollView();

				DoCommitsResize(new Rect(0, position.height - commitsWindowHeight, position.width, 4));

				var hunkCount = 0;
				float hunkMaxWidth = 0;
				foreach (var entry in commitLog)
				{
					var hunkSize = styles.hunkStyle.CalcSize(GitGUI.GetTempContent(entry.Commit.MessageShort));
					hunkMaxWidth = Mathf.Max(hunkMaxWidth, hunkSize.x);
					hunkCount++;
				}
				viewRect = new Rect(0, 0, hunkMaxWidth, hunkCount * CommitLineHeight);
				scrollRect = new Rect(0, position.height - commitsWindowHeight + 4, position.width, commitsWindowHeight - 4);
				hunksScroll = GUI.BeginScrollView(scrollRect, hunksScroll, viewRect);

				var hunkId = 0;
				foreach (var entry in commitLog)
				{
					var commitContent = GitGUI.GetTempContent(entry.Commit.MessageShort);
					var commitRect = new Rect(0, hunkId * CommitLineHeight, hunkMaxWidth, CommitLineHeight);
					var commitInfoRect = new Rect(commitRect.x, commitRect.y, 24, commitRect.height);
					EditorGUIUtility.AddCursorRect(commitInfoRect,MouseCursor.Link);
					if (Event.current.type == EventType.Repaint)
					{
						var controlId = GUIUtility.GetControlID(commitContent, FocusType.Passive, commitRect);
						styles.hunkStyle.Draw(commitRect, commitContent, controlId, selectedCommit == entry.Commit.Sha);
						GUIStyle.none.Draw(commitInfoRect, GitGUI.IconContent("SubAssetCollapseButton"), false,false,false,false);
					}
					else if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
					{
						if (commitInfoRect.Contains(Event.current.mousePosition))
						{
							PopupWindow.Show(commitInfoRect,new CommitInfoPopupContent(entry.Commit));
							Event.current.Use();
						}
						else if (commitRect.Contains(Event.current.mousePosition))
						{
							selectedCommit = entry.Commit.Sha;
							MoveToLineFromCommit(selectedCommit);
							Repaint();
							Event.current.Use();
						}
					}
					hunkId++;
				}

				GUI.EndScrollView();
			}
		}

		private void DoCommitsResize(Rect rect)
		{
			GUI.DrawTexture(rect,EditorGUIUtility.whiteTexture);
			EditorGUIUtility.AddCursorRect(rect,MouseCursor.ResizeVertical);
			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				isResizingCommits = true;
			}

			if (isResizingCommits)
			{
				commitsWindowHeight = Mathf.Max(position.height - Event.current.mousePosition.y,64);
				Repaint();
			}

			if (Event.current.type == EventType.MouseUp)
				isResizingCommits = false;
		}

		private void MoveToLineFromCommit(string sha)
		{
			foreach (var hunk in blameHunk)
            {
                if (hunk.FinalCommit.Sha != sha) continue;
                linesScroll.y = hunk.FinalStartLineNumber * EditorGUIUtility.singleLineHeight;
                break;
            }
		}

		private void MoveToCommit(string sha)
		{
			for (var j = 0; j < commitLog.Length; j++)
            {
                if (commitLog[j].Commit.Sha != sha) continue;
                hunksScroll.y = j * CommitLineHeight;
                break;
            }
		}

		public class CommitInfoPopupContent : PopupWindowContent
		{
			private readonly Commit commit;

			public CommitInfoPopupContent(Commit commit)
			{
				this.commit = commit;
			}

			public override Vector2 GetWindowSize()
			{
				return new Vector2(360, EditorStyles.wordWrappedLabel.CalcHeight(GitGUI.GetTempContent(commit.Message), 360) + EditorGUIUtility.singleLineHeight * 5);
			}

			public override void OnGUI(Rect rect)
			{
				EditorGUILayout.SelectableLabel(commit.Message, "AS TextArea");
				EditorGUILayout.TextField(GitGUI.GetTempContent("Author"), commit.Author.Name);
				EditorGUILayout.TextField(GitGUI.GetTempContent("Author Email"), commit.Author.Email);
				EditorGUILayout.TextField(GitGUI.GetTempContent("Date"), commit.Author.When.ToString());
				EditorGUILayout.TextField(GitGUI.GetTempContent("Revision (SHA)"), commit.Sha);
			}
		}
	}
}