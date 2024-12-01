using ArtNetSharp;
using ArtNetSharp.Communication;
using DMXLIB;
using DMXLIB.I18N;
using org.dmxc.wkdt.Light.ArtNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace org.dmxc.lumos.Kernel.DMX
{
    public class ArtNetInterface : AbstractDMXInterface, IDMXInterfacePortConfig
    {
        public static readonly string PARA_FORCE_BCAST = T._("Force broadcast");
        public static readonly string PARA_ADD_TARGET = T._("Additional send to IP (optional)");

        public static readonly string PARA_PORT_ADDRESS = T._("PortAddress");

        private static Dictionary<string, ushort> calculatePortAddresses()
        {
            Dictionary<string, ushort>  addresses=new Dictionary<string, ushort>();
            foreach (ushort u in Enumerable.Range(0, ushort.MaxValue / 2))
                addresses.Add(new PortAddress(u).ToString(), u);

            return addresses;
        }

        private bool IsOutput;
        private bool IsInput;

        private readonly byte[] _bufferRx = new byte[512];
        private bool _forceBroadcast = false;
        private IPAddress _additionalTarget = null;
        private ArtNetControllerInstance ArtNetControllerInstance => ArtNetFactory.ArtNetControllerInstance;
        private readonly PortConfig portConfig = null;

        public readonly int PortIndex;

        public ArtNetInterface(in byte portIndex, in DMXInterfaceMetadata metadata)
            : base(metadata)
        {
            PortIndex = portIndex;

            portConfig = new PortConfig(portIndex, new PortAddress(0), false, true) { PortNumber = portIndex, Type = EPortType.InputToArtNet };
            portConfig.Type = EPortType.DMX512;
            ArtNetControllerInstance.AddPortConfig(portConfig);
            ArtNetControllerInstance.DMXReceived += DMXReceived;
        }

        private void DMXReceived(object sender, PortAddress portAddress)
        {
            if (portAddress != portConfig.PortAddress)
                return;

            if (ArtNetControllerInstance.GetReceivedDMX(portAddress) is byte[] data)
                ProcessIncomingDmx(data);
        }

        public PortAddress PortAddress
        {
            get { return portConfig.PortAddress; }
            private set
            {
                if (portConfig.PortAddress == value) return;
                portConfig.PortAddress = value;
                UpdatePortDescription();
            }
        }

        protected override void OnOutputEnable(int port)
        {
            portConfig.Type |= EPortType.InputToArtNet;
            portConfig.Type &= ~EPortType.OutputFromArtNet;
            portConfig.GoodInput = GoodInput.None;
            portConfig.GoodOutput = new GoodOutput(isBeingOutputAsDMX:true);
            IsOutput = true;
        }

        protected override void OnOutputDisable(int port)
        {
            portConfig.Type &= ~EPortType.InputToArtNet;
            portConfig.GoodInput = GoodInput.None;
            portConfig.GoodOutput = new GoodOutput(isBeingOutputAsDMX:true);
            IsOutput = false;
        }
        protected override void OnInputEnable(int port)
        {
            portConfig.Type |= EPortType.OutputFromArtNet;
            portConfig.Type &= ~EPortType.InputToArtNet;
            portConfig.GoodInput = GoodInput.None;
            portConfig.GoodOutput = new GoodOutput(isBeingOutputAsDMX: false);
            IsInput = true;
        }
        protected override void OnInputDisable(int port)
        {
            portConfig.Type &= ~EPortType.OutputFromArtNet;
            portConfig.GoodInput = GoodInput.None;
            portConfig.GoodOutput = new GoodOutput(isBeingOutputAsDMX: false);
            IsInput = false;
        }

        protected override void OnEnable()
        {
            portConfig.GoodInput = GetOutputState(0) ? new GoodInput(dataReceived: true) : new GoodInput(inputDisabled: true);
            portConfig.GoodOutput = GetInputState(0) ? new GoodOutput(isBeingOutputAsDMX: true) : new GoodOutput(isBeingOutputAsDMX: false);
            if (IsInput)
            {
                OnInputEnable(0);
                OnOutputDisable(0);
            }
            if (IsOutput)
            {
                OnOutputEnable(0);
                OnInputDisable(0);
            }
        }

        protected override void OnDisable()
        {
            portConfig.Type = EPortType.DMX512;
            portConfig.GoodInput = GoodInput.None;
            portConfig.GoodOutput = GoodOutput.None;
        }

        public override EInterfaceOptions Options {
            get { return EInterfaceOptions.NONE; }
        }

        public override ESendMode SendMode {
            get { return ESendMode.DELTA_UNIVERSE; }
        }

        public override EReceiveMode ReceiveMode {
            get { return EReceiveMode.DELTA_UNIVERSE; }
        }

        protected override IEnumerable<DMXInterfaceParameter> ParametersInternal {
            get {
                yield return new DMXInterfaceParameter(PARA_PORT_ADDRESS, typeof(PortAddress), EDMXInterfaceParameterType.PERSISTENT)
                {
                    Description =
                    T._("The PortAddress in ArtNet is the Universe in DMXControl.") +
                    Environment.NewLine +
                    T._("Universe in ArtNet is NOT Universe in DMXControl, its Part of the PortAddress.") +
                    Environment.NewLine +
                    Environment.NewLine +
                    T._("The PortAddress is seperated in multiple Parts:") +
                    Environment.NewLine +
                    T._("ArtNet 1 to 2 (8Bit) -> Net (0x00), Subnet(0x0-0xf) and Universe(0x0-0xf).") +
                    Environment.NewLine +
                    T._("ArtNet 3 to 4 (15Bit) -> Net (0x00-0x7f), Subnet(0x0-0xf) and Universe(0x0-0xf).")
                };
                yield return new DMXInterfaceParameter(PARA_FORCE_BCAST, typeof(bool), EDMXInterfaceParameterType.PERSISTENT);
                yield return new DMXInterfaceParameter(PARA_ADD_TARGET, typeof(string), EDMXInterfaceParameterType.PERSISTENT);
            }
        }

        protected override object GetParameterInternal(string parameter) {
            if (Object.Equals(parameter, PARA_PORT_ADDRESS))
                return this.PortAddress.ToString();
            else if (Object.Equals(parameter, PARA_FORCE_BCAST))
                return this._forceBroadcast;
            else if (Object.Equals(parameter, PARA_ADD_TARGET)) {
                if (this._additionalTarget == null) {
                    return "";
                }
                return this._additionalTarget.ToString();
            }
            return null;
        }

        protected override bool SetParameterInternal(string parameter, object value)
        {
            if (Object.Equals(parameter, PARA_PORT_ADDRESS))
            {
                if (value is PortAddress pa)
                {
                    this.PortAddress = pa;
                    return true;
                }
            }
            else if (Object.Equals(parameter, PARA_FORCE_BCAST))
            {
                this._forceBroadcast = Convert.ToBoolean(value);
                return true;
            }
            else if (Object.Equals(parameter, PARA_ADD_TARGET))
            {
                String addr = Convert.ToString(value);
                if (string.IsNullOrEmpty(addr))
                {
                    portConfig.ClearAdditionalIPEndpoints();
                    this._additionalTarget = null;
                }
                else
                {
                    this._additionalTarget = IPAddress.Parse(addr);
                    portConfig.AddAdditionalIPEndpoints(this._additionalTarget);
                }
            }
            return false;
        }
        private void UpdatePortDescription()
        {
            SetPortDetailInfo(0, String.Format(T._("Net: {0} Subnet: {1} Universe: {2} [{3}]"), PortAddress.Net, PortAddress.Subnet, PortAddress.Universe, PortAddress));
        }

        protected override bool TestParameterInternal(string parameter, object value)
        {
            if (String.Equals(parameter, PARA_PORT_ADDRESS))
            {
                if (value is PortAddress pa)
                    return true;
                if (!(value is ushort _ushort))
                    throw new ArgumentException("Value not valid");
                new PortAddress(_ushort);

            }
            else if (String.Equals(parameter, PARA_ADD_TARGET))
            {
                if (!(value is string))
                    throw new ArgumentException("Value not valid");
                string addr = Convert.ToString(value);
                if (!string.IsNullOrEmpty(addr) && !IPAddress.TryParse(addr, out _))
                {
                    throw new ArgumentException("Not a valid IP address");
                }
            }
            return true;
        }
        protected override void PortCompleteInternal(int port)
        {
            portConfig.GoodInput =  new GoodInput(dataReceived:true);
        }

        protected override byte[] GetDMXInternal(int port, int address, int count) {
            byte[] b = new byte[count];
            lock (this._bufferRx) {
                Array.Copy(this._bufferRx, address, b, 0, count);
            }
            return b;
        }

        protected override byte GetDMXInternal(int port, int address) {
            lock (this._bufferRx) {
                return this._bufferRx[address];
            }
        }

        private void ProcessIncomingDmx(byte[] data)
        {
            lock (this._bufferRx) {
                for (int i = 0; i < data.Length; ++i) {
                    if (data[i] != this._bufferRx[i]) {
                        this._bufferRx[i] = data[i];
                        OnDMXInChanged(0, i, _bufferRx[i]);
                        portConfig.GoodOutput = GoodOutput.DATA_TRANSMITTED;
                    }
                }
            }
        }
        protected override void SendDMXInternal(int port, int address, byte[] values)
        {
            ArtNetControllerInstance.WriteDMXValues(PortAddress, values, (ushort)address, (ushort)values.Length);
        }
        protected override void SendDMXInternal(int port, int address, byte value)
        {
            ArtNetControllerInstance.WriteDMXValues(PortAddress, new byte[] { value }, (ushort)address, 1);
        }

        public void SetPortAddress(int port, bool dmxin, int address)
        {
            //Not implemented
        }
    }
}
