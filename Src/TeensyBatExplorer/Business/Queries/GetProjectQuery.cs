using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

using LiteDB;

using TeensyBatExplorer.Models;

namespace TeensyBatExplorer.Queries
{
    public class GetProjectQuery
    {
        public async Task<BatProject> Execute(IStorageFile storageFile)
        {
            using (Stream stream = await storageFile.OpenStreamForWriteAsync())
            {
                using (LiteDatabase db = new LiteDatabase(stream))
                {
                    LiteCollection<BatProject> projectCollection = db.GetCollection<BatProject>();
                    return projectCollection.FindAll().FirstOrDefault();
                }
            }
        }
    }
}
