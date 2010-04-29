using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thrift.Transport;
using Thrift.Protocol;

namespace AlienForce.NoSql.Cassandra
{
	/// <summary>
	/// Placeholder for a proper connection pool.  Should check it back into the pool rather than closing
	/// once there is a real pool.
	/// </summary>
	public class PooledClient : Apache.Cassandra060.Cassandra.Client, IDisposable
	{
		private TSocket _Transport;

		internal PooledClient(TBinaryProtocol protocol, TSocket transport)
			: base(protocol)
		{
			_Transport = transport;
		}

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Dispose(bool disposing)
		{
			try
			{
				this._Transport.Close();
			}
			catch
			{
			}
		}
		#endregion
	}

	public class ClientPool
	{
		public string Hostname { get; private set; }
		public int Port { get; private set; }
		public int MaxConnections { get; private set; }

		public ClientPool(string hostname, int port, int maxConnections)
		{
			Hostname = hostname;
			Port = port;
			MaxConnections = maxConnections;
		}

		public PooledClient GetClient()
		{
			// TODO pool these things
			var transport = new TSocket(Hostname, Port);
			var protocol = new TBinaryProtocol(transport);
			var client = new PooledClient(protocol, transport);

			transport.Open();
			return client;
		}
	}
}
