#!/bin/zsh
set -euo pipefail

script_dir=${0:A:h}
project_root=${script_dir:h}
unity_export="${project_root}/ios/UnityLibrary"
framework_build_dir="${TMPDIR:-/private/tmp}/harareafterhours-unity-device-framework"
active_framework="${unity_export}/UnityFramework.framework"

if [[ ! -d "${unity_export}/Unity-iPhone.xcodeproj" ]]; then
  print -u2 "Missing device Unity export. In Unity, choose Flutter > Export IOS (Release) first."
  exit 1
fi

rm -rf "${framework_build_dir}"
xcodebuild -quiet \
  -project "${unity_export}/Unity-iPhone.xcodeproj" \
  -target UnityFramework \
  -configuration Release \
  -sdk iphoneos \
  "CONFIGURATION_BUILD_DIR=${framework_build_dir}" \
  CODE_SIGNING_ALLOWED=NO \
  CODE_SIGNING_REQUIRED=NO \
  build

rm -rf "${active_framework}"
ditto "${framework_build_dir}/UnityFramework.framework" "${active_framework}"

cd "${project_root}/ios"
pod install

cd "${project_root}"
flutter build ios --release --no-codesign
