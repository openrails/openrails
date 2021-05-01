# Contributing to Open Rails

This document will introduce you to a number of ways you can contribute to Open Rails, and how we expect the process to go - both from your side and our side.

## Discussion

Please see the [Community](http://openrails.org/share/community/) page on our website for details of the forums where Open Rails discussion happens.

## Reporting a bug

If you've found a bug in Open Rails, please report it in [our bug tracker on Launchpad](https://bugs.launchpad.net/or).

## Making a suggestion

If you've got an idea for Open Rails, please report it in [our road-map on Trello](https://trello.com/b/DS2h3Pxc/open-rails-roadmap).

## Writing code

### Choosing what to work on

You are free to make any modifications to the Open Rails code that you like; that's how open source works. However, we won't necessarily include all changes in the official version, so here are the ways to ensure that what you're working on will be accepted:

* If you'd like to work on something that is already known about and accepted, please check through [the confirmed bugs](https://bugs.launchpad.net/or/+bugs?orderby=-importance&field.status%3Alist=TRIAGED) and [accepted feature requests (anything except the first two columns)](https://trello.com/b/DS2h3Pxc/open-rails-roadmap).
* If you don't see anything you'd like to work on in the _confirmed bugs_ or _accepted feature requests_, please reach out to us in the [Elvas Tower forums](http://www.elvastower.com/forums/index.php?/forum/299-open-rails-development-testing-and-support/). You can also [report a bug](https://bugs.launchpad.net/or/+filebug), [make a suggestion for the road-map](https://trello.com/c/zznjApL8/102-click-me-to-read-how-this-works), or [create a blueprint](https://blueprints.launchpad.net/or/+addspec) yourself. Blueprints should only be used by seasoned Open Rails developers. In each case, we'll get back to you on whether we think it is appropriate for inclusion (see [how bugs and feature requests are accepted](#how-bugs-and-features-are-accepted)).

When choosing what to work on from the road-map, if multiple things are interesting to you, we would prefer that you choose the item with the highest priority (see list below). This is only a guideline for helping to choose, though, so if you do want to work on something with a low priority, that's fine - all we ask is that you let us know in the [Elvas Tower forums](http://www.elvastower.com/forums/index.php?/forum/299-open-rails-development-testing-and-support/).

1. The next minor release (e.g. 1.4)
2. Current major release (e.g. 1.x)
3. Next major release (e.g. 2.x)
4. Future

If you're unsure what you could contribute to in the code, and nothing looks interesting in the _confirmed bugs_ and _accepted feature requests_, please get in touch on the [Elvas Tower forums](http://www.elvastower.com/forums/index.php?/forum/299-open-rails-development-testing-and-support/), giving us some idea of your experience and interests, and we'll do our best to find something for you.

### General requirements

All of the main Open Rails code is C# and your contribution is expected to also be in C#. We're currently using [version 7.3 of C#](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7-3), so please take advantage of these features.

Code is expected to follow the [Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/) throughout, especially the [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines), with few exceptions:

* Structures, fields, and enums defining file format components may be named exactly as in the file format
* Public and protected fields are allowed, although care must be taken with public fields

Code style (placement of braces, etc.) is expected to follow the default Visual Studio rules; the [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) provides a good basis for many aspects of this.

### Architecture requirements

Code should be well structured, with small methods performing a single key task (indicated by their name), and larger complex operations formed through calls to multiple smaller methods.

Code architecture, especially for new features, should be consistent with the [Open Rails Architecture](Architecture.md).

### Physics requirements

All fields, parameters and local variables containing real world measurements (e.g. length, area) must have the unit as a suffix. Measurements must be in [SI units](https://en.wikipedia.org/wiki/International_System_of_Units) unless you have an exception granted by the Open Rails Management Team.

All physical formula used must be documented to identify the source of the formula, and ideally a one-line summary of what it does/is for.

All fixed values used in formula must be placed in a constant, with a name and unit suffix, for readability.

### Multi-threading requirements

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

### Getting your code accepted

Your code should be fixing exactly one bug or adding a single new feature; mixing multiple bug fixes or new features makes it harder to review your changes and risks them not being accepted.

### Testing and Unstable Versions

Changes to the Git "master" branch are selected by peer review and the branch is automatically published as the "Testing Version" every Friday.
Changes to the Git "unstable" branch are automatically selected and published as the "Unstable Version" every 15 minutes.
Your changes should always start from the "master" branch and not the "unstable" branch.

### Submitting your code

When you're done writing code, you should make a pull request on GitHub or a merge request on Launchpad. The title and description of the requests should clearly and concisely indicate what bug or feature you've implemented and you will need to include links to whichever of the following are appropriate:

* Bug report
* Road-map card
* Blueprint

## How bugs and features are accepted

### Bug reports

A member of [our developer team](https://launchpad.net/~ordevs/+members) will mark the bug as "Triaged" once they have confirmed that the problem is real and needs fixing.

### Road-map cards

We highly recommend that a [forum thread is created](http://www.elvastower.com/forums/index.php?/forum/256-developing-features/) with each feature request, so that the community may discuss it and flag up any potential issues. We typically allow at least a week for discussion and identification of any issues.

A member of [our management team](https://launchpad.net/~orsupervisors/+members) will read the request and follow the forum discussion being had by the community, classify it by type (using labels), and place it into an appropriate list in Trello. In the rare event that we do not agree with the feature being added to Open Rails, it will be placed in the "Not planned at this time" list and a comment added explaining why.

### Blueprints

We highly recommend that a [forum thread is created](http://www.elvastower.com/forums/index.php?/forum/256-developing-features/) with each feature request, so that the community may discuss it and flag up any potential issues. We typically allow at least a week for discussion and identification of any issues.

A member of [our management team](https://launchpad.net/~orsupervisors/+members) will read the request and follow the forum discussion being had by the community, and approve its direction if appropriate.

## Reviewing pull requests

If you are reviewing someone elses code for Open Rails, you will need to ensure that they have met the above "Writing code" guidelines as best as possible. This will necessitate, at minimum:

* Check for linked bug report or feature request
* Check bug report is triaged, and feature request is approved
  * For a bug report, it should have status "Triaged"
  * For a road-map card, it should not be in the first two columns ("Unsorted" and "Not planned")
  * For a blueprint, it should have direction "Approved"
* Read through all of the changes to the code
* Check that all new code follows the requirements:
  * General (including naming)
  * Architecture
  * Physics
  * Multi-threading
* Be sure that all of the changes are necessary
* Be sure that no changes are missing
* Be on the lookout for data being access across threads

### Leeway when reviewing

Although we'd like all code written to exactly match the guidelines given in this document, that is not practical - not least because nobody is likely able to remember every single detail of the guidelines at any one time, whether writing or reviewing code. Therefore, there is always going to be some leeway between the guidelines and what is accepted into Open Rails.

You should take special care when reviewing first-time and new contributors, to ensure that we accept their contribution even when it does not strictly conform to the guidelines, as this will encourage them to continue contributing.

For all contributions that deviate from the guidelines, there are a few approaches you can take:

* Politely and constructively suggest changes on the pull request (if possible, include the desired code)
* Make the changes yourself (GitHub provides instructions to push changes to other people's pull requests)
* Accept the code as-is, leaving a note for how to improve for the next pull request

It is expected that most contributors will quickly correct their code based on feedback, either in the same pull request or subsequent ones, depending on the path taken above. However, if a contributor continues to not meet the same part of the guidelines, you are free to become more strict with them - it's still helpful to suggest the corrected code, but do not feel obliged to spend time helping the same person with the same part of the guidelines repeatedly.

## Merging pull requests

If you are merging a pull request, you will need to ensure that the merge commit message contains links to whichever of the following are appropriate:

* Bug report
* Road-map card
* Blueprint

These links will be used by automated and manual processes to check on the progress of the project.
