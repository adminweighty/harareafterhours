#!/bin/zsh
# Export current Unity gameplay and prepare the Flutter host for a physical iPhone.
# Run with zsh; no global PATH or Xcode settings are changed.
set -euo pipefail

script_dir=${0:A:h}
project_root=${script_dir:h}
unity_project=${UNITY_PROJECT:-"${project_root}/unity/unity_game"}
unity_editor=${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity}
flutter_bin=${FLUTTER_BIN:-/Users/udeanmbano/Development/flutter/bin/flutter}
unity_export="${project_root}/ios/UnityLibrary"

if [[ "${1:-}" == "--help" ]]; then
  print 'Usage: zsh tool/prepare_ios_device.sh'
  print 'Exports release Unity, builds an iphoneos framework and refreshes CocoaPods.'
  print 'Then use flutter run -d <iPhone UDID> lib/main.dart.'
  print 'Overrides: UNITY_PROJECT, UNITY_EDITOR, FLUTTER_BIN.'
  exit 0
fi
if (( $# > 0 )); then print -u2 'Unexpected argument. Use --help.'; exit 2; fi
[[ -x "$unity_editor" && -x "$flutter_bin" ]] || { print -u2 'Unity or Flutter executable missing; set UNITY_EDITOR and FLUTTER_BIN.'; exit 1; }
[[ -f "$unity_project/ProjectSettings/ProjectVersion.txt" ]] || { print -u2 'Unity project not found.'; exit 1; }
command -v pod >/dev/null || { print -u2 'CocoaPods is required.'; exit 1; }

work_dir=$(mktemp -d /private/tmp/harare-ios-device.XXXXXX)
print "Build logs and recoverable previous export: $work_dir"
# The Unity exporter regenerates this directory. Preserve the complete previous
# export (including its simulator framework) before invoking it.
if [[ -d "$unity_export" ]]; then
  mv "$unity_export" "$work_dir/previous-UnityLibrary"
fi
"$unity_editor" -batchmode -nographics -quit -projectPath "$unity_project" \
  -buildTarget iOS -executeMethod FlutterUnityIntegration.Editor.Build.DoBuildIOSRelease \
  -logFile "$work_dir/unity-export.log"
[[ -d "$unity_export/Unity-iPhone.xcodeproj" ]] || { print -u2 "Unity export failed; previous export retained in $work_dir"; exit 1; }

xcodebuild -quiet -project "$unity_export/Unity-iPhone.xcodeproj" \
  -scheme UnityFramework -configuration Release -sdk iphoneos \
  -destination 'generic/platform=iOS' -derivedDataPath "$work_dir/DerivedData" \
  "CONFIGURATION_BUILD_DIR=$work_dir/framework" CODE_SIGNING_ALLOWED=NO \
  > "$work_dir/native.log" 2>&1
framework="$work_dir/framework/UnityFramework.framework"
# Both SDKs can be arm64; architecture alone cannot identify a device binary.
xcrun vtool -show-build "$framework/UnityFramework" | /usr/bin/grep -Eq 'platform IOS$' || {
  print -u2 'Refusing to embed a non-device Unity framework.'; exit 1;
}
ditto "$framework" "$unity_export/UnityFramework.framework"
cd "$project_root"
"$flutter_bin" pub get
(cd ios && pod install)
print 'Physical-iPhone Unity framework prepared. Runner uses automatic development signing.'
print "Build logs and previous generated export retained at: $work_dir"
print "Run: $flutter_bin run -d 00008110-000E74320E82801E lib/main.dart"
