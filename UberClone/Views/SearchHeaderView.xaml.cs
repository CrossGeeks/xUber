using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace UberClone.Views
{
    public partial class SearchHeaderView : Grid
    {
        public Thickness BackButtonPadding { get; set; } = new Thickness(0, 0, 0, 0);
        public SearchHeaderView()=> InitializeComponent();

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
            }
        }

        public void FocusDestination() => destinationEntry.Focus();
    }
}
