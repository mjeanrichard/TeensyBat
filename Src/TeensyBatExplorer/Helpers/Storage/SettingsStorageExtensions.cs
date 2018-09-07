// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinard Jean-Richard
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
using System.IO;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Streams;

using TeensyBatExplorer.Helpers.Xaml;

namespace TeensyBatExplorer.Helpers.Storage
{
    // Use these extension methods to store and retrieve local and roaming app data
    // More details regarding storing and retrieving app data at https://docs.microsoft.com/windows/uwp/app-settings/store-and-retrieve-app-data
    public static class SettingsStorageExtensions
    {
        public static bool IsRoamingStorageAvailable(this ApplicationData appData)
        {
            return appData.RoamingStorageQuota == 0;
        }

        public static async Task SaveAsync<T>(this StorageFolder folder, string name, T content)
        {
            var file = await folder.CreateFileAsync(GetFileName(name), CreationCollisionOption.ReplaceExisting);
            var fileContent = await Json.SerializeAsync(content);

            await FileIO.WriteTextAsync(file, fileContent);
        }

        public static async Task<T> ReadAsync<T>(this StorageFolder folder, string name)
        {
            if (!File.Exists(Path.Combine(folder.Path, GetFileName(name))))
            {
                return default(T);
            }

            var file = await folder.GetFileAsync($"{name}.json");
            var fileContent = await FileIO.ReadTextAsync(file);

            return await Json.DeserializeAsync<T>(fileContent);
        }

        public static async Task SaveAsync<T>(this ApplicationDataContainer settings, string key, T value)
        {
            settings.SaveString(key, await Json.SerializeAsync(value));
        }

        public static void SaveString(this ApplicationDataContainer settings, string key, string value)
        {
            settings.Values[key] = value;
        }

        public static async Task<T> ReadAsync<T>(this ApplicationDataContainer settings, string key)
        {
            object obj = null;

            if (settings.Values.TryGetValue(key, out obj))
            {
                return await Json.DeserializeAsync<T>((string)obj);
            }

            return default(T);
        }

        public static async Task<StorageFile> SaveFileAsync(this StorageFolder folder, byte[] content, string fileName, CreationCollisionOption options = CreationCollisionOption.ReplaceExisting)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("ExceptionSettingsStorageExtensionsFileNameIsNullOrEmpty".GetLocalized(), nameof(fileName));
            }

            var storageFile = await folder.CreateFileAsync(fileName, options);
            await FileIO.WriteBytesAsync(storageFile, content);
            return storageFile;
        }

        public static async Task<byte[]> ReadFileAsync(this StorageFolder folder, string fileName)
        {
            var item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);

            if (item != null && item.IsOfType(StorageItemTypes.File))
            {
                var storageFile = await folder.GetFileAsync(fileName);
                byte[] content = await storageFile.ReadBytesAsync();
                return content;
            }

            return null;
        }

        public static async Task<byte[]> ReadBytesAsync(this StorageFile file)
        {
            if (file != null)
            {
                using (IRandomAccessStream stream = await file.OpenReadAsync())
                {
                    using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                    {
                        await reader.LoadAsync((uint)stream.Size);
                        var bytes = new byte[stream.Size];
                        reader.ReadBytes(bytes);
                        return bytes;
                    }
                }
            }

            return null;
        }

        private static string GetFileName(string name)
        {
            return string.Concat(name, FileExtension);
        }

        private const string FileExtension = ".json";
    }
}