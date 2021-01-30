﻿using System;
using UniGit.Status;
using UniGit.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniGit.Settings
{
	public abstract class GitSettingsTab : IDisposable
	{
		protected GitSettingsWindow settingsWindow;
		private bool hasFocused;
		private bool initialized;
		protected readonly GitManager gitManager;
		protected readonly GitSettingsJson gitSettings;
		protected readonly GUIContent name;
		protected readonly UniGitData data;
		protected readonly GitCallbacks gitCallbacks;
		protected readonly GitInitializer initializer;

		[UniGitInject]
		internal GitSettingsTab(GUIContent name,
			GitManager gitManager, 
			GitSettingsWindow settingsWindow,
			UniGitData data,
			GitSettingsJson gitSettings,
			GitCallbacks gitCallbacks,
			GitInitializer initializer)
		{
			this.name = name;
			this.gitManager = gitManager;
			this.settingsWindow = settingsWindow;
			this.data = data;
			this.gitSettings = gitSettings;
			this.gitCallbacks = gitCallbacks;
			this.initializer = initializer;
			gitCallbacks.EditorUpdate += OnEditorUpdateInternal;
			gitCallbacks.UpdateRepository += OnGitManagerUpdateInternal;
		}

        private void OnGUIInternal()
        {
            if (gitManager != null && initializer.IsValidRepo && initialized)
            {
                OnGUI();
            }
        }

		internal abstract void OnGUI();

		protected virtual void OnInitialize()
		{
			
		}

		public void OnFocus()
		{
			hasFocused = true;

            if (hasFocused && initialized && gitManager.Repository != null && data.Initialized && initializer.IsValidRepo)
            {
                OnGitManagerUpdateInternal(data.RepositoryStatus, null);
            }
        }

		public void OnLostFocus()
		{
			hasFocused = false;
        }

		public virtual VisualElement ConstructContents()
		{
			return new IMGUIContainer(OnGUIInternal);
		}

		public virtual void OnGitUpdate(GitRepoStatus status, string[] paths)
		{
			
		}

		private void OnGitManagerUpdateInternal(GitRepoStatus status, string[] paths)
		{
			//only update the window if it is initialized. That means opened and visible.
			//the editor window will initialize itself once it's focused
			if (!initialized || !initializer.IsValidRepo) return;
			OnGitUpdate(status, paths);
		}

		private void OnEditorUpdateInternal()
        {
            //Only initialize if the editor Window is focused
            if (!hasFocused || initialized || gitManager.Repository == null) return;
            if (!data.Initialized) return;
            initialized = true;
            if (!initializer.IsValidRepo) return;
            OnInitialize();
            OnGitManagerUpdateInternal(data.RepositoryStatus, null);
        }

		public void Dispose()
		{
			if(gitCallbacks == null) return;
			gitCallbacks.EditorUpdate -= OnEditorUpdateInternal;
			gitCallbacks.UpdateRepository -= OnGitManagerUpdateInternal;
		}

		public GUIContent Name => name;
    }
}