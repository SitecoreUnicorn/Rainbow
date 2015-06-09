# Rainbow

An advanced serialization library for Sitecore 7 and above.

Rainbow consists of multiple components:

* The core Rainbow project contains largely interface glue to abstract away how something is serialized or deserialized, how it's stored, etc.
* Rainbow.Storage.Yaml implements serializing and deserializing items using a YAML-based format. This format is ridiculously easier to read and merge than standard Sitecore serialization format, lacking any content length attributes, supporting any type of newline characters, and supporting pretty-printing field values (e.g. multilists, layout) for simpler merging when conflicts occur.
* Rainbow.Storage.Sc implements a data store using the Sitecore database. This can be used to read and write items to Sitecore using the data store interface.
* Rainbow.Diff implements a generic comparison between data stores. You can use this to generate a list of differences either to sync or display.

Rainbow will be a core component of [Unicorn](https://github.com/kamsar/Unicorn) v3, providing the serialization services for the future.