﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Audyssey;
using Audyssey.MultEQApp;
using Audyssey.MultEQAvr;
using Audyssey.MultEQAvrAdapter;
using Audyssey.MultEQTcp;

namespace Ratbuddyssey
{
    /// <summary>
    /// Interaction logic for RatbuddysseyHome.xaml
    /// </summary>
    public partial class RatbuddysseyHome : Page
    {
        private AudysseyMultEQReferenceCurveFilter audysseyMultEQReferenceCurveFilter = new AudysseyMultEQReferenceCurveFilter();
        private AudysseyMultEQApp audysseyMultEQApp = null;
        private AudysseyMultEQAvr audysseyMultEQAvr = null;
        private AudysseyMultEQAvrAdapter audysseyMultEQAvrAdapter = null;
        private AudysseyMultEQTcpSniffer audysseyMultEQTcpSniffer = null;

        public RatbuddysseyHome()
        {
            InitializeComponent();
            channelsView.SelectionChanged += ChannelsView_SelectionChanged;
            plot.PreviewMouseWheel += Plot_PreviewMouseWheel;
        }

        ~RatbuddysseyHome()
        {
        }

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey files (*.ady)|*.ady";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                filename = dlg.FileName;
                currentFile.Content = filename;
                // Load document 
                String audysseyFile = File.ReadAllText(filename);
                // Parse JSON data
                audysseyMultEQApp = JsonConvert.DeserializeObject<AudysseyMultEQApp>(audysseyFile, new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                });
                // Data Binding
                if (audysseyMultEQApp != null)
                {
                    // cleanup: do not leave dangling
                    if (audysseyMultEQAvr != null)
                    {
                        audysseyMultEQAvr = null;
                    }
                    if (audysseyMultEQAvrAdapter != null)
                    {
                        audysseyMultEQAvrAdapter = null;
                    }
                    if (audysseyMultEQTcpSniffer != null)
                    {
                        audysseyMultEQTcpSniffer = null;
                    }
                    // update checkboxes
                    if (connectReceiver.IsChecked)
                    {
                        connectReceiver.IsChecked = false;
                    }
                    if (connectSniffer.IsChecked)
                    {
                        connectSniffer.IsChecked = false;
                    }
                    this.DataContext = audysseyMultEQApp;
                }
            }
        }

        private void ReloadFile_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("This will reload the .ady file and discard all changes since last save", "Are you sure?", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                // Reload document 
                String audysseyFile = File.ReadAllText(filename);
                // Parse JSON data
                audysseyMultEQApp = JsonConvert.DeserializeObject<AudysseyMultEQApp>(audysseyFile, new JsonSerializerSettings
                {
                    FloatParseHandling = FloatParseHandling.Decimal
                });
                // Data Binding
                if (audysseyMultEQApp != null)
                {
                    this.DataContext = audysseyMultEQApp;
                }
            }
        }

        private void SaveFile_OnClick(object sender, RoutedEventArgs e)
        {
            string reSerialized = JsonConvert.SerializeObject(audysseyMultEQApp, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
#if DEBUG
            filename = System.IO.Path.ChangeExtension(filename, ".json");
#endif
            if ((reSerialized != null) && (!string.IsNullOrEmpty(filename)))
            {
                File.WriteAllText(filename, reSerialized);
            }
        }

        private void SaveFileAs_OnClick(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = filename;
            dlg.DefaultExt = ".ady";
            dlg.Filter = "Audyssey calibration (.ady)|*.ady";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                filename = dlg.FileName;
                string reSerialized = JsonConvert.SerializeObject(audysseyMultEQApp, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                if ((reSerialized != null) && (!string.IsNullOrEmpty(filename)))
                {
                    File.WriteAllText(filename, reSerialized);
                }
            }
        }

        private void ConnectReceiver_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectReceiver)
            {
                if (connectReceiver.IsChecked)
                {
                    if (audysseyMultEQAvr == null)
                    {
                        // create receiver instance
                        audysseyMultEQAvr = new AudysseyMultEQAvr(true);
                        // adapter to interface MultEQAvr properties as if they were MultEQApp properties 
                        audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                        // data Binding to adapter
                        this.DataContext = audysseyMultEQAvrAdapter;
                    }
                    else
                    {
                        // object exists but not sure if we connected ethernet
                        audysseyMultEQAvr.Connect();
                    }
                    // attach sniffer
                    if (connectSniffer.IsChecked)
                    {
                        if (audysseyMultEQTcpSniffer == null)
                        {
                            audysseyMultEQTcpSniffer = new AudysseyMultEQTcpSniffer(audysseyMultEQAvr);
                        }
                    }
                    // check if binding and propertychanged work
                    audysseyMultEQAvr.AudysseyToAvr(); //TODO
                }
                else
                {
                    this.DataContext = null;
                    audysseyMultEQAvrAdapter = null;
                    audysseyMultEQAvr = null;
                    // immediately clean up the object
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                // display connection details
                currentFile.Content = (audysseyMultEQTcpSniffer != null ? "Host: " + audysseyMultEQTcpSniffer.GetTcpHostAsString() : "") + (audysseyMultEQAvr != null ? " Client:" + audysseyMultEQAvr.GetTcpClientAsString() : "");
            }
        }

        private void ConnectSniffer_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender == connectSniffer)
            {
                if (connectSniffer.IsChecked)
                {
                    // can only attach sniffer to receiver if receiver object exists 
                    if (audysseyMultEQAvr == null)
                    {
                        // receiver instance
                        audysseyMultEQAvr = new AudysseyMultEQAvr(false);
                        // create adapter to interface MultEQAvr properties as if they were MultEQApp properties 
                        audysseyMultEQAvrAdapter = new AudysseyMultEQAvrAdapter(audysseyMultEQAvr);
                        // data Binding to adapter
                        this.DataContext = audysseyMultEQAvrAdapter;
                    }
                    // onyl create sniffer if it not already exists
                    if (audysseyMultEQTcpSniffer == null)
                    {
                        // create sniffer attached to receiver
                        audysseyMultEQTcpSniffer = new AudysseyMultEQTcpSniffer(audysseyMultEQAvr);
                    }
                }
                else
                {
                    if (audysseyMultEQTcpSniffer != null)
                    {
                        audysseyMultEQTcpSniffer = null;
                        // if not interested in receiver then close connection and delete objects
                        if (connectReceiver.IsChecked == false)
                        {
                            this.DataContext = null;
                            audysseyMultEQAvrAdapter = null;
                            audysseyMultEQAvr = null;
                        }
                        // immediately clean up the object
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
                // Display connection details
                currentFile.Content = (audysseyMultEQTcpSniffer != null ? "Host: " + audysseyMultEQTcpSniffer.GetTcpHostAsString() : "") + (audysseyMultEQAvr != null ? " Client:" + audysseyMultEQAvr.GetTcpClientAsString() : "");
            }
        }

        private void ExitProgram_OnClick(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.Close();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Shout out to AVS Forum, use at your own risk!");
        }
    }
}