﻿//
//  EverlookConfiguration.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.IO;
using Everlook.Export.Audio;
using Everlook.Export.Image;
using Everlook.Export.Model;
using Gdk;
using IniParser;
using IniParser.Model;

namespace Everlook.Configuration
{
    /// <summary>
    /// Everlook configuration handler. Reads and writes local user configuration of the application.
    /// This class is threadsafe.
    /// </summary>
    public class EverlookConfiguration
    {
        /*
            Section names
        */

        private const string Export = nameof(Export);
        private const string Privacy = nameof(Privacy);
        private const string Viewport = nameof(Viewport);
        private const string Explorer = nameof(Explorer);

        /// <summary>
        /// The publicly accessibly instance of the configuration.
        /// </summary>
        public static readonly EverlookConfiguration Instance = new EverlookConfiguration();

        private readonly object _readLock = new object();
        private readonly object _writeLock = new object();

        private readonly FileIniDataParser _defaultParser = new FileIniDataParser();
        private readonly IniData _configurationData;

        /// <summary>
        /// Gets or sets the sprinting speed multiplier.
        /// </summary>
        public double SprintMultiplier
        {
            get => GetDoubleOption(Viewport, nameof(this.SprintMultiplier), 1.0);
            set => SetOption(Viewport, nameof(this.SprintMultiplier), value);
        }

        /// <summary>
        /// Gets or sets the movement speed multiplier of the camera in the viewport.
        /// </summary>
        public double CameraSpeed
        {
            get => GetDoubleOption(Viewport, nameof(this.CameraSpeed), 1.0);
            set => SetOption(Viewport, nameof(this.CameraSpeed), value);
        }

        /// <summary>
        /// Gets or sets the rotation speed multiplier of the camera in the viewport.
        /// </summary>
        public double RotationSpeed
        {
            get => GetDoubleOption(Viewport, nameof(this.RotationSpeed), 1.0);
            set => SetOption(Viewport, nameof(this.RotationSpeed), value);
        }

        /// <summary>
        /// Gets or sets the rotation speed multiplier of the camera in the viewport.
        /// </summary>
        public double CameraFOV
        {
            get => GetDoubleOption(Viewport, nameof(this.CameraFOV), 45.0);
            set => SetOption(Viewport, nameof(this.CameraFOV), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether unknown file types should be shown in the file explorer.
        /// </summary>
        public bool ShowUnknownFilesWhenFiltering
        {
            get => GetBooleanOption(Explorer, nameof(this.ShowUnknownFilesWhenFiltering));
            set => SetOption(Explorer, nameof(this.ShowUnknownFilesWhenFiltering), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a uniquely identifiable machine ID should be included in the
        /// statistical data.
        /// </summary>
        public bool SendMachineID
        {
            get => GetBooleanOption(Privacy, nameof(this.SendMachineID));
            set => SetOption(Privacy, nameof(this.SendMachineID), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a uniquely identifiable install ID should be included in the
        /// statistical data.
        /// </summary>
        public bool SendInstallID
        {
            get => GetBooleanOption(Privacy, nameof(this.SendInstallID));
            set => SetOption(Privacy, nameof(this.SendInstallID), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the operating system should be included in the statistical data.
        /// </summary>
        public bool SendOperatingSystem
        {
            get => GetBooleanOption(Privacy, nameof(this.SendOperatingSystem));
            set => SetOption(Privacy, nameof(this.SendOperatingSystem), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application version should be included in the statistical data.
        /// </summary>
        public bool SendAppVersion
        {
            get => GetBooleanOption(Privacy, nameof(this.SendAppVersion));
            set => SetOption(Privacy, nameof(this.SendAppVersion), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether runtime information (Mono, FX, Core, etc) should be included in the
        /// statistical data.
        /// </summary>
        public bool SendRuntimeInformation
        {
            get => GetBooleanOption(Privacy, nameof(this.SendRuntimeInformation));
            set => SetOption(Privacy, nameof(this.SendRuntimeInformation), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether bounding boxes should be occluded by geometry.
        /// </summary>
        public bool OccludeBoundingBoxes
        {
            get => GetBooleanOption(Viewport, nameof(this.OccludeBoundingBoxes));
            set => SetOption(Viewport, nameof(this.OccludeBoundingBoxes), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically play audio files when selected in the UI.
        /// </summary>
        public bool AutoplayAudioFiles
        {
            get => GetBooleanOption(Explorer, nameof(this.AutoplayAudioFiles));
            set => SetOption(Explorer, nameof(this.AutoplayAudioFiles), value);
        }

        /// <summary>
        /// Gets or sets the viewport background colour.
        /// </summary>
        public RGBA ViewportBackgroundColour
        {
            get => GetColourOption(Viewport, nameof(this.ViewportBackgroundColour));
            set => SetOption(Viewport, nameof(this.ViewportBackgroundColour), value);
        }

        /// <summary>
        /// Gets or sets the colour of wireframes.
        /// </summary>
        public RGBA WireframeColour
        {
            get => GetColourOption(Viewport, nameof(this.WireframeColour));
            set => SetOption(Viewport, nameof(this.WireframeColour), value);
        }

        /// <summary>
        /// Gets or sets the default export directory.
        /// </summary>
        public string DefaultExportDirectory
        {
            get => GetOption(Export, nameof(this.DefaultExportDirectory));
            set => SetOption(Export, nameof(this.DefaultExportDirectory), value, false);
        }

        /// <summary>
        /// Gets or sets the default format for exported models.
        /// </summary>
        public ModelFormat DefaultModelExportFormat
        {
            get => GetEnumOption(Export, nameof(this.DefaultModelExportFormat), ModelFormat.Collada);
            set => SetOption(Export, nameof(this.DefaultModelExportFormat), value);
        }

        /// <summary>
        /// Gets or sets the default format for exported images.
        /// </summary>
        public ImageFormat DefaultImageExportFormat
        {
            get => GetEnumOption(Export, nameof(this.DefaultImageExportFormat), ImageFormat.PNG);
            set => SetOption(Export, nameof(this.DefaultImageExportFormat), value);
        }

        /// <summary>
        /// Gets or sets the default format for exported audio.
        /// </summary>
        public AudioFormat DefaultAudioExportFormat
        {
            get => GetEnumOption(Export, nameof(this.DefaultAudioExportFormat), AudioFormat.Original);
            set => SetOption(Export, nameof(this.DefaultAudioExportFormat), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to keep the directory structure of exported files.
        /// </summary>
        public bool KeepFileDirectoryStructure
        {
            get => GetBooleanOption(Export, nameof(this.KeepFileDirectoryStructure));
            set => SetOption(Export, nameof(this.KeepFileDirectoryStructure), value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the application should be allowed to send anonymous stats.
        /// </summary>
        public bool AllowSendingStatistics
        {
            get => GetBooleanOption(Privacy, nameof(this.AllowSendingStatistics), true);
            set => SetOption(Privacy, nameof(this.AllowSendingStatistics), value);
        }

        private EverlookConfiguration()
        {
            lock (_readLock)
            {
                if (!File.Exists(GetConfigurationFilePath()))
                {
                    // Create the default configuration file
                    Directory.CreateDirectory(Directory.GetParent(GetConfigurationFilePath()).FullName);
                    File.Create(GetConfigurationFilePath()).Close();

                    _configurationData = LoadFromDisk();
                    WriteDefaults();
                    Commit();
                }
                else
                {
                    /*
                        This section is for updating old configuration files
                        with new sections introduced in updates.

                        It's good practice to wrap each updating section in a
                        small informational header with the date and change.
                    */

                    // Uncomment this when needed
                    _configurationData = LoadFromDisk();

                    /*
                        Place any changes after this comment.
                    */

                    AddNewConfigurationOption(_configurationData, Viewport, nameof(this.RotationSpeed), "1.0");
                    AddNewConfigurationOption(_configurationData, Viewport, nameof(this.CameraFOV), "45.0");

                    MoveConfigurationOption
                    (
                        _configurationData,
                        "General",
                        Viewport,
                        nameof(this.ViewportBackgroundColour)
                    );

                    DeleteConfigurationSection(_configurationData, "General");

                    Commit();
                    LoadFromDisk();
                }
            }
        }

        /// <summary>
        /// Reloads the configuration from disk.
        /// </summary>
        /// <returns>The configuration data.</returns>
        public IniData LoadFromDisk()
        {
            lock (_readLock)
            {
                return _defaultParser.ReadFile(GetConfigurationFilePath());
            }
        }

        /// <summary>
        /// Commits the current configuration and writes it to disk.
        /// </summary>
        public void Commit()
        {
            lock (_writeLock)
            {
                WriteConfig(_defaultParser, _configurationData);
            }
        }

        private void WriteDefaults()
        {
            _configurationData.Sections.Clear();

            _configurationData.Sections.AddSection(Export);
            _configurationData.Sections.AddSection(Privacy);
            _configurationData.Sections.AddSection(Viewport);
            _configurationData.Sections.AddSection(Explorer);

            _configurationData[Export].AddKey(nameof(this.DefaultExportDirectory), Export);

            var modelExportKeyData = new KeyData(nameof(this.DefaultModelExportFormat))
            {
                Value = "0"
            };

            var modelExportKeyComments = new List<string>
            {
                "Valid options: ",
                "0: Collada",
                "1: Wavefront OBJ"
            };
            modelExportKeyData.Comments = modelExportKeyComments;

            _configurationData[Export].AddKey(modelExportKeyData);

            var imageExportKeyData = new KeyData(nameof(this.DefaultImageExportFormat))
            {
                Value = "0"
            };

            var imageExportKeyComments = new List<string>
            {
                "Valid options: ",
                "0: PNG",
                "1: JPG",
                "2: TGA",
                "3: TIF",
                "4: BMP"
            };
            imageExportKeyData.Comments = imageExportKeyComments;

            _configurationData[Export].AddKey(imageExportKeyData);

            var audioExportKeyData = new KeyData(nameof(this.DefaultAudioExportFormat))
            {
                Value = "0"
            };

            var audioExportKeyComments = new List<string>
            {
                "Valid options: ",
                "0: WAV",
                "1: MP3",
                "2: OGG",
                "3: FLAC"
            };
            audioExportKeyData.Comments = audioExportKeyComments;

            _configurationData[Export].AddKey(audioExportKeyData);

            _configurationData[Export].AddKey(nameof(this.KeepFileDirectoryStructure), "false");

            _configurationData[Privacy].AddKey(nameof(this.AllowSendingStatistics), "false");
            _configurationData[Privacy].AddKey(nameof(this.SendMachineID), "true");
            _configurationData[Privacy].AddKey(nameof(this.SendInstallID), "true");
            _configurationData[Privacy].AddKey(nameof(this.SendOperatingSystem), "true");
            _configurationData[Privacy].AddKey(nameof(this.SendAppVersion), "true");
            _configurationData[Privacy].AddKey(nameof(this.SendRuntimeInformation), "true");

            _configurationData[Viewport].AddKey(nameof(this.ViewportBackgroundColour), "rgb(133, 146, 173)");
            _configurationData[Viewport].AddKey(nameof(this.WireframeColour), "rgb(234, 161, 0)");
            _configurationData[Viewport].AddKey(nameof(this.OccludeBoundingBoxes), "false");
            _configurationData[Viewport].AddKey(nameof(this.CameraSpeed), "1.0");
            _configurationData[Viewport].AddKey(nameof(this.RotationSpeed), "1.0");
            _configurationData[Viewport].AddKey(nameof(this.CameraFOV), "45.0");
            _configurationData[Viewport].AddKey(nameof(this.SprintMultiplier), "2.0");

            _configurationData[Explorer].AddKey(nameof(this.ShowUnknownFilesWhenFiltering), "true");
            _configurationData[Explorer].AddKey(nameof(this.AutoplayAudioFiles), "true");
        }

        private void AddNewConfigurationSection(IniData configData, string keySection)
        {
            if (!configData.Sections.ContainsSection(keySection))
            {
                configData.Sections.AddSection(keySection);
            }
        }

        private void AddNewConfigurationOption(IniData configData, string keySection, string keyName, string keyData)
        {
            if (configData.Sections[keySection].ContainsKey(keyName))
            {
                return;
            }

            configData.Sections[keySection].AddKey(keyName);
            configData.Sections[keySection][keyName] = keyData;
        }

        private void RenameConfigurationOption
        (
            IniData configData,
            string keySection,
            string oldKeyName,
            string newKeyName
        )
        {
            if (!configData.Sections[keySection].ContainsKey(oldKeyName))
            {
                return;
            }

            var oldKeyData = configData.Sections[keySection][oldKeyName];

            AddNewConfigurationOption(configData, keySection, newKeyName, oldKeyData);

            configData.Sections[keySection].RemoveKey(oldKeyName);
        }

        private void RenameConfigurationSection(IniData configData, string oldSectionName, string newSectionName)
        {
            if (!configData.Sections.ContainsSection(oldSectionName))
            {
                return;
            }

            var oldSectionData = configData.Sections.GetSectionData(oldSectionName);
            configData.Sections.RemoveSection(oldSectionName);
            configData.Sections.AddSection(newSectionName);
            configData.Sections.SetSectionData(newSectionName, oldSectionData);
        }

        private void DeleteConfigurationSection(IniData configData, string sectionName)
        {
            if (configData.Sections.ContainsSection(sectionName))
            {
                configData.Sections.RemoveSection(sectionName);
            }
        }

        private void MoveConfigurationOption
        (
            IniData configData,
            string oldKeySection,
            string newKeySection,
            string keyName
        )
        {
            if (configData.Sections[newKeySection].ContainsKey(keyName))
            {
                return;
            }

            var keyValue = configData.Sections[oldKeySection][keyName];
            configData.Sections[newKeySection].AddKey(keyName, keyValue);

            configData.Sections[oldKeySection].RemoveKey(keyName);
        }

        /// <summary>
        /// Sets an option in the configuration. Any input object is converted to its string representation.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="keyName">The key name.</param>
        /// <param name="optionValue">The value to set the option to.</param>
        /// <param name="storeAsLowerCase">Whether or not to store the resulting option as a lower-case string.</param>
        /// <typeparam name="T">Any object which has a string representation.</typeparam>
        private void SetOption<T>(string section, string keyName, T optionValue, bool storeAsLowerCase = true)
            where T : notnull
        {
            var value = optionValue.ToString();
            if (value is null)
            {
                throw new InvalidOperationException
                (
                    "The value was null when transformed into its string representation."
                );
            }

            if (storeAsLowerCase)
            {
                value = value.ToLowerInvariant();
            }

            _configurationData[section][keyName] = value;
        }

        /// <summary>
        /// Gets an option from the configuration.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="keyName">The key name.</param>
        /// <returns>The string value of the option.</returns>
        private string GetOption(string section, string keyName)
        {
            return _configurationData[section][keyName];
        }

        /// <summary>
        /// Gets a boolean option from the configuration.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="keyName">The key name.</param>
        /// <param name="defaultValue">The default value of the option, if no valid option could be parsed.</param>
        /// <returns>true or false, depending on what the option is set to.</returns>
        private bool GetBooleanOption(string section, string keyName, bool defaultValue = false)
        {
            if (bool.TryParse(_configurationData[section][keyName], out var optionValue))
            {
                return optionValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets an integer option from the configuration.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="keyName">The key name.</param>
        /// <param name="defaultValue">The default value of the option, if no valid option could be parsed.</param>
        /// <returns>The value of the option.</returns>
        private int GetIntegerOption(string section, string keyName, int defaultValue = -1)
        {
            if (int.TryParse(_configurationData[section][keyName], out var optionValue))
            {
                return optionValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets an enum option from the configuration. This function is case-insensitive.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="keyName">The key name.</param>
        /// <param name="defaultValue">The default value of the option, if no valid option could be parsed.</param>
        /// <returns>The value of the option.</returns>
        private T GetEnumOption<T>(string section, string keyName, T defaultValue) where T : struct
        {
            if (Enum.TryParse(_configurationData[section][keyName], true, out T optionValue))
            {
                return optionValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a colour option from the configuration.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="keyName">The key name.</param>
        /// <param name="defaultValue">The default value of the option, if no valid option could be parsed.</param>
        /// <returns>The value of the option.</returns>
        private RGBA GetColourOption(string section, string keyName, RGBA defaultValue = default(RGBA))
        {
            var value = default(RGBA);
            if (value.Parse(_configurationData[section][keyName]))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a double precision floating-point option from the configuration.
        /// </summary>
        /// <param name="section">The section name.</param>
        /// <param name="keyName">The key name.</param>
        /// <param name="defaultValue">The default value of the option, if no valid option could be parsed.</param>
        /// <returns>The value of the option.</returns>
        private double GetDoubleOption(string section, string keyName, double defaultValue = default(double))
        {
            if (double.TryParse(_configurationData[section][keyName], out var optionValue))
            {
                return optionValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Writes the config data to disk. This method is thread-blocking, and all write operations
        /// are synchronized via lock(WriteLock).
        /// </summary>
        /// <param name="dataParser">The parser dealing with the current data.</param>
        /// <param name="data">The data which should be written to file.</param>
        private static void WriteConfig(FileIniDataParser dataParser, IniData data)
        {
            dataParser.WriteFile(GetConfigurationFilePath(), data);
        }

        private static string GetConfigurationFilePath()
        {
            return Path.Combine
            (
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Everlook",
                "everlook.ini"
            );
        }
    }
}
