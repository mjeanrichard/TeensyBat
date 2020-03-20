using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;

using LiteDB;

using SQLitePCL;

using TeensyBatExplorer.Business.Models;

namespace TeensyBatExplorer.Business.Queries
{
    public static class ProjectManagerQueries
    {
        public static IReadOnlyList<BatCall> GetCallsForNode(this ProjectManager projectManager, int nodeNumber)
        {
            LiteDatabase db = projectManager.GetDatabase();
            return db.GetCollection<BatCall>().Find(c => c.Node.NodeId == nodeNumber).ToList();
        }

        public static BatNode GetBatNode(this ProjectManager projectManager, int nodeNumber)
        {
            LiteDatabase db = projectManager.GetDatabase();
            return db.GetCollection<BatNode>().FindOne(n => n.NodeId == nodeNumber);
        }

    }



    public class GetProjectQuery
    {
        public async Task<BatProject> Execute(IStorageFile storageFile)
        {
            using (Stream stream = await storageFile.OpenStreamForWriteAsync())
            {
                using (LiteDatabase db = new LiteDatabase(stream))
                {
                    ILiteCollection<BatProject> projectCollection = db.GetCollection<BatProject>();
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
                    ILiteCollection<BatCall> batCallCollection = db.GetCollection<BatCall>();
                    return batCallCollection.FindAll().ToList();
                }
            }
        }
    }
}
