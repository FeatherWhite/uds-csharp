using Microsoft.VisualStudio.TestTools.UnitTesting;
using Triumph.UdsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.UdsLibrary.Tests
{
    [TestClass()]
    public class UdsCommunicationTests
    {
        [TestMethod()]
        public void DiagnosticReceiveCanFrameTest()
        {
            UdsCommunication uds = new UdsCommunication();
            uds.DiagnosticRequestPid(Uds.DiagnosticPidRequestType.DIAGNOSTIC_ENHANCED_PID, 0x790, new byte[] { 0x00,0x05});

            uds.DiagnosticReceiveCanFrame();
        }
    }
}