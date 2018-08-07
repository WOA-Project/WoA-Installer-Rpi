# Windows 10 ARM64 Installer for Lumia 950/XL and Raspberry Pi 3
This is a GUI tool that will help you install Windows ARM64 (Windows On ARM) with ease.

# Supported devices
- Lumia 950
- Lumia 950 XL
- Raspberry Pi 3

## WoA Installer for Lumia 950/XL
![image](https://user-images.githubusercontent.com/3109851/43066098-05c1f41c-8e64-11e8-935c-92748f36ecfd.png)

## WoA Installer for Raspberry Pi 3
![image](https://user-images.githubusercontent.com/3109851/43066047-e7134552-8e63-11e8-8ac7-895e601b60e3.png)

# Requirements
## Lumia 950/XL
- A Lumia 950/XL with an unlocked bootloader that can correctly enter Mass Storage Mode
- A Windows 10 ARM64 Image (.wim)
- A USB-C cable to connect the Lumia to your PC

## Raspberry Pi
- Raspberry Pi 3 Model B (or B+)
- MicroSD card. Recommended with A1 rating.

# About Core Packages
Please, notice the WoA Installer is only a tool with helps you with the deployment. WoA Installer needs a set of binaries, AKA the **Core Packages**, to do its job. **These binaries are not not mine** and are bundled and offered just for convenience to make your life easier, since this tool is focused on simplicity. 

Find them below.

# Downloads

## WoA Installer
- There are 2 version of WoA Installer. One for Raspberry Pi and another for the supported Lumias. You can download them from the [Releases](https://github.com/SuperJMN/WoA-Installer/releases) section

## Core Packages
- [Core Package for Raspberry Pi 3](https://1drv.ms/f/s!AtXoQFW327DIyMxxCDU_uUM6o6dn2A)
- Core Package for Lumia 950/XL: NOT AVAILABLE YET

## Installing the Core Package
Run WoA Installer and go to the **Advanced** section. Click on `[Import Core Package]` and select the package directly. Don't attempt to uncompress it. After the import operation, you will be able to use deploy WoA withing the application under `Windows deployment`.

# Donations are welcome!
If you find this useful, feel free to [buy me a coffee](http://paypal.me/superjmn). Thanks in advance!!

# Credits and Acknowledgements

This WoA Installer is possible because the great community behind it. I would like to thank the brilliant minds behind this technicall wonder. If you think you should be listed, please, contact me using the e-mail address on my profile.

- [Ben Imbushuo](https://github.com/imbushuo) for Lumia's UEFI and misc stuff
- [Gustave M.](https://twitter.com/gus33000) for drivers, for support, for testing...
- Ard Bisheuvel for initial ATF and UEFI ports
- Bas Timmer ([NTAuthority](https://github.com/nta)) for the Windows USB driver
- [Andrei Warkentin](https://github.com/Googulator) for the 64-bit Pi UEFI, UEFI Pi (HDMI, USB, SD/MMC) drivers, improved ATF and Windows boot/runtime support.
- [Googulator](https://github.com/Googulator) for his method to install WoA in the Raspberry Pi
- Mario Bălănică for his [awesome tool](https://www.worproject.ml), and for tips and support :)
	- daveb77
    - thchi12
    - falkor2k15
    - driver1998
    - XperfectTR
    - woachk
    - novaspirit
    - zlockard 
     
    ...for everything from ACPI/driver work to installation procedures, testing and so on.
- Microsoft for the 32-bit IoT firmware.

In addition to:

- [Eric Zimmerman](https://github.com/EricZimmerman) for [The Registry Project](https://github.com/EricZimmerman/Registry)
- [Jan Karger](https://github.com/punker76) [MahApps.Metro](https://mahapps.com)
- [ReactiveUI](https://reactiveui.net)
- [Adam Hathcock](https://github.com/adamhathcock) for [SharpCompress](https://github.com/adamhathcock/sharpcompress)

And our wonderful groups at Telegram for their testing and support!
- [LumiaWOA](https://t.me/joinchat/Ey6mehEPg0Fe4utQNZ9yjA)
- [RaspberryPiWOA](https://t.me/raspberrypiwoa)

## Related projects
These are the related projects. The Core Packages comes from them. Big thanks!

### Microsoft Lumia
- [Lumia950XLPkg](https://github.com/imbushuo/Lumia950XLPkg)
### Raspberry Pi
- [RaspberryPiPkg](https://github.com/andreiw/RaspberryPiPkg)
- [Microsoft IoT-BSP](https://github.com/ms-iot/bsp)
- [Raspberry Pi ATF](https://github.com/andreiw/raspberry-pi3-atf)
- [WOR Project](https://www.worproject.ml) by [Mario Bălănică](https://github.com/mariobalanica)
