using CanKit.Abstractions.API.Can.Definitions;
using CanKit.Abstractions.API.Common.Definitions;
using IsoTpLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triumph.CAN;
using ZLG.CAN;

namespace Triumph.Uds
{
    /// <summary>
    /// 同时支持USBCANII和USBCANFD
    /// </summary>
    public class IsoTpZLGCAN : IIsoTp
    {
        public UDSIsoTpModel hdl { get; set; }

        IsoTp isoTp = new IsoTp();

        ZLGCANCommunication can;

        //public delegate void LoggerInfoFunc(string message);
        public event LoggerInfoFunc LogInfo;

        public IsoTpZLGCAN(ZLGCANCommunication can)
        {
            this.can = can;
            isoTp.SendCan = can.Send;
        }

        private void Receive(uint channel)
        {
            while (true)
            {
                var gets = can.Receive(channel);
                var query = Get(hdl.physTa, gets);
                if (query.Count() <= 0)
                {
                    query = Get(hdl.funcTa, gets);
                    if (query.Count() <= 0)
                    {
                        //Console.WriteLine("no frame received");
                        return;
                    }
                }
                foreach (var q in query)
                {
                    byte[] sourceArray = q.CanFrame.Data.ToArray();
                    byte[] destinationArray = new byte[8]; // 目标数组

                    // 将源数组的前8个字节拷贝到目标数组
                    Array.Copy(sourceArray, destinationArray, 8);
                    if (q.CanFrame.ID == hdl.physTa)
                    {
                        isoTp.link = hdl.physLink;
                        isoTp.OnCanMessage(destinationArray, (byte)destinationArray.Length);
                    }
                    else if (q.CanFrame.ID == hdl.funcTa)
                    {
                        if (hdl.physLink.ReceiveStatus != IsoTpReceiveStatus.Idle)
                        {
                            Console.WriteLine("func frame received but cannot process because link is not idle");
                            return;
                        }
                        isoTp.link = hdl.funcLink;
                        isoTp.OnCanMessage(destinationArray, (byte)destinationArray.Length);
                    }
                }
            }
        }
        

        private CanReceiveData[] Get(uint id, CanReceiveData[] array)
        {
            CanReceiveData[] ret = new CanReceiveData[0];
            if (array != null)
            {
                if (array.Length > 0)
                {
                    var query = array.
                        Where(data => data.CanFrame.ID == (int)id);
                    ret = query.ToArray();
                    int index = 0;
                    foreach (var q in query)
                    {
                        //ret[index].frame.can_id = GetId(q.frame.can_id);
                        string txt = $"帧类型{q.CanFrame.FrameKind} CanId:0x{q.CanFrame.ID.ToString("X")}" +
                            $",通道:{hdl.Channel} 接收:{BitConverter.ToString(q.CanFrame.Data.ToArray(), 0, 8)}";
                        LogInfo?.Invoke(txt);
                        Console.WriteLine(txt);
                        index++;
                    }
                }
            }
            return ret;
        }

        public int Receive(byte[] buf, ushort bufsize, UDSSDU_t? info)
        {
            ushort outSize = 0;
            isoTp.link = hdl.physLink;
            IsoTpReturnCode ret = isoTp.Receive(buf, bufsize, ref outSize);
            if (ret == IsoTpReturnCode.OK)
            {
                if (info.HasValue)
                {
                    UDSSDU_t temp = info.Value;
                    temp.A_TA = hdl.physSa;
                    temp.A_SA = hdl.physTa;
                    temp.A_TA_Type = UDS_A_TA_Type_t.PHYSICAL;
                }
            }
            else if (ret == IsoTpReturnCode.NO_DATA)
            {
                isoTp.link = hdl.funcLink;
                ret = isoTp.Receive(buf, bufsize, ref outSize);
                if (ret == IsoTpReturnCode.OK)
                {
                    if (info.HasValue)
                    {
                        UDSSDU_t temp = info.Value;
                        temp.A_TA = hdl.funcSa;
                        temp.A_SA = hdl.funcTa;
                        temp.A_TA_Type = UDS_A_TA_Type_t.FUNCTIONAL;
                    }
                }
                else if (ret == IsoTpReturnCode.NO_DATA)
                {
                    return 0;
                }
                else
                {
                    Console.WriteLine("unhandled return code from func link");
                }
            }
            else
            {
                Console.WriteLine("unhandled return code from phys link");
            }
            return outSize;
        }
        public uint Poll()
        {
            uint status = 0;
            isoTp.Channel = hdl.Channel;
            Receive(hdl.Channel);
            isoTp.link = hdl.physLink;
            isoTp.Poll();
            if (hdl.physLink.SendStatus == IsoTpSendStatus.InProgress)
            {
                status |= (uint)UDSTpStatusFlags.UDS_TP_SEND_IN_PROGRESS;
            }
            if (hdl.physLink.SendStatus == IsoTpSendStatus.Error)
            {
                status |= (uint)UDSTpStatusFlags.UDS_TP_ERR;
            }

            return status;
        }
        public int Send(byte[] buf, ushort len, UDSSDU_t? info)
        {
            int ret = -1;
            isoTp.Channel = hdl.Channel;
            UDS_A_TA_Type_t ta_type = (info.HasValue) ? info.Value.A_TA_Type : (byte)UDS_A_TA_Type_t.PHYSICAL;
            switch (ta_type)
            {
                case UDS_A_TA_Type_t.PHYSICAL:
                    isoTp.link = hdl.physLink;
                    break;
                case UDS_A_TA_Type_t.FUNCTIONAL:
                    isoTp.link = hdl.funcLink;
                    if (len > 7)
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
        public UDSErr_t Init(uint sourceAddr, uint targetAddr, uint sourceAddrFunc, uint targetAddrFunc, uint channel = 0)
        {
            hdl = new UDSIsoTpModel();
            hdl.physSa = sourceAddr;
            hdl.physTa = targetAddr;
            hdl.funcSa = sourceAddrFunc;
            hdl.funcTa = targetAddrFunc;
            hdl.Channel = channel;
            isoTp.Channel = channel;
            hdl.physLink = new IsoTpLink()
            {
                SendArbitrationId = sourceAddr,
                SendBuffer = hdl.SendBuf,
                SendBufSize = (ushort)hdl.SendBuf.Length,
                ReceiveBuffer = hdl.RecvBuf,
                ReceiveBufSize = (ushort)hdl.RecvBuf.Length
            };
            hdl.funcLink = new IsoTpLink()
            {
                SendArbitrationId = sourceAddrFunc,
                SendBuffer = hdl.SendBuf,
                SendBufSize = (ushort)hdl.SendBuf.Length,
                ReceiveBuffer = hdl.RecvBuf,
                ReceiveBufSize = (ushort)hdl.RecvBuf.Length

            };
            isoTp.link = hdl.funcLink;

            return UDSErr_t.UDS_OK;
        }
    }
}
