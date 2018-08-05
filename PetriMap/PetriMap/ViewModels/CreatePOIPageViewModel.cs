using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using PetriMap.Resources;
using Plugin.Geolocator;
using Plugin.Permissions;
using Prism.Navigation;
using Prism.Services;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.GoogleMaps.Bindings;

namespace PetriMap.ViewModels
{
    public class CreatePOIPageViewModel : ViewModelBase, INavigationAware
    {
        string _description;
        bool _isLoading;
        string _myGpsCoordinates;
        bool _hasGpsCoordinates;
        double _latitude;
        double _longitude;
        string _sendButtonText;
        ICommand _getMyLocationCommand;
        ICommand _sendFormCommand;
        IPageDialogService _dialogService;
        bool _isFindingUserLocation;

        public CreatePOIPageViewModel(INavigationService navigationService,
                                      IPageDialogService dialogService
                                      ) : base(navigationService)
        {
            _dialogService = dialogService;
            SendButtonText = "Senden!";
            _latitude = double.NaN;
            _longitude = double.NaN;
        }
        public ObservableCollection<Pin> MyLocation { get; set; } = new ObservableCollection<Pin>();

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                RaisePropertyChanged();
            }
        }

        public bool IsFindingUserLocation
        {
            get { return _isFindingUserLocation; }
            set { SetProperty(ref _isFindingUserLocation, value); }
        }


        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                SendButtonText = IsLoading ? "Bitte warten" : "Senden";
                RaisePropertyChanged();
            }
        }

        public bool IsBusy { get; private set; }

        public double Longitude
        {
            get { return _longitude; }
            set
            {
                _longitude = value;
                RaisePropertyChanged();
            }
        }

        public double Latitude
        {
            get { return _latitude; }
            set
            {
                _latitude = value;
                RaisePropertyChanged();
            }
        }



        public bool HasGpsCoordinates
        {
            get { return _hasGpsCoordinates; }
            set
            {
                _hasGpsCoordinates = value;
                RaisePropertyChanged();
            }
        }

        public string SendButtonText
        {
            get { return _sendButtonText; }
            set
            {
                _sendButtonText = value;
                RaisePropertyChanged();
            }
        }

        public MoveToRegionRequest MoveToRegionRequest
        {
            get;
        } = new MoveToRegionRequest();

        public ICommand GetMyLocationCommand
        => _getMyLocationCommand ?? (_getMyLocationCommand = new Command(async () => await ExecuteGetMyLocationCommand(null)));

        public ICommand SendFormCommand
            => _sendFormCommand ?? (_sendFormCommand = new Command(ExecuteSendFormCommand));


        private async Task<Plugin.Geolocator.Abstractions.Position> ExecuteGetMyLocationCommand(object obj)
        {
            if (IsBusy)
                return null;

            Plugin.Geolocator.Abstractions.Position myPosition = null;


            try
            {
                IsLoading = true;
                HasGpsCoordinates = false;
                // Check if we have permission
                var locationStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Location);

                if (locationStatus != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    var myPermissions = await CrossPermissions.Current.RequestPermissionsAsync(Plugin.Permissions.Abstractions.Permission.Location);

                    if (myPermissions[Plugin.Permissions.Abstractions.Permission.Location] != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                    {
                        await _dialogService.DisplayAlertAsync(TextResources.Error, "Berechtigung benötigt!", TextResources.Ok);
                        return null;
                    }
                }

                if (!CrossGeolocator.Current.IsGeolocationEnabled)
                {
                    await _dialogService.DisplayAlertAsync(TextResources.LocationRequired, "Bitte schalte GPS ein, um die aktuelle Position zu ermitteln oder gib Deine Adresse manuell ein.",
                        TextResources.Ok);

                    return null;
                }

                if (!CrossGeolocator.Current.IsGeolocationAvailable)
                {
                    await _dialogService.DisplayAlertAsync(TextResources.Error, "Die GPS-Erkennung ist momentan nicht verfügbar. Probiere es später nochmal oder gib Deine Adresse manuell ein.",
                        TextResources.Ok);
                    return null;
                }


                // Try get current position
                try
                {
                    IsFindingUserLocation = true;
                    myPosition = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(20), null);
                }
                catch (Exception e)
                {
                    // Try get the last known position as fallback
                    myPosition = await CrossGeolocator.Current.GetLastKnownLocationAsync();
                    //EventTracking.Tracker.TrackError("ReportProblemViewModel", "ExecuteGetMyLocationCommand::GetPositionAsync", e.ToString());
                }
                finally
                {
                    IsFindingUserLocation = false;
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                await _dialogService.DisplayAlertAsync(TextResources.Error, TextResources.UnknownError + $"\n{e}", TextResources.Ok);

                //EventTracking.Tracker.TrackError("ReportProblemViewModel", "ExecuteGetMyLocationCommand", e.ToString());
            }
            finally
            {
                UpdateMap(myPosition);

                Latitude = myPosition.Latitude;
                Longitude = myPosition.Longitude;

                IsLoading = false;
                IsBusy = false;

            }
            return myPosition;
        }

        private void UpdateMap(Plugin.Geolocator.Abstractions.Position position)
        {
            try
            {


                var gFormsPos = new Xamarin.Forms.GoogleMaps.Position(position.Latitude, position.Longitude);
                MyLocation.Clear();
                MyLocation.Add(new Pin()
                {
                    Position = gFormsPos,
                    Label = "My Position"
                });


                MoveToRegionRequest.MoveToRegion(MapSpan.FromCenterAndRadius(gFormsPos,
                                                                             Distance.FromKilometers(1)));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override void Destroy()
        {
            base.Destroy();

        }

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            if (parameters.GetNavigationMode() == NavigationMode.Back)
                return;

            await ExecuteGetMyLocationCommand(null);


        }

        private async void ExecuteSendFormCommand(object obj)
        {

        }


    }

}
