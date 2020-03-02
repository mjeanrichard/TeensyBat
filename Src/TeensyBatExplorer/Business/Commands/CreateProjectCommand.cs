﻿// 
// Teensy Bat Explorer - Copyright(C) 2019 Meinrad Jean-Richard
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

using LiteDB;

using TeensyBatExplorer.Business.Models;

namespace TeensyBatExplorer.Business.Commands
{
    public class CreateProjectCommand
    {
        public async Task ExecuteAsyc(StorageFile storageFile)
        {
            using (Stream stream = await storageFile.OpenStreamForWriteAsync())
            {
                using (LiteDatabase db = new LiteDatabase(stream))
                {
                    LiteCollection<BatProject> projectCollection = db.GetCollection<BatProject>();
                    BatProject batProject = new BatProject() { CreatedOn = DateTime.Now, Name = "New Project" };
                    projectCollection.Insert(batProject);
                }
            }
        }
    }
}