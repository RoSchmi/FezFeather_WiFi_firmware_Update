// FezFeather_WiFi_firmware_Update
// Tool to update firmware of Winc1500 WiFi chip on GHI S
// Copyright RoSchmi 2021 License Apache v2.0

// For more details see:
// https://forums.ghielectronics.com/t/problems-to-update-winc1500-firmware-on-fez-feather/23716


// Cave: For TinyCLR v2.1.0 preview 4 The length of the WiFi Access point to connect to may have only 3 characters length (e.g. xyz)   !!!!
// This is going to be fixed in future versions

// Links:

// -https://ww1.microchip.com/downloads/en/devicedoc/ota firmwareupdate to 19.5.4 v1.1.pdf
// -https://docs.ghielectronics.com/software/tinyclr/tutorials/wifi.html
// -https://ww1.microchip.com/downloads/en/DeviceDoc/ATWINC1500_FW_19_7_3_02NOV2020.zip
// -https://www.rejetto.com


using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Net;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.Network;
using GHIElectronics.TinyCLR.Native;
using System.Net.Sockets;

namespace FezFeather_WiFi_Firmware_Update
{
    class Program
    {
        // Set your WiFi Credentials here or store them in the Resources
        static string wiFiSSID_1 = ResourcesSecret.GetString(ResourcesSecret.StringResources.SSID_2);
        //static string wiFiSSID_1 = "xyz";

        static string wiFiKey_1 = ResourcesSecret.GetString(ResourcesSecret.StringResources.Key_2);
        //static string wiFiKey_1 = "MySecretWiFiKey";

        #region Region Fields and Declarations

        private static GpioPin AppButton;

        private static bool linkReady = false;

        private static NetworkController networkController;

        static NetworkIPProperties ipProperties;
        static byte[] address = new byte[4] { 0,0,0,0};

        public static GpioPin LED = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PE11);


        #endregion
        static void Main()
        {

            LED.SetDriveMode(GpioPinDriveMode.Output);
            AppButton = GpioController.GetDefault().OpenPin(SC20260.GpioPin.PB7);
            AppButton.SetDriveMode(GpioPinDriveMode.InputPullUp);
            AppButton.ValueChanged += AppButton_ValueChanged;

            // Print System.Clock state
            Debug.WriteLine(Power.GetSystemClock() == SystemClock.Low ? "Using low cpu-frequency" : "Using high cpu-frequency");

            SetupWiFi7Click_SC20100_MicroBus1();

            // Print the version of the installed WiFi firmware:
            // The Version installed when I bought the board was: Winc1500 Firmware Version: 19.6.1.16761
            Debug.WriteLine("Actual Winc1500 Firmware Version: " + GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0.Winc15x0Interface.GetFirmwareVersion());

            Debug.WriteLine("Supported Firmware Versions are: ");
            for (int i = 0; i < GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0.Winc15x0Interface.FirmwareSupports.Length; i++)
            {
                System.Diagnostics.Debug.WriteLine("Supported firmware version #" +
                    (i + 1).ToString() + ": " + GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0.Winc15x0Interface.FirmwareSupports[i].ToString());
            }

            // Signals start of program (for tests)
            for (int i = 0; i < 6; i++)
            {
                LED.Write(GpioPinValue.High);
                Thread.Sleep(600);

                LED.Write(GpioPinValue.Low);
                Thread.Sleep(600);
            }

            Debug.WriteLine("New Ip-Address is: " + ipProperties.Address);
            Debug.WriteLine("New Gateway is: " + ipProperties.GatewayAddress);
            Debug.WriteLine("New DNS-Server is: " + ipProperties.DnsAddresses[0]);




            // Used to verify that fileserver can be reached
            
            /*
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
               var ip = IPAddress.Parse("192.168.1.24");
               s.Connect(new IPEndPoint(ip, 8000));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            */

            // Used to verify that a TestFile can be downloaded
            /*
            
            var url = "http://192.168.1.24:8000/TestFile.bin";

            int read = 0, total = 0;
            byte[] result = new byte[512];

            try
            {
                using (var req = HttpWebRequest.Create(url) as HttpWebRequest)
                {
                    req.KeepAlive = false;
                    req.ReadWriteTimeout = 2000;

                    using (var res = req.GetResponse() as HttpWebResponse)
                    {
                        using (var stream = res.GetResponseStream())
                        {
                            do
                            {
                                read = stream.Read(result, 0, result.Length);
                                total += read;

                                Debug.WriteLine("read : " + read);
                                Debug.WriteLine("total : " + total);

                                String page = "";

                                page = new String(System.Text.Encoding.UTF8.GetChars
                                    (result, 0, read));

                                Debug.WriteLine("Response : " + page);
                            }

                            while (read != 0);
                        }
                    }
                }
            }
            catch
            {
            }
            */


            Debug.WriteLine("\r\n\r\n WiFi-Module Firmware-Update: Press and release App-Button to start Udate\r\n");
     
            Thread.Sleep(Timeout.Infinite);

        }

        private static void AppButton_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.RisingEdge)
            {
                Debug.WriteLine("Started to update WiFi-Firmware");

                //https://microchipdeveloper.com/wf:atwinc1500-app-example-firmware-update

                
                //Download and install firmware from an OTA download (web) server:
                //   Must upload firmware file to root folder in server
                //   (e.g.  http://192.168.0.137/m2m_ota_3a0.bin).


                string theUrl = "http://192.168.1.24:8000/m2m_ota_3a0.bin";

                bool success = false;
                
                success = GHIElectronics.TinyCLR.Drivers.Microchip.Winc15x0.Winc15x0Interface.FirmwareUpdate(theUrl, new TimeSpan(0, 3, 0));  // 3 minutes
                if (success)
                {
                    Debug.WriteLine("Success");
                    for (int i = 0; i < 10; i++)
                    {
                        LED.Write(GpioPinValue.High);
                        Thread.Sleep(600);

                        LED.Write(GpioPinValue.Low);
                        Thread.Sleep(600);
                    }         
                }
                else
                {
                    Debug.WriteLine("Failed to update firmware");
                    for (int i = 0; i < 50; i++)
                    {
                        LED.Write(GpioPinValue.High);
                        Thread.Sleep(200);

                        LED.Write(GpioPinValue.Low);
                        Thread.Sleep(200);
                    }      
                }
            }
        }

        #region event NetworkController_NetworkLinkConnectedChanged
        private static void NetworkController_NetworkLinkConnectedChanged
            (NetworkController sender, NetworkLinkConnectedChangedEventArgs e)
        {
            // Raise event connect/disconnect
        }
        #endregion

        #region event NetworkController_NetworkAddressChanged
        private static void NetworkController_NetworkAddressChanged
            (NetworkController sender, NetworkAddressChangedEventArgs e)
        {
            //NetworkIPProperties ipProperties = sender.GetIPProperties();
            //var address = ipProperties.Address.GetAddressBytes();
            ipProperties = sender.GetIPProperties();
            address = ipProperties.Address.GetAddressBytes();

            linkReady = address[0] != 0;
        }
        #endregion

        #region private method SetupWiFi7Click_SC20100_MicroBus1
        static void SetupWiFi7Click_SC20100_MicroBus1()
        {
            var enablePin = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PA8);

            enablePin.SetDriveMode(GpioPinDriveMode.Output);
            enablePin.Write(GpioPinValue.High);

            SpiNetworkCommunicationInterfaceSettings networkCommunicationInterfaceSettings =
                new SpiNetworkCommunicationInterfaceSettings();


            var cs = GHIElectronics.TinyCLR.Devices.Gpio.GpioController.GetDefault().
                OpenPin(GHIElectronics.TinyCLR.Pins.SC20260.GpioPin.PD15);


            var settings = new GHIElectronics.TinyCLR.Devices.Spi.SpiConnectionSettings()
            {

                ChipSelectLine = cs,
                ClockFrequency = 4000000,
                Mode = GHIElectronics.TinyCLR.Devices.Spi.SpiMode.Mode0,
                ChipSelectType = GHIElectronics.TinyCLR.Devices.Spi.SpiChipSelectType.Gpio,
                ChipSelectHoldTime = TimeSpan.FromTicks(10),
                ChipSelectSetupTime = TimeSpan.FromTicks(10)
            };

            networkCommunicationInterfaceSettings.SpiApiName =
                GHIElectronics.TinyCLR.Pins.SC20100.SpiBus.Spi3;

            networkCommunicationInterfaceSettings.GpioApiName =
                GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.Id;

            networkCommunicationInterfaceSettings.SpiSettings = settings;
            networkCommunicationInterfaceSettings.InterruptPin = GHIElectronics.TinyCLR.Devices.Gpio.GpioController.GetDefault().
                OpenPin(GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.PB12);
            networkCommunicationInterfaceSettings.InterruptEdge = GpioPinEdge.FallingEdge;
            networkCommunicationInterfaceSettings.InterruptDriveMode = GpioPinDriveMode.InputPullUp;
            networkCommunicationInterfaceSettings.ResetPin = GHIElectronics.TinyCLR.Devices.Gpio.GpioController.GetDefault().
                OpenPin(GHIElectronics.TinyCLR.Pins.SC20100.GpioPin.PB13);
            networkCommunicationInterfaceSettings.ResetActiveState = GpioPinValue.Low;

            networkController = NetworkController.FromName
                ("GHIElectronics.TinyCLR.NativeApis.ATWINC15xx.NetworkController");

            WiFiNetworkInterfaceSettings networkInterfaceSetting = new WiFiNetworkInterfaceSettings()
            {
                Ssid = wiFiSSID_1,
                Password = wiFiKey_1,
            };

            networkInterfaceSetting.Address = new IPAddress(new byte[] { 192, 168, 1, 122 });
            networkInterfaceSetting.SubnetMask = new IPAddress(new byte[] { 255, 255, 255, 0 });
            networkInterfaceSetting.GatewayAddress = new IPAddress(new byte[] { 192, 168, 1, 1 });

           // networkInterfaceSetting.DnsAddresses = new IPAddress[] { new IPAddress(new byte[]
           // { 75, 75, 75, 75 }), new IPAddress(new byte[] { 75, 75, 75, 76 }) };

            //networkInterfaceSetting.MacAddress = new byte[] { 0x00, 0x4, 0x00, 0x00, 0x00, 0x00 };
            networkInterfaceSetting.MacAddress = new byte[] { 0x4A, 0x28, 0x05, 0x2A, 0xA4, 0x0F };

            networkInterfaceSetting.DhcpEnable = true;
            
            networkInterfaceSetting.TlsEntropy = new byte[] { 1, 2, 3, 4 };

            networkController.SetInterfaceSettings(networkInterfaceSetting);
            networkController.SetCommunicationInterfaceSettings
                (networkCommunicationInterfaceSettings);

            networkController.SetAsDefaultController();

            networkController.NetworkAddressChanged += NetworkController_NetworkAddressChanged;
            networkController.NetworkLinkConnectedChanged +=
                NetworkController_NetworkLinkConnectedChanged;


            networkController.Enable();


            while (linkReady == false) ;

            // Network is ready to used
        }
        #endregion
    }
    
}
