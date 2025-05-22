namespace ApWifi.App
{
    public class ApConfig
    {
        public string Ssid { get; set; } = "RaspberryPi5-WiFiSetup";
        public string Password { get; set; } = "raspberry";
        public string Interface { get; set; } = "wlan0";
        public int Channel { get; set; } = 7;
        public string Ip { get; set; } = "192.168.4.1";
        public string DhcpStart { get; set; } = "192.168.4.50";
        public string DhcpEnd { get; set; } = "192.168.4.150";
    }
}
