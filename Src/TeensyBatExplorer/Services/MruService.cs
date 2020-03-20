// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
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
using System.Linq;

using Windows.Storage;
using Windows.Storage.AccessCache;

using Newtonsoft.Json;

using TeensyBatExplorer.Business.Models;

namespace TeensyBatExplorer.Services
{
    public class ProjectMetadata
    {
        public string Filename { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedOn { get; set; }

        [JsonIgnore]
        public string Token { get; set; }
    }

    public class MruService
    {
        public void AddProject(IStorageFile storageFile, BatProject batProject)
        {
            string json = string.Empty;
            if (batProject != null)
            {
                json = JsonConvert.SerializeObject(new ProjectMetadata() { Name = batProject.Name, CreatedOn = batProject.CreatedOn, Filename = storageFile.Name });
            }

            StorageApplicationPermissions.MostRecentlyUsedList.Add(storageFile, $"{MetadataType.ProjectFile}:{json}");
        }

        public IEnumerable<ProjectMetadata> GetProjects()
        {
            List<AccessListEntry> projects = StorageApplicationPermissions.MostRecentlyUsedList.Entries.Where(e => e.Metadata.StartsWith(MetadataType.ProjectFile.ToString())).ToList();
            foreach (AccessListEntry project in projects)
            {
                string metadata = project.Metadata;
                int pos = metadata.IndexOf(':');
                if (pos > 0 && pos < metadata.Length - 2)
                {
                    ProjectMetadata projectMetadata = JsonConvert.DeserializeObject<ProjectMetadata>(metadata.Substring(pos+1));
                    projectMetadata.Token = project.Token;
                    yield return projectMetadata;
                }
                else
                {
                    ProjectMetadata projectMetadata = new ProjectMetadata();
                    projectMetadata.Token = project.Token;
                    yield return projectMetadata;
                }
            }
        }

        private enum MetadataType
        {
            ProjectFile
        }
    }
}