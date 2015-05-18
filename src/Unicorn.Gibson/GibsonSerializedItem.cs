using System;
using System.Linq;
using Gibson;
using Gibson.Model;
using Sitecore.Configuration;
using Sitecore.Data;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Gibson
{
	public class GibsonSerializedItem : ISerializedItem
	{
		private readonly SerializationStore _sourceStore;
		private readonly ISerializableItem _source;
		private readonly GibsonDeserializer _deserializer;

		public GibsonSerializedItem(ISerializableItem source, GibsonDeserializer deserializer, SerializationStore sourceStore)
		{
			_sourceStore = sourceStore;
			_source = source;
			_deserializer = deserializer; // todo: SCENT. Fix deps.
		}

		public string ItemPath
		{
			get { return _source.Path; }
		}

		public string DatabaseName
		{
			get { return _source.DatabaseName; }
		}

		public string DisplayIdentifier
		{
			get { return string.Format("{0}:{1} ({2})", DatabaseName, ItemPath, Id); }
		}

		public string ProviderId
		{
			get { return _source.Id.ToString(); }
		}

		public ISerializedItem GetItem()
		{
			return this;
		}

		public ISerializedReference[] GetChildReferences(bool recursive)
		{
			return _sourceStore.GetChildren(_source.Id).Select(x => (ISerializedReference)new GibsonSerializedItem(x, _deserializer, _sourceStore)).ToArray();
		}

		public ISerializedItem[] GetChildItems()
		{
			return _sourceStore.GetChildren(_source.Id).Select(x => (ISerializedItem)new GibsonSerializedItem(x, _deserializer, _sourceStore)).ToArray();
		}

		public void Delete()
		{
			_sourceStore.Remove(Id.Guid);
		}

		public ID Id
		{
			get { return new ID(_source.Id); }
		}

		public ID ParentId
		{
			get { return new ID(_source.ParentId); }
		}

		public string Name
		{
			get { return _source.Name; }
		}

		public ID BranchId
		{
			get { return new ID(_source.BranchId); }
		}

		public ID TemplateId
		{
			get { return new ID(_source.TemplateId); }
		}

		public string TemplateName
		{
			get { return Factory.GetDatabase(DatabaseName).GetItem(TemplateId).Name; }
		}

		public FieldDictionary SharedFields
		{
			get
			{
				var fd = new FieldDictionary();
				foreach (var field in _source.SharedFields)
				{
					fd.Add(new ID(field.FieldId).ToString(), field.Value);
				}

				return fd;
			}
		}

		public ItemVersion[] Versions
		{
			get
			{
				return _source.Versions.Select(x =>
				{
					var uVersion = new ItemVersion(x.Language.Name, x.VersionNumber);
					foreach (var field in x.Fields)
					{
						uVersion.Fields.Add(new ID(field.FieldId).ToString(), field.Value);
					}

					return uVersion;
				}).ToArray();
			}
		}

		public ISourceItem Deserialize(bool ignoreMissingTemplateFields)
		{
			return _deserializer.Deserialize(_source, ignoreMissingTemplateFields);
		}

		public bool IsStandardValuesItem
		{
			get
			{
				// TODO: scent. this is shared across pretty much any serialized item that is sitecore.
				string[] array = ItemPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length > 0)
				{
					if (array.Any(s => s.Equals("templates", StringComparison.OrdinalIgnoreCase)))
					{
						return array.Last().Equals("__Standard Values", StringComparison.OrdinalIgnoreCase);
					}
				}

				return false;
			}
		}
	}
}
