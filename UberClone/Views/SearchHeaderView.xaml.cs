using System.Runtime.CompilerServices;
using Xamarin.Forms;
using System;
using System.ComponentModel;

namespace UberClone.Views
{
    public partial class SearchHeaderView : Grid
    {
        public Thickness BackButtonPadding { get; set; } = new Thickness(0, 0, 0, 0);
        public SearchHeaderView()
        {
            InitializeComponent();

        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if(propertyName== IsVisibleProperty.PropertyName)
            {
                if (IsVisible)
                {
                    FocusDestination();
                    ic_Back.Margin = BackButtonPadding;
                }
                else
                {
                    destinationEntry.Unfocus();
                }   
            }
        }

        public void OnDestinationEntryChange(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName== IsFocusedProperty.PropertyName)
            {
                (this.Parent.Parent as MapPage).HandleSearchContentView(destinationEntry.IsFocused);
            }
        }

        public void FocusDestination()
        {
            destinationEntry.Focus();
        }
    }
}
