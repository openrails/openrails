.. _intro:

************
Introduction
************

What is Open Rails?
===================

Open Rails software (OR) is a community developed and maintained project
from `openrails.org <http://www.openrails.org/>`_. Its objective is to create a new transport simulator
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

Open Rails is published under the GPL license which is "copyleft"[1]_ to ensure 
that the source code always remains publicly available.

.. [1] https://gnu.org/copyleft

.. _intro-MSTSneeded:

Does Open Rails Require You to Have MSTS Installed?
===================================================

No, it is not *required* by the Open Rails software itself. However. a great deal 
of the content accessed by OR includes files originally delivered with MSTS 
(e.g., tracks or general sounds). These files must be obtained from a properly 
licensed installation of MSTS.

There are examples where no MSTS content is used (often payware) and in such 
cases Open Rails does not require MSTS to be installed. Read :ref:`here 
<compatibility-folders>` for further 
detail.

In all cases, all content files (original or MSTS) must be organized in an 
MSTS-compatible folder structure. Such a structure is described :ref:`here 
<compatibility-folders>`. In this manual such a folder structure will be 
called an *MSTS installation* for convenience, even if this wording is not 
completely correct. 

A proof that Open Rails itself does not need an MSTS installation at all to 
run is `this route <http://www.burrinjuck.coalstonewcastle.com.au/route/route-install/>`.

Community
=========

Open Rails software is offered without technical support. Users are encouraged 
to use their favorite train simulation forums to get support from the community. 
We suggest:

- `Train-Sim.Com <http://forums.flightsim.com/vbts/>`_
- `UK Train Sim <http://forums.uktrainsim.com/index.php>`_
- `Elvas Tower <http://www.elvastower.com/forums/index.php?/index>`_

For users interested in multiplayer sessions, a forum is set up for you to 
seek and announce hosting sessions: http://www.tsimserver.com.

Raildriver Support
==================

Open Rails offers native support for the RailDriver Desktop Train Cab 
Controller. Instructions for setting up RailDriver for Open Rails are included 
in the Installation Manual that is included with the Open Rails Installer, or it 
can be downloaded separately from the Open Rails website.

Highlights of the Current Version
=================================

Focus on Compatibility
----------------------

With Release 1.0 the published goal was reached to make as much of the 
existing MSTS content as possible run in Open Rails. The development 
team's initial focus has been to provide a fairly complete visual 
replacement for MSTS that effectively builds on that content, achieving 
all the compatibility that is worthwhile, at the same time delivering a 
system which is faster and more robust than MSTS.

Focus on Operations
-------------------

Release 1.1 cleared the way to improving on MSTS in many ways which can be 
summarized as moving from Foundation to Realism and eventually to 
Independence. That release already included features that are beyond MSTS; 
non-player trains can have movement orders (i.e. pickups, drop offs) based 
on files in MSTS format. The player can change the driven train. Multi-user 
operation has also been available for some time. 

.. _intro-reality:

Focus on Realistic Content
--------------------------

The physics underlying adhesion, traction, engine components and their 
performance are based on a world-class simulation model that takes into 
account all of the major components of diesel, electric and steam 
traction. Release 1.2 refines elements such as braking, where braking friction 
now varies with speed, over-braking which now leads to skidding and wheel-slip 
is now modelled for steam locos too. 

Existing models that do not have the upgraded Open Rails capabilities 
continue, of course, to perform well.

In Version 1.x releases, ancillary programs (*tools*) are also 
delivered, including:

- Track Viewer: a complete track viewer and path editor
- Timetable Editor: a tool for preparing :ref:`Timetables <timetable>`
