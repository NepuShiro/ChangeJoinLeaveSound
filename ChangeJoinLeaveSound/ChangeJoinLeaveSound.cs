using System;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using SkyFrost.Base;

namespace ChangeJoinLeaveSound
{
	public class ChangeJoinLeaveSound : ResoniteMod
	{
		public override string Name => "ChangeJoinLeaveSound";
		public override string Author => "NepuShiro";
		public override string Version => "1.0.1";
		public override string Link => "https://github.com/NepuShiro/ChangeJoinLeaveSound/";

		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<bool> SoundsEnabled = new ModConfigurationKey<bool>("SoundsEnabled", "Enable playing sounds when a user joins or leaves", () => true);
		
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<Uri> NotificationSoundUri = new ModConfigurationKey<Uri>("NotificationSound", "Notification sound for user joining or leaving - Disabled when null", () => new Uri("resdb:///a8199a88afc3beb1f4d96c5da644fa8fa1cf86235606bd401e95a603ebabacdb.wav"));
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<colorX> NotificationJoinColor = new ModConfigurationKey<colorX>("NotificationJoinColor", "The color for the join notification", () => RadiantUI_Constants.Dark.GREEN);

		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<bool> OverrideLeaveSound = new ModConfigurationKey<bool>("OverrideLeaveSound", "Override the notification sound for leaving", () => true);
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<Uri> NotificationLeaveSoundUri = new ModConfigurationKey<Uri>("NotificationLeaveSound", "Notification sound for leaving - Only used if override is enabled - Disabled when null", () => new Uri("resdb:///76512b9835ae223e6586e7cfe610880a34e7574f6c0d507512c8632b2c517475.wav"));
		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<colorX> NotificationLeaveColor = new ModConfigurationKey<colorX>("NotificationLeaveColor", "The color for the leave notification", () => RadiantUI_Constants.Dark.RED);

		private static ModConfiguration config;

		public override void OnEngineInit()
		{
			config = GetConfiguration();
			config!.Save(true);

			Harmony harmony = new Harmony("net.NepuShiro.ChangeJoinLeaveSound");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(NotificationPanel))]
		private class NotificationPatch
		{
			[HarmonyPrefix, HarmonyPatch(nameof(NotificationPanel.UserJoined))]
			private static bool UserJoinedPatch(NotificationPanel __instance, NotificationSettings ____settings, string userId, string username, Uri sessionThumbnailUrl)
			{
				if (!config.GetValue(SoundsEnabled)) return true; 
				
				try
				{
					Uri thumbnail = GetUserThumbnail(userId);
					StaticAudioClip joinSound = __instance.Slot.AttachAudioClip(config.GetValue(NotificationSoundUri), true);
					__instance.AddNotification(userId, __instance.GetLocalized("Notifications.UserJoined", $"<color={LegacyUIStyle.NameTint(userId, 0.5f).ToHexString()}>"+"<b>{0}</b></color>"), sessionThumbnailUrl, config.GetValue(NotificationJoinColor), ____settings?.UserJoinAndLeave.Value ?? NotificationType.None, username, thumbnail, joinSound);
				}
				catch (Exception ex)
				{
					Error(ex);
				}
				return false;
			}
			
			[HarmonyPrefix, HarmonyPatch(nameof(NotificationPanel.UserLeft))]
			private static bool UserLeftPatch(NotificationPanel __instance, NotificationSettings ____settings, string userId, string username, Uri sessionThumbnailUrl)
			{
				if (!config.GetValue(SoundsEnabled) || !config.GetValue(OverrideLeaveSound)) return true;

				try
				{
					Uri thumbnail = GetUserThumbnail(userId);
					StaticAudioClip leaveSound = __instance.Slot.AttachAudioClip(config.GetValue(NotificationLeaveSoundUri), true);
					__instance.AddNotification(userId, __instance.GetLocalized("Notifications.UserLeft", $"<color={LegacyUIStyle.NameTint(userId, 0.5f).ToHexString()}>"+"<b>{0}</b></color>"), sessionThumbnailUrl, config.GetValue(NotificationLeaveColor), ____settings?.UserJoinAndLeave.Value ?? NotificationType.None, username, thumbnail, leaveSound);
				}
				catch (Exception ex)
				{
					Error(ex);
				}
				return false;
			}
			
			private static Uri GetUserThumbnail(string userId)
			{
				Contact contact = Engine.Current.Cloud.Contacts.GetContact(userId);
				
				return string.IsNullOrEmpty(contact?.Profile?.IconUrl)
					? null 
					: new Uri(contact.Profile.IconUrl);
			}
		}
	}
}
