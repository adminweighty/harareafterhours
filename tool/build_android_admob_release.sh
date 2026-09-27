#!/bin/zsh
set -euo pipefail

script_dir=${0:A:h}
project_root=${script_dir:h}
config_file="${project_root}/.admob.env"
flutter_bin=${FLUTTER_BIN:-/Users/udeanmbano/Development/flutter/bin/flutter}

[[ -f "${config_file}" ]] || {
  print -u2 'Missing .admob.env. Copy .admob.env.example and add the Android AdMob IDs.'
  exit 1
}

set -a
source "${config_file}"
set +a

: "${ADMOB_ANDROID_APP_ID:?Add ADMOB_ANDROID_APP_ID to .admob.env}"
: "${ADMOB_ANDROID_BANNER_ID:?Add ADMOB_ANDROID_BANNER_ID to .admob.env}"

export ORG_GRADLE_PROJECT_ADMOB_ANDROID_APP_ID="${ADMOB_ANDROID_APP_ID}"
cd "${project_root}"
"${flutter_bin}" build apk --release \
  --dart-define="ADMOB_ANDROID_BANNER_ID=${ADMOB_ANDROID_BANNER_ID}"
