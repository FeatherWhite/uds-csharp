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
        public const int PID_BYTE_INDEX = 1;
        public const uint NEGATIVE_RESPONSE_MODE_INDEX = 1;
        public const uint NEGATIVE_RESPONSE_NRC_INDEX = 2;

        public const byte STATE_IDLE = 0;
        public const byte STATE_SENDING = 1;
        public const byte STATE_AWAIT_SEND_COMPLETE = 2;
        public const byte STATE_AWAIT_RESPONSE = 3;
        public const byte STATE_PROCESS_RESPONSE = 4;

        private IsoTp isoTp;
        public uint SendId { get; set; }
        public UdsCommunication()
        {
            byte[] sendbuf = new byte[255];
            byte[] receivebuf = new byte[255];
            isoTp = new IsoTp();
            isoTp.InitLink(SendId, sendbuf, 255, receivebuf, 255);
            link = new DiagnosticRequestLink();
            link.Request = new DiagnosticRequest();
            link.Response = new DiagnosticResponse();
        }
        public DiagnosticRequestLink link { get; set; }

        private ushort AutosetPIDLength(byte mode,ushort pid,byte pidLength)
        {
            byte length = 0;
            if(pidLength == 0)
            {
                if(mode <= 0xa || mode == 0x3e)
                {
                    length = 1;
                }
                else if(pid > 0xffff || ((pid & 0xff00) > 0x0))
                {
                    length = 2;
                }
                else
                {
                    length = 2;
                }
            }
            return length;
        }

        public void DiagnosticRequestPid(DiagnosticPidRequestType pidRequestType,uint arbitrationId, ushort pid)
        {
            link.Request = new DiagnosticRequest()
            {
                ArbitrationId = arbitrationId,
                Mode = (byte)(pidRequestType == DiagnosticPidRequestType.DIAGNOSTIC_STANDARD_PID ? 0x01 : 0x22),
                HasPid = true,
                Pid = pid
            };
            StartDiagnosticRequest();
        }

        public void DiagnosticRequestPid(DiagnosticPidRequestType pidRequestType, uint arbitrationId, byte[] pid)
        {
            byte[] newPid = new byte[pid.Length];
            if (pid.Length > 1)
            {
                int j = 0;
                for (int i = pid.Length - 1; i > -1; i--)
                {
                    newPid[j] = pid[i];
                    j++;
                }
            }          

            link.Request = new DiagnosticRequest()
            {
                ArbitrationId = arbitrationId,
                Mode = (byte)(pidRequestType == DiagnosticPidRequestType.DIAGNOSTIC_STANDARD_PID ? 0x01 : 0x22),
                HasPid = true,
                Pid = BitConverter.ToUInt16(newPid,0),
                PidLength = (byte)pid.Length
            };
            StartDiagnosticRequest(); 
        }

        public void StartDiagnosticRequest()
        {
            link.Success = false;
            link.Completed = false;
            SendDiagnosticRequest();
            if (!link.Completed)
            {
                SetupReceiveHandle();
            }
        }

        public void SetupReceiveHandle()
        {
            isoTp.link.ReceiveArbitrationId = link.Request.ArbitrationId + ARBITRATION_ID_OFFSET;
        }

        public void SendDiagnosticRequest()
        {
            byte[] sendbuf = new byte[MAX_DIAGNOSTIC_PAYLOAD_SIZE];
            sendbuf[MODE_BYTE_INDEX] = link.Request.Mode;
            if (link.Request.HasPid)
            {
                //link.Request.PidLength = 
                //    (byte)AutosetPIDLength(link.Request.Mode, link.Request.Pid, link.Request.PidLength);
                byte[] pid = new byte[link.Request.PidLength];
                pid = BitConverter.GetBytes(link.Request.Pid);
                byte[] newPid = new byte[link.Request.PidLength];
                if (pid.Length > 1)
                {
                    int j = 0;
                    for (int i = pid.Length - 1; i > -1; i--)
                    {
                        newPid[j] = pid[i];
                        j++;
                    }
                }
                Array.Copy(newPid, 0, sendbuf, PID_BYTE_INDEX, link.Request.PidLength);
            }
            if (link.Request.PayloadLength > 0)
            {
                Array.Copy(link.Request.Payload, 0, sendbuf, PID_BYTE_INDEX + link.Request.PidLength, link.Request.PayloadLength);
            }
            if(isoTp.link.SendStatus == IsoTpSendStatus.Error)
            {
                link.Completed = true;
                link.Success = false;
            }
        }

        public void DiagnosticReceiveCanFrame()
        {
            link.Response = new DiagnosticResponse()
            {
                MultiFrame = false,
                Success = false,
                Completed = false
            };
            IsoTpReceive();
            if(isoTp.link.SendStatus == IsoTpSendStatus.Idle)
            {
                if (isoTp.link.ReceiveStatus == IsoTpReceiveStatus.Full)
                {
                    link.Response.MultiFrame = isoTp.link.ReceiveSize > 8;
                    if(isoTp.link.ReceiveBufSize > 0)
                    {
                        link.Response.Mode = isoTp.link.ReceiveBuffer[0];
                        if(HandleNegativeResponse() || HandlePositiveResponse())
                        {
                            Console.WriteLine($"诊断应答接收 {BitConverter.ToString(link.Response.payload)}");
                        }
                        link.Success = true;
                        link.Completed = true;
                    }
                    else
                    {
                        Console.WriteLine("接收空应答");
                    }
                }
            }
        }

        /// <summary>
        /// 此处需要修改
        /// </summary>
        /// <param name="payload"></param>
        private void IsoTpSend(byte[] payload)
        {
            isoTp.Send(payload, (byte)payload.Length);
            //从ECU中接收到的数据
            isoTp.OnCanMessage(new byte[8] { 0x30, 0x0f, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00 }, 8);
            while (true)
            {
                isoTp.Poll();
                if (isoTp.link.SendStatus == IsoTpSendStatus.Idle
                    || isoTp.link.SendStatus == IsoTpSendStatus.Error)
                {
                    break;
                }
            }
        }
        private byte[] IsoTpReceive()
        {
            isoTp.OnCanMessage(new byte[] { 0x10, 0x45, 0x62, 0x00, 0x05, 0x04, 0x05, 0x06 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x21, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x22, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0x00 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x23, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0x00 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x24, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0x00 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x25, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0x00 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x26, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0x00 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x27, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0x00 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x28, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0x00 }, 8);
            isoTp.Poll();
            isoTp.OnCanMessage(new byte[] { 0x29, 0x0E, 0x0F, 0x10, 0x11, 0x00, 0x00, 0xAA }, 8);
            isoTp.Poll();
            if (isoTp.link.ReceiveStatus == IsoTpReceiveStatus.Full)
            {
            }
            return isoTp.link.ReceiveBuffer;            
        }

        private bool HandleNegativeResponse()
        {
            bool isNegativeResponse = false;
            if(link.Response.Mode == NEGATIVE_RESPONSE_MODE)
            {
                isNegativeResponse = true;
                if(isoTp.link.ReceiveSize > NEGATIVE_RESPONSE_MODE_INDEX)
                {
                    link.Response.Mode = isoTp.link.ReceiveBuffer[NEGATIVE_RESPONSE_MODE_INDEX];
                }
                if (isoTp.link.ReceiveSize > NEGATIVE_RESPONSE_NRC_INDEX)
                {
                    link.Response.NegativeResponseCode = 
                        (DiagnosticNegativeResponseCode)isoTp.link.ReceiveBuffer[NEGATIVE_RESPONSE_NRC_INDEX];
                }
                link.Response.Success = false;
                link.Response.Completed = true;
            }
            return isNegativeResponse;
        }

        private bool HandlePositiveResponse()
        {
            bool isPositiveResponse = false;
            if (link.Response.Mode == link.Request.Mode + MODE_RESPONSE_OFFSET)
            {
                isPositiveResponse = true;
                link.Response.Mode = link.Request.Mode;
                link.Response.HasPid = false;
                if(link.Request.HasPid && isoTp.link.ReceiveSize > 1)
                {
                    link.Response.HasPid = true;
                    if(link.Request.PidLength == 2)
                    {
                        byte[] pid = new byte[link.Request.PidLength];
                        int j = 0;
                        for(int i = link.Request.PidLength;i >= PID_BYTE_INDEX; i--)
                        {
                            pid[j] = isoTp.link.ReceiveBuffer[i];
                            j++;
                        }
                        link.Response.Pid = BitConverter.ToUInt16(pid,0);
                    }
                    else
                    {
                        link.Response.Pid = isoTp.link.ReceiveBuffer[PID_BYTE_INDEX];
                    }
                }

                if((!link.Request.HasPid && !link.Response.HasPid)
                                || link.Response.Pid != link.Request.Pid)
                {
                    link.Response.Success = false;
                    link.Response.Completed = true;

                    byte receiveIndex = Convert.ToByte(1 + link.Request.PidLength);
                    link.Response.payload_length = Max(0, Convert.ToByte(isoTp.link.ReceiveSize - receiveIndex));
                    link.Response.payload_length = Min(UdsConfig.MAX_UDS_RESPONSE_PAYLOAD_LENGTH, link.Response.payload_length);
                    link.Response.payload = new byte[link.Response.payload_length];
                    if(link.Response.payload_length > 0)
                    {
                        Array.Copy(isoTp.link.ReceiveBuffer,receiveIndex , link.Response.payload, 0, link.Response.payload_length);
                    }
                }
                else
                {
                    isPositiveResponse = false;
                }
            }
            return isPositiveResponse;
        }
        private byte Max(byte x,byte y)
        {
            return x > y ? x : y;
        }
        private byte Min(byte x, byte y)
        {
            return x < y ? x : y;
        }

        private UDSErr_t PreRequestCheck()
        {
            if(link == null)
            {
                return UDSErr_t.UDS_ERR_INVALID_ARG;
            }
            if(link.State != STATE_IDLE)
            {
                return UDSErr_t.UDS_ERR_BUSY;
            }
            link.RecvSize = 0;
            link.SendSize = 0;
            if(isoTp == null)
            {
                return UDSErr_t.UDS_ERR_TPORT;
            }
            return UDSErr_t.UDS_OK;
        }

        public UDSErr_t UDSSendRDBI(ushort[] didList,ushort numDataIdentifiers)
        {
            const ushort didLenBytes = 2;
            UDSErr_t err = PreRequestCheck();
            if(err != UDSErr_t.UDS_OK)
            {
                return err;
            }
            if(didList == null || numDataIdentifiers == 0)
            {
                return UDSErr_t.UDS_ERR_INVALID_ARG;
            }
            link.SendBuffer[0] = (byte)UDSDiagnosticServiceId.kSID_READ_DATA_BY_IDENTIFIER;
            for(int i = 0; i < numDataIdentifiers; i++)
            {
                ushort offset = (ushort)(1 + didLenBytes * i);
                if((offset + 2) > link.SendBuffer.Length)
                {
                    return UDSErr_t.UDS_ERR_INVALID_ARG;
                }
                link.SendBuffer[offset] = Convert.ToByte((didList[i] & 0xFF00) >> 8);
                link.SendBuffer[offset + 1] = Convert.ToByte(didList[i] & 0xFF);
            }
            link.SendSize = Convert.ToUInt16(1 + (numDataIdentifiers * didLenBytes));

        }
    }
}
