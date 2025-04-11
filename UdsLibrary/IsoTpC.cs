using IsoTpLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Triumph.Uds
{
     public class UDSISOTpC_t
     {
        public IsoTpLink physLink { get; set; }
        public IsoTpLink funcLink { get; set; }
        public byte[] SendBuf { get; set; } = new byte[Config.UDS_ISOTP_MTU];
        public byte[] RecvBuf { get; set; } = new byte[Config.UDS_ISOTP_MTU];
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
        public int Send(byte[] buf,ushort len ,UDSSDU_t? info)
        {
            int ret = -1;
            IsoTpLink link;
            UDS_A_TA_Type_t ta_type = (info != null) ? info.Value.A_TA_Type : (byte)UDS_A_TA_Type_t.UDS_A_TA_TYPE_PHYSICAL;
            switch (ta_type)
            {
                case UDS_A_TA_Type_t.UDS_A_TA_TYPE_PHYSICAL:
                    link = hdl.physLink;
                    break;
                case UDS_A_TA_Type_t.UDS_A_TA_TYPE_FUNCTIONAL:
                    link = hdl.funcLink;
                    if(len > 7)
                    {
                        ret = -3;
                        goto done;
                    }
                break;
                default:
                    ret = -4;
                    goto done;
            }

            IsoTpReturnCode sendStatus = isoTp.Send(buf, len);
            switch (sendStatus)
            {
                case IsoTpReturnCode.OK:
                    ret = len;
                    goto done;
                case IsoTpReturnCode.INPROGRESS:
                case IsoTpReturnCode.OVERFLOW:
                default:
                    ret = (int)sendStatus;
                    goto done;
            }
            done:
                return ret;
        }


    }
    public enum UDSTpStatusFlags : int
    {
        UDS_TP_IDLE = 0x0000,
        UDS_TP_SEND_IN_PROGRESS = 0x0001,
        UDS_TP_RECV_COMPLETE = 0x0002,
        UDS_TP_ERR = 0x0004,
    };
    public enum UDS_A_Mtype_t
    {
        UDS_A_MTYPE_DIAG = 0,
        UDS_A_MTYPE_REMOTE_DIAG,
        UDS_A_MTYPE_SECURE_DIAG,
        UDS_A_MTYPE_SECURE_REMOTE_DIAG,
    }    

    public enum UDS_A_TA_Type_t
    {
        UDS_A_TA_TYPE_PHYSICAL = 0, // unicast (1:1)
        UDS_A_TA_TYPE_FUNCTIONAL,   // multicast
    }    


    /**
     * @brief Service data unit (SDU)
     * @details data interface between the application layer and the transport layer
     */
    public struct UDSSDU_t
    {       
        public UDS_A_Mtype_t A_Mtype; // message type (diagnostic, remote diagnostic, secure diagnostic, secure
                               // remote diagnostic)
        public uint A_SA;         // application source address
        public uint A_TA;         // application target address
        public UDS_A_TA_Type_t A_TA_Type; // application target address type (physical or functional)
        public uint A_AE;             // application layer remote address
    }

}
