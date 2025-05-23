﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.Uds
{
    public interface IIsoTp
    {
        UDSErr_t Init(uint sourceAddr, uint targetAddr, uint sourceAddrFunc, uint targetAddrFunc, uint channel = 0);
        public int Send(byte[] buf, ushort len, UDSSDU_t? info);
        public uint Poll();
        public int Receive(byte[] buf, ushort bufsize, UDSSDU_t? info);
    }
}
