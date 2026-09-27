import 'package:flutter/material.dart';
import 'package:google_mobile_ads/google_mobile_ads.dart';

import '../services/admob_service.dart';

/// A menu-only banner. Active gameplay never reserves space for advertising.
class AdMobBanner extends StatelessWidget {
  const AdMobBanner({super.key});

  @override
  Widget build(BuildContext context) {
    if (!AdMobService.isSupported) return const SizedBox.shrink();
    return ValueListenableBuilder<bool>(
      valueListenable: AdMobService.instance.canRequestAds,
      builder: (context, allowed, _) =>
          allowed ? const _LoadedBanner() : const SizedBox.shrink(),
    );
  }
}

class _LoadedBanner extends StatefulWidget {
  const _LoadedBanner();

  @override
  State<_LoadedBanner> createState() => _LoadedBannerState();
}

class _LoadedBannerState extends State<_LoadedBanner> {
  BannerAd? _ad;

  @override
  void initState() {
    super.initState();
    final ad = BannerAd(
      adUnitId: AdMobService.bannerAdUnitId,
      request: const AdRequest(),
      size: AdSize.banner,
      listener: BannerAdListener(
        onAdLoaded: (loaded) {
          if (!mounted) {
            loaded.dispose();
            return;
          }
          setState(() => _ad = loaded as BannerAd);
        },
        onAdFailedToLoad: (failed, _) => failed.dispose(),
      ),
    );
    ad.load();
  }

  @override
  void dispose() {
    _ad?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final ad = _ad;
    if (ad == null) return const SizedBox.shrink();
    return Semantics(
      label: 'Advertisement',
      container: true,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text(
            'ADVERTISEMENT',
            style: TextStyle(
              color: Color(0xFF7F9A9B),
              fontSize: 9,
              letterSpacing: 1.2,
            ),
          ),
          const SizedBox(height: 4),
          SizedBox(
            width: ad.size.width.toDouble(),
            height: ad.size.height.toDouble(),
            child: AdWidget(ad: ad),
          ),
        ],
      ),
    );
  }
}
