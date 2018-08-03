using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
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
            Title = "Petrimap - XForms";
        }
    }
}
