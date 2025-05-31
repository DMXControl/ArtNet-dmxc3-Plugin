using ArtNetSharp;
using ArtNetSharp.Communication;
using DMXLIB;
using System.Net;

namespace org.dmxc.lumos.Kernel.DMX
{
    public class ArtNetControllerInstance : ControllerInstance, IDMXInterfaceSynchronizer {
        public override ushort OEMProductCode => Constants.OEM_CODE;
        public override ushort ESTAManufacturerCode => Constants.ESTA_MANUFACTURER_CODE;
        private bool enableDmxOutput = true;
        public override bool EnableDmxOutput => enableDmxOutput;
        protected override string UrlProduct => @"https://dmxcontrol.de/de/";
        protected override string UrlUserGuid => @"https://wiki-de.dmxcontrol-projects.org/index.php?title=Inhalts%C3%BCbersicht_Hauptprogramm_DMXC3";
        protected override string UrlSupport => @"https://forum.dmxcontrol-projects.org/";
        public ArtNetControllerInstance():base(ArtNet.Instance)
        {
            string longName = "DMXControl 3";
            try
            {
                string hostname = Dns.GetHostName();
                if (hostname.Length > (64 - 15))
                {
                    hostname = hostname.Substring(0, (64 - 15));
                }
                longName += " (" + hostname + ")";
            }
            catch { }
            Name = longName;
            ShortName = "DMXControl 3";
        }

        void IDMXInterfaceSynchronizer.BeforeInterfacesSend()
        {
            //this.enableDmxOutput = false;
        }

        void IDMXInterfaceSynchronizer.AfterInterfacesSend()
        {
            //this.enableDmxOutput = true;
        }

        new public ILog Logger
        {
            get;
            set;
        }
    }
}
