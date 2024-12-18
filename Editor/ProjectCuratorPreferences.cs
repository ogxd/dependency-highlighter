using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ogxd.ProjectCurator
{
	[Flags]
	internal enum Warnings
	{
		None = 0,
		NotPresentInDatabase = 1 << 0,
		NonReciprocity = 1 << 1,
		// Note, I've not included the above warnings as they often print due to
		// caching/json deserialization issues—an indicator of the internal workings of
		// project curator, and not something the user can act on.
		Default = None
	}

	[FilePath(Path, FilePathAttribute.Location.ProjectFolder)]
	internal class ProjectCuratorPreferences : ScriptableSingleton<ProjectCuratorPreferences>
	{
		private const string Path = "UserSettings/ProjectCuratorPreferences.asset";

		[SerializeField]
		internal Warnings WarningVisibility = Warnings.Default;
		
		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new SettingsProvider("Preferences/Project Curator", SettingsScope.User)
			{
				label = "Project Curator",
				// activateHandler is called when the user clicks on the Settings item in the Settings window.
				activateHandler = (_, rootElement) =>
				{
					var settings = new SerializedObject(instance);
					
					var wrapper = new VisualElement
					{
						style =
						{
							marginBottom = 2,
							marginTop = 2,
							marginLeft = 8,
							marginRight = 8,
							flexDirection = FlexDirection.Column
						}
					};

					var title = new Label
					{
						text = "Project Curator",
						style =
						{
							fontSize = 20,
							marginBottom = 12,
							unityFontStyleAndWeight = FontStyle.Bold
						}
					};
					wrapper.Add(title);

					var properties = new VisualElement
					{
						style =
						{
							flexDirection = FlexDirection.Column
						}
					};
					wrapper.Add(properties);
					properties.Add(new PropertyField(settings.FindProperty(nameof(WarningVisibility))));
					wrapper.Bind(settings);
					rootElement.Add(wrapper);
				},
				keywords = new HashSet<string>(new[] { "Project Curator", "Warning Visibility" }),
				deactivateHandler = () => instance.Save(true)
			};

			return provider;
		}
	}
}