using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace UberClone.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
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
