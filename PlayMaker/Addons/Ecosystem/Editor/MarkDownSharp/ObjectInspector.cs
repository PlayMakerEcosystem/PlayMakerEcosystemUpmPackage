﻿//-----------------------------------------------------------------------
// <copyright file="ObjectInspector.cs" company="Hutong Games LLC">
// Copyright (c) Hutong Games LLC. All rights reserved.
// </copyright>
// <author name="Jean Fabre"</author>
//-----------------------------------------------------------------------

using HutongGames.PlayMakerEditor.Addons.Ecosystem.MarkdownSharp;
using UnityEngine;
using UnityEditor;

namespace HutongGames.PlayMakerEditor.Addons.Ecosystem
{
	[CustomEditor(typeof(UnityEngine.Object))]
	public class ObjectInspector : UnityEditor.Editor
	{

		MarkdownGUI _markdownGui;
		Vector2 _scroll;
		void DrawMarkDownInspector()
		{
			GUI.enabled = true;
			if (_markdownGui == null)
			{
				_markdownGui = new MarkdownGUI();
				_markdownGui.ProcessSource
					(
						Utils.GetFileContents
							(
								AssetDatabase.GetAssetPath(target)
							)
					);
				_markdownGui.ProcessSource
					(
						Utils.GetFileContents
						(
						AssetDatabase.GetAssetPath(target)
						)
						);
			}

			_scroll = GUILayout.BeginScrollView(_scroll);
			
			if ( _markdownGui.OnGUILayout_MardkDownTextArea())
			{
				//Debug.Log("hello");
				Repaint();
			}

			GUILayout.EndScrollView();
		}

		#region Internal

		/// <summary>
		/// The is mark down file.
		/// </summary>
		bool isMarkDownFile;

		/// <summary>
		/// redirect to draw the parsed marked down file or the default inspector.
		/// </summary>
		public override void OnInspectorGUI()
		{
			if (isMarkDownFile)
			{
				DrawMarkDownInspector();
			}else{
				DrawDefaultInspector();
			}
		}

		/// <summary>
		/// Detect if we deal with an markdown file, because of its extension.
		/// </summary>
		protected virtual void OnEnable()
		{        
			string assetPath = AssetDatabase.GetAssetPath(target);
			if ((assetPath != null) && (assetPath.EndsWith(".md"))) {
				isMarkDownFile = true;
			}
		}	

		#endregion
	}
}

