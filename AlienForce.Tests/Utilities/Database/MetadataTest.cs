using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AlienForce.Utilities.Database;

namespace AlienForce.Tests.Utilities.Database
{
	[TestClass]
	public class MetadataTest
	{
		[TestMethod]
		public void TestMetadata()
		{
			SqlServerMetadata meta = new SqlServerMetadata("Fig", "Pay");

			var procs = meta.GetStoredProcedures();
			Console.WriteLine("Foo");
		}
	}
}
