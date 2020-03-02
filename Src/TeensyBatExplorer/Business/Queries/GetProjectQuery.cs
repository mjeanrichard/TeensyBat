using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;

using LiteDB;

using TeensyBatExplorer.Business.Models;

namespace TeensyBatExplorer.Business.Queries
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
    public class GetLogDetailsQuery
    {
        public async Task<IEnumerable<BatCall>> Execute(IStorageFile storageFile)
        {
            using (Stream stream = await storageFile.OpenStreamForWriteAsync())
            {
                using (LiteDatabase db = new LiteDatabase(stream))
                {
                    LiteCollection<BatCall> batCallCollection = db.GetCollection<BatCall>();
                    return batCallCollection.FindAll().ToList();
                }
            }
        }
    }
}
