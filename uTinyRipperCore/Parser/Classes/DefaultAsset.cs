﻿using uTinyRipper.Converters;
using uTinyRipper.YAML;

namespace uTinyRipper.Classes
{
	public abstract class DefaultAsset : NamedObject
	{
		public DefaultAsset(AssetInfo assetInfo):
			base(assetInfo)
		{
		}

		public override void Read(AssetReader reader)
		{
			base.Read(reader);

			Message = reader.ReadString();
			IsWarning = reader.ReadBoolean();
		}

		protected override YAMLMappingNode ExportYAMLRoot(IExportContainer container)
		{
			YAMLMappingNode node = base.ExportYAMLRoot(container);
			node.Add(MessageName, Message);
			node.Add(IsWarningName, IsWarning);
			return node;
		}

		public string Message { get; private set; }
		public bool IsWarning { get; private set; }

		public const string MessageName = "m_Message";
		public const string IsWarningName = "m_IsWarning";
	}
}