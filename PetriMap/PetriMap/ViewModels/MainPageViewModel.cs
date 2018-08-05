using System.Windows.Input;
using Prism.Navigation;
using Xamarin.Forms;

namespace PetriMap.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private ICommand _navigateCommand;

        public ICommand NavigateCommand => _navigateCommand ?? (_navigateCommand = new Command<string>(ExecuteNavigateCommand));

        private async void ExecuteNavigateCommand(string url)
        {
            await NavigationService.NavigateAsync(url);
        }

        public MainPageViewModel(INavigationService navigationService) 
            : base (navigationService)
        {
            Title = "Home";
        }
    }
}
