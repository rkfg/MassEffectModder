This is modification tool for video game series Mass Effect 1-3

[User guide](https://github.com/MassEffectModder/docs/raw/master/MassEffectModder_End_User_Guide_Rev2.pdf)

# Difference from upstream

Fixed building on Linux using Mono and xbuild (use script.sh to build, the binary will be in `MassEffectModder/bin/Release/MassEffectModder.exe`). The upstream version uses some Visual Basic and Windows Media libraries that are not available on Mono or I didn't find them so I just removed all references to them or patched as good as I could.

As a result, some functionality must be broken now, like decoding images and at least one prompt box. However, my goal was to allow installing ALOT texture packs and that certainly works, but you still need Wine + wine-mono to run the result because kernel32.dll is used internally.
