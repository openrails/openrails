# Open Rails Code Guidelines

## Logging

- Use the most appropriate [logging severity](#logging-severity)
- Use the most appropriate logging method:
  - When parsing files, use [file format logging methods](#file-format-logging-methods)
  - Otherwise, use [general logging methods](#general-logging-methods)
- Use [simple past tense](https://en.wikipedia.org/wiki/Simple_past) with the verb at the front; e.g. "Ignored missing texture file" instead of "Missing texture file was ignored".
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
