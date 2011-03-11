﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;
using Orchard.ContentManagement.MetaData;
using Orchard.Environment.Descriptor;
using Orchard.FileSystems.AppData;
using Orchard.ImportExport.Models;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Recipes.Models;
using Orchard.Recipes.Services;

namespace Orchard.ImportExport.Services {
    [UsedImplicitly]
    public class ImportExportService : IImportExportService {
        private readonly IOrchardServices _orchardServices;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentDefinitionWriter _contentDefinitionWriter;
        private readonly IAppDataFolder _appDataFolder;
        private readonly IRecipeParser _recipeParser;
        private readonly IRecipeManager _recipeManager;
        private readonly IShellDescriptorManager _shellDescriptorManager;
        private const string ExportsDirectory = "Exports";

        public ImportExportService(
            IOrchardServices orchardServices,
            IContentDefinitionManager contentDefinitionManager,
            IContentDefinitionWriter contentDefinitionWriter,
            IAppDataFolder appDataFolder,
            IRecipeParser recipeParser, 
            IRecipeManager recipeManager, 
            IShellDescriptorManager shellDescriptorManager) {
            _orchardServices = orchardServices;
            _contentDefinitionManager = contentDefinitionManager;
            _contentDefinitionWriter = contentDefinitionWriter;
            _appDataFolder = appDataFolder;
            _recipeParser = recipeParser;
            _recipeManager = recipeManager;
            _shellDescriptorManager = shellDescriptorManager;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public void Import(string recipeText) {
            var recipe = _recipeParser.ParseRecipe(recipeText);
            CheckRecipeSteps(recipe);
            _recipeManager.Execute(recipe);
            UpdateShell();
        }

        public string Export(IEnumerable<string> contentTypes, ExportOptions exportOptions) {
            var exportDocument = CreateExportRoot();

            if (exportOptions.ExportMetadata) {
                exportDocument.Element("Orchard").Add(ExportMetadata(contentTypes));
            }

            if (exportOptions.ExportSiteSettings) {
                exportDocument.Element("Orchard").Add(ExportSiteSettings());
            }

            if (exportOptions.ExportData) {
                exportDocument.Element("Orchard").Add(ExportData(contentTypes, exportOptions.VersionHistoryOptions));
            }

            return WriteExportFile(exportDocument.ToString());
        }

        private XDocument CreateExportRoot() {
            var exportRoot = new XDocument(
                new XDeclaration("1.0", "", "yes"),
                new XComment("Exported from Orchard"),
                new XElement("Orchard",
                             new XElement("Recipe",
                                          new XElement("Name", "Generated by Orchard.ImportExport"),
                                          new XElement("Author", _orchardServices.WorkContext.CurrentUser.UserName)
                                 )
                    )
                );
            return exportRoot;
        }

        private XElement ExportMetadata(IEnumerable<string> contentTypes) {
            var typesElement = new XElement("Types");
            var partsElement = new XElement("Parts");
            var typesToExport = _contentDefinitionManager.ListTypeDefinitions()
                .Where(typeDefinition => contentTypes.Contains(typeDefinition.Name))
                .ToList();
            var partsToExport = new List<string>();

            foreach (var contentTypeDefinition in typesToExport) {
                foreach (var contentPartDefinition in contentTypeDefinition.Parts) {
                    if (partsToExport.Contains(contentPartDefinition.PartDefinition.Name)) {
                        continue;
                    }
                    partsToExport.Add(contentPartDefinition.PartDefinition.Name);
                    partsElement.Add(_contentDefinitionWriter.Export(contentPartDefinition.PartDefinition));
                }
                typesElement.Add(_contentDefinitionWriter.Export(contentTypeDefinition));
            }

            return new XElement("Metadata", typesElement, partsElement);
        }

        private XElement ExportSiteSettings() {
            var settings = new XElement("Settings");
            var hasSetting = false;

            foreach (var sitePart in _orchardServices.WorkContext.CurrentSite.ContentItem.Parts) {
                var setting = new XElement(sitePart.PartDefinition.Name);

                foreach (var property in sitePart.GetType().GetProperties()) {
                    var propertyType = property.PropertyType;
                    // Supported types (we also know they are not indexed properties).
                    if (propertyType == typeof(string) || propertyType == typeof(bool) || propertyType == typeof(int)) {
                        // Exclude read-only properties.
                        if (property.GetSetMethod() != null) {
                            setting.SetAttributeValue(property.Name, property.GetValue(sitePart, null));
                            hasSetting = true;
                        }
                    }
                }

                if (hasSetting) {
                    settings.Add(setting);
                    hasSetting = false;
                }
            }

            return settings;
        }

        private XElement ExportData(IEnumerable<string> contentTypes, VersionHistoryOptions versionHistoryOptions) {
            return new XElement("Data");
        }

        private string WriteExportFile(string exportDocument) {
            var exportFile = string.Format("Export-{0}-{1}.xml", _orchardServices.WorkContext.CurrentUser.UserName, DateTime.UtcNow.Ticks);
            if (!_appDataFolder.DirectoryExists(ExportsDirectory)) {
                _appDataFolder.CreateDirectory(ExportsDirectory);
            }

            var path = _appDataFolder.Combine(ExportsDirectory, exportFile);
            _appDataFolder.CreateFile(path, exportDocument);

            return _appDataFolder.MapPath(path);
        }

        private void CheckRecipeSteps(Recipe recipe) {
            foreach (var step in recipe.RecipeSteps) {
                switch (step.Name) {
                    case "Metadata":
                    case "Settings":
                    case "Data":
                        break;
                    default:
                        throw new InvalidOperationException(T("Step {0} is not a supported import step.", step.Name).Text);
                }
            }
        }

        private void UpdateShell() {
            var descriptor = _shellDescriptorManager.GetShellDescriptor();
            _shellDescriptorManager.UpdateShellDescriptor(descriptor.SerialNumber, descriptor.Features, descriptor.Parameters);
        }
    }
}