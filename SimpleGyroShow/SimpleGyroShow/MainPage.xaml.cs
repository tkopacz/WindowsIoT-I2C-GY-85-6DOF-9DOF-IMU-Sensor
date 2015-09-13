using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SimpleGyroShow {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
        }


        I2cDevice i2cHMC5883, //Gyro - need to calibrate
                  i2cADXL345, //ADXL345 accelerometer
                  i2cITG3200; //Compass
        private async void Page_Loaded(object sender, RoutedEventArgs e) {
            try {
                string deviceSelector = I2cDevice.GetDeviceSelector();
                var i2cDeviceControllers = await DeviceInformation.FindAllAsync(deviceSelector);
                if (i2cDeviceControllers.Count == 0) {
                    return;
                }
                I2cConnectionSettings i2cSettings;
                i2cSettings = new I2cConnectionSettings(0x53); //Accel
                i2cADXL345 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
                i2cADXL345.Write(new byte[] { 0x31, 0x01 });
                i2cADXL345.Write(new byte[] { 0x2D, 0x08 });

                i2cSettings = new I2cConnectionSettings(0x1E); //Compass
                i2cHMC5883 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
                if (i2cHMC5883 == null) throw new IOException("HMC5883");
                i2cHMC5883.Write(new byte[] { 0x02, 0x00 /*select mode register*/});

                i2cSettings = new I2cConnectionSettings(0x68); //Gyro
                i2cITG3200 = await I2cDevice.FromIdAsync(i2cDeviceControllers[0].Id, i2cSettings);
                i2cITG3200.Write(new byte[] { 0x3E, 0x00 });
                i2cITG3200.Write(new byte[] { 0x15, 0x07 });
                i2cITG3200.Write(new byte[] { 0x16, 0x1E });
                i2cITG3200.Write(new byte[] { 0x17, 0x00 });
            } catch (Exception ex) {
                Debug.WriteLine($"Setup, {ex.ToString()}");
            }
            //Calibration: gyro todo:
            try {
                byte[] bufCmdHMC5883 = new byte[1] { 0x03 };
                byte[] bufCmdADXL345 = new byte[1] { 0x32 };
                byte[] bufCmdITG3200 = new byte[1] { 0x1B };
                byte[] buf1 = new byte[1];
                byte[] buf6 = new byte[6];
                byte[] buf8 = new byte[8];
                int HMC5883x = 0, HMC5883y = 0, HMC5883z = 0,
                    ADXL345x = 0, ADXL345y = 0, ADXL345z = 0,
                    ITG3200x = 0, ITG3200y = 0, ITG3200z = 0, ITG3200temp = 0;

                while (true) {
                    i2cHMC5883.Write(new byte[1] { 0x03 }); //Compass
                    await Task.Delay(1);
                    i2cHMC5883.Read(buf6);
                    HMC5883x = (int)buf6[0] << 8 | buf6[1];
                    HMC5883y = (int)buf6[2] << 8 | buf6[3];
                    HMC5883z = (int)buf6[4] << 8 | buf6[5];

                    i2cADXL345.Write(bufCmdADXL345); //Accelerometer
                    await Task.Delay(1);
                    i2cADXL345.Read(buf6);
                    ADXL345x = (int)buf6[0] << 8 | buf6[1];
                    ADXL345y = (int)buf6[2] << 8 | buf6[3];
                    ADXL345z = (int)buf6[4] << 8 | buf6[5];

                    i2cITG3200.Write(bufCmdITG3200); //Gyro
                    await Task.Delay(1);
                    i2cITG3200.Read(buf8);
                    ITG3200x = (int)buf8[0] << 8 | buf8[1];
                    ITG3200y = (int)buf8[2] << 8 | buf8[3];
                    ITG3200z = (int)buf8[4] << 8 | buf8[5];
                    ITG3200temp = (int)buf8[6] << 8 | buf8[7];
                    Debug.WriteLine($"Compass, HMC5883: {HMC5883x:X4} - {HMC5883y:X4} - {HMC5883z:X4}, Accel, ADXL345: {ADXL345x:X4} - {ADXL345y:X4} - {ADXL345z:X4}, Gyro, ITG3200: {ITG3200x:X4} - {ITG3200y:X4} - {ITG3200z:X4} - {ITG3200temp:X4}");
                }
            } catch (Exception ex) {
                Debug.WriteLine($"Read, {ex.ToString()}");
            }


        }
    }
}
