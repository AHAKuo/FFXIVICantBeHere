# I Can't Be Here

Simple plugin, toggleable via DTR bar, makes you leave instances as soon as they are over. Contains a configurable grace period.

## Install

1. In game, type `/xlsettings` and open the **Experimental** tab.
2. Add this URL under **Custom Plugin Repositories**, press **+**, then **Save**:

   ```
   https://raw.githubusercontent.com/AHAKuo/ahadevtools/main/pluginmaster.json
   ```

3. Open `/xlplugins`, search for **I Can't Be Here** and install it.

## Usage

- **Server info bar** entry `Auto-leave: On/Off`: left click toggles, right click opens settings. While a countdown runs it shows `Leaving in Ns`; left click to stay this time.
- `/icantbehere` (or `/icbh`) opens settings. `/icbh on`, `/icbh off`, `/icbh toggle`, `/icbh stay`.
- **Grace period**: seconds to wait after the duty completes (default 5). Leaving also waits for cutscenes and combat to end.

Part of ahadev's plugins. License: AGPL-3.0.
