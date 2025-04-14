using IsoTpLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.Uds
{
    public class Client
    {
        uint p2_ms;
        uint p2Star_ms;
        public IsoTpZLGUSBCANII Tp { get; set; }
        uint p2Timer;
        byte state;
        public byte options { get; set; }
        byte defaultOptions;
        byte optionsCopy;

        public const byte STATE_IDLE = 0;
        public const byte STATE_SENDING = 1;
        public const byte STATE_AWAIT_SEND_COMPLETE = 2;
        public const byte STATE_AWAIT_RESPONSE = 3;
        public const byte STATE_PROCESS_RESPONSE = 4;

        public const byte UDS_SUPPRESS_POS_RESP = 0x1;  // set the suppress positive response bit
        public const byte UDS_FUNCTIONAL = 0x2;         // send the request as a functional request
        public const byte UDS_IGNORE_SRV_TIMINGS = 0x8; // ignore the server-provided p2 and p2_star

        public const byte UDS_NEG_RESP_LEN = 3;
        public const byte UDS_0X10_REQ_LEN = 2;
        public const byte UDS_0X10_RESP_LEN = 6;
        public const byte UDS_0X11_REQ_MIN_LEN = 2;
        public const byte UDS_0X11_RESP_BASE_LEN = 2;
        public const byte UDS_0X23_REQ_MIN_LEN = 4;
        public const byte UDS_0X23_RESP_BASE_LEN = 1;
        public const byte UDS_0X22_RESP_BASE_LEN = 1;
        public const byte UDS_0X27_REQ_BASE_LEN = 2;
        public const byte UDS_0X27_RESP_BASE_LEN = 2;
        public const byte UDS_0X28_REQ_BASE_LEN = 3;
        public const byte UDS_0X28_RESP_LEN = 2;
        public const byte UDS_0X2E_REQ_BASE_LEN = 3;
        public const byte UDS_0X2E_REQ_MIN_LEN = 4;
        public const byte UDS_0X2E_RESP_LEN = 3;
        public const byte UDS_0X31_REQ_MIN_LEN = 4;
        public const byte UDS_0X31_RESP_MIN_LEN = 4;
        public const byte UDS_0X34_REQ_BASE_LEN = 3;
        public const byte UDS_0X34_RESP_BASE_LEN = 2;
        public const byte UDS_0X35_REQ_BASE_LEN = 3;
        public const byte UDS_0X35_RESP_BASE_LEN = 2;
        public const byte UDS_0X36_REQ_BASE_LEN = 2;
        public const byte UDS_0X36_RESP_BASE_LEN = 2;
        public const byte UDS_0X37_REQ_BASE_LEN = 1;
        public const byte UDS_0X37_RESP_BASE_LEN = 1;
        public const byte UDS_0X38_REQ_BASE_LEN = 4;
        public const byte UDS_0X38_RESP_BASE_LEN = 3;
        public const byte UDS_0X3E_REQ_MIN_LEN = 2;
        public const byte UDS_0X3E_REQ_MAX_LEN = 2;
        public const byte UDS_0X3E_RESP_LEN = 2;
        public const byte UDS_0X85_REQ_BASE_LEN = 2;
        public const byte UDS_0X85_RESP_LEN = 2;

        public const byte UDS_LEV_DS_DS = 0x01;
        public const byte UDS_LEV_DS_PRGS = 0x02;
        public const byte UDS_LEV_DS_EXTDS = 0x03;
        public const byte UDS_LEV_DS_SSDS = 0x04;

        public const byte UDS_LEV_RT_HR = 0x01;
        public const byte UDS_LEV_RT_KOFFONR = 0x02;
        public const byte UDS_LEV_RT_SR = 0x03;
        public const byte UDS_LEV_RT_ERPSD = 0x04;
        public const byte UDS_LEV_RT_DRPSD = 0x05;

        public const byte UDS_LEV_CTRLTP_ERXTX = 0;
        public const byte UDS_LEV_CTRLTP_ERXDTX = 1;
        public const byte UDS_LEV_CTRLTP_DRXETX = 2;
        public const byte UDS_LEV_CTRLTP_DRXTX = 3;

        public const byte UDS_CTP_NCM = 1;
        public const byte UDS_CTP_NWMCM = 2;
        public const byte UDS_CTP_NWMCM_NCM = 3;

        public const byte UDS_LEV_RCTP_STR = 1;
        public const byte UDS_LEV_RCTP_STPR = 2;
        public const byte UDS_LEV_RCTP_RRR = 3;

        public const byte UDS_MOOP_ADDFILE = 1;
        public const byte UDS_MOOP_DELFILE = 2;
        public const byte UDS_MOOP_REPLFILE = 3;
        public const byte UDS_MOOP_RDFILE = 4;
        public const byte UDS_MOOP_RDDIR = 5;
        public const byte UDS_MOOP_RSFILE = 6;

        public const byte LEV_DTCSTP_ON = 1;
        public const byte LEV_DTCSTP_OFF = 2;

        public const byte UDS_MAX_DIAGNOSTIC_SERVICES = 0x7F;

        public ushort RecvSize { get; private set; }
        public ushort SendSize { get; private set; }
        public byte[] RecvBuffer { get; private set; } = new byte[Config.UDS_CLIENT_RECV_BUF_SIZE];
        public byte[] SendBuffer { get; private set; } = new byte[Config.UDS_CLIENT_SEND_BUF_SIZE];
        public byte State { get {return state; } }
        public uint Channel { get; set; } = 0;

        public UDSErr_t Init()
        {
            state = STATE_IDLE;
            p2_ms = Config.UDS_CLIENT_DEFAULT_P2_MS;
            p2Star_ms = Config.UDS_CLIENT_DEFAULT_P2_STAR_MS;
            if(p2Star_ms < p2_ms)
            {
                //UDS_LOGE(__FILE__, "p2_star_ms must be >= p2_ms");
                p2Star_ms = p2_ms;
            }
            return UDSErr_t.UDS_OK;
        }
                
        private UDSErr_t SendRequest()
        {
            optionsCopy = options;
            if((optionsCopy & UDS_SUPPRESS_POS_RESP) == 1)
            {
                SendBuffer[1] |= 0x80;
            }
            ChangeState(STATE_SENDING);
            UDSErr_t err = PollLowLevel();
            return err;
        }
        private UDSErr_t PollLowLevel()
        {
            UDSErr_t err = UDSErr_t.UDS_OK;
            if(Tp == null)
            {
                return UDSErr_t.UDS_ERR_MISUSE;
            }
            uint tpStatus = Tp.Poll();
            switch (state)
            {
                case STATE_IDLE:
                    options = defaultOptions;
                    break;
                case STATE_SENDING:
                    {
                        {
                            UDSSDU_t info = new UDSSDU_t();
                            int len = Tp.Receive(RecvBuffer, (ushort)RecvBuffer.Length, info);
                            if (len < 0)
                            {
                                Console.WriteLine($"transport returned error {len}");
                            }
                            else if (len == 0)
                            {
                                //Log.Info("transport returned no data");
                            }
                            else
                            {
                                Console.WriteLine($"received {len} unexpected bytes");
                            }
                        }
                        Array.Clear(RecvBuffer, 0, RecvBuffer.Length);
                        RecvSize = 0;
                        byte taType = Convert.ToByte(optionsCopy &
                            (UDS_FUNCTIONAL != 0 ? (byte)UDS_A_TA_Type_t.FUNCTIONAL : (byte)UDS_A_TA_Type_t.PHYSICAL));
                        UDSSDU_t info2 = new UDSSDU_t()
                        {
                            A_Mtype = UDS_A_Mtype_t.DIAG,
                            A_TA_Type = (UDS_A_TA_Type_t)taType,
                        };
                        int ret = Tp.Send(SendBuffer, SendSize, info2);
                        if (ret < 0)
                        {
                            err = UDSErr_t.UDS_ERR_TPORT;
                            Console.WriteLine($"transport error {ret}");
                        }
                        else if (ret == 0)
                        {
                            Console.WriteLine("send in progress...");
                        }
                        else if (SendSize == ret)
                        {
                            Console.WriteLine($"send complete {ret} byte");
                            ChangeState(STATE_AWAIT_SEND_COMPLETE);
                        }
                        else
                        {
                            err = UDSErr_t.UDS_ERR_BUFSIZ;
                        }
                        break;
                    }
                case STATE_AWAIT_SEND_COMPLETE:
                    if ((optionsCopy & UDS_FUNCTIONAL) != 0)
                    {
                        ChangeState(STATE_IDLE);
                    }
                    if ((tpStatus & (uint)UDSTpStatusFlags.UDS_TP_SEND_IN_PROGRESS) != 0)
                    {
                        Console.WriteLine("waiting,send in progress...");
                    }
                    else
                    {
                        //需添加回调函数
                        if ((optionsCopy & UDS_SUPPRESS_POS_RESP) != 0)
                        {
                            Console.WriteLine("send complete,change to idle");
                            ChangeState(STATE_IDLE);
                        }
                        else
                        {
                            Console.WriteLine($"send complete,change to state await response");
                            ChangeState(STATE_AWAIT_RESPONSE);
                            p2Timer = UDSMillis() + p2_ms;
                        }
                    }
                    break;
                case STATE_AWAIT_RESPONSE:
                    { 
                        UDSSDU_t info = new UDSSDU_t();
                        int len = Tp.Receive(RecvBuffer, (ushort)RecvBuffer.Length, info);
                        if (len < 0)
                        {
                            err = UDSErr_t.UDS_ERR_TPORT;
                            ChangeState(STATE_IDLE);
                        }
                        else if (len == 0)
                        {
                            if (UDSTimeAfter(UDSMillis(), p2Timer))
                            {
                                Console.WriteLine("p2 timeout");
                                err = UDSErr_t.UDS_ERR_TIMEOUT;
                                ChangeState(STATE_IDLE);
                            }
                        }
                        else
                        {
                                Console.WriteLine($"received {len} bytes.");
                                RecvSize = (ushort)len;
                                //RecvBuffer = Tp.hdl.RecvBuf;
                                err = ValidateServerResponse();
                                if (err == UDSErr_t.UDS_OK)
                                {
                                    err = HandleServerResponse();
                                }
                                if (err == UDSErr_t.UDS_OK)
                                {
                                    //此处需添加回调函数
                                    ChangeState(STATE_IDLE);
                                }
                        }
                        break;
            }
                default:
                    break;
            }
            return err;
        }

        private UDSErr_t ValidateServerResponse()
        {
            if(RecvSize < 1)
            {
                return UDSErr_t.UDS_ERR_RESP_TOO_SHORT;
            }
            if (RecvBuffer[0] == 0x7F)
            {
                if(RecvSize < 2)
                {
                    return UDSErr_t.UDS_ERR_RESP_TOO_SHORT;
                }
                else if (SendBuffer[0] != RecvBuffer[1])
                {
                    return UDSErr_t.UDS_ERR_SID_MISMATCH;
                }
                else if (RecvBuffer[2] == (byte)UDSErr_t.UDS_NRC_RequestCorrectlyReceived_ResponsePending)
                {
                    return UDSErr_t.UDS_OK;
                }
                else
                {
                    return (UDSErr_t)RecvBuffer[2];
                }
            }
            else
            {
                if (UDSResponseSIDOf(SendBuffer[0]) != RecvBuffer[0])
                {
                    return UDSErr_t.UDS_ERR_SID_MISMATCH;
                }
                if (SendBuffer[0] == (byte)UDSDiagnosticServiceId.kSID_ECU_RESET)
                {
                    if(RecvSize < 2)
                    {
                        return UDSErr_t.UDS_ERR_RESP_TOO_SHORT;
                    }
                    else if (SendBuffer[1] != RecvBuffer[1])
                    {
                        return UDSErr_t.UDS_ERR_SID_MISMATCH;
                    }
                }
            }
            return UDSErr_t.UDS_OK;
        }

        private UDSErr_t HandleServerResponse()
        {
            if (RecvBuffer[0] == 0x7F)
            {
                if((byte)UDSErr_t.UDS_NRC_RequestCorrectlyReceived_ResponsePending == RecvBuffer[2])
                {
                    p2Timer = UDSMillis() + p2Star_ms;
                    //Log.Info($"got RCRRP, set p2 timer to %{p2Timer}");
                    Array.Clear(RecvBuffer, 0, RecvBuffer.Length);
                    RecvSize = 0;
                    ChangeState(STATE_AWAIT_RESPONSE);
                    return UDSErr_t.UDS_NRC_RequestCorrectlyReceived_ResponsePending;
                }
            }
            else
            {
                byte respSid = RecvBuffer[0];
                switch (UDSRequestSIDOf(respSid))
                {
                    case (byte)UDSDiagnosticServiceId.kSID_DIAGNOSTIC_SESSION_CONTROL:
                        if(RecvSize < UDS_0X10_RESP_LEN)
                        {
                            Console.WriteLine($"Error: SID {UDSDiagnosticServiceId.kSID_DIAGNOSTIC_SESSION_CONTROL.ToString("X")} response too short");
                            ChangeState(STATE_IDLE);
                            return UDSErr_t.UDS_ERR_RESP_TOO_SHORT;
                        }
                        if((optionsCopy & UDS_IGNORE_SRV_TIMINGS) != 0)
                        {
                            ChangeState(STATE_IDLE);
                            return UDSErr_t.UDS_OK;
                        }
                        ushort p2 = (ushort)((RecvBuffer[2] << 8) | RecvBuffer[3]);
                        uint p2Star = (uint)((RecvBuffer[4] << 8) + RecvBuffer[5]) * 10;
                        Console.WriteLine($"received new timings: p2: %{p2} , p2*: % {p2Star}");
                        p2_ms = p2;
                        p2Star_ms = p2Star;
                        break;
                    default:
                        break;
                }
            }
            return UDSErr_t.UDS_OK;
        }

        private byte UDSResponseSIDOf(byte requestSid)
        {
            return (byte)(requestSid + 0x40);
        }

        private byte UDSRequestSIDOf(byte responseSid)
        {
            return (byte)(responseSid - 0x40);
        }

        private bool UDSTimeAfter(uint a, uint b)
        {
            return a > b;
        }

        private uint UDSMillis()
        {
            return (uint)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % uint.MaxValue);
        }

        private void ChangeState(byte state)
        {
            if(this.state != state)
            {
                //Log.Info("Client state changed from {0} to {1}", this.state, state);
                this.state = state;
                switch (state)
                {
                    case STATE_IDLE:
                        //client->fn(client, UDS_EVT_Idle, NULL);
                        break;
                    case STATE_SENDING:
                        break;
                    case STATE_AWAIT_SEND_COMPLETE:
                        break;
                    case STATE_AWAIT_RESPONSE:
                        break;
                    case STATE_PROCESS_RESPONSE:
                        break;
                    default:
                        //UDS_ASSERT(0);
                        break;
                }
            }
        }
        private UDSErr_t PreRequestCheck()
        {
            if (this == null)
            {
                return UDSErr_t.UDS_ERR_INVALID_ARG;
            }
            if (state != STATE_IDLE)
            {
                return UDSErr_t.UDS_ERR_BUSY;
            }
            RecvSize = 0;
            SendSize = 0;
            if (Tp == null)
            {
                return UDSErr_t.UDS_ERR_TPORT;
            }
            return UDSErr_t.UDS_OK;
        }
        public UDSErr_t Poll()
        {
            UDSErr_t err = PollLowLevel();
            if (err != UDSErr_t.UDS_OK && err != UDSErr_t.UDS_NRC_RequestCorrectlyReceived_ResponsePending)
            {
                ChangeState(STATE_IDLE);
            }
            return err;
        }

        public UDSErr_t UDSSendRDBI(ushort[] didList, ushort numDataIdentifiers)
        {
            const ushort didLenBytes = 2;
            UDSErr_t err = PreRequestCheck();
            if (err != UDSErr_t.UDS_OK)
            {
                return err;
            }
            if (didList == null || numDataIdentifiers == 0)
            {
                return UDSErr_t.UDS_ERR_INVALID_ARG;
            }
            SendBuffer[0] = (byte)UDSDiagnosticServiceId.kSID_READ_DATA_BY_IDENTIFIER;
            for (int i = 0; i < numDataIdentifiers; i++)
            {
                ushort offset = (ushort)(1 + didLenBytes * i);
                if ((offset + 2) > SendBuffer.Length)
                {
                    return UDSErr_t.UDS_ERR_INVALID_ARG;
                }
                SendBuffer[offset] = Convert.ToByte((didList[i] & 0xFF00) >> 8);
                SendBuffer[offset + 1] = Convert.ToByte(didList[i] & 0xFF);
            }
            SendSize = Convert.ToUInt16(1 + (numDataIdentifiers * didLenBytes));
            return SendRequest();
        }

        public UDSErr_t UDSUnpackRDBIResponse<T>(UDSRDBIVar<T>[] vars ,ushort numVars)
        {
            ushort offset = UDS_0X22_RESP_BASE_LEN;
            if (vars == null)
            {
                return UDSErr_t.UDS_ERR_INVALID_ARG;
            }
            for(int i = 0; i < numVars; i++)
            {
                if(offset + sizeof(ushort) > RecvSize)
                {
                    return UDSErr_t.UDS_ERR_RESP_TOO_SHORT;
                }
                ushort did = Convert.ToUInt16((RecvBuffer[offset] << 8) | RecvBuffer[offset + 1]);
                if(did != vars[i].DID)
                {
                    return UDSErr_t.UDS_ERR_DID_MISMATCH;
                }
                if(offset + sizeof(ushort) + vars[i].Len > RecvSize)
                {
                    return UDSErr_t.UDS_ERR_RESP_TOO_SHORT;
                }
                if (vars[i].UnpackFn != null)
                {
                    vars[i].UnpackFn(vars[i].Data, RecvBuffer, offset + sizeof(ushort), vars[i].Len);
                }
                else
                {
                    return UDSErr_t.UDS_ERR_INVALID_ARG;
                }
                offset += (ushort)(sizeof(ushort) + vars[i].Len);
            }
            return UDSErr_t.UDS_OK;
        }

        public UDSErr_t UDSSendWDBI(ushort dataIdentifier, byte[] data)
        {
            UDSErr_t err = PreRequestCheck();
            ushort size = Convert.ToUInt16(data.Length);
            if (err != UDSErr_t.UDS_OK)
            {
                return err;
            }
            if(data == null || size == 0)
            {
                return UDSErr_t.UDS_ERR_INVALID_ARG;
            }
            SendBuffer[0] = (byte)UDSDiagnosticServiceId.kSID_WRITE_DATA_BY_IDENTIFIER;
            if(SendBuffer.Length <= 3 || SendBuffer.Length <= 3 + size)
            {
                return UDSErr_t.UDS_ERR_BUFSIZ;
            }
            SendBuffer[1] = Convert.ToByte((dataIdentifier & 0xFF00) >> 8);
            SendBuffer[2] = Convert.ToByte(dataIdentifier & 0xFF);
            Array.Copy(data, 0, SendBuffer, 3, size);
            SendSize = Convert.ToUInt16(3 + size);
            return SendRequest();
        }
    }
}
