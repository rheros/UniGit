﻿using System;
using LibGit2Sharp;
using UniGit.Status;
using UniGit.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniGit
{
	public abstract class GitUpdatableWindow : EditorWindow, IGitWatcher
	{
		//used an object because the EditorWindow saves Booleans even if private
        [NonSerialized] protected GitManager gitManager;
		[NonSerialized] protected GitSettingsJson gitSettings;
		[NonSerialized] protected GitReflectionHelper reflectionHelper;
		[NonSerialized] protected UniGitData data;
		[NonSerialized] protected GitCallbacks gitCallbacks;
		[NonSerialized] protected ILogger logger;
        [NonSerialized] protected GitInitializer initializer;
		[NonSerialized] protected GitSettingsManager settingsManager;
		[NonSerialized] protected UniGitPaths paths;
        [NonSerialized] protected IGitResourceManager resourceManager;
        [NonSerialized] protected bool isFocused;

        #region VisualElements

        private VisualElement invalidRepoElement;
        private Label invalidRepoPathLabel;

        #endregion

        protected virtual void OnEnable()
		{
			GitWindows.AddWindow(this);
			if(gitManager != null)
				titleContent.image = gitManager.GetGitStatusIcon();

			ConstructGUI(rootVisualElement);
        }

        [UniGitInject]
		private void Construct(GitManager gitManager,
	        GitReflectionHelper reflectionHelper,
	        UniGitData data,
	        ILogger logger,
	        GitSettingsJson gitSettings,
	        GitCallbacks gitCallbacks,
	        GitInitializer initializer,
	        GitSettingsManager settingsManager,
	        UniGitPaths paths,
            IGitResourceManager resourceManager)
        {
            this.resourceManager = resourceManager;
			this.paths = paths;
			this.settingsManager = settingsManager;
			this.logger = logger;
			this.gitSettings = gitSettings;
			this.initializer = initializer;

			if (gitManager == null)
			{
				logger.Log(LogType.Error,"Git manager cannot be null.");
				return;
			}
			if (this.gitCallbacks != null)
			{
				Unsubscribe(this.gitCallbacks);
			}
			
			this.data = data;
			this.gitManager = gitManager;
			this.gitManager.AddWatcher(this);
			this.reflectionHelper = reflectionHelper;
			Subscribe(gitCallbacks);
		}

		#region Editor Specific Updates

		//Called only in the editor as we can't force Editor recompile to reinject dependencies
		protected virtual void OnRepositoryCreate()
		{
			
		}

		#endregion

		protected virtual void ConstructGUI(VisualElement root)
		{
			root.styleSheets.Add(resourceManager.LoadUniGitAsset<StyleSheet>("Editor/UI/RootSheet.uss"));

            invalidRepoElement = root.Q("InvalidRepository");
            invalidRepoPathLabel = invalidRepoElement.Q<Label>("RepoPath");
            var findRepositoryButton = invalidRepoElement.Q<Button>("FindRepository");
            var invalidRepoHelpButton = invalidRepoElement.Q<Button>("Help");
            invalidRepoHelpButton.clickable.clicked += () => GitLinks.GoTo(GitLinks.ConfigurationHelp);

            invalidRepoElement.styleSheets.Add(resourceManager.LoadUniGitAsset<StyleSheet>("Editor/UI/InvalidRepositorySheet.uss"));
			if (invalidRepoElement != null)
			{
				invalidRepoElement.Q<Button>("CreateRepository").clickable.clicked += () =>
				{
					if (!initializer.IsValidRepo)
					{
						initializer.InitializeRepositoryAndRecompile();
					}
				};
            }

			findRepositoryButton.clickable.clicked += () =>
			{
				settingsManager.ShowChooseMainRepositoryPathPopup(this);
			};

			invalidRepoPathLabel.text = this.paths.RepoPath;
        }

		protected virtual void Subscribe(GitCallbacks callbacks)
		{
			if (callbacks == null)
			{
				logger.Log(LogType.Error,"Trying to subscribe to null callbacks");
				return;
			}
			callbacks.EditorUpdate += OnEditorUpdateInternal;
			callbacks.UpdateRepository += OnGitManagerUpdateRepositoryInternal;
			callbacks.OnRepositoryLoad += OnRepositoryLoad;
			callbacks.UpdateRepositoryStart += UpdateTitleIcon;
			callbacks.RepositoryCreate += OnRepositoryCreate;
		}

		protected virtual void Unsubscribe(GitCallbacks callbacks)
		{
			if (callbacks == null) return;
			callbacks.EditorUpdate -= OnEditorUpdateInternal;
			callbacks.UpdateRepository -= OnGitManagerUpdateRepositoryInternal;
			callbacks.OnRepositoryLoad -= OnRepositoryLoad;
			callbacks.UpdateRepositoryStart -= UpdateTitleIcon;
			callbacks.RepositoryCreate -= OnRepositoryCreate;
		}

		protected virtual void OnFocus()
        {
            isFocused = true;
        }

		protected virtual void OnLostFocus()
        {
            isFocused = false;
        }

		private void OnGitManagerUpdateRepositoryInternal(GitRepoStatus status,string[] paths)
		{
			UpdateTitleIcon();

			//only update the window if it is initialized. That means opened and visible.
			//the editor window will initialize itself once it's focused
			if (!IsInitialized || !initializer.IsValidRepo || !HasFocus) return;
			OnGitUpdate(status, paths);
		}

		private void UpdateTitleIcon()
		{
			titleContent.image = gitManager.GetGitStatusIcon();
			Repaint();
		}

		private void OnEditorUpdateInternal()
		{
			//Only initialize if the editor Window is focused
			if (HasFocus && gitManager.Repository != null && data.Initialized)
			{
				if (!IsInitialized)
				{
					IsInitialized = true;
					if (!initializer.IsValidRepo) return;
					IsDirty = false;
					OnInitialize();
					OnGitManagerUpdateRepositoryInternal(data.RepositoryStatus, null);
					//simulate repository loading for first initialization
					OnRepositoryLoad(gitManager.Repository);
					Repaint();
				}
				else if (IsDirty)
				{
					if (!initializer.IsValidRepo) return;
					IsDirty = false;
					OnGitManagerUpdateRepositoryInternal(data.RepositoryStatus, null);
					//simulate repository loading for first initialization
					OnRepositoryLoad(gitManager.Repository);
					Repaint();
				}
			}

			if (HasFocus)
			{
				OnEditorUpdate();
			}
		}

		public void MarkDirty()
		{
			IsDirty = true;
		}

		protected virtual void Update()
		{
			var validRepo = gitManager != null && initializer.IsValidRepo;
			if (invalidRepoElement != null)
			{
				invalidRepoElement.style.display = !validRepo ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (invalidRepoPathLabel != null)
            {
                invalidRepoPathLabel.text = this.paths.RepoPath;
            }
        }

		protected void OnDisable()
		{
			GitWindows.RemoveWindow(this);
		}

		protected void OnDestroy()
		{
			if (gitManager != null)
			{
				if(gitCallbacks != null) Unsubscribe(gitCallbacks);
				gitManager.RemoveWatcher(this);
			}
		}

		#region Safe Controlls

		public void LoseFocus()
		{
			GUIUtility.keyboardControl = 0;
			EditorGUIUtility.editingTextField = false;
			Repaint();
		}

		#endregion

		[field: NonSerialized]
        public bool IsInitialized { get; private set; }

        public bool HasFocus => reflectionHelper.HasFocusFunction.Invoke(this);

		public virtual bool IsWatching => HasFocus;

		public bool IsValid => this;

		[field: NonSerialized]
        protected bool LastHadFocus { get; }

        [field: NonSerialized]
        protected bool IsDirty { get; private set; }

        protected abstract void OnGitUpdate(GitRepoStatus status,string[] paths);
		protected abstract void OnInitialize();
		protected abstract void OnRepositoryLoad(Repository repository);
		protected abstract void OnEditorUpdate();
	}
}