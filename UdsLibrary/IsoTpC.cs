using IsoTpLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
        public UDSISOTpC_t hdl { get; set; } = new UDSISOTpC_t()
        {
            physLink = new IsoTpLink(),
            funcLink = new IsoTpLink()
        };

        IsoTp isoTp;

        public IsoTpC()
        {
            isoTp = new IsoTp();
        }
        public uint Poll()
        {
            uint status = 0;
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
            UDS_A_TA_Type_t ta_type = (info.HasValue) ? info.Value.A_TA_Type : (byte)UDS_A_TA_Type_t.PHYSICAL;
            switch (ta_type)
            {
                case UDS_A_TA_Type_t.PHYSICAL:
                    link = hdl.physLink;
                    break;
                case UDS_A_TA_Type_t.FUNCTIONAL:
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
            isoTp.link = link;
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

        public int Receive(byte[] buf, ushort bufsize, UDSSDU_t? info)
        {
            ushort outSize = 0;
            isoTp.link = hdl.physLink;
            IsoTpReturnCode ret = isoTp.Receive(buf, bufsize,ref outSize);
            if(ret == IsoTpReturnCode.OK)
            {
                if (info.HasValue)
                {
                    UDSSDU_t temp = info.Value;
                    temp.A_TA = hdl.physSa;
                    temp.A_SA = hdl.physTa;
                    temp.A_TA_Type = UDS_A_TA_Type_t.PHYSICAL;
                }
            }
            else if(ret == IsoTpReturnCode.NO_DATA)
            {
                isoTp.link = hdl.funcLink;
                ret = isoTp.Receive(buf, bufsize, ref outSize);
                if(ret == IsoTpReturnCode.OK)
                {
                    if (info.HasValue)
                    {
                        UDSSDU_t temp = info.Value;
                        temp.A_TA = hdl.funcSa;
                        temp.A_SA = hdl.funcTa;
                        temp.A_TA_Type = UDS_A_TA_Type_t.FUNCTIONAL;
                    }
                }
                else if(ret == IsoTpReturnCode.NO_DATA)
                {
                    return 0;
                }
                else
                {
                    //Log.Info("unhandled return code from func link");
                }
            }
            else
            {
                //Log.Info("unhandled return code from phys link");
            }
            return outSize;
        }
        public UDSErr_t UDSISOTpCInit(UDSISOTpCConfig_t? cfg)
        {
            if (!cfg.HasValue)
            {
                return UDSErr_t.UDS_ERR_INVALID_ARG;
            }
            hdl.physSa = cfg.Value.source_addr;
            hdl.physTa = cfg.Value.target_addr;
            hdl.funcSa = cfg.Value.source_addr_func;
            hdl.funcTa = cfg.Value.target_addr_func;
            isoTp.link = hdl.physLink;
            isoTp.InitLink(hdl.physSa, hdl.SendBuf, (ushort)hdl.SendBuf.Length, hdl.RecvBuf, (ushort)hdl.RecvBuf.Length);
            isoTp.link = hdl.funcLink;
            isoTp.InitLink(hdl.funcTa, hdl.RecvBuf, (ushort)hdl.SendBuf.Length, hdl.RecvBuf, (ushort)hdl.RecvBuf.Length);
            return UDSErr_t.UDS_OK;
        }
        public UDSErr_t UDSISOTpCInit()
        {
            isoTp.link = hdl.physLink;
            isoTp.InitLink(hdl.physSa, hdl.SendBuf, (ushort)hdl.SendBuf.Length, hdl.RecvBuf, (ushort)hdl.RecvBuf.Length);
            isoTp.link = hdl.funcLink;
            isoTp.InitLink(hdl.funcTa, hdl.RecvBuf, (ushort)hdl.SendBuf.Length, hdl.RecvBuf, (ushort)hdl.RecvBuf.Length);
            return UDSErr_t.UDS_OK;
        }
    }
    public enum UDSTpStatusFlags : uint
    {
        UDS_TP_IDLE = 0x0000,
        UDS_TP_SEND_IN_PROGRESS = 0x0001,
        UDS_TP_RECV_COMPLETE = 0x0002,
        UDS_TP_ERR = 0x0004,
    };
    public enum UDS_A_Mtype_t
    {
        DIAG = 0,
        REMOTE_DIAG,
        SECURE_DIAG,
        SECURE_REMOTE_DIAG,
    }

    public enum UDS_A_TA_Type_t : byte
    {
        PHYSICAL = 0, // unicast (1:1)
        FUNCTIONAL,   // multicast
    }

    public struct UDSISOTpCConfig_t
    {
        public uint source_addr;
        public uint target_addr;
        public uint source_addr_func;
        public uint target_addr_func;
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
