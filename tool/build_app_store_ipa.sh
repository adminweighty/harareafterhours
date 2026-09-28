#!/bin/zsh
# Build and sign an App Store IPA for Weighty Premier Solutions.
set -euo pipefail

script_dir=${0:A:h}
project_root=${script_dir:h}
flutter_bin=${FLUTTER_BIN:-/Users/udeanmbano/Development/flutter/bin/flutter}
archive_path="${project_root}/build/ios/archive/Runner.xcarchive"
export_path="${project_root}/build/ios/ipa"
export_options="${project_root}/ios/ExportOptions-AppStore.plist"

[[ -x "$flutter_bin" ]] || { print -u2 'Flutter executable missing; set FLUTTER_BIN.'; exit 1; }
[[ -f "$export_options" ]] || { print -u2 'Missing ios/ExportOptions-AppStore.plist.'; exit 1; }

cd "$project_root"
zsh "$script_dir/prepare_ios_device.sh"
"$flutter_bin" build ipa --release --no-codesign "$@"

xcodebuild -exportArchive \
  -archivePath "$archive_path" \
  -exportPath "$export_path" \
  -exportOptionsPlist "$export_options" \
  -allowProvisioningUpdates

ipa_path=$(find "$export_path" -maxdepth 1 -name '*.ipa' -print -quit)
[[ -n "$ipa_path" ]] || { print -u2 'IPA export completed without an IPA file.'; exit 1; }
print "App Store IPA ready: $ipa_path"
