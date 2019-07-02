using System;
using Android.App;
using Android.Runtime;
using Plugin.CurrentActivity;

namespace UberClone.Droid
{
#if DEBUG
    [Application(Debuggable = true)]
#else
	[Application(Debuggable = false)]
#endif
	[MetaData("com.google.android.maps.v2.API_KEY",
			  Value = Constants.GoogleMapsApiKey)]
	public class MainApplication : Application
	{
		public MainApplication(IntPtr handle, JniHandleOwnership transer)
		  : base(handle, transer)
		{
		}

		public override void OnCreate()
		{
			base.OnCreate();
			CrossCurrentActivity.Current.Init(this);
		}
	}
}
