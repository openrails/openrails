#!/usr/bin/env bash
cd ../..
rm -rf "Program/Content/Web"
mkdir -p "Program/Content/Web"
cp -r "Source/RunActivity/Viewer3D/WebServices/Web/." "Program/Content/Web/"
rm -f Program/RunActivityLAA.*

# Effects compilation

dotnet tool restore

for file in Source/RunActivity/Content/*.fx; do
    filename=$(basename "$file" .fx)
    echo "Compiling $file -> Program/Content/$filename.mgfx"

    dotnet tool run mgfxc \
        "$file" \
        "Program/Content/$filename.mgfx" \
        /Profile:OpenGL
done
