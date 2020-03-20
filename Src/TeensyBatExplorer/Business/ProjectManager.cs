using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

using LiteDB;

using TeensyBatExplorer.Business.Models;
using TeensyBatExplorer.Services;

namespace TeensyBatExplorer.Business
{
    public class ProjectManager : IDisposable
    {
        private readonly MruService _mruService;
        private StorageFile _storageFile;
        private Stream _stream;
        private LiteDatabase _db;
        private BatProject _batProject;

        public bool IsProjectOpen { get; private set; }

        public BatProject Project => _batProject;

        public ProjectManager(MruService mruService)
        {
            _mruService = mruService;
        }

        public async Task OpenProject(StorageFile storageFile)
        {
            if (IsProjectOpen)
            {
                Close();
            }
            _storageFile = storageFile;
            _stream = await storageFile.OpenStreamForWriteAsync();
            _db = new LiteDatabase(_stream);
            ILiteCollection<BatProject> projectCollection = _db.GetCollection<BatProject>();
            _batProject = projectCollection.FindAll().FirstOrDefault();

            _mruService.AddProject(storageFile, _batProject);
        
            IsProjectOpen = true;
        }

        public async Task CreateNewProject(StorageFile storageFile)
        {
            if (IsProjectOpen)
            {
                Close();
            }
            _storageFile = storageFile;
            _stream = await storageFile.OpenStreamForWriteAsync();
            _stream.SetLength(0);
            _db = new LiteDatabase(_stream);

            ILiteCollection<BatProject> projectCollection = _db.GetCollection<BatProject>();
            _batProject = new BatProject() { CreatedOn = DateTime.Now, Name = "New Project" };
            projectCollection.Insert(_batProject);
            _db.Checkpoint();

            _mruService.AddProject(storageFile, _batProject);

            IsProjectOpen = true;
        }

        public LiteDatabase GetDatabase()
        {
            if (IsProjectOpen)
            {
                return _db;
            }

            return null;
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
