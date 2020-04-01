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
using System.IO;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using TeensyBatExplorer.Core.Commands;
using TeensyBatExplorer.Core.Models;

namespace TeensyBatExplorer.Core
{
    public class ProjectManager : IDisposable
    {
        private readonly AddToMruCommand _addToMruCommand;

        private string _filename;
        private BatProject _batProject;

        public ProjectManager(AddToMruCommand addToMruCommand)
        {
            _addToMruCommand = addToMruCommand;
        }

        public bool IsProjectOpen { get; private set; }

        public BatProject Project => _batProject;

        public event EventHandler<EventArgs> ProjectChanged;

        public async Task OpenProject(string filename)
        {
            if (IsProjectOpen)
            {
                Close();
            }

            _filename = filename;
            using (ProjectContext context = new ProjectContext(filename))
            {
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
                await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE 'Projects' (
	                'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	                'Name'	TEXT NOT NULL,
	                'CreatedOn'	TEXT NOT NULL
                );

                CREATE TABLE 'DataFileEntries' (
	                'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	                'FftCount'	INTEGER NOT NULL,
                    'StartTimeMS' INTEGER NOT NULL,
                    'MaxPeakFrequency' REAL NOT NULL,
                    'AvgPeakFrequency' REAL NOT NULL,
                    'StartTime' TEXT NOT NULL,
                    'IsBat' INTEGER NOT NULL,

                    'HighFreqSampleCount' INTEGER NOT NULL,
                    'HighPowerSampleCount' INTEGER NOT NULL,
                    'MaxLevel' INTEGER NOT NULL,
                   
                    'DataFileId' INTEGER NOT NULL,
                    'NodeId' INTEGER NOT NULL
                );

                CREATE TABLE 'BatteryData' (
	                'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	                'Voltage'	INTEGER NOT NULL,
                    'DateTime'    TEXT NOT NULL,
                    'Timestamp'    INTEGER NOT NULL,

                    'DataFileId' INTEGER NOT NULL
                );

                CREATE TABLE 'TemperatureData' (
	                'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	                'Temperature'	INTEGER NOT NULL,
                    'DateTime'    TEXT NOT NULL,
                    'Timestamp'    INTEGER NOT NULL,
                   
                    'DataFileId' INTEGER NOT NULL
                );

                CREATE TABLE 'FftBlocks' (
	                'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	                'Index'	INTEGER NOT NULL,
	                'Loudness'	INTEGER NOT NULL,
	                'SampleNr'	INTEGER NOT NULL,
	                'Data'	BLOB NOT NULL,
                   
                    'DataFileEntryId' INTEGER NOT NULL
                );

                CREATE TABLE 'Nodes' (
	                'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                    'NodeNumber' INTEGER NOT NULL
                );

                CREATE TABLE 'Call' (
	                'Id'    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                    'StartTime'    TEXT NOT NULL,
                    'NodeId'    INTEGER NULL
                );

                CREATE TABLE 'DataFiles' (
	                'Id'    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                    'NodeNumber'    INTEGER NOT NULL,
                    'FirmwareVersion'    INTEGER NOT NULL,
                    'HardwareVersion'    INTEGER NOT NULL,
                    'Debug'    INTEGER NOT NULL,
                    'OriginalStartTime'    TEXT NOT NULL,
                    'StartTime'    TEXT NOT NULL,
                    'PreCallBufferSize'    INTEGER NOT NULL,
                    'AfterCallBufferSize'    INTEGER NOT NULL,
                    'CallStartThreshold'    INTEGER NOT NULL,
                    'CallEndThreshold'    INTEGER NOT NULL,
                    'ErrorCountCallBuffFull'    INTEGER NOT NULL,
                    'ErrorCountPointerBufferFull'    INTEGER NOT NULL,
                    'ErrorCountDataBufferFull'    INTEGER NOT NULL,
                    'ErrorCountProcessOverlap'    INTEGER NOT NULL,
                    
                    'Filename'    TEXT NOT NULL,
                    'NodeId'    INTEGER NULL
                );

                CREATE TABLE 'DataFileMessages' (
	                'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                    'DataFileId' INTEGER NOT NULL,
                    'Message' TEXT NOT NULL,
                    'Level' TEXT NOT NULL,
                    'Position' INTEGER NULL
                );

            ");

                BatProject project = new BatProject();
                project.Name = "New Project";
                context.Projects.Add(project);
                await context.SaveChangesAsync();
            }

            await OpenProject(filename);
        }

        public ProjectContext GetContext()
        {
            if (IsProjectOpen)
            {
                return new ProjectContext(_filename);
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
    }
}