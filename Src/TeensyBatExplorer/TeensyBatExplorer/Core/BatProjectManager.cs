// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//  
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;

using Microsoft.Toolkit.Uwp.Helpers;

using Newtonsoft.Json;

using TeensyBatExplorer.Core.BatLog;
using TeensyBatExplorer.Core.BatLog.Raw;

namespace TeensyBatExplorer.Core
{
    public class BatProjectManager
    {
        private static async Task SerializeJson<T>(T data, StorageFile file)
        {
            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
                    {
                        JsonSerializer s = new JsonSerializer();
                        s.Serialize(jsonWriter, data);
                    }
                }
            }
        }

        private static async Task<T> DeserializeJson<T>(StorageFile file)
        {
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(reader))
                    {
                        JsonSerializer s = new JsonSerializer();
                        return s.Deserialize<T>(jsonTextReader);
                    }
                }
            }
        }

        private const string ProjectFileName = "project.json";
        private const string NodeFileName = "node.json";
        private const string RawFileName = "raw.dat";

        private static readonly Regex InvalidFolderCharsRegex = new Regex($"[\\?\\*{Regex.Escape(new string(Path.GetInvalidPathChars()))}]", RegexOptions.Compiled);
        private readonly Settings _settings;
        private readonly BatNodeLogReader _batNodeLogReader;
        private readonly LogAnalyzer _logAnalyzer;
        private List<BatProject> _projects;

        public BatProjectManager(Settings settings, BatNodeLogReader batNodeLogReader, LogAnalyzer logAnalyzer)
        {
            _settings = settings;
            _batNodeLogReader = batNodeLogReader;
            _logAnalyzer = logAnalyzer;
        }

        public async Task<IEnumerable<BatProject>> FindAllProjects()
        {
            IStorageFolder storageFolder = await GetProjectStorageFolder();
            IReadOnlyList<StorageFolder> projectFolders = await storageFolder.GetFoldersAsync();
            List<BatProject> projects = new List<BatProject>();
            foreach (StorageFolder projectFolder in projectFolders)
            {
                BatProject batProject = await OpenProject(projectFolder);
                projects.Add(batProject);
            }

            _projects = projects;
            return _projects;
        }

        private async Task<BatProject> OpenProject(StorageFolder projectFolder)
        {
            if (await projectFolder.FileExistsAsync(ProjectFileName))
            {
                StorageFile projectFile = await projectFolder.GetFileAsync(ProjectFileName);
                return await DeserializeJson<BatProject>(projectFile);
            }

            throw new InvalidOperationException("ProjectFile Not Found!");
        }

        public async Task<IStorageFolder> GetProjectStorageFolder()
        {
            IStorageFolder folder;
            string projectFolderToken = _settings.ProjectFolderToken;
            if (string.IsNullOrWhiteSpace(projectFolderToken) || !StorageApplicationPermissions.FutureAccessList.ContainsItem(projectFolderToken))
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add(".batproj");
                folder = await DispatcherHelper.ExecuteOnUIThreadAsync(async () => await folderPicker.PickSingleFolderAsync());
                if (folder == null)
                {
                    return null;
                }

                projectFolderToken = StorageApplicationPermissions.FutureAccessList.Add(folder);
                _settings.ProjectFolderToken = projectFolderToken;
            }
            else
            {
                folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(projectFolderToken);
            }

            return folder;
        }

        public async Task Save(BatProject project)
        {
            StorageFolder projectFolder = await GetProjectFolder(project, true);
            await UpdateProjectFile(project, projectFolder);
            foreach (BatNode node in project.Nodes)
            {
                if (node.IsDirty)
                {
                    await SaveNodeData(node, projectFolder);
                    node.IsDirty = false;
                }
            }
        }

        private async Task SaveNodeData(BatNode node, IStorageFolder projectFolder)
        {
            StorageFile nodeFile = await projectFolder.CreateFileAsync(Path.Combine(node.FolderName, NodeFileName), CreationCollisionOption.ReplaceExisting);
            await SerializeJson(node.NodeData, nodeFile);
        }

        public async Task<BatNode> AddNode(BatProject project, IStorageFile rawDataFile)
        {
            BatNode newNode = new BatNode();
            newNode.NodeData = new BatNodeData();
            project.AddNode(newNode);

            StorageFolder projectFolder = await GetProjectFolder(project);
            StorageFolder nodeFolder = await projectFolder.CreateFolderAsync(newNode.FolderName, CreationCollisionOption.FailIfExists);
            await rawDataFile.CopyAsync(nodeFolder, RawFileName, NameCollisionOption.ReplaceExisting);

            await AnalyzeNode(project, newNode, rawDataFile);

            StorageFile nodeFile = await nodeFolder.CreateFileAsync(NodeFileName, CreationCollisionOption.ReplaceExisting);
            await SerializeJson(newNode.NodeData, nodeFile);

            await UpdateProjectFile(project);

            return newNode;
        }

        public async Task AnalyzeNode(BatProject project, BatNode node)
        {
            StorageFolder projectFolder = await GetProjectFolder(project);
            if (node.NodeData == null)
            {
                await LoadNodeData(project, node);
            }

            StorageFile rawDataFile = await projectFolder.GetFileAsync(Path.Combine(node.FolderName, RawFileName));
            await AnalyzeNode(project, node, rawDataFile);
        }

        private async Task AnalyzeNode(BatProject project, BatNode node, IStorageFile rawDataFile)
        {
            RawNodeData rawData = await _batNodeLogReader.Load(rawDataFile);
            _logAnalyzer.Analyze(project, node, rawData);
        }

        private async Task UpdateProjectFile(BatProject project)
        {
            StorageFolder projectFolder = await GetProjectFolder(project, true);
            await UpdateProjectFile(project, projectFolder);
        }

        private async Task UpdateProjectFile(BatProject project, StorageFolder projectFolder)
        {
            StorageFile projectFile = await projectFolder.CreateFileAsync(ProjectFileName, CreationCollisionOption.ReplaceExisting);
            using (Stream stream = await projectFile.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
                    {
                        JsonSerializer s = new JsonSerializer();
                        s.Serialize(jsonWriter, project);
                    }
                }
            }
        }

        private async Task<StorageFolder> GetProjectFolder(BatProject project, bool allowRename = false)
        {
            IStorageFolder storageFolder = await GetProjectStorageFolder();

            string newFolderName = InvalidFolderCharsRegex.Replace(project.Name, "_");

            StorageFolder projectFolder = null;
            if (!string.IsNullOrWhiteSpace(project.FolderName))
            {
                projectFolder = await storageFolder.GetFolderAsync(project.FolderName);
            }

            if (projectFolder == null)
            {
                projectFolder = await storageFolder.CreateFolderAsync(newFolderName, CreationCollisionOption.GenerateUniqueName);
            }
            else if (allowRename && !newFolderName.Equals(project.FolderName, StringComparison.OrdinalIgnoreCase))
            {
                await projectFolder.RenameAsync(newFolderName, NameCollisionOption.GenerateUniqueName);
            }

            project.FolderName = projectFolder.Name;
            return projectFolder;
        }

        public async Task LoadNodeData(BatProject project, BatNode node)
        {
            if (node.NodeData != null)
            {
                return;
            }

            StorageFolder projectFolder = await GetProjectFolder(project);
            StorageFile nodeFile = await projectFolder.GetFileAsync(Path.Combine(node.FolderName, NodeFileName));
            node.NodeData = await DeserializeJson<BatNodeData>(nodeFile);
        }
    }
}