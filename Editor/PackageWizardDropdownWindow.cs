using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditorInternal;
using UnityEngine;
using Oni.Editor;

namespace PackageWizard.Editor
{
    // todo: add github CI/CD to package template
    // todo: figure out an approach for nested repos
    // todo: make Bento collection package
    // todo: update template

    internal class PackageWizardDropdownWindow : EditorWindow
    {
        // basically just a big input form where we validate all the fields to make sure we don't generate
        // a broken package
        
        private string _name;
        private string _organization;
        private string _displayName;
        private string _description;
        private bool _hasMinimalUnityVersion;
        private PackageUnityVersion _unityVersion = new();
        private bool _initializeGitRepo;
        private string _asmdefPrefix;
        private List<PackageDependency> _dependencies = new();

        private bool _advancedFoldoutOpen;
        
        private static GUIStyle _dropdownStyle;

        private enum ErrorPriority
        {
            PackageName = 0,
            PackageOrganization = 1,
            DependencyName = 2,
            DependencySemVer = 3,
            RequiredFieldEmpty = 4,
        }
        
        // using SortedDictionary as a type of priority queue
        private readonly SortedDictionary<ErrorPriority, string> _errors = new();
        private ReorderableList _dependencyList;

        private EditorWindow _packageManagerWindow;
        
        private static readonly Vector2 _defaultWindowSize = new Vector2(320f, 260f);

        public static void ShowDropdown()
        {
            // Find the PackageManagerWindow and disable its contents (this is what the other options do in the add menu) 
            var packageManagerWindowType = typeof(IPackageManagerExtension).Assembly.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow");
            var packageManagerWindow = GetWindow(packageManagerWindowType);
            packageManagerWindow.rootVisualElement.SetEnabled(false);
            
            // Create a PackageWizardDropdownWindow and position it under the Package Manager window's header
            var window = CreateInstance<PackageWizardDropdownWindow>();
            window._packageManagerWindow = packageManagerWindow;
            window.UpdateWindowSize();
            window.ShowPopup();
        }

        private void OnEnable()
        {
            // Approximately the margin that the other package manager dropdown options have
            _dropdownStyle = new GUIStyle(GUIStyle.none)
            {
                margin = new RectOffset(6, 6, 3, 6)
            };
            
            // Create a ReorderableList for displaying dependencies
            _dependencyList = new ReorderableList(_dependencies, typeof(PackageDependency), true, true, true, true)
            {
                // for some reason ReorderableList has no margins between elements (despite the default list drawing in
                // inspector having them, so we'll handle it manually
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                drawElementCallback = (rect, index, _, _) =>
                {
                    // reduce the element height and position by the amount of margin we added to elementHeight
                    rect.height -= EditorGUIUtility.standardVerticalSpacing;
                    rect.y += EditorGUIUtility.standardVerticalSpacing / 2f;
                    
                    var element = _dependencies[index];
                    var (nameRect, versionRect) = OniEditorGUI.SplitRectByWidthPercentagesWithSpacing(rect, EditorGUIUtility.standardVerticalSpacing, 2f / 3f, 1f / 3f);

                    // Draw package name field
                    bool isNameValid = OniEditorGUI.ValidatedTextField(
                        nameRect, 
                        element.PackageName, 
                        "",
                        PackageValidation.ValidateCompleteName, 
                        out element.PackageName
                    );
                    if (!isNameValid)
                    {
                        if (string.IsNullOrEmpty(element.PackageName))
                        {
                            _errors.TryAdd(ErrorPriority.RequiredFieldEmpty, "*Required");
                        }
                        else
                        {
                            _errors.Add(ErrorPriority.DependencyName, $"Invalid dependency package name '{element.PackageName}'");
                        }
                    }
                    // Disable the semver field and don't assess if it has errors until there is a package name
                    using (new EditorGUI.DisabledScope(!isNameValid))
                    {
                        if (!OniEditorGUI.ValidatedTextField(versionRect,
                                element.Version,
                                "semver",
                                PackageValidation.ValidateVersion,
                                out element.Version
                            ) && isNameValid)
                        {
                            _errors.Add(ErrorPriority.DependencySemVer, $"Invalid version '{element.Version}'");
                        }
                    }

                    _dependencies[index] = element;
                },
                drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "Dependencies");
                }
            };
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            
            // todo: *technically* the 'Add package by name...' option has padding around the fields, idc right now though
            
            // todo: find a non-hardcoded way to get Editor background color
            EditorGUI.DrawRect(new Rect(0, 0, position.x, position.y),
                EditorGUIUtility.isProSkin ? new Color(0.19f, 0.19f, 0.19f) : new Color(0.76f, 0.76f, 0.76f));

            using (new EditorGUILayout.VerticalScope(_dropdownStyle))
            {
                // Draw header
                OniEditorGUI.LabelWithIcon(new GUIContent("Add new package"), EditorGUIUtility.IconContent("Package Manager"), EditorStyles.boldLabel);

                // Draw package, organization, and display name fields, validating that they are not null and meet package naming requirements
                if (!OniEditorGUI.ValidatedTextField(_name, "name*", PackageValidation.ValidateName, out _name))
                {
                    if (string.IsNullOrEmpty(_name))
                    {
                        _errors.TryAdd(ErrorPriority.RequiredFieldEmpty, "*Required");
                    }
                    else
                    {
                        _errors.Add(ErrorPriority.PackageName, "Invalid character in package name");
                    }
                }
                if (!OniEditorGUI.ValidatedTextField(_organization, "organization*", PackageValidation.ValidateOrganizationName, out _organization))
                {
                    if (string.IsNullOrEmpty(_name))
                    {
                        _errors.TryAdd(ErrorPriority.RequiredFieldEmpty, "*Required");
                    }
                    else
                    {
                        _errors.Add(ErrorPriority.PackageOrganization, "Invalid character in organization name");
                    }
                }
                if (!OniEditorGUI.ValidatedTextField(_displayName, "display name*", value => !string.IsNullOrEmpty(value), out _displayName))
                {
                    _errors.TryAdd(ErrorPriority.RequiredFieldEmpty, "*Required");
                }
                
                // Draw description field (optional)
                _description = OniEditorGUI.TextArea(_description, "description", GUILayout.Height(EditorGUIUtility.singleLineHeight * 4f));
                
                // Drawing this after all of the text fields for *cleanliness*
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    _advancedFoldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(_advancedFoldoutOpen, "Advanced");
                    if (_advancedFoldoutOpen)
                    {
                        // Is there a minimum Unity version?
                        _hasMinimalUnityVersion = EditorGUILayout.ToggleLeft("Minimal Unity Version", _hasMinimalUnityVersion);
                        if (_hasMinimalUnityVersion)
                        {
                            OniEditorGUI.UnityVersionField(ref _unityVersion.Major, ref _unityVersion.Minor, ref _unityVersion.Release);
                        }

                        _initializeGitRepo = EditorGUILayout.ToggleLeft("Initialize Git Repository", _initializeGitRepo);

                        // Optionally override the assembly definition naming in the template
                        _asmdefPrefix =
                            OniEditorGUI.TextFormatPreviewField(_asmdefPrefix, "asmdef prefix", value => $"{value}.Runtime");
                    
                        // Draw package dependency list
                        _dependencyList.DoLayoutList();
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                
                EditorGUILayout.Space();
                
                // Draw footer
                var (labelRect, buttonRect) = OniEditorGUI.SplitRectByWidthPercentages(EditorGUILayout.GetControlRect(), 2f / 3f, 1f / 3f);
                if (_errors.Count == 0)
                {
                    // Draw package name preview and enabled Create button
                    EditorGUI.LabelField(labelRect, $"com.{_organization}.{_name}", EditorStyles.miniLabel);
                    if (GUI.Button(buttonRect, "Create"))
                    {
                        // Finally, create the new package via PackageWizard
                        PackageWizard.New(_name, _organization, _displayName, _description, _dependencies, _unityVersion, _initializeGitRepo, _asmdefPrefix);
                        Close();
                        
                        // todo: would be nice to select the newly created manifest, but it seems like we can't load the asset directly after refreshing packages
                    }
                }
                else
                {
                    // Draw top priority error message and disabled create button
                    using (new OniEditorGUI.ColorScope(Color.red))
                    {
                        EditorGUI.LabelField(labelRect, _errors.First().Value, EditorStyles.miniLabel);
                    }
                    using (new OniEditorGUI.EnabledScope(false))
                    {
                        GUI.Button(buttonRect, "Create");
                    }
                }
            }
            
            _errors.Clear();
            UpdateWindowSize();
        }

        private void UpdateWindowSize()
        {
            int rowCount = 10;
            if (_advancedFoldoutOpen)
            {
                rowCount += 3;
                if (_hasMinimalUnityVersion)
                {
                    rowCount++;
                }
            }
                           
            position = new Rect()
            {
                x = _packageManagerWindow.position.x,
                y = _packageManagerWindow.position.y + EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing * 2f + 1f,
                width = _defaultWindowSize.x,
                height = ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * rowCount) 
                         + (6f * 2f) // EditorGUILayout.Space has a height of 6f
                         + EditorGUIUtility.standardVerticalSpacing * 3f // accounting for the vertical layout group box
                         + (_advancedFoldoutOpen ? _dependencyList.GetHeight() + 2f : 0f)
            };
        }

        private void OnLostFocus()
        {
            // Re-enable the Package Manager window's contents
            _packageManagerWindow.rootVisualElement.SetEnabled(true);
            
            // Close this dropdown
            Close();
        }
    }
}