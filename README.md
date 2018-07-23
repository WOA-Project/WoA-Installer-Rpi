# Windows 10 ARM64 Installer for Lumia 950/XL and Raspberry Pi 3
This is a GUI tool to install Windows On ARM for supported devices
- Lumia 950
- Lumia 950 XL
- Raspberry Pi 3

# WoA Installer for Lumia 950/XL
![image](https://user-images.githubusercontent.com/3109851/43066098-05c1f41c-8e64-11e8-935c-92748f36ecfd.png)

# WoA Installer for Raspberry Pi 3
![image](https://user-images.githubusercontent.com/3109851/43066047-e7134552-8e63-11e8-8ac7-895e601b60e3.png)

# Requirements for Lumia 950/XL
- A Lumia 950/XL with an unlocked bootloader that can correctly enter Mass Storage Mode
- A Windows 10 ARM64 Image (.wim)
- A USB-C cable to connect the Lumia to your PC

# Requirements for Raspberry Pi
- Raspberry Pi 3 Model B (or B+)
- MicroSD card. Recommended with A1 rating

# Importing the Core Package
WoA Installer needs a package with the binary files required for deployment.

## Download the Core Packages
- [Core Package for Lumia 950/XL](https://1drv.ms/f/s!AtXoQFW327DIyMwPsYJNrdJTkgAW2g)
- [Core Package for Raspberry Pi 3](https://1drv.ms/f/s!AtXoQFW327DIyMxxuCDKD5wMEfma8Q)

Run WoA Installer and go to the **Advanced** section. Click on `[Import Core Package]` and select the package directly. Don't attempt to uncompress it. After the import operation, you will be able to use deploy WoA withing the application under `Windows deployment`.

# Donations are welcome!
If you find this useful, feel free to [buy me a coffee](http://paypal.me/superjmn
) Thanks in advance!!

# Acknowledgements
- [Eric Zimmerman](https://github.com/EricZimmerman) for [Registry project](https://github.com/EricZimmerman/Registry)
- Gustave M. (retrieving metadata from Windows Images)
- MahApps.Metro https://mahapps.com/
- https://reactiveui.net/
- the people at [LumiaWOA](https://t.me/joinchat/Ey6mehEPg0Fe4utQNZ9yjA) for their testing.
- [Googulator](https://github.com/Googulator) for his method to install WoA in the Raspberry Pi
