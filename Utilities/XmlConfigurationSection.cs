using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Xml;
using System.Security.Cryptography;

namespace AlienForce.Utilities
{
	/// <summary>
	/// A simple configuration handler to expose a configuration section as raw XML.
	/// A little silly since you could just read the XML file yourself, but this will
	/// allow you to keep the config file "correct."
	/// </summary>
	/// <example>
	///  &lt;section name="BenTen.CachedLocalString.Filters" type="AALabs.BenTen.Utilities.XmlConfigurationHandler, AALabs.BenTen.Utilities" allowDefinition="MachineToApplication"/&gt;
	/// </example>
	/// <remarks>Importantly, when you GetConfig on one of these sections, you
	/// will be getting an XmlNode, NOT an XmlConfigurationHandler.  Put a "configSource" attribute on the node if you want to retrieve the configuration
	/// from an external file.</remarks>
	public class XmlConfigurationSection : ConfigurationSection
	{
		private XmlNode _Node;
		private XmlNode _OriginalNode;

		/// <summary>
		/// Default constructor for the runtime.  Must call Deserialize methods before this is useful.
		/// </summary>
		protected XmlConfigurationSection() { }

		/// <summary>
		/// Construct a new section for saving to the configuration file.
		/// </summary>
		/// <param name="sectionName"></param>
		public XmlConfigurationSection(string sectionName)
		{
			_Node = new XmlDocument().CreateElement(sectionName);
		}

		/// <summary>
		/// Gets the configuration data as raw XML
		/// </summary>
		public XmlNode Node 
		{
			get
			{
				return _Node; 
			} 
		}

		/// <summary>
		/// If the core config has a "file=" attribute, we will fetch that file and set the Node
		/// to be the DocumentElement of that file.  If you do some fancy inheritance stuff,
		/// you can get to the original node in the config via this property.
		/// </summary>
		public XmlNode OriginalNode
		{
			get
			{
				return _OriginalNode;
			}
		}

		/// <summary>
		/// If true, and a secondary file as specified in the "file" attribute of the primary config file 
		/// sections root node (i.e. the section this handler is registered for), we will use machine
		/// protected data storage for the satellite file.
		/// </summary>
		public bool SatelliteFileIsProtected { get; set; }

		/// <summary>
		/// Retrieves the Xml Node
		/// </summary>
		/// <returns></returns>
		protected override object GetRuntimeObject()
		{
			return _Node;
		}

		/// <summary>
		/// Reads XML from the configuration file.
		/// </summary>
		/// <param name="reader">The <see cref="T:System.Xml.XmlReader"></see> that reads from the configuration file.</param>
		/// <param name="serializeCollectionKey">true to serialize only the collection key properties; otherwise, false.</param>
		protected override void DeserializeSection(XmlReader reader)
		{
			XmlDocument xd = new XmlDocument();
			_Node = xd.ReadNode(reader);
			if (_Node.Attributes["file"] != null)
			{
				FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(this.CurrentConfiguration.FilePath), _Node.Attributes["file"].Value));
				if (fi.Exists)
				{
					_OriginalNode = Node;
					var xdr = new XmlDocument();
					if (_Node.Attributes["protected"] != null && _Node.Attributes["protected"].Value.ToLower() == "true")
					{
						SatelliteFileIsProtected = true;
						xdr.LoadXml(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(fi.FullName), Encoding.UTF8.GetBytes(_Node.Name), DataProtectionScope.LocalMachine)));
					}
					else
					{
						SatelliteFileIsProtected = false;
						xdr.Load(fi.FullName);
					}
					_Node = xdr.DocumentElement;
				}
			}
		}

		/// <summary>
		/// Serialize the node to the parentElement
		/// </summary>
		/// <param name="parentElement"></param>
		/// <param name="name"></param>
		/// <param name="saveMode"></param>
		/// <returns></returns>
		protected override string SerializeSection(ConfigurationElement parentElement, string name, ConfigurationSaveMode saveMode)
		{
			StringWriter sWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
			XmlTextWriter xWriter = new XmlTextWriter(sWriter);
			xWriter.Formatting = Formatting.Indented;
			xWriter.Indentation = 1;
			xWriter.IndentChar = '\t';
			this.SerializeToXmlElement(xWriter, name);
			xWriter.Flush();
			return sWriter.ToString();
		}

		/// <summary>
		/// Writes the contents of this configuration element to the configuration file when implemented in a derived class.
		/// </summary>
		/// <param name="writer">The <see cref="T:System.Xml.XmlWriter"></see> that writes to the configuration file.</param>
		/// <param name="serializeCollectionKey">true to serialize only the collection key properties; otherwise, false.</param>
		/// <returns>
		/// true if any data was actually serialized; otherwise, false.
		/// </returns>
		protected override bool SerializeToXmlElement(XmlWriter writer, string elementName)
		{
			var whichNode = _OriginalNode ?? _Node;
			if (whichNode != null)
			{
				if (elementName != whichNode.Name)
				{
					throw new InvalidOperationException(String.Format("An attempt was made to save XmlConfigurationSection {0} to section {1}.", whichNode.Name, elementName));
				}
				whichNode.WriteTo(writer);
				if (_OriginalNode != null)
				{
					FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(this.CurrentConfiguration.FilePath), _OriginalNode.Attributes["file"].Value));
					if (SatelliteFileIsProtected)
					{
						File.WriteAllBytes(fi.FullName, ProtectedData.Protect(Encoding.UTF8.GetBytes(_Node.OwnerDocument.OuterXml), Encoding.UTF8.GetBytes(_Node.Name), DataProtectionScope.LocalMachine));
					}
					else
					{
						_Node.OwnerDocument.Save(fi.FullName);
					}
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Throws NotImplementedException for XmlConfigurationSection
		/// </summary>
		protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Throws NotImplementedException for XmlConfigurationSection
		/// </summary>
		protected override bool SerializeElement(XmlWriter writer, bool serializeCollectionKey)
		{
			throw new NotImplementedException();
		}
	}
}
