# DeviceChangeFix
The code that checks the available controllers and amkes them usable in FF14 is pretty slow and freezes all controller input while it is running. Unfortunately this code is run every time a device in Windows is added or removed, no matter if it's a controller, generic hardware device or even virtual device.

Every time this happens controller input freezes for about 1-2 seconds. This is especially annoying when using Game Pass since Windows randomly adds and ejects virtual drives for Game Pass games.

This Plugin keeps track of how many controllers are actually connected and only runs the polling code when that number changes.
  
