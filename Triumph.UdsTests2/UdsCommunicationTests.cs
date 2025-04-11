using Microsoft.VisualStudio.TestTools.UnitTesting;
using Triumph.UdsLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triumph.Uds;

namespace Triumph.UdsLibrary.Tests
{
    [TestClass()]
    public class UdsCommunicationTests
    {
        Client client;
        [TestInitialize]
        public void TestInitialize()
        {
            client = new Client();
            client.Init();
            client.Tp.hdl.physSa = 0x7E8;
            client.Tp.hdl.physTa = 0x7E0;
            client.Tp.hdl.funcSa = 0xFFFFFFFF;
            client.Tp.hdl.funcTa = 0x7DF;
            client.Tp.UDSISOTpCInit();

        }
        [TestMethod]
        public void Test0x22UnpackRDBIResponse()
        {
            ushort[] did_list = { 0xf190 };
            client.UDSSendRDBI(did_list,1);
        }
    }
}