﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniGit.Utils
{
	public class ContextGenericMenuPopup : PopupWindowContent, IGenericMenu
	{
		private const float AnimationTime = 0.4f;
		private const float HoverPressTime = 0.5f;
		private class Element
		{
			public Element parent;
			public GUIContent Content;
			public bool on;
			public GenericMenu.MenuFunction Func;
			public GenericMenu.MenuFunction2 Func2;
			public object data;
			public bool isSeparator;
			public bool isDisabled;
			public List<Element> children;

			public bool IsParent => children != null && children.Count > 0;
        }
		private readonly GitOverlay gitOverlay;
		private readonly List<Element> elements;
		private readonly GitAnimation gitAnimation;
		private GUIStyle elementStyle;
		private GUIStyle separatorStyle;
		private Element currentElement;
		private Element lastElement;
		private GitAnimation.GitTween transitionTween;
		private int animDir = 1;
		private bool isClickHovering;
		private int lastHoverControlId;
		private double lastHoverStartTime;

		[UniGitInject]
		public ContextGenericMenuPopup(GitOverlay gitOverlay,GitAnimation gitAnimation)
		{
			this.gitOverlay = gitOverlay;
			this.gitAnimation = gitAnimation;
			elements = new List<Element>();
			transitionTween = GitAnimation.Empty;
		}

		private Element FindOrBuildTree(GUIContent content,ref GUIContent newContent)
		{
			if (string.IsNullOrEmpty(content.text)) return null;
			var elementStrings = content.text.Split(new [] {'/'},StringSplitOptions.RemoveEmptyEntries);

			if (elementStrings.Length > 1)
			{
				var lastParent = elements.FirstOrDefault(e => e.Content.text == elementStrings[0]);

				if (lastParent == null)
				{
					lastParent = new Element() {children = new List<Element>(),Content = new GUIContent(elementStrings[0],content.image)};
					elements.Add(lastParent);
				}
				else if(lastParent.children == null)
				{
					return null;
				}

				for (var i = 1; i < elementStrings.Length-1; i++)
				{
					var newLastParent = lastParent.children.FirstOrDefault(e => e.Content.text == elementStrings[i]);
					if (newLastParent == null)
					{
						newLastParent = new Element() {children = new List<Element>(), Content = new GUIContent(elementStrings[i],content.image)};
						lastParent.children.Add(newLastParent);
						newLastParent.parent = lastParent;
					}
					else if (newLastParent.children == null)
						return null;

					lastParent = newLastParent;
				}

				newContent = new GUIContent(elementStrings[elementStrings.Length-1],content.image,content.tooltip);
				return lastParent;
			}

			return null;
		}

		public void AddItem(GUIContent content, bool on, GenericMenu.MenuFunction func)
		{
			var parent = FindOrBuildTree(content, ref content);
			AddElement(new Element { Content = content, on = on, Func = func },parent);
		}

		public void AddDisabledItem(GUIContent content)
		{
			var parent = FindOrBuildTree(content, ref content);
			AddElement(new Element() { isDisabled = true, Content = content },parent);
		}

		public void AddItem(GUIContent content, bool on, GenericMenu.MenuFunction2 func,object data)
		{
			var parent = FindOrBuildTree(content, ref content);
			AddElement(new Element { Content = content, on = on, Func2 = func, data = data },parent);
		}

		public void AddSeparator(string text)
		{
			var content = new GUIContent(text);
			var parent = FindOrBuildTree(content, ref content);
			AddElement(new Element() {isSeparator = true, Content = content}, parent);
		}

		private void AddElement(Element element, Element parent)
		{
			if (parent == null)
				elements.Add(element);
			else
			{
				parent.children.Add(element);
				element.parent = parent;
			}
		}

		public override Vector2 GetWindowSize()
		{
			if (elementStyle == null) InitStyles();
			var maxSize = Vector2.zero;
			if (transitionTween.Valid)
			{
				if(lastElement != null && lastElement.children != null) CalculateMaxSize(ref maxSize, lastElement.children,true);
			}
			else
			{
				if(currentElement != null && currentElement.children != null) CalculateMaxSize(ref maxSize, currentElement.children,true);
			}

			CalculateMaxSize(ref maxSize, elements,false);

			return maxSize;
		}

		private void CalculateMaxSize(ref Vector2 maxSize,List<Element> elements,bool includeBackButton)
		{
			var maxHeight = includeBackButton ? elementStyle.CalcSize(GitGUI.GetTempContent("Back")).y : 0;
			foreach (var element in elements)
			{
                var size = element.isSeparator ? separatorStyle.CalcSize(element.Content) : elementStyle.CalcSize(element.Content);
				maxSize.x = Mathf.Max(maxSize.x, size.x);
				maxHeight += size.y;
			}

			maxSize.y = Mathf.Max(maxSize.y, maxHeight);
		}

		private void InitStyles()
        {
            elementStyle = new GUIStyle("CN EntryBackEven")
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(32, 32, 4, 4),
                fontSize = 12,
                fixedHeight = 26,
                imagePosition = ImagePosition.ImageLeft
            };
			separatorStyle = new GUIStyle("DefaultLineSeparator") {fixedHeight = 2,margin = new RectOffset(32,32,4,4)};
		}

		private void DrawElementList(Rect rect,List<Element> elements,bool drawBack)
		{
			float height = 0;

			if (drawBack)
			{
				var backRect = new Rect(rect.x, rect.y + height, rect.width, elementStyle.fixedHeight);
				var backControlId = GUIUtility.GetControlID(GitGUI.GetTempContent("Back"), FocusType.Passive, backRect);
				if (Event.current.type == EventType.Repaint)
				{
					EditorGUIUtility.AddCursorRect(backRect, MouseCursor.Link);
					elementStyle.Draw(backRect, GitGUI.GetTempContent("Back"), false, false, backRect.Contains(Event.current.mousePosition), false);
					((GUIStyle)"AC LeftArrow").Draw(new Rect(backRect.x,backRect.y + ((backRect.height - 16) / 2f),16,16),GUIContent.none, false,false,false,false);

					if (backRect.Contains(Event.current.mousePosition) && !transitionTween.Valid)
					{
						if (lastHoverControlId != backControlId)
						{
							lastHoverControlId = backControlId;
							lastHoverStartTime = EditorApplication.timeSinceStartup;
						}
						isClickHovering = true;
						DrawHoverClickIndicator();
					}
				}
				else if (((Event.current.type == EventType.MouseDown && Event.current.button == 0 && backRect.Contains(Event.current.mousePosition)) || (lastHoverControlId == backControlId && EditorApplication.timeSinceStartup > lastHoverStartTime + HoverPressTime)) && !transitionTween.Valid && currentElement != null)
				{
					lastElement = currentElement;
					currentElement = currentElement.parent;
					transitionTween = gitAnimation.StartAnimation(AnimationTime, editorWindow,GitSettingsJson.AnimationTypeEnum.ContextMenu);
					lastHoverStartTime = EditorApplication.timeSinceStartup;
					animDir = -1;
				}
				height = elementStyle.fixedHeight;
			}

			foreach (var element in elements)
            {
                var elementRect = new Rect(rect.x, rect.y + height, rect.width, elementStyle.fixedHeight);
                var controlId = GUIUtility.GetControlID(element.Content, FocusType.Passive, elementRect);

                if (element.isSeparator)
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        GUI.backgroundColor = new Color(1, 1, 1, 0.2f);
                        var separatorRect = new Rect(rect.x + separatorStyle.margin.left, rect.y + height, rect.width - separatorStyle.margin.left - separatorStyle.margin.right, separatorStyle.fixedHeight);
                        separatorStyle.Draw(separatorRect, element.Content, false, false, false, false);
                        GUI.backgroundColor = Color.white;
                    }
                    height += separatorStyle.fixedHeight;
                }
                else
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        GUI.enabled = !element.isDisabled;
                        if (GUI.enabled)
                        {
                            EditorGUIUtility.AddCursorRect(elementRect, MouseCursor.Link);
                        }
						
                        elementStyle.Draw(elementRect, element.Content, controlId, elementRect.Contains(Event.current.mousePosition));
                        if(element.children != null)
                            ((GUIStyle)"AC RightArrow").Draw(new Rect(elementRect.x + elementRect.width - 21, elementRect.y + ((elementRect.height - 21) / 2f), 21, 21),GUIContent.none,false,false,false,false);

                        if (elementRect.Contains(Event.current.mousePosition) && !transitionTween.Valid)
                        {
                            if (element.IsParent)
                            {
                                if (lastHoverControlId != controlId)
                                {
                                    lastHoverControlId = controlId;
                                    lastHoverStartTime = EditorApplication.timeSinceStartup;
                                }
                                isClickHovering = true;
                                DrawHoverClickIndicator();
                            }
                            else
                            {
                                lastHoverControlId = controlId;
                                lastHoverStartTime = EditorApplication.timeSinceStartup;
                                isClickHovering = false;
                            }
                        }
                    }
                    else if (element.IsParent && ((lastHoverControlId == controlId && EditorApplication.timeSinceStartup > lastHoverStartTime + HoverPressTime) || (Event.current.type == EventType.MouseDown && elementRect.Contains(Event.current.mousePosition))))
                    {
                        lastElement = currentElement;
                        currentElement = element;
                        transitionTween = gitAnimation.StartAnimation(AnimationTime,GitSettingsJson.AnimationTypeEnum.ContextMenu);
                        lastHoverStartTime = EditorApplication.timeSinceStartup;
                        animDir = 1;
                        editorWindow.Repaint();
                    }
                    else if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && elementRect.Contains(Event.current.mousePosition) && !transitionTween.Valid)
                    {
                        editorWindow.Close();
                        if (element.Func != null)
                        {
                            element.Func.Invoke();
                        }
                        else
                        {
                            element.Func2?.Invoke(element.data);
                        }
                    }
                    height += elementStyle.fixedHeight;
                }
            }
		}

		public override void OnGUI(Rect rect)
		{
			if (elementStyle == null) InitStyles();
			if ((Event.current.type == EventType.MouseMove && rect.Contains(Event.current.mousePosition)) || transitionTween.Valid || isClickHovering) editorWindow.Repaint();

			if (Event.current.type == EventType.Repaint)
			{
				isClickHovering = false;
			}

			if (transitionTween.Valid)
			{
				var lastElementRect = new Rect(rect.x - (rect.width * (1 - GitAnimation.ApplyEasing(transitionTween.Percent)) * animDir), rect.y,rect.width,rect.height);
				if (lastElement != null && lastElement.IsParent)
				{
					DrawElementList(lastElementRect,lastElement.children,true);
				}
				else
				{
					DrawElementList(lastElementRect, elements,false);
				}
				var currentElementRect = new Rect(rect.x + (rect.width * GitAnimation.ApplyEasing(transitionTween.Percent)) * animDir, rect.y, rect.width, rect.height);
				if (currentElement != null && currentElement.IsParent)
				{
					DrawElementList(currentElementRect, currentElement.children, true);
				}
				else
				{
					DrawElementList(currentElementRect, elements, false);
				}
			}
			else
			{
				if (currentElement != null && currentElement.children != null)
				{
					DrawElementList(rect, currentElement.children,true);
				}
				else
				{
					DrawElementList(rect,elements,false);
				}
			}
		}

		private void DrawHoverClickIndicator()
		{
			if (Event.current.type == EventType.Repaint)
			{
				GUI.color = new Color(1,1,1,0.3f);
				var tex = gitOverlay.icons.loadingCircle.image;
				var index = Mathf.RoundToInt(Mathf.Lerp(0, 7, (float) (EditorApplication.timeSinceStartup - lastHoverStartTime) / HoverPressTime));
				GUI.DrawTextureWithTexCoords(new Rect(Event.current.mousePosition - new Vector2(12,5),new Vector2(34,34)), tex,new Rect((index % 4 / 4f), 0.5f - Mathf.FloorToInt(index / 4f) * 0.5f, 1/4f,0.5f));
				GUI.color = Color.white;
			}
		}
	}
}