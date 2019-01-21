# Final steps

In order to finish the installation, you need to perform these steps manually.

* Reboot into Windows 10.
* Go to the **Device Manager**
* Under **Unknown Devices**, look for for one described as **'ACPI\MSHW1004\0
'** 
  * **[Tip]** open each one by double clicking on them and look into the **Details** tab for this identifier. It should be one of the first 10 elements of the list.
  * Once you find it choose to *Update* it. 
  * In the driver update wizard, select "Choose driver from PC". 
  * Select **'Browse**'.  
  * Navigate to **'C:\Users\Public\OEMPanel'** and accept the driver. The driver should be found. After that, a *red warning dialog window* should appear because the driver is not signed. **Confirm** that you want to install it.
* After the driver has been installed, your screen will become black for a while and it will automatically turn on again
* Congratulations! You already have accelerated graphics!!

# Known problems
  After installting the driver, the phone **may crash, hang or reboot**. If it's unresponsive for around 1 minute, just force a soft reset by holding the [Power] button for 10 seconds.
  Next time you boot your phone will have GPU installed ;)


# WARNING
Don't disable Dual Boot after installing the GPU drivers or the phone **will NOT boot** and you will have to flash the stock FFU.

