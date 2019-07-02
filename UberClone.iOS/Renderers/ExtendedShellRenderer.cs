using System;
using CoreAnimation;
using CoreGraphics;
using UberClone.iOS.Renderers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Shell), typeof(ExtendedShellRenderer))]

namespace UberClone.iOS.Renderers
{
    public class ExtendedShellRenderer : ShellRenderer
    {
        private UIImageView _flyoutBackground = null;

        protected override void OnElementSet(Shell element)
        {
            base.OnElementSet(element);
        }

        protected override IShellFlyoutContentRenderer CreateShellFlyoutContentRenderer()
        {

            var flyout = base.CreateShellFlyoutContentRenderer();
            var tv = (UITableView)flyout.ViewController.View.Subviews[0];
            tv.ScrollEnabled = false;

            var tvs = (ShellTableViewSource)tv.Source;
            tvs.Groups.RemoveAt(1); 
            return flyout;
        }
    }
}