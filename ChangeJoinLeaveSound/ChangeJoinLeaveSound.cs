using System;
using System.Reflection;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace ChangeJoinLeaveSound
{
	public class ChangeJoinLeaveSound : ResoniteMod
	{
		internal const string VERSION_CONSTANT = "1.0.0";
		public override string Name => "ChangeJoinLeaveSound";
		public override string Author => "NepuShiro";
		public override string Version => VERSION_CONSTANT;
		public override string Link => "https://github.com/NepuShiro/ChangeJoinLeaveSound/";

		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<bool> SoundsEnabled = new("SoundsEnabled", "Enable playing sounds when a user joins or leaves", () => true);
		
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<Uri> NotificationSoundUri = new("NotificationSound", "Notification sound for user joining or leaving - Disabled when null", () => new("resdb:///a8199a88afc3beb1f4d96c5da644fa8fa1cf86235606bd401e95a603ebabacdb.wav"));
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<colorX> NotificationJoinColor = new("NotificationJoinColor", "The color for the join notification", () => RadiantUI_Constants.Dark.GREEN);

		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<bool> OverrideLeaveSound = new("OverrideLeaveSound", "Override the notification sound for leaving", () => true);
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<Uri> NotificationLeaveSoundUri = new("NotificationLeaveSound", "Notification sound for leaving - Only used if override is enabled - Disabled when null", () => new("resdb:///76512b9835ae223e6586e7cfe610880a34e7574f6c0d507512c8632b2c517475.wav"));
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<colorX> NotificationLeaveColor = new("NotificationLeaveColor", "The color for the leave notification", () => RadiantUI_Constants.Dark.RED);

		private static ModConfiguration config;

		public override void OnEngineInit()
		{
			config = GetConfiguration();
			config.Save(true);

			Harmony harmony = new("com.NepuShiro.ChangeJoinLeaveSound");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(NotificationPanel))]
		private class NotificationPatch
		{
			static Uri _joinSound;
			public static Uri JOIN_SOUND
			{
				get
				{
					if (_joinSound == null)
					{
						_joinSound = config.GetValue(NotificationSoundUri);
					}
					return _joinSound;
				}
			}

			static Uri _leaveSound;
			public static Uri LEAVE_SOUND
			{
				get
				{
					if (_leaveSound == null)
					{
						_leaveSound = config.GetValue(NotificationLeaveSoundUri);
					}
					return _leaveSound;
				}
			}
			private static NotificationSettings GetSettings(NotificationPanel instance)
			{
				FieldInfo settingsField = typeof(NotificationPanel).GetField("_settings", BindingFlags.NonPublic | BindingFlags.Instance);
				return settingsField?.GetValue(instance) as NotificationSettings;
			}

			[HarmonyPrefix]
			[HarmonyPatch(nameof(NotificationPanel.UserJoined))]
			static bool UserJoinedPatch(NotificationPanel __instance, string userId, string username, Uri sessionThumbnailUrl)
			{
				if (!config.GetValue(SoundsEnabled)) return true; 
				
				var _settings = GetSettings(__instance);
				__instance.RunSynchronously(async delegate
				{
					Uri thumbnail = await GetUserThumbnail(userId);
					StaticAudioClip joinSound = __instance.Slot.AttachAudioClip(JOIN_SOUND, true);
					__instance.AddNotification(userId, __instance.GetLocalized("Notifications.UserJoined", "<b>{0}</b>"), sessionThumbnailUrl, config.GetValue(NotificationJoinColor), _settings?.UserJoinAndLeave.Value ?? NotificationType.None, username, thumbnail, joinSound);
				});
				return false;
			}

			[HarmonyPrefix]
			[HarmonyPatch(nameof(NotificationPanel.UserLeft))]
			static bool UserLeftPatch(NotificationPanel __instance, string userId, string username, Uri sessionThumbnailUrl)
			{
				if (!config.GetValue(SoundsEnabled) || !config.GetValue(OverrideLeaveSound)) return true;

				var _settings = GetSettings(__instance);
				__instance.RunSynchronously(async delegate
				{
					Uri thumbnail = await GetUserThumbnail(userId);
					StaticAudioClip leaveSound = __instance.Slot.AttachAudioClip(LEAVE_SOUND, true);
					__instance.AddNotification(userId, __instance.GetLocalized("Notifications.UserLeft", "<b>{0}</b>"), sessionThumbnailUrl, config.GetValue(NotificationLeaveColor), _settings?.UserJoinAndLeave.Value ?? NotificationType.None, username, thumbnail, leaveSound);
				});
				return false;
			}
			
			private static async Task<Uri> GetUserThumbnail(string userId)
			{
				var cloudUserProfile = (await Engine.Current.Cloud.Users.GetUser(userId))?.Entity?.Profile;
				Uri.TryCreate(cloudUserProfile?.IconUrl, UriKind.Absolute, out Uri thumbnail);
				thumbnail ??= OfficialAssets.Graphics.Thumbnails.AnonymousHeadset;

				return thumbnail;
			}
		}
	}
}
