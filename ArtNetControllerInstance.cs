using ArtNetSharp;
using ArtNetSharp.Communication;
using DMXLIB;

namespace org.dmxc.lumos.Kernel.DMX
{
    public class ArtNetControllerInstance : ControllerInstance, IDMXInterfaceSynchronizer {
        public override ushort OEMProductCode => Constants.OEM_CODE;
        public override ushort ESTAManufacturerCode => Constants.ESTA_MANUFACTURER_CODE;
        private bool enableDmxOutput;
        public override bool EnableDmxOutput => enableDmxOutput;
        public ArtNetControllerInstance():base(ArtNet.Instance)
        {
            Name = "DMXControl 3";
            ShortName = "DMXControl 3";
        }

        void IDMXInterfaceSynchronizer.BeforeInterfacesSend()
        {
            this.enableDmxOutput = false;
        }

        void IDMXInterfaceSynchronizer.AfterInterfacesSend()
        {
            this.enableDmxOutput = true;
        }

        new public ILog Logger
        {
            get;
            set;
        }
    }
}
