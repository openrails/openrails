# Design for Virtual Filesystem

Based on http://www.elvastower.com/forums/index.php?/topic/16633-overall-file-system-and-data-structure/page__st__10__p__78514#entry78514
and http://www.elvastower.com/forums/index.php?/topic/16633-overall-file-system-and-data-structure/page__view__findpost__p__262013

## Containment
Content packages are folders which may contain any number of files and folders and reference other packages.
This avoid duplication of files which is convenient for developing content.

As an alternative, a package may contain a single zip file which is treated as though it had been unzipped into the package folder. A package can be added with a single copy operation and removed with a single delete operation, which is convenient for distributing content and for simpler installing and uninstalling of content.
The name of the zip file would not be significant.

## OR and MSTS
Packages contain one of two possible organisations for folders: "OR" and "MSTS".
In the MSTS organisation, the content is organised using the conventional folders GLOBAL, ROUTES, SOUND, TRAINS, etc.
In the OR organisation, the content is organised in folders by usage: Shapes, Textures etc.
In this organisation, there are no folders called Global or Common.cab shared by using paths such as "..\\..\\".

### Path Delimiter
"/" is used to avoid confusion with the "\\" escape character and also to differentiate this filesystem from the Windows filesystem.

## Assembling a Virtual Filesystem
Open Rails provides access to the files from multiple content packages as in these examples:

### Scenario A: Two different OR content packages in ZIP format.
    =YoRyan_SuperlinerInteriors.zip=
        OR/
            YoRyan_SuperlinerInteriors/
                Shapes/
                    supercoach.s
                    superdiner.s
                    superlounge.s
                Textures/
                    supercoach.ace
                    superdiner.ace
                    superlounge.ace

    =YoRyan_DerailFlag.zip=
        OR/
            YoRyan_DerailFlag/
                Shapes/
                    derailflag.s
                Textures/
                    derailflag.ace

The simulator combines both of these packages into a single virtual filesystem:

    OR/
        YoRyan_SuperlinerInteriors/
            Shapes/
                supercoach.s
                superdiner.s
                superlounge.s
            Textures/
                supercoach.ace
                superdiner.ace
                superlounge.ace
        YoRyan_DerailFlag/
            Shapes/
                derailflag.s
            Textures/
                derailflag.ace

### Scenario B: Two identically-named OR content packages in ZIP format.
In this case, the second package contains a file intended to override the file of the same name in the first package.

    =YoRyan_SuperlinerInteriors.zip=
        OR/
            YoRyan_SuperlinerInteriors/
                Shapes/
                    supercoach.s
                    superdiner.s
                    superlounge.s
                Textures/
                    supercoach.ace
                    superdiner.ace
                    superlounge.ace

    =SupercoachReplacementSeats=
        OR/
            YoRyan_SuperlinerInteriors/    *same package name as previous package
                Textures/
                    supercoach.ace

and the virtual filesystem shows the second package overriding the first.

    OR/
        YoRyan_SuperlinerInteriors/
            Shapes/
                supercoach.s
                superdiner.s
                superlounge.s
            Textures/
                supercoach.ace    *from SupercoachReplacementSeats.zip
                superdiner.ace
                superlounge.ace

The load order can be modified by the user.                

### Scenario C: Multiple MSTS content packages in ZIP format.
This example has a base package for the Kuju stock stuff, an XTracks package and packages for activities and timetables.

    =MicrosoftTrainSimulator1.2.zip=
        MSTS/
            GLOBAL/
                ...
            SOUND/
                ...
            ROUTES/
                ...
            TRAINS/
                ...

    =XTracks_Nov2019.zip=
        MSTS/
            GLOBAL/
                SHAPES/
                    ...
                    
    =YoRyan_MSTSTimetables.zip=
        MSTS/
            ROUTES/
                USA1/
                    ACTIVITIES/
                        YoRyan_USA1_Amtrak.timetable-or
                    PATHS/
                        ...
                USA2/
                    ACTIVITIES/
                        YoRyan_USA2_EmpireBuilder.timetable-or
                    PATHS/
                        ...
            TRAINS/
                CONSISTS/
                    ...

    =Surfliner2.zip=
        MSTS/
            ROUTES/
                Surfliner2/
                    ...

## Paths and References
Files in a package can contain references to files in the same or other packages. For example,
a reference from one OR content package to another:

    /OR/SomePackage/Shapes/bungalow.shape-or

which might refer to physical file:

    C:\Users\Ryan\Open Rails\SomePackage.zip\SomePackage\Shapes\bungalow.shape-or

A reference from one MSTS content package to another:

    /MSTS/SomePackage/Shapes/bungalow.s

This also works as a reference from an OR content package to an MSTS one.

References from one MSTS content package to another may also use a legacy reference such as:

    ..\..\SomePackage\Shapes\bungalow.s

but files in the OR content packages must start from the /OR root and may not use ".."


### For OR assets that are inextricably linked with other assets (paths and activities vis-a-vis routes)
An activity must be linked to a route. In OR packages, this constraint will be achieved with a more restrictive reference, such as:

    "Route": "/OR/SomePackage/Routes/SomeRoute.route-or"
