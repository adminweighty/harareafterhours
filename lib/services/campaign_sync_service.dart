import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/campaign_snapshot.dart';

class CampaignSyncException implements Exception {
  const CampaignSyncException(this.message);

  final String message;

  @override
  String toString() => message;
}

class CampaignSyncService {
  CampaignSyncService({http.Client? client, String? baseUrl})
    : _client = client ?? http.Client(),
      _ownsClient = client == null,
      _baseUrl = (baseUrl ?? _defaultBaseUrl).replaceFirst(RegExp(r'/$'), '');

  static const String _defaultBaseUrl = String.fromEnvironment(
    'CAMPAIGN_API_URL',
    defaultValue: 'http://localhost:8080',
  );

  final http.Client _client;
  final bool _ownsClient;
  final String _baseUrl;

  String get baseUrl => _baseUrl;

  Future<CampaignSnapshot?> load(String profileId) async {
    final http.Response response = await _request(
      () => _client.get(
        _campaignUri(profileId),
        headers: const <String, String>{'Accept': 'application/json'},
      ),
    );

    if (response.statusCode == 404) return null;
    if (response.statusCode != 200) {
      throw CampaignSyncException(_responseMessage(response));
    }
    return CampaignSnapshot.fromJson(_decodeObject(response));
  }

  Future<CampaignSnapshot> save(CampaignSnapshot snapshot) async {
    final http.Response response = await _request(
      () => _client.put(
        _campaignUri(snapshot.profileId),
        headers: const <String, String>{
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        },
        body: jsonEncode(snapshot.toJson()),
      ),
    );

    if (response.statusCode != 200) {
      throw CampaignSyncException(_responseMessage(response));
    }
    return CampaignSnapshot.fromJson(_decodeObject(response));
  }

  void dispose() {
    if (_ownsClient) _client.close();
  }

  Uri _campaignUri(String profileId) {
    return Uri.parse(
      '$_baseUrl/v1/profiles/${Uri.encodeComponent(profileId)}/campaign',
    );
  }

  Future<http.Response> _request(
    Future<http.Response> Function() request,
  ) async {
    try {
      return await request().timeout(const Duration(seconds: 8));
    } on TimeoutException {
      throw const CampaignSyncException(
        'The campaign service did not respond in time.',
      );
    } on http.ClientException {
      throw CampaignSyncException('Campaign service unavailable at $_baseUrl.');
    } on FormatException {
      throw const CampaignSyncException(
        'Campaign service returned an invalid response.',
      );
    }
  }

  Map<String, dynamic> _decodeObject(http.Response response) {
    try {
      final Object? decoded = jsonDecode(response.body);
      if (decoded is! Map) {
        throw const CampaignSyncException(
          'Campaign service returned an unexpected payload.',
        );
      }
      return Map<String, dynamic>.from(decoded);
    } on FormatException {
      throw const CampaignSyncException(
        'Campaign service returned an invalid response.',
      );
    }
  }

  String _responseMessage(http.Response response) {
    try {
      final Object? decoded = jsonDecode(response.body);
      if (decoded is Map && decoded['error'] is String) {
        return decoded['error'] as String;
      }
    } on FormatException {
      // Fall back to the status code below.
    }
    return 'Campaign service returned HTTP ${response.statusCode}.';
  }
}
