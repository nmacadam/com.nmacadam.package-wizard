/* Hibzz PackageCreator © 2023 by Hibzz Games is licensed under CC BY 4.0
 * https://github.com/hibzzgames/Hibzz.PackageCreator
 *
 * AddCreateButtonToPackageWindow.cs mirrors PackageCreator's PackageManagerExtension.cs functionality/implementation.
 * Modifications:
 * - Reflection implementation
 * - Menu button usage
 */
using System;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace PackageWizard.Editor
{
    internal class AddCreateButtonToPackageWindow : VisualElement, IPackageManagerExtension
    {
        private bool _hasInjected = false;
        
        [InitializeOnLoadMethod]
        internal static void Initialize()
        {
            PackageManagerExtensions.RegisterExtension(new AddCreateButtonToPackageWindow());
        }

        public VisualElement CreateExtensionUI()
        {
            _hasInjected = false;
            return this;
        }

        public void OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (!_hasInjected && TryInjectMenuItem())
            {
                _hasInjected = true;
            }
        }

        public void OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {}

        public void OnPackageRemoved(PackageInfo packageInfo)
        {}

        private bool TryInjectMenuItem()
        {
            var toolbarType = typeof(IPackageManagerExtension).Assembly.GetType("UnityEditor.PackageManager.UI.Internal.PackageManagerToolbar");
            if (toolbarType == null)
            {
                Debug.LogError($"Could not get Type 'UnityEditor.PackageManager.UI.Internal.PackageManagerToolbar'");
                return false;
            }

            var root = this.GetRoot().Q<TemplateContainer>();
            var toolbar = new Reflector(typeof(UQueryExtensions))
                .Call("Q", new[] { toolbarType }, new object[] { root, null, new string[] { } })
                ?.ToReflector();

            if (toolbar == null || toolbar.Instance == null)
            {
                return false;
            }

            var dropdownItem = toolbar
                .GetProperty("addMenu")
                .Call("AddBuiltInDropdownItem")
                .ToReflector();

            dropdownItem.SetProperty("text", "Add new package...");
            dropdownItem.SetProperty("action", (Action) PackageWizardDropdownWindow.ShowDropdown);

            return true;
        }
    }
}
