﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace WebPackTaskRunner
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    class AdornmentProvider : IWpfTextViewCreationListener
    {
        private const string _propertyName = "ShowWatermark";
        private const double _initOpacity = 0.5D;

        private SettingsManager _settingsManager;
        private static bool _isVisible, _hasLoaded;

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        public SVsServiceProvider serviceProvider { get; set; }

        private void LoadSettings()
        {
            if (_hasLoaded)
                return;

            _hasLoaded = true;

            _settingsManager = new ShellSettingsManager(serviceProvider);
            SettingsStore store = _settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            LogoAdornment.VisibilityChanged += AdornmentVisibilityChanged;

            _isVisible = store.GetBoolean(Constants.VSIX_NAME, _propertyName, true);
        }

        private void AdornmentVisibilityChanged(object sender, bool isVisible)
        {
            WritableSettingsStore wstore = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            _isVisible = isVisible;

            if (!wstore.CollectionExists(Constants.VSIX_NAME))
                wstore.CreateCollection(Constants.VSIX_NAME);

            wstore.SetBoolean(Constants.VSIX_NAME, _propertyName, isVisible);
        }

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
                return;

            LoadSettings();

            string fileName = Path.GetFileName(document.FilePath).ToLowerInvariant();

            // Check if filename is absolute because when debugging, script files are sometimes dynamically created.
            if (string.IsNullOrEmpty(fileName) || !Path.IsPathRooted(document.FilePath))
                return;

            if (fileName.StartsWith("webpack.", StringComparison.OrdinalIgnoreCase) && fileName.Contains(".config."))
            {
                textView.Properties.GetOrCreateSingletonProperty(() => new LogoAdornment(textView, _isVisible, _initOpacity));
            }
        }
    }
}