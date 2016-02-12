.. _roadmap:

*****************
Plans and Roadmap
*****************

Here are some highlights that the community can expect from the Open Rails team 
after v1.0. A more complete roadmap can be found at 
https://launchpad.net/or/+milestones

User Interface
==============

A new Graphical User Interface (GUI) within the game. 

Operations
==========

In addition to the new Timetable concept described in this document, some 
further improvements are planned: 

- Extended ability to customize signals to accommodate regional, geographic, or 
  operational differences
- Ability to use mixed signal environments -- from dark territory to fully 
  automatic in-cab train control within the same route 
- Specifying random variations for AI trains in consist and delays. 
- Specifying separate speed profiles for passenger or freight trains. 
- AI trains which can split or combine 
- A schedule for AI trains which can depend on other trains (e.g. wait a 
  limited time). 

Open Rails Route Editor
=======================

Now that the project is moving beyond MSTS, we are at last able to specify the 
Open Rails Route Editor. This will free us from the constraints and fragility 
of the MSTS tool. The editor will, of course, use GIS data, edit the terrain 
and allow objects to be placed and moved. 

In particular, it will be possible to lay both track pieces and procedural 
track. The procedural track may bend up and down to follow the contours of 
the land and twist to build banked curves and spirals. There will be support 
for transition curves and it will be easy to lay a new track parallel to an 
existing one. 

The new Route Editor will not be backwards-compatible with MSTS routes. It 
will work with Open Rails routes and there will be a utility to create an 
Open Rails route from an MSTS route. 

No timetable is available for this work.
