using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using System.Reflection;
using UberClone.ViewModels;

namespace UberClone.Views
{
    public partial class MapPage : ContentPage
    {
        #region Bindable properties
        public static readonly BindableProperty CenterMapCommandProperty =
            BindableProperty.Create(nameof(CenterMapCommand), typeof(ICommand), typeof(MapPage), null, BindingMode.TwoWay);

        public ICommand CenterMapCommand
        {
            get { return (ICommand)GetValue(CenterMapCommandProperty); }
            set { SetValue(CenterMapCommandProperty, value); }
        }

        public static readonly BindableProperty DrawRouteCommandProperty =
            BindableProperty.Create(nameof(DrawRouteCommand), typeof(ICommand), typeof(MapPage), null, BindingMode.TwoWay);

        public ICommand DrawRouteCommand
        {
            get { return (ICommand)GetValue(DrawRouteCommandProperty); }
            set { SetValue(DrawRouteCommandProperty, value); }
        }

        public static readonly BindableProperty UpdateCommandProperty =
          BindableProperty.Create(nameof(UpdateCommand), typeof(ICommand), typeof(MapPage), null, BindingMode.TwoWay);


        public ICommand UpdateCommand
        {
            get { return (ICommand)GetValue(UpdateCommandProperty); }
            set { SetValue(UpdateCommandProperty, value); }
        }

        public static readonly BindableProperty CleanPolylineCommandProperty =
          BindableProperty.Create(nameof(CleanPolylineCommand), typeof(ICommand), typeof(MapPage), null, BindingMode.TwoWay);


        public ICommand CleanPolylineCommand
        {
            get { return (ICommand)GetValue(CleanPolylineCommandProperty); }
            set { SetValue(CleanPolylineCommandProperty, value); }
        }


        public static readonly BindableProperty GetActualLocationCommandProperty =
            BindableProperty.Create(nameof(GetActualLocationCommand), typeof(ICommand), typeof(MapPage), null, BindingMode.TwoWay);

        public ICommand GetActualLocationCommand
        {
            get { return (ICommand)GetValue(GetActualLocationCommandProperty); }
            set { SetValue(GetActualLocationCommandProperty, value); }
        }
        #endregion

        public MapPage()
        {
            InitializeComponent();
            BindingContext = new MapPageViewModel();

            DrawRouteCommand = new Command<List<Xamarin.Forms.GoogleMaps.Position>>(DrawRoute);
            UpdateCommand = new Command<Xamarin.Forms.GoogleMaps.Position>(Update);
            CenterMapCommand = new Command<Location>(OnCenterMap);
            CleanPolylineCommand = new Command(CleanPolyline);
            map.UiSettings.ZoomControlsEnabled = false;
            AddMapStyle();
        }


        void AddMapStyle()
        {
            var assembly = typeof(App).GetTypeInfo().Assembly;
            var stream = assembly.GetManifestResourceStream($"UberClone.MapStyle.json");
            string styleFile;
            using (var reader = new System.IO.StreamReader(stream))
            {
                styleFile = reader.ReadToEnd();
            }

            map.MapStyle = MapStyle.FromJson(styleFile);
        }

        async void Update(Xamarin.Forms.GoogleMaps.Position position)
        {
            if (map.Pins.Count == 1 && map.Polylines != null && map.Polylines?.Count > 1)
                return;

            var cPin = map.Pins.FirstOrDefault();

            if (cPin != null)
            {
                cPin.Position = new Position(position.Latitude, position.Longitude);
                cPin.Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("ic_taxi.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "ic_taxi.png", WidthRequest = 25, HeightRequest = 25 });

                await map.MoveCamera(CameraUpdateFactory.NewPosition(new Position(position.Latitude, position.Longitude)));
                var previousPosition = map?.Polylines?.FirstOrDefault()?.Positions?.FirstOrDefault();
                map.Polylines?.FirstOrDefault()?.Positions?.Remove(previousPosition.Value);
            }
            else
            {
                //END TRIP
                map.Polylines?.FirstOrDefault()?.Positions?.Clear();
            }

        }

        void CleanPolyline()
        {
            map.Polylines.Clear();
        }

        void DrawRoute(List<Xamarin.Forms.GoogleMaps.Position> list)
        {
            map.Polylines.Clear();
            var polyline = new Xamarin.Forms.GoogleMaps.Polyline();
            polyline.StrokeColor = Color.Black;
            polyline.StrokeWidth = 3;

            foreach (var p in list)
            {
                polyline.Positions.Add(p);

            }
            map.Polylines.Add(polyline);
            map.MoveToRegion(MapSpan.FromCenterAndRadius(new Position(polyline.Positions[0].Latitude, polyline.Positions[0].Longitude), Xamarin.Forms.GoogleMaps.Distance.FromMiles(0.50f)));

            var pin = new Xamarin.Forms.GoogleMaps.Pin
            {
                Type = PinType.SearchResult,
                Position = new Position(polyline.Positions.First().Latitude, polyline.Positions.First().Longitude),
                Label = "Pin",
                Address = "Pin",
                Tag = "CirclePoint",
                Icon =(Device.RuntimePlatform == Device.Android)?BitmapDescriptorFactory.FromBundle("ic_circle_point.png") :  BitmapDescriptorFactory.FromView(new Image() { Source = "ic_circle_point.png", WidthRequest = 25, HeightRequest = 25 })

            };
            map.Pins.Add(pin);
        }


        void OnCenterMap(Location location)
        {
            map.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Position(location.Latitude, location.Longitude), Distance.FromMiles(1)));

            LoadNearCars(location);
        }

        void LoadNearCars(Location location)
        {
            map.Polylines.Clear();
            map.Pins.Clear();
            for (int i = 0; i < 7; i++)
            {
                var random = new Random();

                map.Pins.Add(new Xamarin.Forms.GoogleMaps.Pin
                {
                    Type = PinType.Place,
                    Position = new Position(location.Latitude + (random.NextDouble() * 0.008), location.Longitude + (random.NextDouble() * 0.008)),
                    Label = "Car",
                    Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("ic_car.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "ic_car.png", WidthRequest = 25, HeightRequest = 25 }),
                    Tag = string.Empty
                });
            }
        }

        //Desactivate pin tap
        void Map_PinClicked(object sender, PinClickedEventArgs e)
        {
            e.Handled = true;
        }

        public void HandleSearchContentView(bool show)
        {
           searchContentView.IsVisible = show;
        }

        public void Handle_Tapped(object sender, EventArgs e)
        {

            CustomMasterDetailPage.Current.IsPresented = true;
        }

        public void Handle_CameraIdled(object sender, CameraIdledEventArgs e)
        {
            chooseLocationButton?.Command.Execute(map.CameraPosition.Target);
        }

        public void OnDoneClicked(object sender, EventArgs e)
        {
            headerSearch.FocusDestination();
        }
        
    }
}
