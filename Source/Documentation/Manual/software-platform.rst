.. _software-platform:

****************************
Open Rails Software Platform
****************************

Architecture
============

To better understand how the Open Rails game operates, performs, and functions, 
the architecture diagram below lays out how the software code is organized. The 
architecture of the Open Rails software allows for modular extension and 
development, while providing standardized methods to customize the simulation 
experience.

.. note:: Please note that this diagram includes many capabilities and 
          functions that are yet to be implemented.

.. image:: images/software-platform.png

Open Rails Game Engine
======================

The Open Rails software is built on Microsoft's XNA game platform using XNA 
Framework 3.1 and .NET Framework 3.5 SP1. Source code is developed in 
Microsoft's Visual C# programming language.

The XNA Framework is based on the native implementation of .NET Compact 
Framework for Xbox 360 development and .NET Framework on Windows. It includes 
an extensive set of class libraries, specific to game development, to promote 
maximum code reuse across target platforms. The framework runs on a version 
of the Common Language Runtime that is optimized for gaming to provide a 
managed execution environment. The runtime is available for Windows XP, 
Windows Vista, Windows 7, Windows 8, and Xbox 360. Since XNA games are 
written for the runtime, they can run on any platform that supports the XNA 
Framework with minimal or no modification of the Game engine.

.. warning:: A license fee is payable to Microsoft to use XNA Game Studio for Xbox 360 games. At this time, the Open Rails team has not investigated whether the Open Rails software is suitable for Xbox.

Frames per Second (FPS) Performance
===================================

FPS rate is as default not linked to the sync rate of the monitor. However, with :ref:`this option <options-vsync>` FPS rate may be set at the value of the monitor refresh rate.

Game Clock and Internal Clock
=============================

Like other simulation software, Open Rails software uses two internal 
*clocks*; a game clock and an internal clock. The game clock is required to 
synchronize the movement of trains, signal status, and present the correct 
game environment. The internal clock is used synchronize the software process 
for optimal efficiency and correct display of the game environment.

The Open Rails team is dedicated to ensuring the game clock properly manages 
time in the simulation, so that a train will cover the proper distance in the 
correct time. The development team considers this vital aspect for an 
accurate simulation by ensuring activities run consistently across community 
members' computer systems.

Resource Utilization
====================

Because Open Rails software is designed for Microsoft's XNA game framework, 
it natively exploits today's graphics cards' ability to offload much of the 
display rendering workload from the computer's CPU.

Multi-Threaded Coding
=====================

The Open Rails software is designed from the ground up to support up to 4 
CPUs, either as virtual or physical units. Instead of a single thread looping 
and updating all the elements of the simulation, the software uses four 
threads for the main functions of the software.

- Thread 1 -- Main Render Loop (RenderProcess) 
- Thread 2 -- Physics and Animation (UpdaterProcess)
- Thread 3 -- Shape and Texture Loading/Unloading (LoaderProcess) 
- Thread 4 -- Sound

There are other threads used by the multiplayer code as each opened 
communication is handled by a thread.

The RenderProcess runs in the main game thread. During its initialization, it 
starts two subsidiary threads, one of which runs the UpdaterProcess and the 
other the LoaderProcess. It is important that the UpdaterProcess stays a 
frame ahead of RenderProcess, preparing any updates to camera, sky, terrain, 
trains, etc. required before the scene can be properly rendered. If there are 
not sufficient compute resources for the UpdaterProcess to prepare the next 
frame for the RenderProcess, the software reduces the frame rate until it can 
*catch up*.

Initial testing indicates that *stutters* are significantly reduced because 
the process (LoaderProcess) associated with loading shapes and textures when 
crossing tile boundaries do not compete with the main rendering loop 
(RenderProcess) for the same CPU cycles. Thread safety issues are handled 
primarily through data partitioning rather than locks or semaphores to 
maximise performance.

Ongoing testing by the Open Rails team and the community will determine what 
and where the practical limits of the software lie. As the development team 
receives feedback from the community, improvements and better optimization of 
the software will contribute to better overall performance -- potentially 
allowing high polygon models with densely populated routes at acceptable 
frame rates.
