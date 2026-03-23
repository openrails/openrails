# Open Rails Code Guidelines

## Logging

- Use the most appropriate [logging severity](#logging-severity)
- Use the most appropriate logging method:
  - When parsing files, use [file format logging methods](#file-format-logging-methods)
  - Otherwise, use [general logging methods](#general-logging-methods)
- Use [simple past tense](https://en.wikipedia.org/wiki/Simple_past) with the verb at the front; e.g. "Ignored missing texture file" instead of "Missing texture file was ignored"
- Do not make reference to the user or Open Rails itself
- Include the most relevant source file and line number at the end of the message (_file format logging methods_ do this for you)

### Logging severity

- Error: Fatal problem where continuing is not possible (only used in very limited places)
- Warning: Resolved problem or manageable bad data (e.g. data that is missing/duplicate/unknown/unexpected)
  - Ignored missing ...
  - Ignored duplicate ...
  - Skipped unknown ...
  - Expected ...; got ...
- Information: No problem but is notable (e.g used default for important but commonly missing data)
  - Used default for ...

### File format logging methods

When parsing files, use the specific logger methods below to automatically include accurate source information (file name, line number):

- When using `JsonReader`, use `JsonReader.TraceWarning` and `JsonReader.TraceInformation`
- When using `SBR.Open`, use `SBR.TraceWarning` and `SBR.TraceInformation`
- When using `STFReader`, use `STFException.TraceWarning` and `STFException.TraceInformation` (static methods)

### General logging methods

- For application start-up (adjacent to existing code), use [Console.Write](https://learn.microsoft.com/en-gb/dotnet/api/system.console.write) and [Console.WriteLine](https://learn.microsoft.com/en-gb/dotnet/api/system.console.writeline)
- For exceptions when loading a file, use [Trace.WriteLine](https://learn.microsoft.com/en-gb/dotnet/api/system.diagnostics.trace.writeline) as follows:
  ```csharp
  try
  {
      // Something that might break
  }
  catch (Exception error)
  {
      Trace.WriteLine(new FileLoadException(fileName, error));
  }
  ```
- For exceptions in other cases, use [Trace.WriteLine](https://learn.microsoft.com/en-gb/dotnet/api/system.diagnostics.trace.writeline) as follows:
  ```csharp
  try
  {
      // Something that might break
  }
  catch (Exception error)
  {
      Trace.WriteLine(error);
  }
  ```
- Otherwise, use [Trace.TraceError](https://learn.microsoft.com/en-gb/dotnet/api/system.diagnostics.trace.traceerror), [Trace.TraceWarning](https://learn.microsoft.com/en-gb/dotnet/api/system.diagnostics.trace.tracewarning), [Trace.TraceInformation](https://learn.microsoft.com/en-gb/dotnet/api/system.diagnostics.trace.traceinformation), for example:
  ```csharp
  Trace.TraceError("Player locomotive {1} cannot be loaded in {0}", conFileName, wagonFileName);
  Trace.TraceWarning("Skipped unknown texture addressing mode {1} in shape {0}", shapeFileName, addressingMode);
  Trace.TraceInformation("Ignored missing animation data in shape {0}", shapeFileName);
  ```

## Translations

- Use translations for all user-visible text
- Use English (United States) for all translations
- Use [entire sentences](#entire-sentences) appropriately
- Use [nested values](#nested-values) appropriately
- Use [context](#context) appropriately
- Use [dynamic values](#dynamic-values) appropriately

### Entire sentences

Not all languages put words in the same order, or even have a concept of "words". For this reason, only translate entire sentences. Never concatenate multiple translations to form a longer string of text. [Nested values](#nested-values) are allowed under specific conditions.

### Nested values

In a formatted string, each placeholder is replaced with a nested value, which appears adjacent of the formatting string. For this reason, delimiters are needed to separate nested values from the surrounding text. Otherwise, it does not meet the [entire sentences](#entire-sentences) requirement. For example:

- **Allowed:** Opening "{0}" doors...
- **Allowed:** Opening doors on: {0}
- **Not allowed:** Opening {0} doors...
- **Not allowed:** Opening doors on {0}

### Context

Not all sentences or values which must be translated will make sense when seen on their own. For this reason, additional context is added in the code to provide translators with the necessary information to correctly translate them. The context is only shown to translators and does not form part of the translation itself.

### Dynamic values

Dynamic values are those that depend on the state of the application and can be used stand-alone or as [nested values](#nested-values). They may or may not use [context](#context).

- For boolean values, use two static values
- For enumerated values, use `GetStringAttribute` and `GetParticularStringAttribute` as attributes and static methods
- For numeric values with units, use `Format...()` methods from static class `FormatStrings`

### Examples

- For a context-free static sentence:
  ```csharp
  Catalog.GetString("Opening doors on left...");
  ```
- For a context-free static sentence with a nested value:
  ```csharp
  Catalog.GetStringFmt("Opening doors on: {0}", ...);
  // Shortcut for: string.Format(Catalog.GetString("Opening doors on: {0}"), ...);
  ```
- For a context-sensitive static sentence:
  ```csharp
  Catalog.GetParticularString("Doors", "Opening on left...");
  ```
- For a context-sensitive static sentence with a nested value:
  ```csharp
  // No shortcut available, so use:
  string.Format(Catalog.GetParticularString("Doors", "Open on: {0}"), ...);
  ```
- For a context-free boolean dynamic value:
  ```csharp
  return doorsOpen ? Catalog.GetString("Doors open") : Catalog.GetString("Doors closed")
  ```
- For a context-sensitive boolean dynamic value:
  ```csharp
  return doorsOpen ? Catalog.GetParticularString("Doors", "Open") : Catalog.GetParticularString("Doors", "Closed")
  ```
- For a context-free enumerated dynamic value:
  ```csharp
  enum Actions
  {
      [GetString("Doors Left Open")]DoorsLeftOpen,
      [GetString("Doors Left Close")]DoorsLeftClose,
  }
  return GetStringAttribute.GetPrettyName(action);
  ```
- For a context-sensitive enumerated dynamic value:
  ```csharp
  enum Actions
  {
      [GetParticularString("Doors", "Left Open")]DoorsLeftOpen,
      [GetParticularString("Doors", "Left Close")]DoorsLeftClose,
  }
  return GetParticularStringAttribute.GetParticularPrettyName("Doors", action);
  ```
- For a numeric unit dynamic value:
  ```csharp
  // There are about 30 of these `FormatStrings.Format...()` functions available:
  return FormatStrings.FormatDistance(distanceM, isMetric)
  ```
