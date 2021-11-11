using Xamarin.Forms;

namespace UberClone.Views
{
    public partial class CustomMasterDetailPage : MasterDetailPage
    {
        public static CustomMasterDetailPage Current { get; set; }

        public CustomMasterDetailPage()
        {
            InitializeComponent();
            Current = this;
        }
    }
}
