.. _intro:

************
Introduction
************

What is Open Rails?
===================

Open Rails software (OR) is a community developed and maintained project
from openrails.org. Its objective is to create a new transport simulator
platform that is first, compatible with routes, activities, consists,
locomotives, and rolling stock created for Microsoft Train Simulator (MSTS);
and secondly, a platform for future content creation freed of the constraints
of MSTS (in this manual MSTS means MSTS with MSTS Bin extensions, if not
explicitly stated in a different way).

Our goal is to enhance the railroad simulation hobby through a
community-designed and supported platform built to serve as a lasting
foundation for an accurate and immersive simulation experience. By making
the source code of the platform freely available under the GPL license,
we ensure that OR software will continually evolve to meet the technical,
operational, graphical, and content building needs of the community. Open
architecture ensures that our considerable investment in building accurate
representations of routes and rolling stock will not become obsolete. Access
to the source code eliminates the frustration of undocumented behavior and
simplifies understanding the internal operation of the simulator without the
time-consuming trial and error-prone experimentation typically needed today.

Open Rails software is just what the name implies -- a railroad simulation
platform that's open for inspection, open for continuous improvement, open
to third parties and commercial enterprises, open to the community and, best 
of all, an open door to the future.

About Open Rails
================

To take advantage of almost a decade of content developed by the train 
simulation community, Open Rails software is an independent game 
platform that has backward compatibility with MSTS content.  By 
leveraging the community's knowledge base on how to develop content for 
MSTS, Open Rails software provides a rich environment for both community 
and payware contributors.

The primary objective of the Open Rails project is to create a railroad 
simulator that will provide *true to life* operational experience. The 
Open Rails software is aimed at the serious train simulation hobbyist; 
someone who cares about locomotive physics, train handling, signals, AI 
behavior, dispatching, and most of all running trains in a realistic, 
prototypical manner. While the project team will strive to deliver an 
unparalleled graphical experience, *eye candy* is not the primary 
objective of Open Rails software.

By developing a completely new railroad simulator, Open Rails software 
offers the potential to better utilize current and next generation 
computer resources, like graphics processing units (GPUs), multi-core 
CPUs, advanced APIs such as PhysX, and widescreen monitors, among many 
others. The software is published so that the user community can 
understand how the software functions to facilitate feedback and to 
improve the capabilities of Open Rails software.

Open Rails is published under the GPL license which is "copyleft"  to ensure 
that the source code always remains publicly available.

Does Open Rails Need MSTS to Run?
=================================

This is not a correctly set question. Open Rails is able to run a vast 
majority of MSTS content (routes, trains, activities). Open Rails does 
not need MSTS executable files (e.g. .exe or .dll files), neither does 
it need .ini files.

However, if the MSTS content uses content files originally delivered with 
MSTS, such as tracks or general sounds (this applies in particular to 
routes), obviously to run such content OR needs such files.

If instead (and there are examples of this) the MSTS content does not use 
such original content files, again obviously OR does not need original 
MSTS files. Read here for further detail.

In both cases, MSTS content files (original and not) must be organized in an 
MSTS-compatible folder structure. Such a structure is described here. In 
this manual such a folder structure will be called an *MSTS 
installation* for clarity, even if this wording is not completely 
correct. 

A proof that Open Rails itself does not need an MSTS installation at all to 
run is e.g. this route.

Community
=========

At the present time, Open Rails software is offered without technical 
support. Therefore, users are encouraged to use their favorite train 
simulation forums to get support from the community.

- `Train-Sim.Com <http://forums.flightsim.com/vbts/>`_
- `UK Train Sim <http://forums.uktrainsim.com/index.php>`_
- `Elvas Tower <http://www.elvastower.com/forums/index.php?/index>`_

For users interested in multiplayer sessions, a forum is set up for you to 
seek and announce hosting sessions: http://www.tsimserver.com.

The Open Rails team is NOT planning on hosting a forum on the Open Rails 
website. We believe that the best solution is for the current train 
simulation forum sites to remain the destination for users who want to 
discuss topics relating to Open Rails software. The Open Rails team 
monitors and actively participates in these forums.

Highlights of the Current Version
=================================

Focus on Compatibility
----------------------

With this release the announced goal has been reached to make as much of the 
existing MSTS content as possible run in Open Rails. The development 
team's initial focus has been to provide a fairly complete visual 
replacement for MSTS that effectively builds on that content, achieving 
all the compatibility that is worthwhile, at the same time delivering a 
system which is faster and more robust than MSTS.

Focus on Operations
-------------------

Release 1.0 clears the way to improving on MSTS in many ways which can be 
summed up as moving from Foundation to Realism and eventually to 
Independence, and already includes features that are beyond MSTS. 
Non-player trains can already have a first release movement orders (i.e. 
pickups, drop offs) based on files in MSTS format. Deadlocks between 
player and non-player trains, that are frequent in MSTS, have been 
practically eliminated.

Focus on Realistic Content
--------------------------

The physics underlying adhesion, traction, engine components and their 
performance are based on a world-class simulation model that takes into 
account all of the major components of diesel, electric and steam 
engines. This includes elements like friction resistance in curves and 
tunnels, a very sophisticated steam locomotive physics modeling, many 
optional curves to define precise locomotive physics, coupler forces and 
much more. It is foreseen that beyond release 1.0 Open Rails will 
approach the level of physics realism only available in professional 
simulators.

Existing models that do not have the upgraded Open Rails capabilities 
continue, of course, to perform well.

In the package of this version also ancillary programs (*tools*) are 
delivered, including:

- Track Viewer: a complete track viewer and path editor
- Activity Editor: a draft new activity editor to move beyond MSTS
- Timetable Editor: a tool for preparing Timetables
