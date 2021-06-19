﻿using System.Collections.Generic;
using LibGit2Sharp;
using UniGit.Utils;
using UnityEngine;

namespace UniGit
{
    public class GitOverlay
    {
        public class Icons
        {
            public GUIContent validIcon;
            public GUIContent validIconSmall;
            public GUIContent modifiedIcon;
            public GUIContent modifiedIconSmall;
            public GUIContent addedIcon;
            public GUIContent addedIconSmall;
            public GUIContent untrackedIcon;
            public GUIContent untrackedIconSmall;
            public GUIContent ignoredIcon;
            public GUIContent ignoredIconSmall;
            public GUIContent conflictIcon;
            public GUIContent conflictIconSmall;
            public GUIContent deletedIcon;
            public GUIContent deletedIconSmall;
            public GUIContent renamedIcon;
            public GUIContent renamedIconSmall;
            public GUIContent loadingIconSmall;
            public GUIContent objectIcon;
            public GUIContent objectIconSmall;
            public GUIContent metaIcon;
            public GUIContent metaIconSmall;
            public GUIContent fetch;
            public GUIContent merge;
            public GUIContent checkout;
            public GUIContent loadingCircle;
            public GUIContent stashIcon;
            public GUIContent unstashIcon;
            public GUIContent lfsObjectIcon;
            public GUIContent lfsObjectIconSmall;
            public GUIContent donateSmall;
            public GUIContent starSmall;
            public GUIContent trashIconSmall;
            public GUIContent submoduleIcon;
            public GUIContent submoduleIconSmall;
            public GUIContent submoduleTagIcon;
            public GUIContent submoduleTagIconSmall;
        }

        [UniGitInject]
        public GitOverlay(IGitResourceManager resourceManager)
        {
            icons = new Icons()
            {
                validIcon = new GUIContent(resourceManager.GetTexture("success")),
                validIconSmall = new GUIContent(resourceManager.GetTexture("success_small")),
                modifiedIcon = new GUIContent(resourceManager.GetTexture("error")),
                modifiedIconSmall = new GUIContent(resourceManager.GetTexture("error_small")),
                addedIcon = new GUIContent(resourceManager.GetTexture("add")),
                addedIconSmall = new GUIContent(resourceManager.GetTexture("add_small")),
                untrackedIcon = new GUIContent(resourceManager.GetTexture("info")),
                untrackedIconSmall = new GUIContent(resourceManager.GetTexture("info_small")),
                ignoredIcon = new GUIContent(resourceManager.GetTexture("minus")),
                ignoredIconSmall = new GUIContent(resourceManager.GetTexture("minus_small")),
                conflictIcon = new GUIContent(resourceManager.GetTexture("warning")),
                conflictIconSmall = new GUIContent(resourceManager.GetTexture("warning_small")),
                deletedIcon = new GUIContent(resourceManager.GetTexture("deleted")),
                deletedIconSmall = new GUIContent(resourceManager.GetTexture("deleted_small")),
                renamedIcon = new GUIContent(resourceManager.GetTexture("renamed")),
                renamedIconSmall = new GUIContent(resourceManager.GetTexture("renamed_small")),
                loadingIconSmall = new GUIContent(resourceManager.GetTexture("loading")),
                objectIcon = new GUIContent(resourceManager.GetTexture("object")),
                objectIconSmall = new GUIContent(resourceManager.GetTexture("object_small")),
                metaIcon = new GUIContent(resourceManager.GetTexture("meta")),
                metaIconSmall = new GUIContent(resourceManager.GetTexture("meta_small")),
                fetch = new GUIContent(resourceManager.GetTexture("GitFetch")),
                merge = new GUIContent(resourceManager.GetTexture("GitMerge")),
                checkout = new GUIContent(resourceManager.GetTexture("GitCheckout")),
                loadingCircle = new GUIContent(resourceManager.GetTexture("loading_circle")),
                stashIcon = new GUIContent(resourceManager.GetTexture("stash")),
                unstashIcon = new GUIContent(resourceManager.GetTexture("unstash")),
                lfsObjectIcon = new GUIContent(resourceManager.GetTexture("lfs_object")),
                lfsObjectIconSmall = new GUIContent(resourceManager.GetTexture("lfs_object_small")),
                donateSmall = new GUIContent(resourceManager.GetTexture("donate"), "Donate"),
                starSmall = new GUIContent(resourceManager.GetTexture("star")),
                trashIconSmall = new GUIContent(resourceManager.GetTexture("trash")),
                submoduleIcon = new GUIContent(resourceManager.GetTexture("file_submodule")),
                submoduleIconSmall = new GUIContent(resourceManager.GetTexture("file_submodule_small")),
                submoduleTagIcon = new GUIContent(resourceManager.GetTexture("submodule_tag")),
                submoduleTagIconSmall = new GUIContent(resourceManager.GetTexture("submodule_tag_small")),
            };
        }

        public GUIContent GetDiffTypeIcon(FileStatus type, bool small)
        {
            GUIContent content = null;

            if (type.IsFlagSet(FileStatus.Ignored))
            {
                content = small ? icons.ignoredIconSmall : icons.ignoredIcon;
            }
            else if (type.IsFlagSet(FileStatus.NewInIndex))
            {
                content = small ? icons.addedIconSmall : icons.addedIcon;
            }
            else if (type.IsFlagSet(FileStatus.NewInWorkdir))
            {
                content = small ? icons.untrackedIconSmall : icons.untrackedIcon;
            }
            else if (type.IsFlagSet(FileStatus.ModifiedInWorkdir | FileStatus.ModifiedInIndex))
            {
                content = small ? icons.modifiedIconSmall : icons.modifiedIcon;
            }
            else if (type.IsFlagSet(FileStatus.Conflicted))
            {
                content = small ? icons.conflictIconSmall : icons.conflictIcon;
            }
            else if (type.IsFlagSet(FileStatus.RenamedInIndex | FileStatus.RenamedInWorkdir))
            {
                content = small ? icons.renamedIconSmall : icons.renamedIcon;
            }
            else if (type.IsFlagSet(FileStatus.DeletedFromIndex | FileStatus.DeletedFromWorkdir))
            {
                content = small ? icons.deletedIconSmall : icons.deletedIcon;
            }
            return content != null ? SetupTooltip(content, type) : GUIContent.none;
        }

        public IEnumerable<GUIContent> GetDiffTypeIcons(FileStatus type, bool small)
        {
            if (type.IsFlagSet(FileStatus.Ignored))
            {
                yield return SetupTooltip(small ? icons.ignoredIconSmall : icons.ignoredIcon, type);
            }
            if (type.IsFlagSet(FileStatus.NewInIndex))
            {
                yield return SetupTooltip(small ? icons.addedIconSmall : icons.addedIcon, type);
            }
            if (type.IsFlagSet(FileStatus.NewInWorkdir))
            {
                yield return SetupTooltip(small ? icons.untrackedIconSmall : icons.untrackedIcon, type);
            }
            if (type.IsFlagSet(FileStatus.ModifiedInWorkdir | FileStatus.ModifiedInIndex))
            {
                yield return SetupTooltip(small ? icons.modifiedIconSmall : icons.modifiedIcon, type);
            }
            if (type.IsFlagSet(FileStatus.Conflicted))
            {
                yield return SetupTooltip(small ? icons.conflictIconSmall : icons.conflictIcon, type);
            }
            if (type.IsFlagSet(FileStatus.RenamedInIndex | FileStatus.RenamedInWorkdir))
            {
                yield return SetupTooltip(small ? icons.renamedIconSmall : icons.renamedIcon, type);
            }
            if (type.IsFlagSet(FileStatus.DeletedFromIndex | FileStatus.DeletedFromWorkdir))
            {
                yield return SetupTooltip(small ? icons.deletedIconSmall : icons.deletedIcon, type);
            }
        }

        private static GUIContent SetupTooltip(GUIContent content, FileStatus type)
        {
            content.tooltip = type.ToString();
            return content;
        }

        public GUIContent GetDiffTypeIcon(ChangeKind type, bool small)
        {
            switch (type)
            {
                case ChangeKind.Unmodified      :
                    return small ? icons.validIconSmall : icons.validIcon;
                case  ChangeKind.Added          :
                    return small ? icons.addedIconSmall : icons.addedIcon;
                case  ChangeKind.Deleted        :
                    return small ? icons.deletedIconSmall : icons.deletedIcon;
                case  ChangeKind.Modified       :
                    return small ? icons.modifiedIconSmall : icons.modifiedIcon;
                case  ChangeKind.Ignored        :
                    return small ? icons.ignoredIconSmall : icons.ignoredIcon;
                case  ChangeKind.Untracked      :
                    return small ? icons.untrackedIconSmall : icons.untrackedIcon;
                case  ChangeKind.Conflicted     :
                    return small ? icons.conflictIconSmall : icons.conflictIcon;
                case ChangeKind.Renamed:
                    return small ? icons.renamedIconSmall : icons.renamedIcon;
                default:
                    return null;
            }
        }

        public Icons icons { get; }
    }
}