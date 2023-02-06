.. _malfunction:

**********************
In Case Of Malfunction
**********************

Introduction
============

When you have an issue with Open Rails (ORTS), no matter what it is, the OR 
development team is always thankful for reports of possible bugs. Of course, 
it is up to the developers to decide if something is a real bug, but in any 
case your reporting of it is an important step in helping the development 
team to improve Open Rails.

Overview of Bug Types
=====================

The development team uses two ways of keeping track of bugs:

1. So called "Maybe-Bugs" are reported in a simple forum post: see next 
   paragraph for links. This is done in order to give developers a chance to 
   filter out problems caused by circumstances the development team cannot 
   control such as corrupted content.
2. Decided Bugs are issues a developer has looked at and has found to be a 
   real issue in the program code of Open Rails. They are reported at our Issue 
   Tracker at https://github.com/openrails/openrails/issues/.

Maybe-Bugs
==========

If you find an issue with Open Rails you should first file a Maybe-Bug report 
at any of the following forums monitored by the Open Rails development team:

- `Elvas Tower <http://www.elvastower.com/>`_, "Maybe it's a bug" section of 
  the Open Rails  sub-forum. This is the forum that is most frequently checked 
  by the OR development team;
- `TrainSim.com <http://www.trainsim.com/>`_, "Open Rails discussion" section 
  of the Open Rails  sub-forum
- ...more forums may be added in the future

A Maybe-Bug report consists of a simple post in a new topic in the forum. The 
title of the topic should be of the form "Open Rails V#### Bug: +++++", where 
#### is the version number of the Open Rails release you are having problems 
with, and +++++ is a quick description of the problem you are having. This 
format aids the developers in getting a quick idea of the issue being reported.

The first post in this newly started topic should give further information on 
your problem: Start out with exactly what problem you are getting, describing 
it in narrative and supplementing this description with screenshots, error 
messages produced by Open Rails, and so on.

Next give a clear indication of the content you were using (that is: Route, 
Activity, Path, Consist, Locomotive and Rolling Stock; whatever is 
applicable), whether it is freeware or payware, what the exact name of the 
downloaded package was and where it can be obtained. Of course, posting a 
download link to a trustworthy site or directly attaching files to the post 
also is OK.

Continue with an exact description of what you were doing when the problem 
arose (this may already be included in the first paragraph, if the problem is 
train-operation-related). Again, screenshots etc. can be helpful to better 
describe the situation.

Lastly, take a look at your desktop for a text file entitled 
``OpenRailsLog.txt``. Upload and attach this file to the end of your post. This 
is very important as the log file contains all relevant program data the user 
has no chance to ever see, and thus it is one of the most important sources 
of information for the developer trying to solve your problem.

Once your post has been submitted, keep adding further information only in 
additional posts, in order to avoid the risk of people not noticing your 
edits. Also, please be patient with developers responding to your report. 
Most forums are checked only once a day, so it may take some time for a 
developer to see your report.

Important: The more information a developer gets from the first post, the 
quicker he will be able to locate, identify and eventually resolve a bug. On 
the other hand, reports of the form, "I have problem XYZ with recently 
installed Open Rails. Can you help me?" are of little use, as all required 
information must be asked for first.

Important: Please do not rush to report a Decided Bug on the Bug Tracker 
before a developer has declared your problem a real bug!

The above description is available in a condensed "checklist" form below.

Decided bugs
============

Many bug reports never even make it to the status of a Decided Bug, being a content or user error. Some Maybe-Bugs, however, will eventually 
be declared Decided Bugs. Such secured bugs should be reported at our Bug 
Tracker, when the developer taking the report asks you to.

The Open Rails Issue Tracker is found at `<https://github.com/openrails/openrails/issues/>`_, 
following the "Report a bug" link in the upper half to the right of the 
screen. You will need to register at Launchpad in order to be able to report 
a bug.

Once that is done, follow the steps the software takes you through: In 
"Summary" copy and paste the quick description of the bug you also entered as 
a forum thread name for the Maybe-Bug report.

Next, look through the list of topics Launchpad thinks your bug may be 
related to -- maybe your issue has already been reported?

If you cannot relate to any of the suggested bugs, click the "No, I need a 
new bug report" button and continue.

In the "Further Information" field, enter the same info you also gave in the 
Maybe-Bug report (copy and paste). Screenshots may need to be added as 
attachments, and you will also need to re-upload the ``OpenRailsLog.txt`` file. 
Do not forget to include all info you added in additional posts to the 
original Maybe-Bug report, and also add a link to the latter at the bottom of 
the "Further Information" field.

Once your bug has been submitted, keep adding further information only in 
additional posts, in order to avoid the risk of developers missing the 
additional info.

The above description is available in a condensed "checklist" form below.

Important: Do not say "All information is included in the linked thread" as 
skimming through a thread for the crucial bit of information is a really 
annoying task. Instead, please provide a concise, but complete summary of the 
Maybe-Bug thread in the "Further Information" field.

Important: Please do not rush to report a Decided Bug on our Bug Tracker 
before a developer has declared your Maybe-Bug a real bug!

Additional Notes
================

Please do not post feature requests as a Maybe-Bug to the Issue Tracker on 
GitHub!

Please do not report the same bug multiple times, just because the first 
report did not get attention within a short time. Sorting out the resulting 
confusion can slow things down even more.

Please do not report Bugs directly to the Bug Tracker when you are not 100% 
sure it's a real, significant bug, or have not been asked to do so.

Don't be offended by bug statuses - they often sound harsher than they really 
mean, like "Invalid".

Don't expect a speedy response in general -- issues will get looked at as and 
when people have the time.

Be prepared to expand upon the initial report -- it is remarkably easy to 
forget some crucial detail that others need to find and fix your bug, so 
expect to be asked further questions before work can begin.

Try to avoid comments that add no technical or relevant detail -- if you want 
to record that the bug affects you, Launchpad has a dedicated button at the 
top: "Does this bug affect you?".

If you wish to follow the progress of someone else's bug report and get 
e-mail notifications, you can subscribe to bug mail from the sidebar.

Summary: Bug Report Checklists
==============================

"Maybe-Bug"

- New topic in appropriate sub-forum
- Topic Title: "Open Rails V<version> Bug: <description>"
- Description of problem, supplemented by screenshots etc.
- Content used (Route, Activity, Path, Consist, Locomotive & Rolling Stock; 
  choose applicable); Freeware / Payware?; Package name & download location / 
  download link
- Narrative of actions shortly before & at time of problem, supplemented by 
  screenshots etc.
- Attach log file (Desktop: ``OpenRailsLog.txt``)
- Add further info only in additional posts
- Be patient

Decided Bug

- Report to Bug Tracker only if asked to do so
- https://github.com/openrails/openrails/issues/
- "Summary": Description from the topic title of the Maybe-Bug report
- Look for similar, already reported bugs
- Condense whole Maybe-Bug thread into "Further information" field
- Add link to original Maybe-Bug report
- Re-upload and attach OpenRailsLog.txt & explanatory screenshots etc.
- Add further info only in additional posts
- Be patient

Issue Status in GitHub
======================

- **New** -- this is where all bugs start. At this point, the bug has not been 
  looked at by the right people to check whether it is complete or if more 
  details are needed.
- **Incomplete** -- a member of the Open Rails teams has decided that the bug 
  needs more information before it can be fixed. The person who created the bug 
  report does not have to be the one to provide the extra details. A bug 
  remaining incomplete for 60 consecutive days is automatically removed.
- **Opinion** -- the bug has been identified as an opinion, meaning that it isn't 
  clear whether there is actually a bug or how things should be behaving.
- **Invalid** -- a member of the team believes that the report is not actually a 
  bug report. This may be because Open Rails is working as designed and 
  expected or it could just be spam. The bug may be put back to the new state 
  if further information or clarity is provided in comments.
- **Won't Fix** -- a member of the team has decided that this bug will not be 
  fixed at this time. If the bug report is a "feature request", then they have 
  decided that the feature isn't desired right now. This status does not mean 
  something will never happen but usually a better reason for fixing the bug or 
  adding the feature will be needed first.
- **Confirmed** -- a member of the team has been able to experience the bug as 
  well, by following the instructions in the bug report.
- **Triaged** -- a member of the team has assigned the importance level to the 
  bug or has assigned it to a specific milestone. Bugs generally need to get to 
  this state before the developers will want to look at them in detail.
- **In Progress** -- one or more members of the team are currently planning to or 
  actually working on the bug report. They will be identified by the assignee 
  field.
- **Fix Committed** -- the fix for the bug report or feature request has been 
  completed and checked in to the source control system, Subversion. Once 
  there, the fix will usually appear in the next experimental release.
- **Fix Released** -- The code containing the bug fix has been released in an 
  official release. 

Disclaimer
==========

Having posted a bug report in a forum or on GitHub does not generate any 
obligation or liability or commitment for the OR development team to examine 
and fix the bug. The OR development team decides whether it will examine and 
fix the bug on a completely voluntary and autonomous basis.
