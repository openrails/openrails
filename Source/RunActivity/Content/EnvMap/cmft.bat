:: - Images downloaded in 2k HDR format from 
::   https://polyhaven.com/a/champagne_castle_1
::   https://polyhaven.com/a/moonless_golf
:: - Converted to cubemap with CMFT using this bat file.
:: - This bat file also converts it to BC6H format with texconv.exe

:: Download the binary cmft.exe from https://github.com/dariomanesku/cmft-bin
:: Install texconv with: winget install Microsoft.DirectXTex.texconv

@echo off
SET cmft="./cmft.exe"
if not exist ".\texconv-output" mkdir ".\texconv-output"

%cmft% %* --input "champagne_castle_1_2k.hdr"           ^
          ::Filter options                  ^
          --filter radiance                 ^
          --srcFaceSize 512                 ^
          --excludeBase false               ^
          --mipCount 10                     ^
          --glossScale 10                   ^
          --glossBias 3                     ^
          --lightingModel blinnbrdf         ^
          --edgeFixup none                  ^
          --dstFaceSize 512                 ^
          ::Processing devices              ^
          --numCpuProcessingThreads 4       ^
          --useOpenCL true                  ^
          --clVendor anyGpuVendor           ^
          --deviceType gpu                  ^
          --deviceIndex 0                   ^
          ::Aditional operations            ^
          --inputGammaNumerator 1.0         ^
          --inputGammaDenominator 1.0       ^
          --outputGammaNumerator 1.0        ^
          --outputGammaDenominator 1.0      ^
          --generateMipChain true           ^
          ::Output                          ^
          --outputNum 1                     ^
          --output0 "specular-day_rgba16f"  --output0params dds,rgba16f,cubemap

texconv.exe -f BC6H_UF16 -bc x -y specular-day_rgba16f.dds -o .\texconv-output
move /Y ".\texconv-output\specular-day_rgba16f.dds" ".\specular-day_bc6h.dds"

%cmft% %* --input "moonless_golf_2k.hdr"           ^
          ::Filter options                  ^
          --filter radiance                 ^
          --srcFaceSize 512                 ^
          --excludeBase false               ^
          --mipCount 10                     ^
          --glossScale 10                   ^
          --glossBias 3                     ^
          --lightingModel blinnbrdf         ^
          --edgeFixup none                  ^
          --dstFaceSize 512                 ^
          ::Processing devices              ^
          --numCpuProcessingThreads 4       ^
          --useOpenCL true                  ^
          --clVendor anyGpuVendor           ^
          --deviceType gpu                  ^
          --deviceIndex 0                   ^
          ::Aditional operations            ^
          --inputGammaNumerator 1.0         ^
          --inputGammaDenominator 1.0       ^
          --outputGammaNumerator 1.0        ^
          --outputGammaDenominator 1.0      ^
          --generateMipChain true           ^
          ::Output                          ^
          --outputNum 1                     ^
          --output0 "specular-night_rgba16f"  --output0params dds,rgba16f,cubemap

texconv.exe -f BC6H_UF16 -bc x -y specular-night_rgba16f.dds -o .\texconv-output
move /Y ".\texconv-output\specular-night_rgba16f.dds" ".\specular-night_bc6h.dds"

pause
