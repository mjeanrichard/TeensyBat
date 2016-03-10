using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using TeensyBatMap.Domain;

namespace TeensyBatMap.Database
{
    public class DbManager
    {
        private static readonly Regex SqlSplitter = new Regex(@"^--\s*GO\s*$", RegexOptions.Multiline);
        private const string DbName = "batcalls.sqlite";
        private SQLiteAsyncConnection _db;

        public async Task Initialize()
        {
            var dbFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(DbName);
            List<DbVersion> versions = new List<DbVersion>();

            if (dbFile == null)
            {
                _db = new SQLiteAsyncConnection(Path.Combine(ApplicationData.Current.LocalFolder.Path, DbName));
            }
            else
            {
                _db = new SQLiteAsyncConnection(dbFile.Path);
            }

            await _db.ExecuteAsync("PRAGMA foreign_keys = ON;").ConfigureAwait(false);

            int hasVersionTable = await _db.ExecuteScalarAsync<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='DbVersion';").ConfigureAwait(false);
            if (hasVersionTable > 0)
            {
                versions = await _db.Table<DbVersion>().OrderBy(v => v.Version).ToListAsync().ConfigureAwait(false);
            }

            UpdateDb(versions);
        }

        private async void UpdateDb(List<DbVersion> versions)
        {
            List<string> scripts = LoadScripts();

            for (int i = 0; i < versions.Count; i++)
            {
                DbVersion dbVersion = versions[i];
                if (dbVersion.Version != i + 1)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "DB Skript Version {0} is missing.", i + 1));
                }
            }
            if (versions.Count < scripts.Count)
            {
                TypeInfo typeInfo = typeof(DbManager).GetTypeInfo();
                Assembly assembly = typeInfo.Assembly;

                await _db.RunInTransactionAsync(c =>
                {
                    for (int i = versions.Count; i < scripts.Count; i++)
                    {
                        string sqlScript;
                        string scriptName = scripts[i];
                        using (Stream stream = assembly.GetManifestResourceStream(scriptName))
                        {
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                sqlScript = sr.ReadToEnd();
                            }
                        }

                        string[] scriptParts = SqlSplitter.Split(sqlScript);
                        foreach (string scriptPart in scriptParts)
                        {
                            c.Execute(scriptPart);
                        }
                        c.Insert(new DbVersion {Name = scriptName, Version = i + 1});
                    }
                }).ConfigureAwait(false);
            }
        }

        private List<string> LoadScripts()
        {
            List<KeyValuePair<int, string>> scripts = new List<KeyValuePair<int, string>>();
            TypeInfo typeInfo = typeof(DbManager).GetTypeInfo();
            Assembly assembly = typeInfo.Assembly;
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.StartsWith(typeInfo.Namespace))
                {
                    string filename = resourceName.Substring(typeInfo.Namespace.Length + 1);
                    string[] parts = filename.Split(' ');
                    if (parts.Length > 2)
                    {
                        int version;
                        if (int.TryParse(parts[0], out version))
                        {
                            scripts.Add(new KeyValuePair<int, string>(version, resourceName));
                        }
                    }
                }
            }
            return scripts.OrderBy(s => s.Key).Select(s => s.Value).ToList();
        }

        public async Task InsertNewLog(BatNodeLog log)
        {
            await _db.RunInTransactionAsync(c =>
            {
                c.Insert(log);
                c.InsertAll(log.Calls);
            }).ConfigureAwait(false);
        }

        public async Task UpdateLog(BatNodeLog log)
        {
            await _db.UpdateAsync(log).ConfigureAwait(false);
        }

        public async Task<List<BatNodeLog>>  LoadAllLogs()
        {
            return await _db.Table<BatNodeLog>().ToListAsync();
        }

        public async Task<List<BatCall>> GetCalls(BatNodeLog batLog)
        {
            return await _db.Table<BatCall>().Where(c => c.BatNodeLogId == batLog.Id).ToListAsync();
        }
    }
}