// 
// Teensy Bat Explorer - Copyright(C) 2019 Meinrad Jean-Richard
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

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

using TeensyBatExplorer.Helpers.DependencyInjection;
using TeensyBatExplorer.Services;

using Unity;

namespace TeensyBatExplorer
{
    public sealed partial class App : Application
    {
        private readonly Lazy<ActivationService> _activationService;
        private readonly SuspendAndResumeService _suspendAndResumeService;


        public App()
        {
            DependencyContainer.InitializeContainer(this);

            InitializeComponent();

            EnteredBackground += App_EnteredBackground;

            // TODO WTS: Add your app in the app center and set your secret here. More at https://docs.microsoft.com/en-us/appcenter/sdk/getting-started/uwp
            //AppCenter.Start("{Your App Secret}", typeof(Analytics), typeof(Crashes));

            _activationService = new Lazy<ActivationService>(() => DependencyContainer.Current.Resolve<ActivationService>());
            _suspendAndResumeService = DependencyContainer.Current.Resolve<SuspendAndResumeService>();
        }

        private ActivationService ActivationService
        {
            get { return _activationService.Value; }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (!args.PrelaunchActivated)
            {
                await ActivationService.ActivateAsync(args);
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await ActivationService.ActivateAsync(args);
        }

        private async void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            var deferral = e.GetDeferral();
            await _suspendAndResumeService.SaveStateAsync();
            deferral.Complete();
        }
    }
}