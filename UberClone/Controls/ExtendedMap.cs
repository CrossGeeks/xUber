using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.GoogleMaps;
using Map = Xamarin.Forms.GoogleMaps.Map;

namespace UberClone.Controls
{
    public class ExtendedMap : Map, IDisposable
    {
        public static readonly BindableProperty CalculateCommandProperty =
            BindableProperty.Create(nameof(CalculateCommand), typeof(ICommand), typeof(ExtendedMap), null, BindingMode.TwoWay);

        public ICommand CalculateCommand
        {
            get { return (ICommand)GetValue(CalculateCommandProperty); }
            set { SetValue(CalculateCommandProperty, value); }
        }

        public static readonly BindableProperty UpdateCommandProperty =
          BindableProperty.Create(nameof(UpdateCommand), typeof(ICommand), typeof(ExtendedMap), null, BindingMode.TwoWay);

        public ICommand UpdateCommand
        {
            get { return (ICommand)GetValue(UpdateCommandProperty); }
            set { SetValue(UpdateCommandProperty, value); }
        }

        public static readonly BindableProperty GetActualLocationCommandProperty =
          BindableProperty.Create(nameof(GetActualLocationCommand), typeof(ICommand), typeof(ExtendedMap), null, BindingMode.TwoWay);

        public ICommand GetActualLocationCommand
        {
            get { return (ICommand)GetValue(GetActualLocationCommandProperty); }
            set { SetValue(GetActualLocationCommandProperty, value); }
        }

        public static readonly BindableProperty ActivateMapClickedProperty =
          BindableProperty.Create(nameof(ActivateMapClicked), typeof(bool), typeof(ExtendedMap), false, BindingMode.TwoWay);

        public static readonly BindableProperty CenterMapCommandProperty =
          BindableProperty.Create(nameof(CenterMapCommand), typeof(ICommand), typeof(ExtendedMap), null, BindingMode.TwoWay);

        public ICommand CenterMapCommand
        {
            get { return (ICommand)GetValue(CenterMapCommandProperty); }
            set { SetValue(CenterMapCommandProperty, value); }
        }

        public static readonly BindableProperty DrawRouteCommandProperty =
            BindableProperty.Create(nameof(DrawRouteCommand), typeof(ICommand), typeof(ExtendedMap), null, BindingMode.TwoWay);

        public ICommand DrawRouteCommand
        {
            get { return (ICommand)GetValue(DrawRouteCommandProperty); }
            set { SetValue(DrawRouteCommandProperty, value); }
        }

        public static readonly BindableProperty CleanPolylineCommandProperty =
          BindableProperty.Create(nameof(CleanPolylineCommand), typeof(ICommand), typeof(ExtendedMap), null, BindingMode.TwoWay);

        public ICommand CleanPolylineCommand
        {
            get { return (ICommand)GetValue(CleanPolylineCommandProperty); }
            set { SetValue(CleanPolylineCommandProperty, value); }
        }

        public static readonly BindableProperty CameraIdledCommandProperty =
         BindableProperty.Create(nameof(CameraIdledCommand), typeof(ICommand), typeof(ExtendedMap), null, BindingMode.TwoWay);

        public ICommand CameraIdledCommand
        {
            get { return (ICommand)GetValue(CameraIdledCommandProperty); }
            set { SetValue(CameraIdledCommandProperty, value); }
        }

        public bool ActivateMapClicked
        {
            get { return (bool)GetValue(ActivateMapClickedProperty); }
            set { SetValue(ActivateMapClickedProperty, value); }
        }

        public event EventHandler OnCalculate = delegate { };
        public event EventHandler OnLocationError = delegate { };

        public ExtendedMap()
        {
            AddMapStyle();
            PinClicked += ExtendedMap_PinClicked;
            CameraIdled += ExtendedMap_CameraIdled;
        }

        private void ExtendedMap_CameraIdled(object sender, CameraIdledEventArgs e)
        {
            CameraIdledCommand?.Execute(CameraPosition.Target);
        }

        private void ExtendedMap_PinClicked(object sender, PinClickedEventArgs e) => e.Handled = !ActivateMapClicked;

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            if (BindingContext != null)
            {
                CalculateCommand = new Command<List<Position>>(Calculate);
                UpdateCommand = new Command<Position>(Update);
                GetActualLocationCommand = new Command(async () => await GetActualLocation());
                DrawRouteCommand = new Command<List<Xamarin.Forms.GoogleMaps.Position>>(DrawRoute);
                UpdateCommand = new Command<Xamarin.Forms.GoogleMaps.Position>(Update);
                CenterMapCommand = new Command<Location>(OnCenterMap);
                CleanPolylineCommand = new Command(CleanPolyline);
                UiSettings.ZoomControlsEnabled = false;
            }
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

            MapStyle = MapStyle.FromJson(styleFile);
        }


        async void Update(Position position)
        {
            if (Pins.Count == 1 && Polylines != null && Polylines?.Count > 1)
                return;

            var cPin = Pins.FirstOrDefault();

            if (cPin != null)
            {
                cPin.Position = new Position(position.Latitude, position.Longitude);
                cPin.Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("ic_taxi.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "ic_taxi.png", WidthRequest = 25, HeightRequest = 25 });

                await MoveCamera(CameraUpdateFactory.NewPosition(new Position(position.Latitude, position.Longitude)));
                var previousPosition = Polylines?.FirstOrDefault()?.Positions?.FirstOrDefault();
                Polylines?.FirstOrDefault()?.Positions?.Remove(previousPosition.Value);
            }
            else
            {
                //END TRIP
                Polylines?.FirstOrDefault()?.Positions?.Clear();
            }
        }

        void Calculate(List<Position> list)
        {
            OnCalculate?.Invoke(this, default(EventArgs));
            Polylines.Clear();
            var polyline = new Xamarin.Forms.GoogleMaps.Polyline();
            foreach (var p in list)
            {
                polyline.Positions.Add(p);

            }
            Polylines.Add(polyline);
            MoveToRegion(MapSpan.FromCenterAndRadius(new Position(polyline.Positions[0].Latitude, polyline.Positions[0].Longitude), Xamarin.Forms.GoogleMaps.Distance.FromMiles(0.50f)));

            var pin = new Xamarin.Forms.GoogleMaps.Pin
            {
                Type = PinType.Place,
                Position = new Position(polyline.Positions.First().Latitude, polyline.Positions.First().Longitude),
                Label = "First",
                Address = "First",
                Tag = string.Empty,
                Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("ic_taxi.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "ic_taxi.png", WidthRequest = 25, HeightRequest = 25 })

            };
            Pins.Add(pin);
            var pin1 = new Xamarin.Forms.GoogleMaps.Pin
            {
                Type = PinType.Place,
                Position = new Position(polyline.Positions.Last().Latitude, polyline.Positions.Last().Longitude),
                Label = "Last",
                Address = "Last",
                Tag = string.Empty
            };
            Pins.Add(pin1);
        }

        async Task GetActualLocation()
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High);
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    MoveToRegion(MapSpan.FromCenterAndRadius(
                        new Position(location.Latitude, location.Longitude), Distance.FromMiles(0.3)));

                }
            }
            catch (Exception ex)
            {
                OnLocationError?.Invoke(this, default(EventArgs));
            }
        }


        void CleanPolyline() => Polylines.Clear();

        void DrawRoute(List<Position> list)
        {
            Polylines.Clear();
            var polyline = new Polyline();
            polyline.StrokeColor = Color.Black;
            polyline.StrokeWidth = 3;

            foreach (var p in list)
            {
                polyline.Positions.Add(p);

            }
            Polylines.Add(polyline);
            MoveToRegion(MapSpan.FromCenterAndRadius(new Position(polyline.Positions[0].Latitude, polyline.Positions[0].Longitude), Xamarin.Forms.GoogleMaps.Distance.FromMiles(0.50f)));

            var pin = new Pin
            {
                Type = PinType.SearchResult,
                Position = new Position(polyline.Positions.First().Latitude, polyline.Positions.First().Longitude),
                Label = "Pin",
                Address = "Pin",
                Tag = "CirclePoint",
                Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("ic_circle_point.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "ic_circle_point.png", WidthRequest = 25, HeightRequest = 25 })

            };
            Pins.Add(pin);
        }


        void OnCenterMap(Location location)
        {
            MoveToRegion(MapSpan.FromCenterAndRadius(
                new Position(location.Latitude, location.Longitude), Distance.FromMiles(2)));

            LoadNearCars(location);
        }

        void LoadNearCars(Location location)
        {
            Polylines.Clear();
            Pins.Clear();
            for (int i = 0; i < 7; i++)
            {
                var random = new Random();

                Pins.Add(new Pin
                {
                    Type = PinType.Place,
                    Position = new Position(location.Latitude + (random.NextDouble() * 0.008), location.Longitude + (random.NextDouble() * 0.008)),
                    Label = "Car",
                    Icon = (Device.RuntimePlatform == Device.Android) ? BitmapDescriptorFactory.FromBundle("ic_car.png") : BitmapDescriptorFactory.FromView(new Image() { Source = "ic_car.png", WidthRequest = 25, HeightRequest = 25 }),
                    Tag = string.Empty
                });
            }
        }

        public void Dispose()
        {
            PinClicked -= ExtendedMap_PinClicked;
            CameraIdled -= ExtendedMap_CameraIdled;
        }
    }
}