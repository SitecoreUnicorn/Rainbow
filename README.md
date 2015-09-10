# Rainbow

An advanced serialization library for Sitecore 7 and above. Rainbow is designed to be a complete replacement for the Sitecore serialization format and filesystem organization, solving many of the long standing issues with the built in serialization such as path and item name length. For an introduction of why we have Rainbow and how it works, see [this post](http://kamsar.net/index.php/2015/07/Rethinking-the-Sitecore-Serialization-Format-Unicorn-3-Preview-part-1/) about the serialization format and [this post](http://kamsar.net/index.php/2015/08/Reinventing-the-Serialization-File-System-Rainbow-Preview-Part-2/) about SFS (the filesystem hierarchy). Rainbow also includes item comparison tools to find changes between abstract items.

Rainbow is designed to be taken and used by other projects, such as [Unicorn](https://github.com/kamsar/Unicorn); it provides no UI of its own. It is also extremely extensible: you could just as easily replace the serialization format with your own, or change how items are stored on disk. You could even decide that text fields should be stored reversed when serialized, or that certain text fields should be compared case insensitively when determining if differences exist.

Rainbow consists of multiple projects:

* The core `Rainbow` project contains core interfaces and components that most all serialization components might need, for example SFS, item comparison tools, field formatters, and item filtering
* `Rainbow.Storage.Yaml` implements serializing and deserializing items using a YAML-based format. This format is ridiculously easier to read and merge than standard Sitecore serialization format, lacking any content length attributes, supporting any type of newline characters, and supporting pretty-printing field values (e.g. multilists, layout) for simpler merging when conflicts occur.
* `Rainbow.Storage.Sc` implements a data store using the Sitecore database. This can be used to read and write items to Sitecore using the `IDataStore` interface.

## Rainbow Features

### Serialize items using a [YAML-based formatter](https://github.com/kamsar/Rainbow/tree/master/src/Rainbow.Storage.Yaml)
* The format is valid [YAML](http://yaml.org/). Note: only a subset of the YAML spec is allowed for performance reasons.
* Any type of endline support. Yes, even `\r` because one of the default Sitecore database items uses that! No more [`.gitattributes`](http://seankearney.com/post/Using-Team-Development-for-Sitecore-with-GitHub) needed.
* No more `Content-Length` on fields that requires manual recalculation after merge conflicts
* Multilists are stored multi-line so fewer conflicts can occur
* XML fields (layout, rules) are stored pretty-printed so that fewer conflicts can occur
* Customize how fields are stored when serialized yourself with [Field Formatters](https://github.com/kamsar/Rainbow/tree/master/src/Rainbow/Formatting/FieldFormatters)

### [Serialization File System (SFS)](https://github.com/kamsar/Rainbow/tree/master/src/Rainbow/Storage) storage hierarchy
* Human readable hierarchy
* Extremely long item name support
* Unlimited path length support
* Supports items of the same name in the same place, human-readably
* Stores each included subtree in its own hierarchy, reducing file name length

### [Deserialize abstract items into Sitecore](https://github.com/kamsar/Rainbow/tree/master/src/Rainbow.Storage.Sc) with the Sitecore storage provider
* Turn Sitecore items into `IItemData` with the `ItemData` class
* Deserialize an `IItemData` instance into a Sitecore item with `DefaultDeserializer` (used via `SitecoreDataStore`)
* Query the Sitecore tree in abstract with `SitecoreDataStore`, as if it were any other serialization store

### [Item comparison APIs](https://github.com/kamsar/Rainbow/tree/master/src/Rainbow/Diff)
* Compare any item that you can stuff into `IItemData`, which out of the box includes Sitecore items and YAML-serialized items
* Customize comparison for field types or specific fields with [Field Comparers](https://github.com/kamsar/Rainbow/tree/master/src/Rainbow/Diff/Fields)
* Get a complete readout of changes as an object model

## Improvements
Improvements are in comparison to Sitecore serialization and the functionality in Unicorn 2.

* Deleting template fields will no longer cause errors on deserialization for items that are serialized with a value for the deleted field
* Deserialization knows how to properly change field sharing or field versioning and port existing data to the new sharing setting

## Extending Rainbow
Rainbow is designed to be loosely coupled. It's recommended that you employ a Dependency Injection framework (e.g. [SimpleInjector](https://simpleinjector.org)) to make your life constructing Rainbow object easier. This also means that you can inject your own version of any dependency that you like.

Rainbow has extensive unit tests and hopefully easy to understand code, which serve as its living documentation. As of now, Rainbow has 90% test coverage and 220 tests. If you can't find what you're looking for you can find me on Twitter (@kamsar) or on Sitecore Community Slack.