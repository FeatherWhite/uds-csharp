using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IsoTpLibrary;
using Triumph.Uds;

namespace Triumph.UdsLibrary
{
    public class UdsCommunication
    {
        public const uint ARBITRATION_ID_OFFSET = 0x8;
        public const uint MODE_RESPONSE_OFFSET = 0x40;
        public const uint NEGATIVE_RESPONSE_MODE = 0x7f;
        public const uint MAX_DIAGNOSTIC_PAYLOAD_SIZE = 6;
        public const uint MODE_BYTE_INDEX = 0;
        public const uint PID_BYTE_INDEX = 1;
        public const uint NEGATIVE_RESPONSE_MODE_INDEX = 1;
        public const uint NEGATIVE_RESPONSE_NRC_INDEX = 2;

        public const uint OBD2_FUNCTIONAL_BROADCAST_ID = 0x7df;
        public const uint OBD2_FUNCTIONAL_RESPONSE_START = 0x7e8;
        public const uint OBD2_FUNCTIONAL_RESPONSE_COUNT = 8;

        private IsoTp isoTp;
        public uint SendId { get; set; }
        public UdsCommunication()
        {
            byte[] sendbuf = new byte[255];
            byte[] receivebuf = new byte[255];
            isoTp.InitLink(SendId, sendbuf, 255, receivebuf, 255);
        }
        public DiagnosticRequestLink link { get; set; }

    }
}
