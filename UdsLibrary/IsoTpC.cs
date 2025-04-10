using IsoTpLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.Uds
{
     public class UDSISOTpC_t
     {
        public IsoTpLink physLink { get; set; }
        public IsoTpLink funcLink { get; set; }
        public byte[] SendBuf { get; set; } = new byte[UDS_ISOTP_MTU];
        public byte[] RecvBuf { get; set; } = new byte[UDS_ISOTP_MTU];
        public uint physSa { get; set; }
        public uint physTa { get; set; }
        public uint funcSa { get; set; }
        public uint funcTa { get; set; }
    }
    /// <summary>
    /// isotp基础上实现
    /// </summary>
    public class IsoTpC
    {
        UDSISOTpC_t hdl = new UDSISOTpC_t()
        {
            physLink = new IsoTpLink(),
            funcLink = new IsoTpLink()
        };
        IsoTp isoTp;
        public int Poll()
        {
            int status = 0;
            isoTp.link = hdl.physLink;
            isoTp.Poll();
            if(hdl.physLink.SendStatus == IsoTpSendStatus.InProgress)
            {
                status |= (int)UDSTpStatusFlags.UDS_TP_SEND_IN_PROGRESS;
            }
            return status;
        }
    }
    public enum UDSTpStatusFlags : int
    {
        UDS_TP_IDLE = 0x0000,
        UDS_TP_SEND_IN_PROGRESS = 0x0001,
        UDS_TP_RECV_COMPLETE = 0x0002,
        UDS_TP_ERR = 0x0004,
    };
}
