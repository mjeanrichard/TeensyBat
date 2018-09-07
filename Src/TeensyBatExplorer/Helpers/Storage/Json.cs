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

using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TeensyBatExplorer.Helpers.Storage
{
    public static class Json
    {
        public static async Task<T> DeserializeAsync<T>(string value)
        {
            return await Task.Run<T>(() => { return JsonConvert.DeserializeObject<T>(value); });
        }

        public static async Task<string> SerializeAsync(object value)
        {
            return await Task.Run<string>(() => { return JsonConvert.SerializeObject(value); });
        }
    }
}