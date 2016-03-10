using System;
using Microsoft.Practices.Unity;
using TeensyBatMap.Database;
using TeensyBatMap.Domain;
using TeensyBatMap.Views.EditLog;
using TeensyBatMap.Views.LogDetails;

namespace TeensyBatMap.Common
{
    public static class DependencyContainer
    {
        private static UnityContainer _unityContainer;

        public static void InitializeContainer(App app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            UnityContainer unityContainer = new UnityContainer();

            unityContainer.RegisterType<BatContext>(new ContainerControlledLifetimeManager());
            unityContainer.RegisterInstance(new NavigationService(app), new ContainerControlledLifetimeManager());

            _unityContainer = unityContainer;
        }

        public static UnityContainer Current
        {
            get { return _unityContainer; }
        }
    }

    public class NavigationService
    {
        private readonly App _app;

        public NavigationService(App app)
        {
            _app = app;
        }

        public void NavigateToLogDetails(BatNodeLog log)
        {
            _app.RootFrame.Navigate(typeof(LogDetailsPage), log);
        }

        public void EditLog(BatNodeLog batLog)
        {
            _app.RootFrame.Navigate(typeof(EditLogPage), batLog);
        }
    }
}