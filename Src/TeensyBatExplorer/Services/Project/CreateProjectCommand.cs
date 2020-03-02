using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

using LiteDB;

using TeensyBatExplorer.Business.Models;

namespace TeensyBatExplorer.Services.Project
{
    public class ProjectManager : IDisposable
    {
        private StorageFile _storageFile;
        private Stream _stream;
        private LiteDatabase _db;
        private BatProject _batProject;

        public bool IsProjectOpen { get; private set; }

        public async Task OpenProject(StorageFile storageFile)
        {
            if (IsProjectOpen)
            {
                Close();
            }
            _storageFile = storageFile;
            _stream = await storageFile.OpenStreamForWriteAsync();
            _db = new LiteDatabase(_stream);
            LiteCollection<BatProject> projectCollection = _db.GetCollection<BatProject>();
            _batProject = projectCollection.FindAll().FirstOrDefault();
        }

        public async Task CreateNewProject(StorageFile storageFile)
        {
            if (IsProjectOpen)
            {
                Close();
            }
            _storageFile = storageFile;
            _stream = await storageFile.OpenStreamForWriteAsync();
            _db = new LiteDatabase(_stream);

            LiteCollection<BatProject> projectCollection = _db.GetCollection<BatProject>();
            _batProject = new BatProject() { CreatedOn = DateTime.Now, Name = "New Project" };
            projectCollection.Insert(_batProject);
            _db.Checkpoint();
            IsProjectOpen = true;
        }

        public void Close()
        {
            _db.Dispose();
            _db = null;
            _stream.Close();
            _stream = null;
            _storageFile = null;
            _batProject = null;
            IsProjectOpen = false;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
