# Building Open Rails

This document will take you through the steps needed to build Open Rails from the source code and to run and start debugging it.

Line ending in two spaces.  

dummy line

Line ending in br.<br/>

dummy line

Line ending in pspace/p.<p>&nbsp;</p>

dummy line

Line ending in p/p.<p></p>

dummy line


## Overview

To work with the code, you will need an installation of Microsoft Visual Studio and also content that is standalone (doesn't rely on sharing files from 3rd parties). 

Visual Studio provides the Git tools to clone the Open Rails repository and submit your changes as a pull request.

Any new features will also require matching changes to the Open Rails Manual, so a Docker container is provided that works with Visual Studio Code to build the manual in either HTML or PDF.

## Windows

Use any Windows from version 7 with Service Pack 1 upwards. Open Rails can also be developed on a virtual machine.

## Visual Studio

![](images/downloading_visual_studio_installer.png)

Download and install Visual Studio 2019 Community Edition from Microsoft. 
(Other editions will work but this one is free. Version 2022 will also work, but this is reported as significantly slower. Version 2017 is not recommended as in Feb 2022 we are moving towards .Net 5 which is incompatible.)

![](images/authorising_visual_studio_installer.png)

![](images/prompt_from_visual_studio_installer.png)

### VS Installation

![](images/configure_visual_studio_installer.png)

The only "workload" needed is .NET desktop development. Some optional tools which may be useful are also checked.  

![](images/run_visual_studio_installer.png)

![](images/sign_in_to_microsoft.png)

You don't have to create an Microsoft account for 2019, but there is little downside.  

![](images/start_visual_studio.png)

### Open Rails Repository

![](images/cloning_repo.png)

You will be prompted to "Get started" and should "Clone a repository" from 
[https://github.com/openrails/openrails.git](https://github.com/openrails/openrails.git)  

![](images/downloading_repo.png)

![](images/orts_solution.png)

On completion, the Solution Explorer view will list the solution ORTS.sln. Pick it to receive a prompt:

![](images/framework_not_installed.png)

The purpose of the Launcher project is to check whether the user's PC has the software libraries installed to run Open Rails. It uses libraries which are installed on even old PCs, so that it can run on them without error and report any problems.

However the framework needed to build that project is not installed automatically, so choose the option highlighted in the image above.  

![](images/open_rails_projects.png)

Finally we get to see a list of the projects in the Open Rails solution.

Note: There is no project called OpenRails. Instead, the Launcher project builds an executable called OpenRails.exe.

## Compiling

![](images/build_projects.png)

Use Build > Rebuild Solution to compile and link the source code into executable and DLL files.

![](images/successful_build.png)

Initially, your local repository contrains an empty folder Programs. The executable files just created are saved in this folder.

## Executing

![](images/executable_files.png)

At this point you can switch to the Windows File Explorer or the command line and run OpenRails.exe. This will carry out some checks and then run Menu.exe.

![](images/startup_project.png)

When debugging, it will be easier to start the program from within Visual Studio. First, set the desired project to be the Startup Project which will run when you press Debug > Start Debugging.

![](images/debug_project.png)

Now pressing F5, Start or Debug > Start Debugging will launch the project Menu.exe in debugging mode.

## Content

Without content, all you can run is Menu.exe (or OpenRails.exe which runs Menu.exe) and some of the contributed tools.

![](images/menu_option_content.png)

The easiest content to install is [Demo Model 1](http://openrails.org/download/content/).
Another recommendation is the [test track from Coals To Newcastle](9https://www.coalstonewcastle.com.au/physics/route/) as its small size makes it quick to load and it is designed for testing.

![](images/unzip_demo_model_1.png)

Unzip into a new folder.

![](images/add_demo_model_1.png)

Start Open Rails Menu.exe, and use Options > Content > Add to add the folder "Demo Model 1" to the list of installation profiles.

![](images/installation_profiles.png)

Select Demo model 1 as the Installation profile.

![](images/start_activity.png)

Pressing Start will run the simulation program RunActivity.exe. You can also run this from the command line by providing appropriate arguments. If you hold down the Alt key while pressing the Start button, these arguments are copied into the Windows paste buffer.

![](images/copy_arguments.png)

From Visual Studio, using Project > Properties > Debug > Command line arguments, these arguments can be pasted into the RunActivity project. If this is the set to be the Startup Project for the solution, pressing F5 will launch RunActivity.exe on the selected activity with debugging in place.

![](images/launching_runactivity.png)

## Installing Tools for Open Rails Manual

## Submitting Changes
