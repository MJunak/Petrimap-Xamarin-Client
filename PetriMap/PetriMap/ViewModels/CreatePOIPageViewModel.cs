﻿using Prism.Navigation;
using System.Windows.Input;
using Xamarin.Forms;
using System;
using Prism.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using Plugin.Geolocator;
using System.Collections.Generic;
using Plugin.Geolocator.Abstractions;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Xamarin.Forms.GoogleMaps;
using Xamarin.Forms.GoogleMaps.Bindings;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.IO;
using PetriMap.Resources;
using Plugin.Permissions;

namespace PetriMap.ViewModels
{
    public class CreatePOIPageViewModel : ViewModelBase
    {
        #region Fields
        private const string DefaultPhotoPlaceholder = "loading_placeholder.jpg";

        private string _firstName;
        private string _lastName;
        private string _description;
        private string _email;
        private int _selectedCategoryIndex;
        private Stream _photoStream;
        private string _street;
        private string _city;
        private string _postcode;
        private string _country;
        private string _photo;
        private string _locationDescription;
        private bool _isLoading;
        private string _shortAddress;
        private string _myGpsCoordinates;
        private bool _hasGpsCoordinates;
        private double _latitude;
        private double _longitude;
        private bool _isLocationDirty;
        private string _sendButtonText;
        private bool _hasPhoto;
        private ICommand _addPhotoCommand;
        private ICommand _getMyLocationCommand;
        private ICommand _sendFormCommand;
        private IPageDialogService _dialogService;
        private bool _isFindingUserLocation;

        #endregion

        #region Ctor

        public CreatePOIPageViewModel(INavigationService navigationService,
                                      IPageDialogService dialogService
                                      ) : base(navigationService)
        {
            _dialogService = dialogService;
            SendButtonText = "Senden";
            _latitude = double.NaN;
            _longitude = double.NaN;
        }
        #endregion

        #region Properties
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

    

     
        public string Photo
        {
            get { return _photo; }
            set
            {
                _photo = value;
                HasPhoto = !string.IsNullOrEmpty(value);
                RaisePropertyChanged();
            }
        }

        public string LocationDescription
        {
            get { return _locationDescription; }
            set
            {
                _locationDescription = value;
                RaisePropertyChanged();
            }
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

        public string MyGpsCoordinates
        {
            get { return _myGpsCoordinates; }
            set
            {
                _myGpsCoordinates = value;
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

        public bool HasPhoto
        {
            get { return _hasPhoto; }
            set
            {
                _hasPhoto = value;
                RaisePropertyChanged();
            }
        }

        public MoveToRegionRequest MoveToRegionRequest
        {
            get;
        } = new MoveToRegionRequest();

        public ICommand GetMyLocationCommand
            => _getMyLocationCommand ?? (_getMyLocationCommand = new Command(ExecuteGetMyLocationCommand));

        public ICommand SendFormCommand
            => _sendFormCommand ?? (_sendFormCommand = new Command(ExecuteSendFormCommand));

        #endregion

        #region Methods


        private async void ExecuteGetMyLocationCommand(object obj)
        {
            if (IsBusy)
                return;

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
                        return;
                    }
                }

                if (!CrossGeolocator.Current.IsGeolocationEnabled)
                {
                    await _dialogService.DisplayAlertAsync(TextResources.LocationRequired, "Bitte schalte GPS ein, um die aktuelle Position zu ermitteln oder gib Deine Adresse manuell ein.",
                        TextResources.Ok);

                    return;
                }

                if (!CrossGeolocator.Current.IsGeolocationAvailable)
                {
                    await _dialogService.DisplayAlertAsync(TextResources.Error, "Die GPS-Erkennung ist momentan nicht verfügbar. Probiere es später nochmal oder gib Deine Adresse manuell ein.",
                        TextResources.Ok);
                    return;
                }

                Plugin.Geolocator.Abstractions.Position myPosition = null;

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
                IsLoading = false;
                IsBusy = false;
            }
        }

        private void UpdateMap(Address address)
        {
            MyLocation.Clear();
            MyLocation.Add(new Pin()
            {
                Position = new Xamarin.Forms.GoogleMaps.Position(address.Latitude, address.Longitude),
                Label = $"{address.Thoroughfare} {address.SubThoroughfare}, {address.Locality} {address.PostalCode}"
            });

            MoveToRegionRequest.MoveToRegion(MapSpan.FromCenterAndRadius(new Xamarin.Forms.GoogleMaps.Position(address.Latitude, address.Longitude),
                                                                         Distance.FromKilometers(1)));
        }

        public override void Destroy()
        {
            base.Destroy();

            if (_photoStream != null)
                _photoStream.Dispose();
        }

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            if (parameters.GetNavigationMode() == NavigationMode.Back)
                return;

        }

        private async void ExecuteSendFormCommand(object obj)
        {
        
        }

      
        private void UpdateMyGpsCoordinates(Address address)
        {
            MyGpsCoordinates = $"Koordinaten erfasst: {address.Latitude.ToString("0.00")}° {address.Longitude.ToString("0.00")}°";

            _latitude = address.Latitude;
            _longitude = address.Longitude;
        }

       
        #endregion
    }

}
