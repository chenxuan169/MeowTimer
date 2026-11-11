#!/bin/bash
# 把 publish 出来的可执行文件包成 macOS 的 .app 并打 zip
# 用法: ./make-app.sh <版本号>
set -e

VERSION="${1:-1.5.3}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/build"

mkapp() {
  local ARCH="$1"
  local SRC="$2"
  local DIR="$OUT/app-$ARCH"
  local APP="$DIR/MeowTimer.app"

  if [ ! -f "$SRC/MeowTimer" ]; then
    echo "跳过 $ARCH：找不到 $SRC/MeowTimer"
    return
  fi

  rm -rf "$DIR"
  mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

  cp "$SRC/MeowTimer" "$APP/Contents/MacOS/MeowTimer"
  chmod +x "$APP/Contents/MacOS/MeowTimer"

  cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key><string>MeowTimer</string>
  <key>CFBundleDisplayName</key><string>喵时</string>
  <key>CFBundleIdentifier</key><string>com.chenxuan169.meowtimer</string>
  <key>CFBundleVersion</key><string>${VERSION}</string>
  <key>CFBundleShortVersionString</key><string>${VERSION}</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleExecutable</key><string>MeowTimer</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
  <key>LSApplicationCategoryType</key><string>public.app-category.productivity</string>
</dict>
</plist>
PLIST

  printf 'APPL????' > "$APP/Contents/PkgInfo"

  (cd "$DIR" && rm -f "../MeowTimer-$ARCH.zip" && zip -qry "../MeowTimer-$ARCH.zip" "MeowTimer.app")
  echo "已生成 build/MeowTimer-$ARCH.zip"
}

mkapp arm64 "$ROOT/build/arm64"
mkapp x64 "$ROOT/build/x64"
