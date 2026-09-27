// Run from the project root: swift tool/generate_app_icons.swift
// Platform packaging only: resize/pad the imagegen artwork, preserving the design.
import Foundation
import CoreGraphics
import ImageIO
import UniformTypeIdentifiers

let root = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
func read(_ path: String) -> CGImage {
    let url = root.appendingPathComponent(path)
    guard let source = CGImageSourceCreateWithURL(url as CFURL, nil),
          let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else {
        fatalError("Cannot read \(path)")
    }
    return image
}
let master = read("assets/branding/app-icon/master.png")
let foreground = read("assets/branding/app-icon/foreground.png")
precondition(master.width == master.height && foreground.width == foreground.height)
let colorSpace = CGColorSpace(name: CGColorSpace.sRGB)!

// Measure the alpha silhouette, so every visible pixel fits Android's 66dp
// safe circle in a 108dp canvas, with a small anti-aliasing margin.
let w = foreground.width, h = foreground.height
let alphaContext = CGContext(data: nil, width: w, height: h, bitsPerComponent: 8,
    bytesPerRow: w * 4, space: colorSpace,
    bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)!
alphaContext.draw(foreground, in: CGRect(x: 0, y: 0, width: w, height: h))
let pixels = alphaContext.data!.assumingMemoryBound(to: UInt8.self)
var radius = 0.0
var transparent = 0
for y in 0..<h {
    for x in 0..<w {
        if pixels[(y * w + x) * 4 + 3] > 8 {
            radius = max(radius, hypot(Double(x) + 0.5 - Double(w)/2,
                                       Double(y) + 0.5 - Double(h)/2))
        } else { transparent += 1 }
    }
}
precondition(transparent > w * h / 4, "Foreground must have real transparent margins")
let adaptiveScale = min(1, (32.0/108) * Double(w)/radius)
var count = 0
func export(_ source: CGImage, _ size: Int, _ path: String,
            opaque: Bool = true, scale: Double = 1) {
    let context = CGContext(data: nil, width: size, height: size, bitsPerComponent: 8,
        bytesPerRow: size * 4, space: colorSpace,
        bitmapInfo: (opaque ? CGImageAlphaInfo.noneSkipLast : .premultipliedLast).rawValue)!
    if opaque {
        context.setFillColor(CGColor(colorSpace: colorSpace, components: [7/255,17/255,15/255,1])!)
        context.fill(CGRect(x: 0, y: 0, width: size, height: size))
    }
    context.interpolationQuality = .high
    let side = Double(size) * scale
    context.draw(source, in: CGRect(x: (Double(size)-side)/2, y: (Double(size)-side)/2,
                                    width: side, height: side))
    let url = root.appendingPathComponent(path)
    try! FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
    let destination = CGImageDestinationCreateWithURL(url as CFURL, UTType.png.identifier as CFString, 1, nil)!
    CGImageDestinationAddImage(destination, context.makeImage()!, nil)
    precondition(CGImageDestinationFinalize(destination))
    let check = read(path)
    precondition(check.width == size && check.height == size)
    if opaque { precondition([CGImageAlphaInfo.none, .noneSkipFirst, .noneSkipLast].contains(check.alphaInfo)) }
    count += 1
}

let ios = "ios/Runner/Assets.xcassets/AppIcon.appiconset/"
let catalog = try! JSONSerialization.jsonObject(with: Data(contentsOf: root.appendingPathComponent(ios + "Contents.json"))) as! [String: Any]
var written = Set<String>()
for entry in catalog["images"] as! [[String: String]] {
    guard let file = entry["filename"], written.insert(file).inserted else { continue }
    let points = Double(entry["size"]!.split(separator: "x")[0])!
    let scale = Double(entry["scale"]!.dropLast())!
    export(master, Int(points * scale), ios + file)
}
for (density, size, adaptive) in [("mdpi",48,108),("hdpi",72,162),("xhdpi",96,216),("xxhdpi",144,324),("xxxhdpi",192,432)] {
    let folder = "android/app/src/main/res/mipmap-\(density)/"
    export(master, size, folder + "ic_launcher.png")
    export(foreground, adaptive, folder + "ic_launcher_foreground.png", opaque: false, scale: adaptiveScale)
}
export(master, 1024, "assets/branding/app-icon/app-store-1024.png")
export(master, 512, "assets/branding/app-icon/google-play-512.png")
print("Validated \(count) PNGs; opaque iOS/store artwork; Android silhouette within 64dp safe circle. Foreground scale: \(adaptiveScale)")
