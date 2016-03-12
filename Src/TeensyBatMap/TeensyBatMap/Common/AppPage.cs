using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;

namespace TeensyBatMap.Common
{
    public abstract class AppPage<TModel> : Page where TModel : IBaseViewModel
    {
        private TModel _viewModel;

        protected AppPage()
        {
            NavigationHelper = new NavigationHelper(this);
            NavigationHelper.LoadState += NavigationHelperLoadState;
            NavigationHelper.SaveState += NavigationHelperSaveState;
        }

        public NavigationHelper NavigationHelper { get; }

        public TModel ViewModel
        {
            get { return _viewModel; }
            protected set
            {
                _viewModel = value;
                DataContext = value;
            }
        }

        protected virtual async void NavigationHelperLoadState(object sender, LoadStateEventArgs e)
        {}

        protected virtual async void NavigationHelperSaveState(object sender, SaveStateEventArgs e)
        {}

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel = DependencyContainer.Current.Resolve<TModel>(new TypedParameterOverride<NavigationEventArgs>(e), new TypedParameterOverride<NavigationHelper>(NavigationHelper));
	        using (ViewModel.MarkBusy())
	        {
		        await ViewModel.Initialize();
		        NavigationHelper.OnNavigatedTo(e);
	        }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            NavigationHelper.OnNavigatedFrom(e);
        }
    }

    public class TypedParameterOverride<TParam> : ResolverOverride
    {
        private readonly InjectionParameterValue _value;

        public TypedParameterOverride(TParam value)
        {
            _value = InjectionParameterValue.ToParameter(value);
        }

        public override IDependencyResolverPolicy GetResolver(IBuilderContext context, Type dependencyType)
        {
            if (dependencyType == typeof(TParam))
            {
                return _value.GetResolverPolicy(dependencyType);
            }
            return null;
        }
    }
}