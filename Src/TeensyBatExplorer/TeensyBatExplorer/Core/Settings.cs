using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;

namespace TeensyBatExplorer.Core
{
    public class Settings
    {
        public string ProjectFolderToken
        {
            get { return ApplicationData.Current.LocalSettings.Values["ProjectFolderToken"] as string; }
            set { ApplicationData.Current.LocalSettings.Values["ProjectFolderToken"] = value; }
        }
    }
}
