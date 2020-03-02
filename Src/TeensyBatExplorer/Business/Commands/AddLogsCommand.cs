
// Teensy Bat Explorer - Copyright(C)  Meinrad Jean-Richard
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
using System.Linq;
using System.Threading.Tasks;

using Windows.ApplicationModel.Core;
using Windows.Storage;

using LiteDB;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Business.Models;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Services;
using TeensyBatExplorer.Views.Project;

namespace TeensyBatExplorer.Business.Commands
{
    public class AddLogsCommand
    {
        private readonly LogReader _logReader;

        public AddLogsCommand(LogReader logReader)
        {
            _logReader = logReader;
        }

        public async Task ExecuteAsync(IStorageFile projectFile, IEnumerable<IStorageFile> logFiles, BusyState busyState)
        {
            await Task.Run(async () => await ExecuteInternalAsync(projectFile, logFiles, busyState)).ConfigureAwait(false);
        }

        private async Task ExecuteInternalAsync(IStorageFile projectFile, IEnumerable<IStorageFile> logFiles, BusyState busyState)
        {
            BatLog log = new BatLog();
            foreach (IStorageFile file in logFiles)
            {
                await _logReader.Load(file, log);
            }

            using (Stream stream = await projectFile.OpenStreamForWriteAsync())
            {
                using (LiteDatabase db = new LiteDatabase(stream))
                {
                    LiteCollection<BatCall> batCallCollection = db.GetCollection<BatCall>();
                    LiteCollection<BatteryData> batteryDataCollection = db.GetCollection<BatteryData>();
                    LiteCollection<TemperatureData> tempDataCollection = db.GetCollection<TemperatureData>();
                    LiteCollection<BatLog> logCollection = db.GetCollection<BatLog>();

                    if (!CoreApplication.MainView.Dispatcher.HasThreadAccess)
                    {
                        await DispatcherHelper.ExecuteOnUIThreadAsync(() => busyState.MaxProgressValue = log.Calls.Count).ConfigureAwait(true);
                    }
                    else
                    {
                        busyState.MaxProgressValue = log.Calls.Count;
                    }

                    db.BeginTrans();
                    int i = 0;
                    foreach (BatCall logCall in log.Calls)
                    {
                        batCallCollection.Insert(logCall);
                        i++;
                        if (i % 500 == 0)
                        {
                            db.Commit();
                            db.Checkpoint();
                            if (!CoreApplication.MainView.Dispatcher.HasThreadAccess)
                            {
                                await DispatcherHelper.ExecuteOnUIThreadAsync(() => busyState.ProgressValue = i).ConfigureAwait(true);
                            }
                            else
                            {
                                busyState.ProgressValue = i;
                            }
                            db.BeginTrans();
                        }
                    }
                    db.Commit();
                    db.Checkpoint();

                    db.BeginTrans();
                    batteryDataCollection.Insert(log.BatteryData);
                    tempDataCollection.Insert(log.TemperatureData);
                    logCollection.Insert(log);
                    db.Commit();
                }
            }

        }
    }
}