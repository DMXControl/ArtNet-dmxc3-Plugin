using ArtNetSharp;
using DMXLIB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace org.dmxc.lumos.Kernel.DMX
{
    public class ArtNetFactory : AbstractDMXInterfaceFactory
    {
        private int _instCnt = byte.MaxValue - 1; //Every An Artnet-Device can only have 255 Ports - one root on 0 (generated automaticly)
        internal static ArtNet ArtNet { get; } = ArtNet.Instance;
        private static ArtNetControllerInstance artNetControllerInstance;
        internal static ArtNetControllerInstance getArtNetControllerInstance()
        {
            if (artNetControllerInstance == null)
                artNetControllerInstance = new ArtNetControllerInstance();
            return artNetControllerInstance;
        }

        internal List<IDMXInterface> interfaces = new List<IDMXInterface>();
        internal List<int> notUsedPortIndices = new List<int>();

        public ArtNetFactory() : base()
        {
            _ = init();
        }
        private async Task init() // Do this as async Task to avoid Timeout
        {
            ArtNet.AddInstance(getArtNetControllerInstance());
            await Task.CompletedTask;
        }
        ~ArtNetFactory()
        {
            ((IDisposable)getArtNetControllerInstance()).Dispose();
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
                    yield return new DMXInterfaceMetadata(this.VendorID, "ArtNet 4", "ArtNet 4", this.VendorID, "Art-Net 4", String.Empty,
                        new List<DMXPortMetadata>
                            {
                                DMXPortMetadata.GetSimplexPort(0)
                            }.AsReadOnly(), true, true, null, false);
                }
            }
        }

        protected override IDMXInterface CreateInterfaceInternal(DMXInterfaceMetadata meta)
        {
            if (this._instCnt > 0)
            {
                this._instCnt--;
                int portIndex = interfaces.Count + 1;
                if (notUsedPortIndices.Count != 0)
                    portIndex = notUsedPortIndices.Min();

                ArtNetInterface iface = new ArtNetInterface((byte)portIndex, meta);
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
            return getArtNetControllerInstance();
        }

        protected override void OnLoggerChanged()
        {
            getArtNetControllerInstance().Logger = Logger;
        }
    }
}