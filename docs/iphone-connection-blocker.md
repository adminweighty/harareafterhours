# iPhone deployment blocker — September 25, 2026

## Update: connection and installation resolved

The iPhone is now reachable. Personal Team `HQCMK42CYC` signed the release build,
the profile includes the target iPhone, and `devicectl` confirmed installation.
After developer trust, launch succeeded at 14:34. The phone became unavailable
during the follow-up process check; sustained runtime/gameplay is not yet verified.
The connection diagnosis below is historical, not the current blocker.

The physical-iPhone app builds and its code signature verifies. Installation has
not succeeded because macOS CoreDevice cannot establish its device connection.
The existing provisioning profile also needs the target iPhone added once it is
available as an Xcode destination.

## Observed

- Finder can read the connected iPhone 13 Pro Max; USB enumeration succeeds.
- Xcode 26.6 / CoreDevice reports the phone unavailable; Flutter does not list it.
- Pairing by the cached CoreDevice identifier returns error 1011; installing by
  the hardware UDID returns error 1000, device not found.
- macOS `remoted` repeatedly logs `ncm-1> network_connect_in6: [65: No route to host]`.
- Installed iOS developer disk image reports compatible and usable.

## Tests performed

- USB reconnect and user-reported Mac/iPhone restart did not resolve discovery.
- Restarting the user's CoreDeviceService did not resolve discovery.
- Temporarily disabling FortiClient's network extension did not resolve it.
  The extension was restored and confirmed enabled after that test.
- Sophos Connect reports disconnected.
- With user approval, disconnected Check Point VPN using its supported `trac
  disconnect` command. It now reports Idle, but still lists `desktop_policy`.
  The IPv6 route errors and Xcode unavailability continue. Its security service
  was not stopped, and no firewall policy was modified. The VPN is left
  disconnected; reconnecting may require user authentication.

## Next escalation

Have the Mac/network administrator investigate why link-local IPv6 communication
on the iPhone's USB network connection has no usable route. A VPN/filter policy
is a possibility, not a confirmed root cause. Do not erase the iPhone or disable
all endpoint protection as a workaround.

Apple reference: https://developer.apple.com/documentation/technotes/tn3158-resolving-xcode-15-device-connection-issues
