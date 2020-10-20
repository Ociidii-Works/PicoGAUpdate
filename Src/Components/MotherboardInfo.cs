using System;
using System.Management;

namespace PicoGAUpdate.Components
{
    // TODO: Generalize into DeviceInfo
    static public class MotherboardInfo
    {
        static public string Availability
        {
            get
            {
                try
                {
                    foreach (var o in motherboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return GetAvailability(int.Parse(queryObj["Availability"].ToString()));
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public bool HostingBoard
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        if (queryObj["HostingBoard"].ToString() == "True")
                            return true;
                        else
                            return false;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        static public string InstallDate
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return ConvertToDateTime(queryObj["InstallDate"]?.ToString());
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string Manufacturer
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["Manufacturer"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string Model
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["Model"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string PartNumber
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["PartNumber"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        static public string PNPDeviceID
        {
            get
            {
                try
                {
                    foreach (var o in motherboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["PNPDeviceID"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string PrimaryBusType
        {
            get
            {
                try
                {
                    foreach (var o in motherboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["PrimaryBusType"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string Product
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["Product"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public bool Removable
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        if (queryObj["Removable"].ToString() == "True")
                            return true;
                        else
                            return false;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        static public bool Replaceable
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        if (queryObj["Replaceable"].ToString() == "True")
                            return true;
                        else
                            return false;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        static public string RevisionNumber
        {
            get
            {
                try
                {
                    foreach (var o in motherboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["RevisionNumber"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string SecondaryBusType
        {
            get
            {
                try
                {
                    foreach (var o in motherboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["SecondaryBusType"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string SerialNumber
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["SerialNumber"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string Status
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var querObj = (ManagementObject)o;
                        return querObj["Status"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string SystemName
        {
            get
            {
                try
                {
                    foreach (var o in motherboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["SystemName"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        static public string Version
        {
            get
            {
                try
                {
                    foreach (var o in baseboardSearcher.Get())
                    {
                        var queryObj = (ManagementObject)o;
                        return queryObj["Version"]?.ToString();
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        private static ManagementObjectSearcher baseboardSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BaseBoard");
        private static ManagementObjectSearcher motherboardSearcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_MotherboardDevice");

        private static string ConvertToDateTime(string unconvertedTime)
        {
            if (string.IsNullOrEmpty(unconvertedTime))
            {
                return null;
            }
            string convertedTime;
            var year = int.Parse(unconvertedTime.Substring(0, 4));
            var month = int.Parse(unconvertedTime.Substring(4, 2));
            var date = int.Parse(unconvertedTime.Substring(6, 2));
            var hours = int.Parse(unconvertedTime.Substring(8, 2));
            var minutes = int.Parse(unconvertedTime.Substring(10, 2));
            var seconds = int.Parse(unconvertedTime.Substring(12, 2));
            var meridian = "AM";
            if (hours > 12)
            {
                hours -= 12;
                meridian = "PM";
            }
            convertedTime = date.ToString() + "/" + month.ToString() + "/" + year.ToString() + " " +
            hours.ToString() + ":" + minutes.ToString() + ":" + seconds.ToString() + " " + meridian;
            return convertedTime;
        }

        private static string GetAvailability(int availability)
        {
            switch (availability)
            {
                case 1: return "Other";
                case 2: return "Unknown";
                case 3: return "Running or Full Power";
                case 4: return "Warning";
                case 5: return "In Test";
                case 6: return "Not Applicable";
                case 7: return "Power Off";
                case 8: return "Off Line";
                case 9: return "Off Duty";
                case 10: return "Degraded";
                case 11: return "Not Installed";
                case 12: return "Install Error";
                case 13: return "Power Save - Unknown";
                case 14: return "Power Save - Low Power Mode";
                case 15: return "Power Save - Standby";
                case 16: return "Power Cycle";
                case 17: return "Power Save - Warning";
                default: return "Unknown";
            }
        }
    }
}
