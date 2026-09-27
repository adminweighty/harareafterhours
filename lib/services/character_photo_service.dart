import 'dart:typed_data';

import 'package:image_picker/image_picker.dart';

class CharacterPhotoService {
  CharacterPhotoService({ImagePicker? picker})
    : _picker = picker ?? ImagePicker();

  final ImagePicker _picker;

  /// Opens the system gallery and returns a reasonably sized portrait.
  ///
  /// The returned bytes stay in the Flutter client; this service does not send
  /// the image to the campaign API or any third party.
  Future<Uint8List?> choosePortrait() async {
    final XFile? file = await _picker.pickImage(
      source: ImageSource.gallery,
      maxWidth: 1200,
      maxHeight: 1200,
      imageQuality: 88,
    );
    return file?.readAsBytes();
  }
}
