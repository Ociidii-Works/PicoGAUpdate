        public static bool IsOutOfDate(float newVersion)
        {
            Console.WriteLine("Determining driver version...");
            //ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver");
            ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver where deviceclass = 'DISPLAY'");
            ManagementObjectCollection objCollection = objSearcher.Get();
            foreach (var o in objCollection)
            {
                ManagementObject obj = (ManagementObject)o;
                if ((string)obj["Manufacturer"] != "NVIDIA")
                {
                }
                else
                {
                    string device = obj["DeviceName"].ToString();
                    if ((device.Contains("GeForce") || device.Contains("TITAN") || device.Contains("Quadro") || device.Contains("Tesla")))
                    {
                        // Rebuild version according to the nvidia format
                        string[] version = obj["DriverVersion"].ToString().Split('.');
                        {
                            string nvidiaVersion = ((version.GetValue(2) + version.GetValue(3)?.ToString()).Substring(1)).Insert(3, ".");
                            Console.WriteLine("Current Driver Version: " + nvidiaVersion);
                            float currVer = StringToFloat(nvidiaVersion);
                            if (currVer < newVersion)
                            {
                                Console.WriteLine("A new driver version is available! ({0} => {1})", currVer, newVersion);
                                return true;
                            }
                        }

                        Console.WriteLine("Your driver is up-to-date! Well done!");
                    }
                }
            }
            return false;
        }
