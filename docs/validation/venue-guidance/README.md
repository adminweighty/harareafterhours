# Script audit and next-step guidance

Reviewed the user-supplied `/Users/udeanmbano/Downloads/Harare_After_Hours_Game_Script_v3_1.md` against the implemented Unity mission controller and venue system. The document is the design reference; the user's later requests for night settings and navigation remain intentional overrides.

| Area | Actual implementation | Script gap |
| --- | --- | --- |
| M01 | Rudo handover, satchel chase, market/service choice, yard/Tino recovery, Chipo return, optional poster, checkpoints, score and first-completion rewards | Core opening is playable; this does not establish complete cinematic/dialogue parity. The requested night setting differs from the script's morning. |
| M02–M30 | Mission definitions/briefings exist in Flutter; Unity rejects non-M01 selection with `mission_unavailable` | These are not playable story missions. M02 cannot be found or started by visiting another room. |
| The Velvet Room / legacy Big Apple | Previously the shared empty room shell; now an explicitly labeled lounge preview with host, staff, two guests, bar, tables and stage platform | M16 invitation, authored contacts/choices and M17 alarm/parking extraction are not implemented. This preview is not completion of either mission or the mature-content venue variant. |
| Other social interiors | Host/staff/guests and readable entry/exit guidance | Not authored story scenes. |
| City Grocer | Separate optional prototype robbery and escort, with once-only points | Not the script's full later-mission or A01 implementation; guide labels it as a separate prototype rescue. |
| Driving, shooting and racing | Shared systems and optional free-roam activities | Systems do not constitute all authored race/combat mission phases. |

Changes:
- Preserve the `big_apple` venue ID and existing discovery rewards; publicly name it The Velvet Room, as the v3.1 script specifies.
- Social rooms no longer terminate construction before adding people. Four shared rigged character visuals per occupied social room; only one interior exists at a time. No new enemy or story reward is invented.
- TALK at the host opens the existing pause menu and shows live guidance. After talking, EXIT is the room objective.
- New versioned `player_guidance` message supplies current objective, instruction and story availability. The menu updates even if the response arrives after it opens. Entry, host interaction, exit, mission progression and pause refresh it.
- Every M01 stage has an explicit next action. Completed M01 says that M02 is not available and suggests actual optional driving/racing instead of a nonexistent mission trigger.

Validation: Unity play-mode tests passed all four entry/exit paths; actual rescue combat/escort, one-time reward, cleanup and unchanged mission checks; guidance for all M01 states; canonical venue name and retained ID; supported visible meshes for four social NPCs; host TALK event; refreshed guidance on exit. The fixture now backs up/restores the relevant score, mission, loadout, controls, intro and reward preferences. Flutter analysis passed; all 42 Flutter tests passed, including malformed/version-mismatched guidance, live updates inside an open menu, narrow/landscape screens and large text. The screenshot is an Editor render, not physical-phone visual QA.

Next story-development sequence: implement and validate M02, including Chipo's overpayment choice, checkpoints and once-only payout; connect sequential mission selection; then build M03 before opening later story missions. The M16/M17 preview should remain clearly marked until its authored objectives and outcomes exist.

Deployment: Unity iOS export/native framework and Flutter release build succeeded. Installed and launched on Udean’s iPhone. Logs: `/private/tmp/harare-ios-device.MqZInZ` and `/private/tmp/harare-venue-guidance-deploy.log`. Phone launch is confirmed; hands-on venue/menu interaction remains to be checked on device.
