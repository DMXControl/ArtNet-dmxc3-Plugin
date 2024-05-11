using ArtNetSharp;
using ArtNetSharp.Communication;
using DMXLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using ArtNetConstants = ArtNetSharp.Constants;

namespace org.dmxc.lumos.Kernel.DMX
{
    public class ArtNetFactory : AbstractDMXInterfaceFactory
    {
        private int _instCnt = byte.MaxValue - 1; //Every An Artnet-Device can only have 255 Ports - one root on 0
        internal static ArtNet ArtNet { get; } = ArtNet.Instance;
        internal static ArtNetControllerInstance ArtNetControllerInstance { get; } = new ArtNetControllerInstance();
        internal static PortConfig RootPortConfig { get; } = new PortConfig(0, new PortAddress(0), false, false);

        internal List<IDMXInterface> interfaces = new List<IDMXInterface>();
        internal List<int> notUsedPortIndices = new List<int>();

        public ArtNetFactory() : base()
        {
            ArtNetControllerInstance.AddPortConfig(RootPortConfig);
            ArtNet.AddInstance(ArtNetControllerInstance);
        }
        ~ArtNetFactory()
        {
            ((IDisposable)ArtNetControllerInstance).Dispose();
        }

        public override string VendorID
        {
            get { return "Artistic License"; }
        }

        public override IEnumerable<DMXInterfaceMetadata> Interfaces
        {
            get
            {
                if (_instCnt > 0)
                {
                    yield return new DMXInterfaceMetadata(this.VendorID, "ArtNet 4", "ArtNet 4", this.VendorID, "Art-Net", String.Empty,
                        new List<DMXPortMetadata>
                            {
                                DMXPortMetadata.GetDuplexPort(0)
                            }.AsReadOnly(), true, true, null, true);
                }
            }
        }

        protected override IDMXInterface CreateInterfaceInternal(DMXInterfaceMetadata meta)
        {
            if (this._instCnt > 0)
            {
                this._instCnt--;
                int portIndex = interfaces.Count;
                if (notUsedPortIndices.Count != 0)
                    portIndex = notUsedPortIndices.Min();

                ArtNetInterface iface = new ArtNetInterface(portIndex, meta);
                interfaces.Add(iface);
                return iface;
            }
            return null;
        }

        protected override void OnInterfaceDisposing(IDMXInterface sender)
        {
            if (!(sender is ArtNetInterface artNetInterface))
                return;

            this._instCnt++;
            interfaces.Remove(sender);
            notUsedPortIndices.Add(artNetInterface.PortIndex);
        }

        public override IDMXInterfaceSynchronizer GetInterfaceSynchronizer(IDMXInterface iface)
        {
            return ArtNetControllerInstance;
        }

        protected override void OnLoggerChanged()
        {
            ArtNetControllerInstance.Logger = Logger;
        }
    }
}