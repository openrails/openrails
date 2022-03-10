# Building Open Rails

This document (Feb 2022) will take you through the steps needed to build Open Rails from the source code and to run and start debugging it.

## Overview

To work with the code, you will need an installation of Microsoft Visual Studio and also content that is standalone (doesn't rely on sharing files from 3rd parties). 

Visual Studio provides the Git tools to clone the Open Rails repository and submit your changes as a pull request.

Any new features will also require matching changes to the Open Rails Manual, so a Docker container is provided that works with Visual Studio Code to build the manual in either HTML or PDF.

## Windows

64-bit Windows from version 7 with Service Pack 1 upwards. Open Rails can also be developed on a virtual machine. If you want to use 32-bit Windows, then Visual Studio Code will not be available to you for editing the manual, so please ask for help on [the Elvas Tower forum]([http://www.elvastower.com/forums/).

## Visual Studio

![](images/downloading_visual_studio_installer.png)

Download and install Visual Studio 2019 Community Edition from Microsoft. 
(Other editions will work but this one is free. Version 2022 will also work, but this is reported as significantly slower. Version 2017 is not recommended as in Feb 2022 we are moving towards .Net 5 which is incompatible.)<p>&nbsp;</p>

![](images/authorising_visual_studio_installer.png)

![](images/prompt_from_visual_studio_installer.png)

### VS Installation

![](images/configure_visual_studio_installer.png)

The only "workload" needed is .NET desktop development. Some optional tools which may be useful are also checked.<p>&nbsp;</p>

![](images/run_visual_studio_installer.png)

![](images/sign_in_to_microsoft.png)

You don't have to create an Microsoft account for 2019, but there is little downside.<p>&nbsp;</p>

![](images/start_visual_studio.png)

### Open Rails Repository

![](images/cloning_repo.png)

You will be prompted to "Get started" and should "Clone a repository" from 
[https://github.com/openrails/openrails.git](https://github.com/openrails/openrails.git)  <p>&nbsp;</p>

![](images/downloading_repo.png)

![](images/orts_solution.png)

On completion, the Solution Explorer view will list the solution ORTS.sln. 

Pick it to receive a prompt:

![](images/framework_not_installed.png)

The purpose of the Launcher project is to check whether the user's PC has the software libraries installed to run Open Rails. It uses libraries which are installed on even old PCs, so that it can run on them without error and report any problems.

However the framework needed to build that project is not installed automatically, so choose the option highlighted by the cursor in the image above.  <p>&nbsp;</p>

![](images/open_rails_projects.png)

Finally we get to see a list of the projects in the Open Rails solution.

Note: There is no project called OpenRails. Instead, the Launcher project builds an executable called OpenRails.exe.

## Compiling

![](images/build_projects.png)

Use *Build > Rebuild Solution* to compile and link the source code into executable and DLL files.<p>&nbsp;</p>

![](images/successful_build.png)

Initially, your local repository contrains an empty folder Programs. The executable files just created are saved in this folder.

## Executing

![](images/executable_files.png)

At this point you can switch to the Windows File Explorer or the command line and run OpenRails.exe. This will carry out some checks and then run Menu.exe.<p>&nbsp;</p>

![](images/startup_project.png)

When debugging, it will be easier to start the program from within Visual Studio. First, set the desired project to be the Startup Project which will run when you press *Debug > Start Debugging*.<p>&nbsp;</p>

![](images/debug_project.png)

Now pressing F5, Start or *Debug > Start Debugging* will launch the project Menu.exe in debugging mode.

## Content

Without content, all you can run is Menu.exe (or OpenRails.exe which runs Menu.exe) and some of the contributed tools.

![](images/menu_option_content.png)

The easiest content to install is [Demo Model 1](http://openrails.org/download/content/).
Another recommendation is the [test track from Coals To Newcastle](9https://www.coalstonewcastle.com.au/physics/route/) as its small size makes it quick to load and it is designed for testing.<p>&nbsp;</p>

![](images/unzip_demo_model_1.png)

Unzip into a new folder.<p>&nbsp;</p>

![](images/add_demo_model_1.png)

Using Open Rails Menu.exe and *Options > Content > Add*, add the folder "Demo Model 1" to the list of installation profiles.<p>&nbsp;</p>

![](images/installation_profiles.png)

Select Demo model 1 as the Installation profile.<p>&nbsp;</p>

![](images/start_activity.png)

Pressing Start will run the simulation program RunActivity.exe. You can also run this from the command line by providing appropriate arguments. If you hold down the Alt key while pressing the Start button, these arguments are copied into the Windows paste buffer.<p>&nbsp;</p>

![](images/copy_arguments.png)

From Visual Studio, using *Project > Properties > Debug > Command line arguments*, these arguments can be pasted into the RunActivity project. If this is the set to be the Startup Project for the solution, pressing F5 will launch RunActivity.exe on the selected activity with debugging in place.<p>&nbsp;</p>

![](images/launching_runactivity.png)

## Installing Tools for Open Rails Manual

The Open Rails manual is written in [ReStructuredText](https://docutils.sourceforge.io/rst.html) which is processed into a [multi-page HTML document](https://open-rails.readthedocs.io/en/latest/) or a  [single-page PDF document](http://openrails.org/files/OpenRails-Testing-Manual.pdfvmw).

Note: This section is based on this [Pull Request](https://github.com/openrails/openrails/pull/557).
There are several components to  install but, after that, producing a revised manual is just a click away.

### Install Visual Studio Code

This interactive development environment (IDE) is not related to the Visual Studio IDE. 

### Download and install Visual Code

Download from [Download Visual Studio Code - Mac, Linux, Windows](https://code.visualstudio.com/download) and run the installer.

### Add Extension - Remote Containers

![](images/add_vs_extension.png)

Use the icon to add a new extension.<p>&nbsp;</p>

![](images/search_for_remote_containers.png)

Search for the extension "Remote Containers".<p>&nbsp;</p>

![](images/install_remote_containers.png)

And install it into Visual Studio Code.

### Download and install Docker Desktop

![](images/download_docker_desktop.png)

Download from [https://docs.docker.com/desktop/windows/install/](https://docs.docker.com/desktop/windows/install/) and run the installer.

Docker provides a closed environment so that our script to build the manual can be run knowing that it will not be broken by any subsequent changes on the PC.<p>&nbsp;</p>

![](images/settings_for_docker_desktop.png)

Use the default settings.<p>&nbsp;</p>

![](images/progress_for_docker_desktop.png)

Installation takes a few minutes.

You will be prompted to close and restart your PC.<p>&nbsp;</p>

![](images/wsl2_for_docker_desktop.png)

You may receive a prompt to install WSL 2 separately. Click the link to the website.

![](images/enable_virtual_machine.png)

The link takes you to Step 3 and 4.<p>&nbsp;</p>

![](images/run_powershell.png)

Run PowerShell as Administrator.<p>&nbsp;</p>

![](images/enable_virtual_machine_feature.png)

Enter the command provided.<p>&nbsp;</p>

![](images/download_wsl_2.png)

Return to the webpage for Step 4 and download WSL 2 and install it. <p>&nbsp;</p>

![](images/install_wsl_2.png)

Once this is installed, return to the Docker Desktop prompt and press Restart.<p>&nbsp;</p>

![](images/restart_docker_desktop.png)

This re-starts Docker Desktop, not your PC.

Now that Docker Desktop is running, start Visual Studio Code.

Note: Before using Visual Studio Code to edit the manual, you should always start Docker Desktop first.<p>&nbsp;</p>

![](images/find_vsc_commands.png)

Press F1 to find the command list and search for Remote Containers: Open Folder in Container...<p>&nbsp;</p>

![](images/find_manual_folder.png)

Using Windows File Explorer, search your repository for Source\Documentation\Manual<p>&nbsp;</p>

![](images/manual_file_list.png)

All the manual files will be listed. <p>&nbsp;</p>

![](images/build_html_manual.png)

To build the manual, enter a command in the Terminal pane of Visual Studio Code. The default command is _make html_ which builds the file Source\Documentation\Manual\\_build\html\index.html<p>&nbsp;</p>

![](images/html_manual.png)

There is a shortcut for this command: Ctrl+Shift+B.<p>&nbsp;</p>

![](images/pdf_manual.png)

The PDF at Source\Documentation\Manual\_build\latex\Manual.pdf can be built with the command: _make latexpdf_ 

## Git and Contributing to Open Rails

Open Rails uses Git for source code control. In addition to using Git from the command line, there are many programs that provide a graphical front-end. 

Visual Studio 2019 includes facilities for working with Git, see [https://docs.microsoft.com/en-gb/visualstudio/version-control/?view=vs-2019](https://docs.microsoft.com/en-gb/visualstudio/version-control/?view=vs-2019)

For guidance on contributing, see [https://github.com/openrails/openrails/blob/master/Docs/Contributing.md](https://github.com/openrails/openrails/blob/master/Docs/Contributing.md)
