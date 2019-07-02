using System;
using UberClone.Services;
using UberClone.Views;
using Xamarin.Forms;

namespace UberClone
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            GoogleMapsApiService.Initialize(Constants.GoogleMapsApiKey);
            MainPage = new CustomMasterDetailPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
