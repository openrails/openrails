# Contributing to Open Rails

This document will introduce you to a number of ways you can contribute to Open Rails, and how we expect the process to go - both from your side and our side.

## Discussion

Please see the [Community](http://openrails.org/share/community/) page on our website for details of the forums where Open Rails discussion happens.

## Reporting a bug

If you've found a bug in Open Rails, please report it in [our bug tracker on Launchpad](https://bugs.launchpad.net/or).

## Making a suggestion

If you've got an idea for Open Rails, please report it in [our road-map on Trello](https://trello.com/b/DS2h3Pxc/open-rails-roadmap).

## Writing code

You are free to make any modifications to the Open Rails code that you like; that's the benefit of open source. However, if you would like your contribution to be included in the official version, please be sure that you are [fixing a confirmed bug](https://bugs.launchpad.net/or/+bugs?orderby=-importance&field.status%3Alist=TRIAGED) or [implementing an accepted feature request (anything except the first two columns)](https://trello.com/b/DS2h3Pxc/open-rails-roadmap).

We will also treat your contribution according to the following bug and feature prioritization:

1. The next minor release (e.g. 1.4)
2. Current major release (e.g. 1.x)
3. Next major release (e.g. 2.x)

If you're unsure what you could contribute to in the code, please get in touch on the [Elvas Tower](http://www.elvastower.com/) forums, giving us some idea of your experience and interests, and we'll do our best to find something for you.

### General requirements

All of the main Open Rails code is C# and your contribution is expected to also be in C#. We're currently using [version 7.3 of C#](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7-3), so please take advantage of these features.

Code is expected to follow the [Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/) throughout, especially the [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines), with few exceptions:

* Structures, fields, and enums defining file format components may be named exactly as in the file format
* Public and protected fields are allowed, although care must be taken with public fields

Code style (placement of braces, etc.) is expected to follow the default Visual Studio rules; the [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) provides a good basis for many aspects of this.

Code should be well structured, with small methods performing a single key task (indicated by their name), and larger complex operations formed through calls to such methods.

### Physics requirements

All fields, parameters and local variables containing real world measurements (e.g. length, area) must have the unit as a suffix. Measurements must be in [SI units](https://en.wikipedia.org/wiki/International_System_of_Units) unless you have an exception granted by the Open Rails Management Team.

All physical formula used must be documented to identify the source of the formula, and ideally a one-line summary of what it does/is for.

All fixed values used in formula must be placed in a constant, with a name and unit suffix, for readability.

### Multi-threading

Open Rails is a multi-threaded application, which presents some additional complexity. There are four key threads to be aware of:

* Loader
* Updater
* Render
* Sound

Data that is operated only on one thread for its lifetime does not need special attention. However, any data that is operated on by multiple threads - even if only one thread is writing - needs special care and attention.

For each object stored in a field or property that is accessed from multiple threads, the root, you must:

* Never modify the contents of the objects within the root (such as adding or removing items from a List<T>)
* Always copy the root object into a local variable before doing anything else with it
* Update the root object by (as above) copying into a local, cloning/making a new version from the old version, and finally storing into the root
* If multiple threads can update the root, the final store into root must be done using an interlocked compare-and-exchange with a loop in case of failure

If you are in any doubt about the use of data by multiple threads, or your implementation of the above rules, please ask in the [Elvas Tower](http://www.elvastower.com/) forums.

### Pull requests

Pull requests should contain exactly one bug fix or new feature; do not mix multiple bug fixes, multiple features, or bug fixes and features together in any way. The bug report or feature request must be linked from the pull request.

## Reviewing pull requests

If you are reviewing someone elses code for Open Rails, you will need to ensure that they have met the above "Writing code" guidelines as best as possible. This will necessitate, at minimum:

* Check for linked bug report or feature request
* Check bug report is triaged, and feature request is approved
* Read through all of the changes to the code
* Be sure that all of the changes are necessary
* Be sure that no changes are missing
* Check that any new classes, fields, methods, etc., follow the naming guidelines
* Be on the lookout for data being access across threads
