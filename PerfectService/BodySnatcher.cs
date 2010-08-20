using System;
using System.Configuration;

namespace PerfectService
{
	public class BodySnatcher : MarshalByRefObject
	{
		public string GetSettings()
		{
			return ConfigurationManager.AppSettings["ServiceClass"];
		}
	}
}
