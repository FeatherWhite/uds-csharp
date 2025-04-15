using IsoTpLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.Uds
{
    public class UDSRDBIVar<T>
    {
        public ushort DID;
        public ushort Len;
        public byte[] Data;
        public delegate void UnpackFn(byte[] target, byte[] source, int offset, int len,ref T res);
        public UnpackFn Unpack;
        public T Value;
    }
    public class UDSIsoTpModel
    {
        public uint Channel { get; set; }
        public IsoTpLink physLink { get; set; }
        public IsoTpLink funcLink { get; set; }
        public byte[] SendBuf { get; set; } = new byte[Config.UDS_ISOTP_MTU];
        public byte[] RecvBuf { get; set; } = new byte[Config.UDS_ISOTP_MTU];
        public uint physSa { get; set; }
        public uint physTa { get; set; }
        public uint funcSa { get; set; }
        public uint funcTa { get; set; }
    }
}
