# Rainbow

An advanced serialization library for Sitecore 7 and above. Rainbow is designed to be a complete replacement for the Sitecore serialization format and filesystem organization, as well as enabling cross-source item comparison. It is a pure code library that comes with no UI of any kind, that is designed to be used with other libraries that use its serialization services with their own UIs. Libraries that consume Rainbow - such as [Unicorn](https://github.com/SitecoreUnicorn/Unicorn) - gain the ability to abstract themselves from serialization details. Libraries that extend Rainbow can add new serialization formats, new places to store serialized items, and new ways to organize them.

![taste it](https://kamsar.net/nuget/rainbow/logo.png)

## Rainbow Features

### Universal Item Data and Data Stores

Rainbow implements a set of interfaces that wrap the structure of a Sitecore item - item, version, and field. These interfaces provide a universal language that all Rainbow data stores can implement against. You could get an `IItemData` from Sitecore and write it out to disk as a YAML formatted item. You could get an `IItemData` from a web service, and deserialize it into a Sitecore database. You could construct an `IItemData` programmatically, and serialize it to a Sitecore database. Implementations of `IDataStore` provide places to store item data. It's completely universal, and everything Rainbow does revolves around these abstractions.

### [YAML-based serialization formatter](https://github.com/SitecoreUnicorn/Rainbow/tree/master/src/Rainbow.Storage.Yaml) improves the storage format for serialized items
* [This post](https://kamsar.net/index.php/2015/07/Rethinking-the-Sitecore-Serialization-Format-Unicorn-3-Preview-part-1/) goes into more detail about the hows and whys of the YAML serializer
* The format is valid [YAML](https://yaml.org/). YAML is a language that is designed to store object graphs in a human readable fashion. It uses significant whitespace and indentation to denote data boundaries. Note: only a subset of the YAML spec is allowed for performance reasons.
* Any type of endline support. Yes, even `\r` because one of the default Sitecore database items uses that in its text! No more [`.gitattributes`](http://seankearney.com/post/Using-Team-Development-for-Sitecore-with-GitHub) needed.
* No more `Content-Length` on fields that requires manual recalculation after merge conflicts
* Multilists are stored multi-line so fewer conflicts can occur
* XML fields (layout, rules) are stored pretty-printed so that fewer conflicts can occur
* Customize how fields are stored when serialized yourself with [Field Formatters](https://github.com/SitecoreUnicorn/Rainbow/tree/master/src/Rainbow/Formatting/FieldFormatters)

### [Serialization File System (SFS)](https://github.com/SitecoreUnicorn/Rainbow/tree/master/src/Rainbow/Storage) storage hierarchy
* [This post](https://kamsar.net/index.php/2015/08/Reinventing-the-Serialization-File-System-Rainbow-Preview-Part-2/) goes into more detail about SFS
* Human readable file hierarchy
* Extremely long item name support
* Unlimited path length support
* Supports non-unique paths (two items under the same parent with the same name), while keeping it human-readable
* Stores each included subtree in its own hierarchy, reducing file name length
* You can plug in the serialization formatter you desire - such as the YAML provider - to format items how you want in the tree

### [Deserialize abstract items into Sitecore](https://github.com/SitecoreUnicorn/Rainbow/tree/master/src/Rainbow.Storage.Sc) with the Sitecore storage provider
* Turn Sitecore items into `IItemData` with the `ItemData` class
* Deserialize an `IItemData` instance into a Sitecore item with `DefaultDeserializer` (used via `SitecoreDataStore`)
* Query the Sitecore tree in abstract with `SitecoreDataStore`, as if it were any other serialization store

### [Item comparison APIs](https://github.com/SitecoreUnicorn/Rainbow/tree/master/src/Rainbow/Diff)
* Compare any two `IItemData` instances regardless of source
* Customize comparison for field types or specific fields with [Field Comparers](https://github.com/SitecoreUnicorn/Rainbow/tree/master/src/Rainbow/Diff/Fields)
* Get a complete readout of changes as an object model

## Improvements
Improvements are in comparison to Sitecore serialization and the functionality in Unicorn 2.

* Deleting template fields will no longer cause errors on deserialization for items that are serialized with a value for the deleted field (e.g. standard values)
* Deserialization knows how to properly change field sharing or field versioning and port existing data to the new sharing setting

## Rainbow Organization

Rainbow consists of several projects:

* The core `Rainbow` project contains core interfaces and components that most all serialization components might need, for example [SFS](https://kamsar.net/index.php/2015/08/Reinventing-the-Serialization-File-System-Rainbow-Preview-Part-2/), item comparison tools, field formatters, and item filtering
* `Rainbow.Storage.Yaml` implements serializing and deserializing items using a YAML-based format. This format is ridiculously easier to read and merge than standard Sitecore serialization format, lacking any content length attributes, supporting any type of newline characters, and supporting pretty-printing field values (e.g. multilists, layout) for simpler merging when conflicts occur.
* `Rainbow.Storage.Sc` implements a data store using the Sitecore database. This can be used to read and write items to Sitecore using the `IDataStore` interface.

## Using Rainbow with TFS

Rainbow by default allows all characters that Windows allows to be in item filenames (when using SFS). Team Foundation Server does not allow files to be added which contain the `$` character. To enable proper interaction with TFS, enable the `Rainbow.TFS.config.example` patch file that ships with the Rainbow NuGet package.

## Extending Rainbow
Rainbow is designed to be loosely coupled. It's recommended that you employ a Dependency Injection framework (e.g. [SimpleInjector](https://simpleinjector.org)) to make your life constructing Rainbow objects easier. Of course you can also construct objects without DI.

Rainbow has [extensive unit and integration tests](https://github.com/SitecoreUnicorn/Rainbow/tree/master/src) and hopefully easy to understand code, which serve as its living documentation. As of now, Rainbow has 90% test coverage and 220 tests. [Unicorn](https://github.com/SitecoreUnicorn/Unicorn), which uses Rainbow as a service, can also be a useful source of examples.

If you can't find what you're looking for you can find me on Twitter (@kamsar) or on Sitecore Community Slack.