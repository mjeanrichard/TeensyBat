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
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.SQLite.Helpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core
{
    public class ProjectManager : IDisposable
    {
        private readonly AddToMruCommand _addToMruCommand;
        private BatProject _batProject;

        public ProjectManager(AddToMruCommand addToMruCommand)
        {
            _addToMruCommand = addToMruCommand;
        }

        public string Filename { get; private set; }

        public bool IsProjectOpen { get; private set; }

        public BatProject Project => _batProject;

        public event EventHandler<EventArgs> ProjectChanged;

        public async Task OpenProject(string filename)
        {
            if (IsProjectOpen)
            {
                Close();
            }

            Filename = filename;
            using (ProjectContext context = new ProjectContext(filename))
            {
                await Upgrade(context);
                _batProject = await context.Projects.AsNoTracking().SingleAsync();
            }

            await _addToMruCommand.Execute(_batProject, filename);

            IsProjectOpen = true;
            OnProjectChanged();
        }

        public async Task CreateNewProject(string filename)
        {
            if (IsProjectOpen)
            {
                Close();
            }

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using (ProjectContext context = new ProjectContext(filename))
            {
                await Upgrade(context);

                BatProject project = new BatProject();
                project.Name = "New Project";
                context.Projects.Add(project);
                await context.SaveChangesAsync();
            }

            await OpenProject(filename);
        }

        private async Task Upgrade(ProjectContext context)
        {
            using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync())
            {
                using (SharedConnection sharedConnection = new SharedConnection(context.Database.GetDbConnection()))
                {
                    UpgradeLog upgradeLog = new UpgradeLog();
                    UpgradeEngine upgrader = DeployChanges.To.SQLiteDatabase(sharedConnection)
                        .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                        .WithoutTransaction()
                        .LogTo(upgradeLog)
                        .Build();

                    if (upgrader.IsUpgradeRequired())
                    {
                        DatabaseUpgradeResult result = upgrader.PerformUpgrade();
                        if (!result.Successful)
                        {
                            throw result.Error;
                        }

                        upgradeLog.Save(context);
                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                }
            }
        }

        public ProjectContext GetContext()
        {
            if (IsProjectOpen)
            {
                return new ProjectContext(Filename);
            }

            return null;
        }

        public void Close()
        {
            _batProject = null;
            IsProjectOpen = false;
            OnProjectChanged();
        }

        public void Dispose()
        {
            Close();
        }

        protected virtual void OnProjectChanged()
        {
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        private class UpgradeLog : IUpgradeLog
        {
            readonly List<ProjectMessage> _messages = new List<ProjectMessage>();

            public void WriteInformation(string format, params object[] args)
            {
                _messages.Add(new ProjectMessage(BatLogMessageLevel.Information, MessageTypes.Database, format, args));
            }

            public void WriteError(string format, params object[] args)
            {
                _messages.Add(new ProjectMessage(BatLogMessageLevel.Error, MessageTypes.Database, format, args));
            }

            public void WriteWarning(string format, params object[] args)
            {
                _messages.Add(new ProjectMessage(BatLogMessageLevel.Warning, MessageTypes.Database, format, args));
            }

            public void Save(ProjectContext context)
            {
                context.ProjectMessages.AddRange(_messages);
            }
        }
    }
}