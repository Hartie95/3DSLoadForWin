using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace GatewayMobile
{
    /// <summary>
    /// Only the UI Management will be done inside of this class, anything else will be done in the Manager and Server class
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Manager manager;
        public MainPage()
        {
            this.InitializeComponent();
            this.manager = new Manager(Debug, portBox, ip, typeSelection, qrCode, urlBlock, ssButton, qrButton,localFolder);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

       
    }
}
