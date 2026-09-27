#!/bin/zsh
set -euo pipefail

script_dir=${0:A:h}
project_root=${script_dir:h}
unity_export="${project_root}/ios/UnityLibrarySimulator"
framework_build_dir="${TMPDIR:-/private/tmp}/harareafterhours-unity-simulator-framework"
active_framework="${project_root}/ios/UnityLibrary/UnityFramework.framework"

if [[ ! -d "${unity_export}/Unity-iPhone.xcodeproj" ]]; then
  print -u2 "Missing Simulator Unity export. In Unity, choose Flutter > Export IOS Simulator (Debug) first."
  exit 1
fi

rm -rf "${framework_build_dir}"
xcodebuild -quiet \
  -project "${unity_export}/Unity-iPhone.xcodeproj" \
  -target UnityFramework \
  -configuration Debug \
  -sdk iphonesimulator \
  "CONFIGURATION_BUILD_DIR=${framework_build_dir}" \
  CODE_SIGNING_ALLOWED=NO \
  CODE_SIGNING_REQUIRED=NO \
  build

rm -rf "${active_framework}"
ditto "${framework_build_dir}/UnityFramework.framework" "${active_framework}"

cd "${project_root}/ios"
pod install

cd "${project_root}"

if [[ "${1:-}" == "--prepare-only" ]]; then
  print "Prepared the iPhone Simulator Unity framework."
  exit 0
fi

if (( $# > 0 )); then
  flutter run -d "$1"
else
  flutter run
fi
