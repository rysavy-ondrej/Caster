using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using CoAP.Server.Resources;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib;
using CoAP.Log;

namespace SimpleProxy
{
	class Options
	{
		[Option('a', "agent", Required = true,
		HelpText = "SNMP agent host name or ip address. To specify port use 'address:port' syntax.")]
		public string SnmpAgent { get; set; }

		[Option('c', "community", DefaultValue = "public",
  		HelpText = "Specifies SNMP community string. Default value is 'public'.")]
		public string Community { get; set; }

		[Option('p', "port", DefaultValue = 5683,
		HelpText = "Specifies on which CoAP port the proxy listens.")]
		public int CoapPort { get; set; }


		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}


	class SimpleProxy
	{
		public static void Main(string[] args)
		{
			LogManager.Level = LogLevel.All;
			var options = new Options();
			if (CommandLine.Parser.Default.ParseArguments(args, options))
			{
				var theProxy = new SimpleProxy(options);
				theProxy.Run();


			}
		}

		CoAP.Server.CoapServer coapServer;

		SimpleProxy(Options opt)
		{
			var snmpAgentEp = GetSnmpAgentEndPoint(opt.SnmpAgent);
			coapServer = new CoAP.Server.CoapServer(opt.CoapPort);
			coapServer.Add(new CoapMibResource("mg/mib/", opt.Community, snmpAgentEp));
		}

		void Run()
		{
			coapServer.Start();
		}

		static IPEndPoint GetSnmpAgentEndPoint(string hoststring)
		{
			var hostPort = hoststring.Split(':');
			var host = hostPort[0];
			var port = hostPort.Length > 1 ? Int32.Parse(hostPort[1]) : 161;
			var address = Dns.GetHostAddresses(host)[0];
			var agentEp = new IPEndPoint(address, port);
			return agentEp;
		}
	}
	// This proxy will do:
	// - create two UDP sockets
	// 
	// - waiting for CoAP/CoMI requests
	// - processing these requests... 
	//
	//
	//
	//
	class CoapMibResource : CoAP.Server.Resources.Resource
	{
		public CoapMibResource(string prefix, string community, IPEndPoint snmpAgent) : base(prefix)
		{ this._community = new Lextm.SharpSnmpLib.OctetString(community); _snmpAgent = snmpAgent; }

		IPEndPoint _snmpAgent;
		Lextm.SharpSnmpLib.OctetString _community;
		/// <summary>
		/// Processes the request.
		/// </summary>
		/// <returns>The get.</returns>
		/// <param name="exchange">Exchange.</param>
		protected override void DoGet(CoapExchange exchange)
		{
			var path = exchange.LocationPath;
			var oid = path.Substring(this.Name.Length);
			var result = Messenger.Get(VersionCode.V1,
						   _snmpAgent,
						   _community,
						   new List<Variable> { new Variable(new ObjectIdentifier(oid)) },
						   60000);			
			exchange.Respond(result[0].Data.ToString());
		}
	}
}
