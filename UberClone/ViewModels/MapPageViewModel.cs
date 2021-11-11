using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using Stateless;
using TrackingSample.Helpers;
using UberClone.Helpers;
using UberClone.Models;
using UberClone.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using static Stateless.StateMachine<UberClone.Helpers.XUberState, UberClone.Helpers.XUberTrigger>;

namespace UberClone.ViewModels
{
    public class MapPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<GooglePlaceAutoCompletePrediction> Places { get; private set; }
        public ObservableCollection<GooglePlaceAutoCompletePrediction> RecentPlaces { get; private set; }
        public GooglePlaceAutoCompletePrediction RecentPlace1 { get; private set; }
        public GooglePlaceAutoCompletePrediction RecentPlace2 { get; private set; }
        public ObservableCollection<PriceOption> PriceOptions { get; private set; }
        public PriceOption PriceOptionSelected { get; set; }

        public string PickupLocation { get; set; }

        Location OriginCoordinates { get; set; }
        Location DestinationCoordinates { get; set; }

        string _destinationLocation;
        public string DestinationLocation
        {
            get => _destinationLocation;
            set
            {
                _destinationLocation = value;
                if (!string.IsNullOrEmpty(_destinationLocation))
                {
                    GetPlacesCommand.Execute(_destinationLocation);
                }
            }
        }

        GooglePlaceAutoCompletePrediction _placeSelected;
        public GooglePlaceAutoCompletePrediction PlaceSelected
        {
            get => _placeSelected;
            set
            {
                _placeSelected = value;
                if (_placeSelected != null)
                {
                    GetPlaceDetailCommand.Execute(_placeSelected);
                }

            }
        }

        bool _isOriginFocused;
        public bool IsOriginFocused
        {
            get => _isOriginFocused;
            set
            {
                _isOriginFocused = value;
                if (_isOriginFocused)
                {
                    FireTriggerCommand.Execute(XUberTrigger.ChooseOrigin);
                }
            }
        }

        bool _isDestinationFocused;
        public bool IsDestinationFocused
        {
            get => _isDestinationFocused;
            set
            {
                _isDestinationFocused = value;
                if (_isDestinationFocused)
                {
                    FireTriggerCommand.Execute(XUberTrigger.ChooseDestination);
                }
            }
        }

        public XUberState State { get; private set; }

        public bool IsSearching { get; private set; }

        public ICommand DrawRouteCommand { get; set; }
        public ICommand CenterMapCommand { get; set; }
        public ICommand CleanPolylineCommand { get; set; }
        public ICommand GetPlaceDetailCommand { get; }
        public ICommand FireTriggerCommand { get; }
        public ICommand CameraIdledCommand { get; private set; }

        private ICommand GetPlacesCommand { get; }
        public ICommand GetLocationNameCommand { get;  }

        private TriggerWithParameters<GooglePlaceAutoCompletePrediction> CalculateRouteTrigger  { get; }

        private readonly IGoogleMapsApiService _googleMapsApi = new GoogleMapsApiService();
        private readonly StateMachine<XUberState, XUberTrigger> _stateMachine;

        public MapPageViewModel()
        {
            RecentPlaces = new ObservableCollection<GooglePlaceAutoCompletePrediction>()
            {
                {new GooglePlaceAutoCompletePrediction(){ PlaceId="ChIJq0wAE_CJr44RtWSsTkp4ZEM", StructuredFormatting=new StructuredFormatting(){ MainText="Random Place", SecondaryText="Paseo de los locutores #32" } } },
                {new GooglePlaceAutoCompletePrediction(){ PlaceId="ChIJq0wAE_CJr44RtWSsTkp4ZEM", StructuredFormatting=new StructuredFormatting(){ MainText="Green Tower", SecondaryText="Ensanche Naco #4343, Green 232" } } },
                {new GooglePlaceAutoCompletePrediction(){ PlaceId="ChIJm02ImNyJr44RNs73uor8pFU", StructuredFormatting=new StructuredFormatting(){ MainText="Tienda Aurora", SecondaryText="Rafael Augusto Sanchez" } } },
            };

            RecentPlace1 = RecentPlaces[0];
            RecentPlace2 = RecentPlaces[1];

            PriceOptions = new ObservableCollection<PriceOption>()
            {
                {new PriceOption(){ Tag="xUBERX", Category="Economy", CategoryDescription="Affortable, everyday rides", PriceDetails=new System.Collections.Generic.List<PriceDetail>(){
                    { new PriceDetail(){ Type="xUber X", Price=332, ArrivalETA="12:00pm", Icon="https://carcody.com/wp-content/uploads/2019/11/Webp.net-resizeimage.jpg" } },
                  { new PriceDetail(){ Type="xUber Black", Price=150, ArrivalETA="12:40pm", Icon="https://i0.wp.com/uponarriving.com/wp-content/uploads/2019/08/uberxl.jpg" } }}
                 } },
                {new PriceOption(){Tag="xUBERXL", Category="Extra Seats", CategoryDescription="Affortable rides for group up to 6" ,  PriceDetails=new System.Collections.Generic.List<PriceDetail>(){
                    { new PriceDetail(){ Type="xUber XL", Price=332, ArrivalETA="12:00pm", Icon="https://i0.wp.com/uponarriving.com/wp-content/uploads/2019/08/uberxl.jpg" } }
                  } } }
            };


            var _stateMachine = new StateMachine<XUberState, XUberTrigger>(XUberState.Initial);

            CalculateRouteTrigger = _stateMachine.SetTriggerParameters<GooglePlaceAutoCompletePrediction>(XUberTrigger.CalculateRoute);

            _stateMachine.Configure(XUberState.Initial)
                .OnEntry(Initialize)
                .OnExit(() => { Places = new ObservableCollection<GooglePlaceAutoCompletePrediction>(RecentPlaces); })
                .OnActivateAsync(GetActualUserLocation)
                .Permit(XUberTrigger.ChooseDestination, XUberState.SearchingDestination)
                .Permit(XUberTrigger.CalculateRoute, XUberState.CalculatingRoute); 

            _stateMachine
                .Configure(XUberState.SearchingOrigin)
                .Permit(XUberTrigger.Cancel, XUberState.Initial)
                .Permit(XUberTrigger.ChooseDestination, XUberState.SearchingDestination);

            _stateMachine
                .Configure(XUberState.SearchingDestination)
                .Permit(XUberTrigger.Cancel, XUberState.Initial)
                .Permit(XUberTrigger.ChooseOrigin, XUberState.SearchingOrigin)
                .PermitIf(XUberTrigger.CalculateRoute, XUberState.CalculatingRoute, () => OriginCoordinates != null);

            _stateMachine
              .Configure(XUberState.CalculatingRoute)
              .OnEntryFromAsync(CalculateRouteTrigger, GetPlacesDetail)
              .Permit(XUberTrigger.ChooseRide, XUberState.ChoosingRide)
              .Permit(XUberTrigger.Cancel, XUberState.Initial); 

            _stateMachine
               .Configure(XUberState.ChoosingRide)
               .Permit(XUberTrigger.Cancel, XUberState.Initial)
               .Permit(XUberTrigger.ChooseDestination, XUberState.SearchingDestination)
               .Permit(XUberTrigger.ConfirmPickUp, XUberState.ConfirmingPickUp);

            _stateMachine
              .Configure(XUberState.ConfirmingPickUp)
              .Permit(XUberTrigger.ChooseRide, XUberState.ChoosingRide)
              .Permit(XUberTrigger.ShowXUberPass, XUberState.ShowingXUberPass);

            _stateMachine
              .Configure(XUberState.ShowingXUberPass)
              .Permit(XUberTrigger.ConfirmPickUp, XUberState.ConfirmingPickUp)
              .Permit(XUberTrigger.WaitForDriver, XUberState.WaitingForDriver);

            _stateMachine
             .Configure(XUberState.WaitingForDriver)
             .Permit(XUberTrigger.CancelTrip, XUberState.Initial)
             .Permit(XUberTrigger.StartTrip, XUberState.TripInProgress);

            GetPlaceDetailCommand = new Command<GooglePlaceAutoCompletePrediction>(async(param) =>
            {
                if (_stateMachine.CanFire(XUberTrigger.CalculateRoute))
                {
                   await _stateMachine.FireAsync(CalculateRouteTrigger, param);

                    State = _stateMachine.State;
                }
            });
            GetPlacesCommand = new Command<string>(async (param) => await GetPlacesByName(param));
            GetLocationNameCommand = new Command<Position>(async (param) => await GetLocationName(param));

            FireTriggerCommand = new Command<XUberTrigger>((trigger) =>
            {
                if(_stateMachine.CanFire(trigger))
                {
                    _stateMachine.Fire(trigger);
                    State = _stateMachine.State;
                }

                IsSearching = (State == XUberState.SearchingOrigin || State == XUberState.SearchingDestination);
            });

            PriceOptionSelected = PriceOptions.First();

            State = _stateMachine.State;

            _stateMachine.ActivateAsync();
        }

        private void Initialize()
        {
            CleanPolylineCommand.Execute(null);
            DestinationLocation = string.Empty;
        }

        private async Task GetActualUserLocation()
        {
            try
            {
                await Task.Yield();
                var request = new GeolocationRequest(GeolocationAccuracy.High,TimeSpan.FromSeconds(5000));
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    OriginCoordinates = location;
                    CenterMapCommand.Execute(location);
                    GetLocationNameCommand.Execute(new Position(location.Latitude, location.Longitude));
                }
            }
            catch (Exception)
            {
                await UserDialogs.Instance.AlertAsync("Error", "Unable to get actual location", "Ok");
            }
        }

        private async Task GetLocationName(Position position)
        {
            try
            {
                var placemarks = await Geocoding.GetPlacemarksAsync(position.Latitude, position.Longitude);
                PickupLocation = placemarks?.FirstOrDefault()?.FeatureName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async Task GetPlacesByName(string placeText)
        {
            var places = await _googleMapsApi.GetPlaces(placeText);
            var placeResult = places.AutoCompletePlaces;
            if (placeResult != null && placeResult.Count > 0)
            {
                Places = new ObservableCollection<GooglePlaceAutoCompletePrediction>(placeResult);
            }
        }

        private async Task GetPlacesDetail(GooglePlaceAutoCompletePrediction placeA)
        {
          
            var place = await _googleMapsApi.GetPlaceDetails(placeA.PlaceId);
            if (place != null)
            {
                DestinationCoordinates = new Location(place.Latitude, place.Longitude);
                if (await LoadRoute())
                {
                    RecentPlaces.Add(placeA);
                }
            }
        }

       private async Task<bool> LoadRoute()
        {
            var retVal = false;

            var googleDirection = await _googleMapsApi.GetDirections($"{OriginCoordinates.Latitude}", $"{OriginCoordinates.Longitude}", $"{DestinationCoordinates.Latitude}", $"{DestinationCoordinates.Longitude}");
            if (googleDirection.Routes != null && googleDirection.Routes.Count > 0)
            {
                var positions = (Enumerable.ToList(PolylineHelper.Decode(googleDirection.Routes.First().OverviewPolyline.Points)));
                DrawRouteCommand.Execute(positions);
                FireTriggerCommand.Execute(XUberTrigger.ChooseRide);
                retVal = true;
            }
            else
            {
                FireTriggerCommand.Execute(XUberTrigger.Cancel);
                await UserDialogs.Instance.AlertAsync(":(", "No route found", "Ok");
            }

            return retVal;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
