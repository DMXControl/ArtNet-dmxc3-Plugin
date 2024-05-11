using ArtNetSharp;
using ArtNetSharp.Communication;
using DMXLIB;
using DMXLIB.I18N;
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

        public static readonly string PARA_NET = T._("Net");
        public static readonly string PARA_SUBNET = T._("Subnet");
        public static readonly string PARA_SEND = T._("Send Universe");
        public static readonly string PARA_RECEIVE = T._("Receive Universe");

        public static readonly string[] PARA_NET_ENUM = Enumerable.Range(0, 128).Select(c => c.ToString()).ToArray();
        public static readonly string[] PARA_SUB_ENUM = Enumerable.Range(0, 16).Select(c => c.ToString()).ToArray();

        private readonly byte[] _bufferTx = new byte[512];
        private readonly byte[] _bufferRx = new byte[512];
        private byte _net = 0;
        private byte _subNet = 0;
        private byte _sendUniverse = 0;
        private byte _receiveUniverse = 0;
        private bool _forceBroadcast = false;
        private IPAddress _additionalTarget = null;
        private ArtNetControllerInstance ArtNetManager => ArtNetFactory.ArtNetControllerInstance;
        private readonly PortConfig dmxcOutput_artNetInput = null;
        private readonly PortConfig dmxcInput_artNetOutput = null;

        public readonly int PortIndex;

        public ArtNetInterface(in int portIndex, in DMXInterfaceMetadata metadata)
            : base(metadata)
        {
            PortIndex = portIndex;

            dmxcOutput_artNetInput = new PortConfig((byte)((2 * portIndex) + 1), this.AddressNet, this.AddressSubNet, AddressSendUniverse, false, true) { Type = EPortType.InputToArtNet };
            dmxcInput_artNetOutput = new PortConfig((byte)((2 * portIndex) + 2), this.AddressNet, this.AddressSubNet, AddressReceiveUniverse, true, false) { Type = EPortType.OutputFromArtNet };
            ArtNetManager.DMXReceived += DMXReceived;
        }

        private void DMXReceived(object sender, PortAddress portAddress)
        {
            if (portAddress != dmxcInput_artNetOutput.PortAddress)
                return;

            if (ArtNetManager.GetReceivedDMX(portAddress) is byte[] data)
                ProcessIncomingDmx(data);
        }

        public byte AddressNet
        {
            get { return _net; }
            private set
            {
                if (_net == value) return;
                _net = value;
                dmxcOutput_artNetInput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressSendUniverse);
                dmxcInput_artNetOutput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressReceiveUniverse);
                UpdatePortDescription();
            }
        }
        public byte AddressSubNet
        {
            get { return _subNet; }
            private set
            {
                if (_subNet == value) return;
                _subNet = value;
                dmxcOutput_artNetInput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressSendUniverse);
                dmxcInput_artNetOutput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressReceiveUniverse);
                UpdatePortDescription();
            }
        }
        public byte AddressSendUniverse
        {
            get { return _sendUniverse; }
            private set
            {
                if (_sendUniverse == value) return;
                _sendUniverse = value;
                dmxcOutput_artNetInput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressSendUniverse);
                dmxcInput_artNetOutput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressReceiveUniverse);
                UpdatePortDescription();
            }
        }
        public byte AddressReceiveUniverse
        {
            get { return _receiveUniverse; }
            private set
            {
                if (_receiveUniverse == value) return;
                _receiveUniverse = value;
                dmxcOutput_artNetInput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressSendUniverse);
                dmxcInput_artNetOutput.PortAddress = new PortAddress(this.AddressNet, this.AddressSubNet, AddressReceiveUniverse);
                UpdatePortDescription();
            }
        }

        protected override void OnOutputEnable(int port)
        {
            dmxcOutput_artNetInput.GoodInput = EGoodInput.None;
            dmxcOutput_artNetInput.PortNumber = (byte)port;
        }

        protected override void OnOutputDisable(int port)
        {
            dmxcOutput_artNetInput.GoodInput = EGoodInput.InputIsDisabled;
            dmxcOutput_artNetInput.PortNumber = (byte)port;
        }
        protected override void OnInputEnable(int port)
        {
            dmxcInput_artNetOutput.PortNumber = (byte)port;
        }
        protected override void OnInputDisable(int port)
        {
            dmxcInput_artNetOutput.PortNumber = (byte)port;
        }

        protected override void OnEnable()
        {
            dmxcOutput_artNetInput.GoodInput = GetOutputState(0) ? EGoodInput.DataReceived : EGoodInput.InputIsDisabled;
            ArtNetManager.AddPortConfig(dmxcOutput_artNetInput); // Input into ArtNet
            ArtNetManager.AddPortConfig(dmxcInput_artNetOutput); // Output out of ArtNet
        }

        protected override void OnDisable()
        {
            ArtNetManager.RemovePortConfig(dmxcOutput_artNetInput);
            ArtNetManager.RemovePortConfig(dmxcInput_artNetOutput);
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
                yield return new DMXInterfaceParameter(PARA_NET, typeof(byte), EDMXInterfaceParameterType.PERSISTENT, PARA_NET_ENUM);
                yield return new DMXInterfaceParameter(PARA_SUBNET, typeof(byte), EDMXInterfaceParameterType.PERSISTENT, PARA_SUB_ENUM);
                yield return new DMXInterfaceParameter(PARA_SEND, typeof(byte), EDMXInterfaceParameterType.PERSISTENT, PARA_SUB_ENUM);
                yield return new DMXInterfaceParameter(PARA_RECEIVE, typeof(byte), EDMXInterfaceParameterType.PERSISTENT, PARA_SUB_ENUM);
                yield return new DMXInterfaceParameter(PARA_FORCE_BCAST, typeof(bool), EDMXInterfaceParameterType.PERSISTENT);
                yield return new DMXInterfaceParameter(PARA_ADD_TARGET, typeof(string), EDMXInterfaceParameterType.PERSISTENT);
            }
        }

        protected override object GetParameterInternal(string parameter) {
            if (Object.Equals(parameter, PARA_NET))
                return this.AddressNet;
            else if (Object.Equals(parameter, PARA_SUBNET))
                return this.AddressSubNet;
            else if (Object.Equals(parameter, PARA_SEND))
                return this.AddressSendUniverse;
            else if (Object.Equals(parameter, PARA_RECEIVE))
                return this.AddressReceiveUniverse;
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

        protected override bool SetParameterInternal(string parameter, object value) {
            if (Object.Equals(parameter, PARA_NET)) {
                this.AddressNet = Convert.ToByte(value);
                return true;
            } else if (Object.Equals(parameter, PARA_SUBNET)) {
                this.AddressSubNet = Convert.ToByte(value);
                return true;
            } else if (Object.Equals(parameter, PARA_SEND)) {
                this.AddressSendUniverse = Convert.ToByte(value);
                return true;
            } else if (Object.Equals(parameter, PARA_RECEIVE)) {
                this.AddressReceiveUniverse = Convert.ToByte(value);
                return true;
            } else if (Object.Equals(parameter, PARA_FORCE_BCAST)) {
                this._forceBroadcast = Convert.ToBoolean(value);
                return true;
            } else if (Object.Equals(parameter, PARA_ADD_TARGET)) {
                String addr = Convert.ToString(value);
                if (string.IsNullOrEmpty(addr)) {
                    dmxcOutput_artNetInput.ClearAdditionalIPEndpoints();
                    this._additionalTarget = null;
                } else {
                    this._additionalTarget = IPAddress.Parse(addr);
                    dmxcOutput_artNetInput.AddAdditionalIPEndpoints(this._additionalTarget);
                }
            }
            return false;
        }

        private void UpdatePortDescription() {
            SetPortDetailInfo(0, String.Format(T._("Net: {0} Subnet: {1} Send: {2} Recv: {3}"), AddressNet, AddressSubNet, AddressSendUniverse, AddressReceiveUniverse));
        }

        protected override bool TestParameterInternal(string parameter, object value) {
            if (String.Equals(parameter, PARA_NET)) {
                if (value is string) return true;
                if (!(value is byte))
                    throw new ArgumentException("Value not valid");
                if ((byte)value < 0 || (byte)value > 127) {
                    throw new ArgumentException("Value out of range (0-127)");
                }
            } else if (String.Equals(parameter, PARA_SUBNET)) {
                if (value is string) return true;
                if (!(value is byte))
                    throw new ArgumentException("Value not valid");
                if ((byte)value < 0 || (byte)value > 15) {
                    throw new ArgumentException("Value out of range (0-15)");
                }
            } else if (String.Equals(parameter, PARA_SEND)) {
                if (value is string) return true;
                if (!(value is byte))
                    throw new ArgumentException("Value not valid");
                if ((byte)value < 0 || (byte)value > 15) {
                    throw new ArgumentException("Value out of range (0-15)");
                }
            } else if (String.Equals(parameter, PARA_RECEIVE)) {
                if (value is string) return true;
                if (!(value is byte))
                    throw new ArgumentException("Value not valid");
                if ((byte)value < 0 || (byte)value > 15) {
                    throw new ArgumentException("Value out of range (0-15)");
                }
            } else if (String.Equals(parameter, PARA_ADD_TARGET)) {
                if (!(value is string))
                    throw new ArgumentException("Value not valid");
                string addr = Convert.ToString(value);
                if (!string.IsNullOrEmpty(addr) && !IPAddress.TryParse(addr, out _)) {
                    throw new ArgumentException("Not a valid IP address");
                }
            }
            return true;
        }
        protected override void PortCompleteInternal(int port)
        {
            dmxcOutput_artNetInput.GoodInput = EGoodInput.DataReceived;
            PortAddress portAddress = new PortAddress(AddressNet, AddressSubNet, AddressSendUniverse);
            ArtNetManager.WriteDMXValues(portAddress, this._bufferTx);
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
                        dmxcInput_artNetOutput.GoodOutput = EGoodOutput.DataTransmitted;
                    }
                }
            }
        }
        

        void IDMXInterfacePortConfig.SetPortAddress(int port, bool dmxin, int address)
        {
            //Currently not used
        }
    }
}
