using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

using Windows.Storage;

/* TODO:
 * cleanup code:
 * better selection in swich case
 * 
 * functions:
 * folder picker windows phone
 * load gateway.dat from server.
 * selection of .dat type(encrypted decrypted)
 * add IPv6 support?
 * close and hibernate event management
 * own code.bin
 * support for bigger files like videos
 * create Settings pannel(normaly hidden)
*/

namespace GatewayMobile
{
    class Manager
    {
        private Server server;
        private String[] ips;
        private int port;
        private DispatcherTimer refreshtimer;

        private TextBlock debug;
        private TextBox fileNameBox;
        private ComboBox ipSelection;
        private ComboBox typeSelection;
        private Image qrCode;
        private TextBlock urlBlock;
        private Button startStopButton;
        private Button generateQRButton;
        private Button showSettingsButton;

        private Grid setingsPannel;
        private TextBox portInput;
        private TextBlock folderPathBlock;
        private Button setFolderButton;
        private Button saveSettingsButton;
        private Button closeSettingsButton;

        private StorageFolder httpFolder;

        public Manager( TextBlock debug,
                        TextBox fileNameBox, 
                        ComboBox ipSelection,
                        ComboBox typeSelection,
                        Image qrCode,
                        TextBlock urlBlock,
                        Button startStopButton,
                        Button generateQRButton,
                        Button showSettingsButton,
                        Grid setingsPannel,
                        TextBox portInput,
                        TextBlock folderPathBlock,
                        Button setFolderButton,
                        Button saveSettingsButton,
                        Button closeSettingsButton)
        {
            this.debug = debug;
            this.ipSelection = ipSelection;
            this.typeSelection = typeSelection;
            this.qrCode = qrCode;
            this.urlBlock = urlBlock;
            this.startStopButton = startStopButton;
            this.generateQRButton = generateQRButton;
            this.setFolderButton = setFolderButton;
            this.setingsPannel = setingsPannel;
            this.fileNameBox = fileNameBox;
            this.showSettingsButton = showSettingsButton;

            this.setingsPannel = setingsPannel;
            this.portInput = portInput;
            this.folderPathBlock = folderPathBlock;
            this.setFolderButton = setFolderButton;
            this.saveSettingsButton = saveSettingsButton;
            this.closeSettingsButton = closeSettingsButton;

            //Init and restore latest used Settings
            Settings.initSettings();

            this.port = Int32.Parse(Settings.getValue("port"));
            this.server = new Server(port);
            this.portInput.Text = port.ToString();

            this.getFolderFromConfig();

            //Init Events
            this.startStopButton.Click += this.startEvent;
            this.generateQRButton.Click += this.genQREvent;
            this.portInput.KeyUp += this.portInputKeyUp;
            this.typeSelection.SelectionChanged += this.fileSelectionChanged;
            this.showSettingsButton.Click += this.showSettingsEvent;
            this.closeSettingsButton.Click += this.closeSettingsEvent;
            this.setFolderButton.Click+= setFolderEvent;

            //set timer refresh the ip list every 5 sec
            this.updateIPs();
            TimeSpan ts=new TimeSpan(0,0,5);
            this.refreshtimer=new DispatcherTimer();
            this.refreshtimer.Tick += ipRefreshEvent;
            this.refreshtimer.Interval = ts;
            this.refreshtimer.Start();

        }
        private void start()
        {
            if (ips == null)
            {
                debug.Text = "Wifi is not connected, please connect and try again!";
            }
            else
            {
                server.Start();
                generateQRCode();
                startStopButton.IsEnabled = false;
                startStopButton.Click -= startEvent;
                startStopButton.Content = "Stop";
                startStopButton.Click += stopEvent;
                startStopButton.IsEnabled = true;

                generateQRButton.IsEnabled = true;

                debug.Text = "Running";
            }
        }
        private void stop()
        {
            server.Stop();
            startStopButton.IsEnabled = false;
            startStopButton.Click -= stopEvent;
            startStopButton.Content = "Start";
            startStopButton.Click += startEvent;
            startStopButton.IsEnabled = true;
            debug.Text="Stopped";

            generateQRButton.IsEnabled = false;
            this.qrCode.Source = null;
            this.urlBlock.Text = "";
        }

        private void updateIPs()
        {
            ips = Server.FindIPAddress();  
            object selected = this.ipSelection.SelectedItem;
            this.ipSelection.Items.Clear(); 
            this.ipSelection.SelectedIndex = -1;
           
            if (ips.Length < 1)
            {
                if(server.IsActive)
                    stop();
                debug.Text = "Wifi is not connected, please connect and try again!";
                startStopButton.IsEnabled = false;   
            }
            else
            {
                int ipNumber = ips.Length;
                for (int i = 0; i < ipNumber; i++)
                {
                    this.ipSelection.Items.Add(ips[i]);
                }

                int position=this.ipSelection.Items.IndexOf(selected);
                if (position < 0)
                    position = 0;
                this.ipSelection.SelectedIndex = position;
                startStopButton.IsEnabled = true;
            } 
        }


        private void generateQRCode()
        {
            String Target = ((ComboBoxItem)typeSelection.SelectedValue).Content.ToString();
            if (Target != "Own")
            {
                switch (Target)
                {
                    case "Gateway":
                        Target = "gw";
                        break;
                    case "Decrypt9":
                        Target = "gw";
                        break;
                    case "RegionThree":
                        Target = "multi?RegionThree.dat";
                        break;
                    case "LoadCode":
                        Target = "multi?LoadCode.dat&code.bin";
                        break;
                    case "LoadROP":
                        Target = "multi?LoadROP.dat";
                        break;
                    case "MemoryDump":
                        Target = "multi?MemoryDump.dat";
                        break;
                    case "VCInject":
                        Target = "multi?VCInject.dat";
                        break;
                }
            }
            else
            {
                Target = "multi?";
                Target += this.fileNameBox.Text;
            }
            string host=(string)ipSelection.SelectedItem;
            int ipv=4;
            if (((String)ipSelection.SelectedItem).Contains(":"))
            {
                ipv = 6;
                host = "["+(string)ipSelection.SelectedItem+"]";
            }


            String url = String.Format("http://{0}:{1}/{2}", host, port.ToString(), Target);
            urlBlock.Text = url;
            qrCode.Source = QRCode.GenerateQR(250, 250, url);
        }

        private async void getFolderFromConfig()
        {
            Windows.Storage.AccessCache.StorageItemAccessList savedFolders = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            String Token = Settings.getValue("folderToken");
            StorageFolder folder=null;
            try
            {
                folder = await savedFolders.GetFolderAsync(Token);
            }
            catch (Exception e)
            {
                folder = null;
            }
            if (folder != null)
            {
                this.httpFolder = folder;
                this.folderPathBlock.Text = folder.Path;
                this.server.setFolder(folder);
            }
        }

        private void setFolder(StorageFolder folder)
        {
            if (folder != null)
            {
                Windows.Storage.AccessCache.StorageItemAccessList savedFolders = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
                String Token = savedFolders.Add(folder, "folderToken");
                Settings.setValue("folderToken", Token);
                this.httpFolder = folder;
                this.folderPathBlock.Text = folder.Path;
                this.server.setFolder(folder);
            }
        }

        private async void setFolder()
        {
            Windows.Storage.Pickers.FolderPicker folderPicker=new Windows.Storage.Pickers.FolderPicker();
            folderPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            folderPicker.FileTypeFilter.Add(".html");
            folderPicker.FileTypeFilter.Add(".dat");
            folderPicker.FileTypeFilter.Add(".bin");


#if WINDOWS_PHONE_APP
            //folderPicker.PickFolderAndContinue();
#else
            StorageFolder folder = null;
            folder = await folderPicker.PickSingleFolderAsync();
            this.setFolder(folder);
#endif
        }

        private void fileSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            String selection = ((ComboBoxItem)cb.SelectedValue).Content.ToString();
            if (selection == "Own")
            {
                this.fileNameBox.IsEnabled = true;
                //Todo show name, parameters? and encrypted? parameter
            }
            else
            {
                this.fileNameBox.IsEnabled = false;
            }
        }

        private void showSettings()
        {
            this.setingsPannel.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void closeSettings()
        {
            this.setingsPannel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        //Event Classes
        private void showSettingsEvent(object sender, RoutedEventArgs e)
        {
            showSettings();
        }

        private void closeSettingsEvent(object sender, RoutedEventArgs e)
        {
            closeSettings();
        }

        private void startEvent(object sender, RoutedEventArgs e)
        {
            start();
        }

        private void stopEvent(object sender, RoutedEventArgs e)
        {
            stop();
        }

        private void genQREvent(object sender, RoutedEventArgs e)
        {
            generateQRCode();
        } 

        private void setFolderEvent(object sender, RoutedEventArgs e)
        {
            setFolder();
        }

        private void portInputKeyUp(object sender, KeyRoutedEventArgs e)
        {
            this.port = server.changePort(Int32.Parse(portInput.Text));
            Settings.setValue("port", this.port.ToString());
            this.portInput.Text = this.port.ToString();
        }

        private void ipRefreshEvent(object sender, object e)
        {
            this.refreshtimer.Stop();
            this.updateIPs();
            this.refreshtimer.Start();
        }
       
    }
}
